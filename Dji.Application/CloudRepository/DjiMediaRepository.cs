// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Cloud.Dto.Media;
using Dji.Application.Service.Common;
using Dji.Core.Enum.DjiEnum.Media;
using System.Globalization;
using System.Linq;

namespace Dji.Application.CloudRepository;

/// <summary>
/// 媒体文件仓库：负责 <c>file_upload_callback</c> 的落库与媒体查询。
/// </summary>
/// <remarks>
/// <para>
/// <b>幂等策略</b>：<c>file_upload_callback</c> 的报文带 <c>need_reply = 1</c>，机场在弱网下会重发，
/// 而重发意味着同一张照片可能被写多次。这里对 <c>object_key</c> 做 upsert
/// （<c>Storageable + WhereColumns</c>），重复报文只刷新元数据不新增行。
/// 之所以选它而不是裸 Insert + 唯一索引 catch：并发重发时 catch 方案会丢掉后到的更完整元数据
/// （例如首报缺 <c>metadata</c>、重发才带上坐标）。
/// </para>
/// <para>
/// <b>为什么是 public</b>：本仓库需被公开的动态 API 服务 <c>DjiMediaService</c> 注入，
/// 若为 <c>internal</c> 会触发 CS0051（同目录其它只服务 <c>internal</c> MQTT 模块的仓库则不受此限）。
/// </para>
/// </remarks>
public class DjiMediaRepository(
    SqlSugarRepository<DjiMediaFile> mediaRes,
    SqlSugarRepository<DjiDevice> deviceRes,
    SqlSugarRepository<DjiWaylineTask> taskRes,
    DjiStorageService storageService,
    ILogger<DjiMediaRepository> logger) : BaseRepository
{
    private readonly SqlSugarRepository<DjiMediaFile> _mediaRes = mediaRes;
    private readonly SqlSugarRepository<DjiDevice> _deviceRes = deviceRes;
    private readonly SqlSugarRepository<DjiWaylineTask> _taskRes = taskRes;
    private readonly DjiStorageService _storage = storageService;
    private readonly ILogger<DjiMediaRepository> _logger = logger;

    #region 查询

    /// <summary>按主键取媒体记录</summary>
    public async Task<DjiMediaFile> GetAsync(long id)
        => id > 0 ? await _mediaRes.GetFirstAsync(m => m.Id == id) : null;

    /// <summary>按对象存储 Key 取记录</summary>
    public async Task<DjiMediaFile> GetByObjectKeyAsync(string objectKey)
        => objectKey.IsNullOrWhiteSpace() ? null : await _mediaRes.GetFirstAsync(m => m.ObjectKey == objectKey);

    /// <summary>取某任务的全部媒体（按拍摄时间正序，便于按航点顺序浏览）</summary>
    public async Task<List<DjiMediaFile>> GetByFlightIdAsync(string flightId)
    {
        if (flightId.IsNullOrWhiteSpace()) return [];
        return await _mediaRes.AsQueryable()
            .Where(m => m.FlightId == flightId)
            .OrderBy(m => m.MediaCreateTime, OrderByType.Asc)
            .ToListAsync();
    }

    #endregion

    #region 写入
    /// <summary>
    /// 落库一条媒体上传结果（幂等）。
    /// </summary>
    /// <param name="file">协议中的文件描述</param>
    /// <param name="dockSn">上报机场 SN</param>
    /// <param name="reportTimestamp">报文时间戳（毫秒）</param>
    /// <returns>落库后的实体；无法落库（缺少关键字段或空间）时返回 null</returns>
    public async Task<DjiMediaFile> SaveUploadResultAsync(MediaFilePayload file, string dockSn, long reportTimestamp)
    {
        if (file == null || file.ObjectKey.IsNullOrWhiteSpace())
        {
            _logger.LogWarning("媒体上传回调缺少 object_key，已忽略，gateway:{DockSn}", dockSn);
            return null;
        }

        // 空间归属随设备走：媒体列表按工作空间隔离，拿不到空间就不能落库（列非空）
        var dock = dockSn.IsNullOrWhiteSpace() ? null : await _deviceRes.GetFirstAsync(m => m.Sn == dockSn);
        if (dock == null || dock.WorkspaceId.IsNullOrWhiteSpace())
        {
            _logger.LogWarning("媒体上传回调的机场未建档或无空间归属，已忽略，gateway:{DockSn}，object_key:{ObjectKey}",
                dockSn, file.ObjectKey);
            return null;
        }

        var existing = await GetByObjectKeyAsync(file.ObjectKey);
        var entity = existing ?? new DjiMediaFile
        {
            WorkspaceId = dock.WorkspaceId,
            ObjectKey = file.ObjectKey,
        };

        // 空间归属可能因设备换绑而变化，每次回传都对齐，避免媒体留在旧空间
        entity.WorkspaceId = dock.WorkspaceId;
        entity.DockSn = dockSn;
        entity.Bucket = _storage.Bucket;
        entity.FileName = file.Name ?? entity.FileName;
        entity.BizPath = file.Path ?? entity.BizPath;
        entity.Suffix = ResolveSuffix(file.Name, file.ObjectKey);
        entity.FileType = MediaFileTypeResolver.Resolve(entity.Suffix);
        entity.FileGroupId = ExtractGroupId(file.ObjectKey);
        entity.ReportTimestamp = reportTimestamp;
        entity.Url = _storage.BuildFileUrl(file.ObjectKey);

        var ext = file.Ext;
        if (ext != null)
        {
            entity.FlightId = ext.FlightId ?? entity.FlightId;
            entity.DroneModelKey = ext.DroneModelKey ?? entity.DroneModelKey;
            entity.PayloadModelKey = ext.PayloadModelKey ?? entity.PayloadModelKey;
            entity.IsOriginal = ext.IsOriginal;
        }

        var metadata = file.Metadata;
        if (metadata != null)
        {
            entity.GimbalYawDegree = metadata.GimbalYawDegree ?? entity.GimbalYawDegree;
            entity.AbsoluteAltitude = metadata.AbsoluteAltitude ?? entity.AbsoluteAltitude;
            entity.RelativeAltitude = metadata.RelativeAltitude ?? entity.RelativeAltitude;
            entity.MediaCreateTime = ParseMediaTime(metadata.CreateTime) ?? entity.MediaCreateTime;
            entity.Longitude = metadata.ShootPosition?.Lng ?? entity.Longitude;
            entity.Latitude = metadata.ShootPosition?.Lat ?? entity.Latitude;
        }

        // 关联本地任务：拿到 flight_id 就能反查任务，进而补齐飞行器 SN（协议回调里没有该字段）
        if (!entity.FlightId.IsNullOrWhiteSpace())
        {
            var task = await _taskRes.GetFirstAsync(m => m.FlightId == entity.FlightId);
            if (task != null)
            {
                entity.TaskId = task.Id;
                entity.DroneSn ??= task.DroneSn;
            }
        }

        try
        {
            await _mediaRes.Context.Storageable(entity)
                .WhereColumns(m => m.ObjectKey)
                .ToStorage()
                .AsInsertable
                .ExecuteCommandAsync();
        }
        catch (Exception ex)
        {
            // 并发重发可能仍撞唯一索引，降级为更新，保证元数据不丢
            _logger.LogWarning(ex, "媒体写入冲突，降级为更新，object_key:{ObjectKey}", entity.ObjectKey);
            await _mediaRes.AsUpdateable(entity).Where(m => m.ObjectKey == entity.ObjectKey).ExecuteCommandAsync();
        }

        return await GetByObjectKeyAsync(entity.ObjectKey);
    }

    /// <summary>
    /// 回填任务字段。
    /// </summary>
    /// <remarks>
    /// 场景：<c>file_upload_callback</c> 早于任务落库到达（回调与进度上报走两条链路，顺序无保证），
    /// 此时 <c>TaskId</c>/<c>DroneSn</c> 为空；任务侧建好后由任务服务调用本方法补齐，
    /// 否则「按任务查媒体」会漏掉最早回传的那批文件。
    /// </remarks>
    public async Task<int> BackfillTaskAsync(string flightId, long taskId, string droneSn)
    {
        if (flightId.IsNullOrWhiteSpace() || taskId <= 0) return 0;
        return await _mediaRes.AsUpdateable()
            .SetColumns(m => new DjiMediaFile { TaskId = taskId, DroneSn = droneSn })
            .Where(m => m.FlightId == flightId && (m.TaskId == null || m.DroneSn == null))
            .ExecuteCommandAsync();
    }

    /// <summary>删除媒体记录（对象存储中的实体文件不在此删除，由运维按桶策略清理）</summary>
    /// <returns>受影响行数</returns>
    public async Task<int> DeleteAsync(List<long> ids)
    {
        if (ids is not { Count: > 0 }) return 0;
        return await _mediaRes.AsDeleteable().Where(m => ids.Contains(m.Id)).ExecuteCommandAsync();
    }

    #endregion

    #region 私有工具

    /// <summary>
    /// 解析拍摄时间。
    /// </summary>
    /// <remarks>
    /// 协议声明为 ISO8601，实际示例是 <c>"2021-05-10 16:04:20"</c>（空格分隔），
    /// 部分固件还会给带时区或带毫秒的形态。此处按「最可能 → 最不可能」依次尝试，
    /// 全部失败时返回 null 而不是抛异常 —— 时间缺失不应阻断整条媒体记录落库。
    /// <para>
    /// <b>刻意不用 <c>AdjustToUniversal</c></b>：设备给的是拍摄地墙上时钟，
    /// 若按 UTC 解释，带 <c>+08:00</c> 偏移的报文会被减 8 小时落库，
    /// 展示时看到的时间与照片上的时间就对不上了。这里保持「报文写的是什么就是什么」。
    /// </para>
    /// </remarks>
    private static DateTime? ParseMediaTime(string value)
    {
        if (value.IsNullOrWhiteSpace()) return null;

        string[] formats =
        [
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-dd HH:mm:ss.fff",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "yyyy/MM/dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ssK",
            "yyyy-MM-ddTHH:mm:ss.fffK",
            "yyyy-MM-dd HH:mm:ss zzz",
        ];

        if (DateTime.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces, out var exact))
        {
            return exact;
        }

        return DateTime.TryParse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var loose)
            ? loose
            : null;
    }

    /// <summary>
    /// 从文件名或 Key 推断后缀（含点，小写）。
    /// </summary>
    /// <remarks>
    /// 优先用文件名；部分固件在 <c>name</c> 里不带后缀，此时退化为用 <c>object_key</c> 的结尾。
    /// </remarks>
    private static string ResolveSuffix(string fileName, string objectKey)
    {
        foreach (var candidate in new[] { fileName, objectKey })
        {
            if (candidate.IsNullOrWhiteSpace()) continue;
            var dot = candidate.LastIndexOf('.');
            if (dot >= 0 && dot < candidate.Length - 1) return candidate[dot..].ToLowerInvariant();
        }

        return string.Empty;
    }

    /// <summary>
    /// 从对象存储 Key 提取文件组 ID。
    /// </summary>
    /// <remarks>
    /// 机场把文件放在 <c>{前缀}/{device_sn}/{文件名}</c> 下，其中前缀由 <c>storage_config_get</c>
    /// 的 <c>object_key_prefix</c> 决定，本平台用的是<b>工作空间 ID</b>。
    /// 因此这里取 Key 的第一段作为「文件组」，用于按批次浏览与统计完整性。
    /// 结构不符（没有斜杠）时返回空，不影响主流程。
    /// </remarks>
    private static string ExtractGroupId(string objectKey)
    {
        if (objectKey.IsNullOrWhiteSpace()) return null;
        var separator = objectKey.IndexOf('/');
        return separator > 0 ? objectKey[..separator] : null;
    }

    #endregion
}
