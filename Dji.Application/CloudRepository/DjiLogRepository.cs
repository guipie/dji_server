// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using System.Linq;
using Dji.Application.Cloud.Dto.Ops;
using Dji.Application.Service.Common;
using Dji.Core.Enum.DjiEnum.Ops;

namespace Dji.Application.CloudRepository;

/// <summary>
/// 设备日志文件仓储（列举建档 + 上传进度收口）。
/// </summary>
/// <remarks>
/// <para>
/// <b>两个难点，都在「匹配」上</b>：
/// </para>
/// <para>
/// ① <b>列举回包与进度回包没有共同的稳定键</b>。
/// 列举给的是 <c>(device_sn, module, boot_index)</c>，进度给的是 <c>(device_sn, module, size, key, fingerprint)</c>。
/// 两者唯一的交集是设备 + 模块 + 文件大小，因此匹配按「指纹 → 设备+模块+大小 → 设备+模块」三级降级。
/// 一旦命中就把 <c>fingerprint</c> / <c>key</c> 回写到行上，后续帧就能走最快的一级径。
/// </para>
/// <para>
/// ② <b>时间单位在文档里自相矛盾</b>：<c>fileupload_list</c> 的 <c>start_time</c> / <c>end_time</c>
/// 标注单位是「秒」，但官方示例给的是 <c>1659427398806</c>（毫秒）。
/// 用「大于 <see cref="MillisecondThreshold"/> 视为毫秒」的阈值判别同时兼容两种设备实现 ——
/// 阈值取 <c>1e11</c>：作为秒是公元 5138 年，作为毫秒是 1973 年，两边都不会误判。
/// </para>
/// </remarks>
public class DjiLogRepository(
    SqlSugarRepository<DjiLogFile> logRes,
    DjiStorageService storageService,
    ILogger<DjiLogRepository> logger) : BaseRepository
{
    /// <summary>毫秒判定阈值：大于该值按毫秒解释，否则按秒（见类注释）</summary>
    private const long MillisecondThreshold = 100_000_000_000L;

    private readonly SqlSugarRepository<DjiLogFile> _logRes = logRes;
    private readonly DjiStorageService _storageService = storageService;
    private readonly ILogger<DjiLogRepository> _logger = logger;

    #region 读取

    /// <summary>按主键取单条</summary>
    public async Task<DjiLogFile> GetAsync(long id)
        => id <= 0 ? null : await _logRes.GetByIdAsync(id);

    /// <summary>按主键批量取（发起上传时用）</summary>
    public async Task<List<DjiLogFile>> GetByIdsAsync(List<long> ids)
        => ids == null || ids.Count == 0 ? [] : await _logRes.AsQueryable().Where(m => ids.Contains(m.Id)).ToListAsync();

    /// <summary>取某机场尚未上传成功的日志（列举后可批量发起）</summary>
    public async Task<List<DjiLogFile>> GetPendingAsync(string dockSn, List<LogModuleEnum> modules = null)
        => await _logRes.AsQueryable()
            .Where(m => m.DockSn == dockSn
                && m.Status != LogUploadStatusEnum.Uploaded
                && m.Status != LogUploadStatusEnum.Uploading)
            .WhereIF(modules is { Count: > 0 }, m => modules.Contains(m.Module))
            .OrderBy(m => m.BeginTime, OrderByType.Desc)
            .ToListAsync();

    /// <summary>取长时间停留在「上传中」的记录（由定时任务收口）</summary>
    public async Task<List<DjiLogFile>> GetStaleUploadingAsync(DateTime before)
        => await _logRes.AsQueryable()
            .Where(m => m.Status == LogUploadStatusEnum.Uploading
                && m.CreateTime != null && m.CreateTime < before)
            .ToListAsync();

    #endregion

    #region 写入

    /// <summary>
    /// 用 <c>fileupload_list</c> 的回包批量建档。
    /// </summary>
    /// <param name="dockSn">网关 SN</param>
    /// <param name="workspaceId">所属工作空间（用于数据隔离）</param>
    /// <param name="devices">回包里的设备列表</param>
    /// <param name="reportTimestamp">报文时间戳（毫秒）</param>
    /// <returns>本次新增的记录数</returns>
    /// <remarks>
    /// <b>已存在的记录绝不覆盖</b>：同一台设备会反复列举（用户可能先传 2 个、过一会儿再传 3 个），
    /// 若每次列举都重置状态，已上传成功的记录会退回「待上传」，进度也会被打回 0。
    /// 因此这里只做「补齐新增」，已存在的行原样保留。
    /// </remarks>
    public async Task<int> SaveListAsync(string dockSn, string workspaceId,
        List<FileuploadListDevice> devices, long reportTimestamp)
    {
        if (dockSn.IsNullOrWhiteSpace() || devices == null || devices.Count == 0) return 0;

        var existings = await _logRes.AsQueryable().Where(m => m.DockSn == dockSn).ToListAsync();
        var existKeys = existings.Select(KeyOf).ToHashSet();

        var newCount = 0;
        foreach (var device in devices)
        {
            if (device?.List == null || device.List.Count == 0) continue;

            var module = ParseModule(device.Module);
            if (module == null)
            {
                _logger.LogWarning("日志列举返回了未知模块，已跳过：gateway={Gateway}，device={DeviceSn}，module={Module}",
                    dockSn, device.DeviceSn, device.Module);
                continue;
            }

            foreach (var item in device.List)
            {
                if (item?.BootIndex == null) continue;

                var key = $"{device.DeviceSn}|{(int)module}|{item.BootIndex}";
                if (!existKeys.Add(key)) continue;

                await _logRes.InsertAsync(new DjiLogFile
                {
                    WorkspaceId = workspaceId,
                    DockSn = dockSn,
                    DeviceSn = device.DeviceSn,
                    Module = module.Value,
                    BootIndex = item.BootIndex.Value,
                    BeginTime = ToLocalTime(item.StartTime),
                    EndTime = ToLocalTime(item.EndTime),
                    FileSize = item.Size,
                    Status = LogUploadStatusEnum.Pending,
                    ReportTimestamp = reportTimestamp,
                    CreateTime = DateTime.Now,
                });

                newCount++;
            }
        }

        return newCount;
    }

    /// <summary>把选中的记录标记为「上传中」（下发 <c>fileupload_start</c> 成功后调用）</summary>
    public async Task<int> MarkUploadingAsync(List<long> ids, string operatorName)
    {
        if (ids == null || ids.Count == 0) return 0;

        return await _logRes.AsUpdateable()
            .SetColumns(m => new DjiLogFile
            {
                Status = LogUploadStatusEnum.Uploading,
                OperatorName = operatorName,
            })
            .Where(m => ids.Contains(m.Id))
            .ExecuteCommandAsync();
    }

    /// <summary>把选中的记录标记为「已取消」（下发 <c>fileupload_update</c> 成功后调用）</summary>
    public async Task<int> MarkCanceledAsync(List<long> ids)
    {
        if (ids == null || ids.Count == 0) return 0;

        return await _logRes.AsUpdateable()
            .SetColumns(m => new DjiLogFile
            {
                Status = LogUploadStatusEnum.Canceled,
                FinishTime = DateTime.Now,
            })
            .Where(m => ids.Contains(m.Id)
                && m.Status != LogUploadStatusEnum.Uploaded
                && m.Status != LogUploadStatusEnum.Canceled)
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 用 <c>fileupload_progress</c> 的单个文件项刷新进度。
    /// </summary>
    /// <returns>命中的记录；找不到时返回 null</returns>
    public async Task<DjiLogFile> UpdateProgressAsync(string dockSn, FileUploadProgressFile file, long reportTimestamp)
    {
        if (dockSn.IsNullOrWhiteSpace() || file == null) return null;

        var module = ParseModule(file.Module);
        var row = await MatchAsync(dockSn, file, module);
        if (row == null)
        {
            _logger.LogWarning("收到无法归属的日志上传进度，已忽略：gateway={Gateway}，device={DeviceSn}，module={Module}，size={Size}，key={Key}",
                dockSn, file.DeviceSn, file.Module, file.Size, file.Key);
            return null;
        }

        var progress = file.Progress;
        var status = LogUploadStateResolver.Parse(progress?.Status);

        // 进度报文里的 result 非 0 才是真失败：status 字段在部分固件上缺失，只有 result 可靠
        if ((progress?.Result ?? 0) != 0) status = LogUploadStatusEnum.Failed;
        if (progress?.Progress == 100 && progress.Result == 0 && progress.Status == null)
            status = LogUploadStatusEnum.Uploaded;

        // 先把值算成局部变量：SqlSugar 的表达式树对三元 / 方法调用（如 BuildFileUrl）翻译不可靠
        var objectKey = file.Key.IsNullOrWhiteSpace() ? row.ObjectKey : file.Key;
        var fingerprint = file.Fingerprint.IsNullOrWhiteSpace() ? row.Fingerprint : file.Fingerprint;
        var fileName = ResolveFileName(objectKey, row.FileName);
        // 上传成功才拼对外地址：中途拼出来的地址是 404，前端会把它当坏链接展示
        var url = status == LogUploadStatusEnum.Uploaded ? _storageService.BuildFileUrl(objectKey) : row.Url;
        var error = (progress?.Result ?? 0) != 0 ? $"上传失败，错误码：{progress.Result}" : null;
        var finishTime = status.IsFinalState() ? ToLocalTime(progress?.FinishTime) ?? DateTime.Now : row.FinishTime;
        // 表达式树里禁止出现 ?. （CS8072），同样先落成局部变量
        var percent = progress?.Progress ?? row.Progress;
        var uploadRate = progress?.UploadRate ?? row.UploadRate;

        await _logRes.AsUpdateable()
            .SetColumns(m => new DjiLogFile
            {
                Status = status,
                Progress = percent,
                UploadRate = uploadRate,
                ObjectKey = objectKey,
                Fingerprint = fingerprint,
                FileName = fileName,
                Url = url,
                ErrorMessage = error,
                ReportTimestamp = reportTimestamp,
                FinishTime = finishTime,
            })
            .Where(m => m.Id == row.Id)
            .ExecuteCommandAsync();

        return row;
    }

    /// <summary>删除记录（物理删除）</summary>
    public async Task<int> DeleteAsync(List<long> ids)
    {
        if (ids == null || ids.Count == 0) return 0;
        return await _logRes.AsDeleteable().Where(m => ids.Contains(m.Id)).ExecuteCommandAsync();
    }

    /// <summary>把长时间卡在「上传中」的记录收口为失败（由定时任务调用）</summary>
    public async Task<int> CloseStaleAsync(DateTime before)
        => await _logRes.AsUpdateable()
            .SetColumns(m => new DjiLogFile
            {
                Status = LogUploadStatusEnum.Failed,
                FinishTime = DateTime.Now,
                ErrorMessage = "上传长时间无进度，已自动收口（设备可能已离线或对象存储不可达）",
            })
            .Where(m => m.Status == LogUploadStatusEnum.Uploading
                && m.CreateTime != null && m.CreateTime < before)
            .ExecuteCommandAsync();

    #endregion

    #region 私有实现

    /// <summary>
    /// 三级降级匹配（指纹 → 设备+模块+大小 → 设备+模块）。
    /// </summary>
    /// <remarks>
    /// 优先匹配「未完成」的记录：已上传成功的同一文件若再次上传（设备重传），
    /// 应当更新那条记录而不是新建，但若同一设备+模块下既有成功的又有待传的，
    /// 把进度记到待传的那条上才符合事实。
    /// </remarks>
    private async Task<DjiLogFile> MatchAsync(string dockSn, FileUploadProgressFile file, LogModuleEnum? module)
    {
        if (!file.Fingerprint.IsNullOrWhiteSpace())
        {
            var byFingerprint = await _logRes.AsQueryable()
                .Where(m => m.DockSn == dockSn && m.Fingerprint == file.Fingerprint)
                .OrderBy(m => m.CreateTime, OrderByType.Desc)
                .FirstAsync();
            if (byFingerprint != null) return byFingerprint;
        }

        // 表达式树里不使用可空枚举（SqlSugar 对 m.Module == (可空枚举) 的翻译不可靠），先取出确定值
        if (module == null) return null;
        var mod = module.Value;
        var deviceSn = file.DeviceSn;

        if (file.Size.HasValue)
        {
            var size = file.Size.Value;
            var bySize = await _logRes.AsQueryable()
                .Where(m => m.DockSn == dockSn && m.Module == mod && m.FileSize == size
                    && (deviceSn == null || m.DeviceSn == deviceSn)
                    && m.Status != LogUploadStatusEnum.Uploaded)
                .OrderBy(m => m.CreateTime, OrderByType.Asc)
                .FirstAsync();
            if (bySize != null) return bySize;
        }

        return await _logRes.AsQueryable()
            .Where(m => m.DockSn == dockSn && m.Module == mod
                && (deviceSn == null || m.DeviceSn == deviceSn)
                && m.Status != LogUploadStatusEnum.Uploaded)
            .OrderBy(m => m.CreateTime, OrderByType.Asc)
            .FirstAsync();
    }

    /// <summary>唯一键（与表上的唯一索引口径一致）</summary>
    private static string KeyOf(DjiLogFile row) => $"{row.DeviceSn}|{(int)row.Module}|{row.BootIndex}";

    /// <summary>协议 <c>module</c>（字符串）转枚举；未知值返回 null</summary>
    private static LogModuleEnum? ParseModule(string module)
    {
        if (module.IsNullOrWhiteSpace() || !int.TryParse(module.Trim(), out var value)) return null;
        return System.Enum.IsDefined(typeof(LogModuleEnum), value) ? (LogModuleEnum)value : null;
    }

    /// <summary>按阈值判别秒 / 毫秒并转本地时间</summary>
    private static DateTime? ToLocalTime(long? value)
    {
        if (value is null or <= 0) return null;

        return value.Value > MillisecondThreshold
            ? DateTimeOffset.FromUnixTimeMilliseconds(value.Value).LocalDateTime
            : DateTimeOffset.FromUnixTimeSeconds(value.Value).LocalDateTime;
    }

    /// <summary>从对象存储 Key 末段取文件名（取不到时保留原值）</summary>
    private static string ResolveFileName(string objectKey, string fallback)
    {
        if (objectKey.IsNullOrWhiteSpace()) return fallback;

        var index = objectKey.LastIndexOf('/');
        var name = index >= 0 ? objectKey[(index + 1)..] : objectKey;
        return name.IsNullOrWhiteSpace() ? fallback : name;
    }

    #endregion
}
