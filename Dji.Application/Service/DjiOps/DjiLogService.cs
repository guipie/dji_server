// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using System.Linq;
using Dji.Application.Cloud.Core;
using Dji.Application.Cloud.Dto.Ops;
using Dji.Application.Cloud.Entity;
using Dji.Application.CloudRepository;
using Dji.Application.Service.Common;
using Dji.Application.Service.DjiOps.Dto;
using Dji.Core.Enum.DjiEnum.Ops;
using Dji.JsonSerialization;

namespace Dji.Application.Service.DjiOps;

/// <summary>
/// 远程日志服务（列举 → 上传 → 下载）。
/// </summary>
/// <remarks>
/// <para>
/// <b>三步链路，本服务负责第一步与第二步的「发起」，第三步由设备直传对象存储</b>：
/// <list type="number">
/// <item><c>List</c>：向设备列举可上传的日志索引，落入 <c>dji_log_file</c>（状态「待上传」）；</item>
/// <item><c>Start</c>：把对象存储凭证 + 选中的索引下发给设备，设备开始直传；</item>
/// <item><c>Cancel</c>：按模块取消（协议只支持到模块粒度）。</item>
/// </list>
/// 上传进度由设备通过 <c>fileupload_progress</c> 推送，<c>MqLogService</c> 落到同一行。
/// </para>
/// <para>
/// <b>为什么「列举」要写库而不是直接返回给前端</b>：<c>fileupload_progress</c> 只回设备 + 模块 + 文件大小，
/// <b>没有 boot_index</b>。若不先把列举结果落库，进度到达时就没有任何东西能与它对应上。
/// 换句话说，这张表是「进度能落地」的前提，不是缓存。
/// </para>
/// <para>
/// <b>为什么上传地址要由云端指定</b>：<c>fileupload_start</c> 的 <c>params.files[].object_key</c>
/// 决定文件在桶里的目录，本平台统一放在 <c>{工作空间}/log/{模块}</c> 下 ——
/// 与 <c>storage_config_get</c>（日志）下发的 Key 前缀一致，两条路径不会各自为政。
/// </para>
/// </remarks>
[ApiDescriptionSettings(ApplicationConst.GroupName, Order = 160)]
public class DjiLogService(
    SqlSugarRepository<Dji.Core.Entity.DjiDevice> deviceRep,
    SqlSugarRepository<DjiLogFile> logRep,
    DjiLogRepository logRepository,
    DjiStorageService storageService,
    MqttGatewayPublish publish,
    UserManager userManager,
    ILogger<DjiLogService> logger) : IDynamicApiController, ITransient
{
    /// <summary>列举回包等待时长（设备要遍历本地日志目录，比普通指令慢）</summary>
    private const int ListReplyTimeoutSeconds = 20;

    /// <summary>下发回包等待时长</summary>
    private const int CommandReplyTimeoutSeconds = 15;

    private readonly SqlSugarRepository<Dji.Core.Entity.DjiDevice> _deviceRep = deviceRep;
    private readonly SqlSugarRepository<DjiLogFile> _logRep = logRep;
    private readonly DjiLogRepository _logRepository = logRepository;
    private readonly DjiStorageService _storageService = storageService;
    private readonly MqttGatewayPublish _publish = publish;
    private readonly UserManager _userManager = userManager;
    private readonly ILogger<DjiLogService> _logger = logger;

    #region 查询

    /// <summary>日志文件分页</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Page")]
    public async Task<SqlSugarPagedList<LogFileOutput>> Page(LogFileSearchInput input)
    {
        input ??= new LogFileSearchInput();

        var query = _logRep.AsQueryable()
            .WhereIF(!input.WorkspaceId.IsNullOrWhiteSpace(), m => m.WorkspaceId == input.WorkspaceId)
            .WhereIF(!input.DockSn.IsNullOrWhiteSpace(), m => m.DockSn == input.DockSn)
            .WhereIF(!input.DeviceSn.IsNullOrWhiteSpace(), m => m.DeviceSn == input.DeviceSn)
            .WhereIF(input.Module.HasValue, m => m.Module == input.Module)
            .WhereIF(input.Status.HasValue, m => m.Status == input.Status)
            .WhereIF(!input.Keyword.IsNullOrWhiteSpace(), m =>
                (m.FileName != null && m.FileName.Contains(input.Keyword.Trim()))
                || (m.ObjectKey != null && m.ObjectKey.Contains(input.Keyword.Trim())))
            .WhereIF(input.StartTime.HasValue, m => m.BeginTime >= input.StartTime)
            .WhereIF(input.EndTime.HasValue, m => m.BeginTime <= input.EndTime)
            .OrderBy(m => m.BeginTime, OrderByType.Desc);

        var paged = await query.Select(m => new LogFileOutput
        {
            Id = m.Id,
            WorkspaceId = m.WorkspaceId,
            DockSn = m.DockSn,
            DeviceSn = m.DeviceSn,
            Module = m.Module,
            BootIndex = m.BootIndex,
            BeginTime = m.BeginTime,
            EndTime = m.EndTime,
            FileSize = m.FileSize,
            Status = m.Status,
            Progress = m.Progress,
            UploadRate = m.UploadRate,
            ObjectKey = m.ObjectKey,
            Fingerprint = m.Fingerprint,
            FileName = m.FileName,
            Url = m.Url,
            ErrorMessage = m.ErrorMessage,
            OperatorName = m.OperatorName,
            CreateTime = m.CreateTime,
            FinishTime = m.FinishTime,
        }).ToPagedListAsync(input.Page, input.PageSize);

        await FillAsync(paged.Items.ToList());
        return paged;
    }

    /// <summary>日志统计（概览卡片）</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Stats")]
    public async Task<LogStatsOutput> Stats(LogStatsInput input)
    {
        input ??= new LogStatsInput();

        var rows = await _logRep.AsQueryable()
            .WhereIF(!input.WorkspaceId.IsNullOrWhiteSpace(), m => m.WorkspaceId == input.WorkspaceId)
            .WhereIF(!input.DockSn.IsNullOrWhiteSpace(), m => m.DockSn == input.DockSn)
            .ToListAsync();

        return new LogStatsOutput
        {
            Total = rows.Count,
            PendingCount = rows.Count(m => m.Status == LogUploadStatusEnum.Pending),
            UploadingCount = rows.Count(m => m.Status == LogUploadStatusEnum.Uploading),
            UploadedCount = rows.Count(m => m.Status == LogUploadStatusEnum.Uploaded),
            FailedCount = rows.Count(m => m.Status == LogUploadStatusEnum.Failed),
            TotalSize = rows.Sum(m => m.FileSize ?? 0),
            UploadedSize = rows.Where(m => m.Status == LogUploadStatusEnum.Uploaded).Sum(m => m.FileSize ?? 0),
        };
    }

    /// <summary>模块字典</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "ModuleOptions")]
    public List<OpsDictOptionOutput> ModuleOptions()
        => typeof(LogModuleEnum).GetEnumDescDictionary()
            .OrderBy(m => m.Key)
            .Select(m => new OpsDictOptionOutput { Value = m.Key, Label = m.Value })
            .ToList();

    /// <summary>上传状态字典</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "StatusOptions")]
    public List<OpsDictOptionOutput> StatusOptions()
        => typeof(LogUploadStatusEnum).GetEnumDescDictionary()
            .OrderBy(m => m.Key)
            .Select(m => new OpsDictOptionOutput { Value = m.Key, Label = m.Value })
            .ToList();

    #endregion

    #region 列举 / 上传 / 取消

    /// <summary>
    /// 向设备列举可上传的日志，并返回该机场当前的日志清单。
    /// </summary>
    /// <remarks>
    /// 设备只返回<b>被问到的模块</b>，因此不传模块时两个模块都问。
    /// 每次列举都是「补齐新增」而非覆盖：已上传成功的记录不会被退回到待上传。
    /// </remarks>
    [HttpPost]
    [ApiDescriptionSettings(Name = "List")]
    public async Task<List<LogFileOutput>> List([FromBody] LogListInput input)
    {
        var dock = await ResolveDockAsync(input.DockSn);
        EnsureOnline(dock);

        var modules = input.Modules is { Count: > 0 }
            ? input.Modules
            : [LogModuleEnum.Drone, LogModuleEnum.Dock];

        var payload = new FileuploadListPayload
        {
            ModuleList = modules.Select(m => ((int)m).ToString()).Distinct().ToList(),
        };

        var request = new CloudMqRequest<FileuploadListPayload>(TopicMethods.FileuploadList, payload, dock.Sn);

        FileuploadListReply reply;
        try
        {
            var envelope = await _publish.PublishWithReplyDataAsync<FileuploadListPayload, FileuploadListReply>(
                Topics.ThingProductServices, request, ListReplyTimeoutSeconds);
            reply = envelope?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "列举设备日志无应答：gateway:{Gateway}", dock.Sn);
            throw Oops.Oh("设备未在超时时间内返回日志列表，请确认机场在线后重试");
        }

        if (reply == null)
            throw Oops.Oh("设备未返回日志列表");

        if ((reply.Result ?? 0) != 0)
            throw Oops.Oh($"设备拒绝列举日志，错误码：{reply.Result}");

        var added = await _logRepository.SaveListAsync(dock.Sn, dock.WorkspaceId, reply.Files, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        _logger.LogInformation("已列举机场 {Gateway} 的日志：设备数 {Devices}，新增索引 {Added}",
            dock.Sn, reply.Files?.Count ?? 0, added);

        return await GetDockLogsAsync(dock);
    }

    /// <summary>
    /// 发起日志上传。
    /// </summary>
    /// <remarks>
    /// <para>前置校验：机场在线、对象存储已配置、所选记录属于该机场且尚未上传成功。</para>
    /// <para>
    /// <b>按模块分组下发</b>：协议的 <c>params.files[].list[]</c> 里只有 <c>boot_index</c>，
    /// 没有设备 SN —— 设备是<b>靠 <c>module</c> 判断该传给谁的</b>（0 → 飞行器，3 → 机场）。
    /// 因此必须以模块为最小分组单位，不能把两个模块的索引混在一起。
    /// </para>
    /// </remarks>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Start")]
    public async Task<int> Start([FromBody] LogUploadStartInput input)
    {
        var dock = await ResolveDockAsync(input.DockSn);
        EnsureOnline(dock);

        if (input.Ids == null || input.Ids.Count == 0)
            throw Oops.Oh("请至少选择一条日志");

        var config = _storageService.BuildStorageConfig(dock.WorkspaceId);
        if (config == null)
            throw Oops.Oh("本平台对象存储未就绪（请检查 Upload.json 的 OSSProvider 段），无法下发日志上传指令");

        var rows = await _logRepository.GetByIdsAsync(input.Ids);
        if (rows.Count == 0) throw Oops.Oh("所选日志记录不存在，请刷新后重试");

        var invalid = rows.Where(m => m.DockSn != dock.Sn).ToList();
        if (invalid.Count > 0)
            throw Oops.Oh("所选日志中包含不属于该机场的记录，请刷新后重试");

        var uploaded = rows.Where(m => m.Status == LogUploadStatusEnum.Uploaded).ToList();
        if (uploaded.Count == rows.Count)
            throw Oops.Oh("所选日志均已上传，无需重复上传");

        var selected = rows.Where(m => m.Status != LogUploadStatusEnum.Uploaded).ToList();

        using var _ = InFlightGuard.Enter($"log|{dock.Sn}", "日志上传");

        // 同一时间只支持一个模块（协议只允许按模块取消，多模块并发时无法分别取消）
        var modules = selected.Select(m => m.Module).Distinct().ToList();
        if (modules.Count > 1)
            throw Oops.Oh("一次只能上传一个模块的日志（飞行器 / 机场），请分开上传");

        var module = modules[0];
        var payload = new FileuploadStartPayload
        {
            Bucket = config.Bucket,
            Region = config.Region,
            Endpoint = config.Endpoint,
            Provider = config.Provider,
            Credentials = new FileuploadCredentials
            {
                AccessKeyId = config.Credentials?.AccessKeyId,
                AccessKeySecret = config.Credentials?.AccessKeySecret,
                Expire = config.Credentials?.Expire ?? 0,
                SecurityToken = config.Credentials?.SecurityToken,
            },
            Params = new FileuploadParams
            {
                Files =
                [
                    new FileuploadFileGroup
                    {
                        // 与 storage_config_get（日志）下发的 Key 前缀保持同一棵树
                        ObjectKey = BuildObjectKey(dock.WorkspaceId, module),
                        Module = ((int)module).ToString(),
                        List = selected.Select(m => new FileuploadBootIndex { BootIndex = m.BootIndex }).ToList(),
                    },
                ],
            },
        };

        var request = new CloudMqRequest<FileuploadStartPayload>(TopicMethods.FileuploadStart, payload, dock.Sn);
        var result = await SendAsync(dock.Sn, request, "发起日志上传");

        if (result != 0)
            throw Oops.Oh($"设备拒绝日志上传，{DescribeResult(result)}");

        var operatorName = _userManager.RealName ?? _userManager.Account;
        var count = await _logRepository.MarkUploadingAsync(selected.Select(m => m.Id).ToList(), operatorName);

        _logger.LogInformation("已发起日志上传：gateway:{Gateway}，module:{Module}，文件数 {Count}，operator:{Operator}",
            dock.Sn, (int)module, count, operatorName);

        return count;
    }

    /// <summary>
    /// 取消日志上传。
    /// </summary>
    /// <remarks>
    /// 协议只支持<b>按模块</b>取消（<c>fileupload_update</c> + <c>module_list</c>），
    /// 无法精确取消某一个文件 —— 因此入参是模块而不是记录 ID。
    /// </remarks>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Cancel")]
    public async Task<int> Cancel([FromBody] LogCancelInput input)
    {
        var dock = await ResolveDockAsync(input.DockSn);
        EnsureOnline(dock);

        if (input.Modules == null || input.Modules.Count == 0)
            throw Oops.Oh("请至少选择一个要取消的模块");

        var payload = new FileuploadUpdatePayload
        {
            Status = "cancel",
            ModuleList = input.Modules.Select(m => ((int)m).ToString()).Distinct().ToList(),
        };

        var request = new CloudMqRequest<FileuploadUpdatePayload>(TopicMethods.FileuploadUpdate, payload, dock.Sn);
        var result = await SendAsync(dock.Sn, request, "取消日志上传");

        if (result != 0)
            throw Oops.Oh($"设备拒绝取消日志上传，{DescribeResult(result)}");

        var pending = await _logRepository.GetPendingAsync(dock.Sn, input.Modules);
        var uploading = (await _logRep.AsQueryable()
                .Where(m => m.DockSn == dock.Sn && input.Modules.Contains(m.Module)
                    && m.Status == LogUploadStatusEnum.Uploading)
                .ToListAsync())
            .Select(m => m.Id);

        var ids = pending.Select(m => m.Id).Concat(uploading).ToList();
        var count = await _logRepository.MarkCanceledAsync(ids);

        _logger.LogInformation("已取消机场 {Gateway} 的日志上传：模块 {Modules}，收口 {Count} 条",
            dock.Sn, string.Join(",", input.Modules), count);

        return count;
    }

    /// <summary>
    /// 删除日志记录。
    /// </summary>
    /// <remarks>
    /// <b>只删记录，不删对象存储里的文件</b>：本平台不持有对象存储的删除权限（下发给设备的是上传凭证），
    /// 且日志文件本身就是证据，误删不可恢复。要清理存储请按桶的生命周期规则处理。
    /// </remarks>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Delete")]
    public async Task<int> Delete([FromBody] LogIdsInput input)
        => await _logRepository.DeleteAsync(input?.Ids);

    #endregion

    #region 私有实现

    /// <summary>取某机场的日志清单（列表接口复用，按产生时间倒序取最近 500 条）</summary>
    private async Task<List<LogFileOutput>> GetDockLogsAsync(Dji.Core.Entity.DjiDevice dock)
    {
        var rows = await _logRep.AsQueryable()
            .Where(m => m.DockSn == dock.Sn)
            .OrderBy(m => m.BeginTime, OrderByType.Desc)
            .Take(500)
            .Select(m => new LogFileOutput
            {
                Id = m.Id,
                WorkspaceId = m.WorkspaceId,
                DockSn = m.DockSn,
                DeviceSn = m.DeviceSn,
                Module = m.Module,
                BootIndex = m.BootIndex,
                BeginTime = m.BeginTime,
                EndTime = m.EndTime,
                FileSize = m.FileSize,
                Status = m.Status,
                Progress = m.Progress,
                UploadRate = m.UploadRate,
                ObjectKey = m.ObjectKey,
                Fingerprint = m.Fingerprint,
                FileName = m.FileName,
                Url = m.Url,
                ErrorMessage = m.ErrorMessage,
                OperatorName = m.OperatorName,
                CreateTime = m.CreateTime,
                FinishTime = m.FinishTime,
            })
            .ToListAsync();

        await FillAsync(rows);
        return rows;
    }

    /// <summary>日志对象存储 Key：<c>{工作空间}/log/{模块}</c></summary>
    private static string BuildObjectKey(string workspaceId, LogModuleEnum module)
        => $"{(workspaceId.IsNullOrWhiteSpace() ? "default" : workspaceId)}/log/{(int)module}";

    private async Task<int> SendAsync<T>(string dockSn, CloudMqRequest<T> request, string actionName)
    {
        try
        {
            var reply = await _publish.PublishWithReplyAsync<T, object>(
                Topics.ThingProductServices, request, CommandReplyTimeoutSeconds);

            var result = reply?.Data?.Result ?? -1;
            if (result != 0)
            {
                _logger.LogWarning("{Action}被拒：gateway:{Gateway}，method:{Method}，result:{Result}",
                    actionName, dockSn, request.Method, result);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Action}无应答：gateway:{Gateway}，method:{Method}", actionName, dockSn, request.Method);
            return -1;
        }
    }

    private static string DescribeResult(int result)
        => result == -1 ? "设备未在超时时间内回包（指挥链路可能中断）" : $"错误码：{result}";

    /// <summary>填充派生字段（机场昵称、设备名、枚举名、大小文本）</summary>
    private async Task FillAsync(List<LogFileOutput> items)
    {
        if (items.Count == 0) return;

        foreach (var item in items)
        {
            item.ModuleName = Desc(item.Module);
            item.StatusName = Desc(item.Status);
            item.IsRunning = !item.Status.IsFinalState();
            item.FileSizeText = FormatSize(item.FileSize);
        }

        var sns = items.Select(m => m.DockSn).Concat(items.Select(m => m.DeviceSn))
            .Where(m => !m.IsNullOrWhiteSpace()).Distinct().ToList();

        if (sns.Count == 0) return;

        var devices = await _deviceRep.AsQueryable().Where(m => sns.Contains(m.Sn)).ToListAsync();
        var nameMap = devices.ToDictionary(m => m.Sn, m => m.Nick.IsNullOrWhiteSpace() ? m.Model : m.Nick);

        foreach (var item in items)
        {
            item.DockNick = nameMap.GetValueOrDefault(item.DockSn);
            var name = nameMap.GetValueOrDefault(item.DeviceSn);
            item.DeviceName = item.Module == LogModuleEnum.Dock
                ? (name.IsNullOrWhiteSpace() ? item.DeviceSn : $"机场本体｜{name}")
                : (name ?? item.DeviceSn);
        }
    }

    /// <summary>字节数转可读文本（日志文件从几十 KB 到几十 MB，用固定单位会让列表难扫读）</summary>
    private static string FormatSize(long? size)
    {
        if (size is null or <= 0) return "-";

        var value = size.Value;
        if (value < 1024) return $"{value} B";
        if (value < 1024 * 1024) return $"{value / 1024.0:0.#} KB";
        if (value < 1024L * 1024 * 1024) return $"{value / 1024.0 / 1024:0.#} MB";
        return $"{value / 1024.0 / 1024 / 1024:0.##} GB";
    }

    private static void EnsureOnline(Dji.Core.Entity.DjiDevice dock)
    {
        if (!dock.IsOnline) throw Oops.Oh("机场离线，无法操作日志");
    }

    private async Task<Dji.Core.Entity.DjiDevice> ResolveDockAsync(string dockSn)
    {
        if (dockSn.IsNullOrWhiteSpace()) throw Oops.Oh("机场不能为空");

        var dock = await _deviceRep.GetFirstAsync(m => m.Sn == dockSn) ?? throw Oops.Oh($"未找到机场：{dockSn}");
        if (dock.Domain != DomainEnum.Dock) throw Oops.Oh($"{dockSn} 不是机场设备");
        if (dock.WorkspaceId.IsNullOrWhiteSpace()) throw Oops.Oh($"机场【{dock.Nick ?? dock.Sn}】尚未绑定工作空间");

        return dock;
    }

    /// <summary>取枚举的 <c>[Description]</c>；显式写全限定名以避免与 NewLife 的同名扩展二义</summary>
    private static string Desc<T>(T value) where T : struct, System.Enum
        => Dji.Core.EnumExtension.GetDescription(value);

    #endregion
}
