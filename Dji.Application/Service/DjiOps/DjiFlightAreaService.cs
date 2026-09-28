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
using Dji.Application.Cloud.Entity;
using Dji.Application.CloudRepository;
using Dji.Application.Service.Common;
using Dji.Application.Service.DjiOps.Dto;
using Dji.Core.Enum.DjiEnum.Ops;

namespace Dji.Application.Service.DjiOps;

/// <summary>
/// 自定义飞行区服务（登记文件 → 通知设备 → 观察同步状态）。
/// </summary>
/// <remarks>
/// <para>
/// <b>本服务不生成飞行区文件</b>：围栏是多边形 / 圆形坐标集合，来源通常是测绘工具或空域管理方的划定结果，
/// 由业务方用自备工具产出 JSON 后走平台的上传接口传到对象存储，再在这里登记。
/// 平台负责的是「版本管理 + 下发给哪台设备 + 有没有真的生效」。
/// </para>
/// <para>
/// <b>「下发成功」不等于「已生效」</b>，这是本模块最需要向使用者讲清楚的一点：
/// <c>flight_areas_update</c> 的应答只表示设备<b>收到了通知</b>，设备随后才异步去云端拉文件。
/// 真正的生效依据是 <c>flight_areas_sync_progress</c> 到达 <c>synchronized</c>，
/// 由 <c>MqFlightAreaService</c> 写回本表。因此列表页必须把 <c>SyncStatus</c> 放在显眼位置，
/// 否则会出现「云端显示已下发、现场其实没生效」的黑洞。
/// </para>
/// </remarks>
[ApiDescriptionSettings(ApplicationConst.GroupName, Order = 170)]
public class DjiFlightAreaService(
    SqlSugarRepository<Dji.Core.Entity.DjiDevice> deviceRep,
    SqlSugarRepository<DjiFlightArea> areaRep,
    DjiFlightAreaRepository areaRepository,
    DjiStorageService storageService,
    MqttGatewayPublish publish,
    ILogger<DjiFlightAreaService> logger) : IDynamicApiController, ITransient
{
    /// <summary>下发指令的应答等待时长</summary>
    private const int CommandReplyTimeoutSeconds = 15;

    private readonly SqlSugarRepository<Dji.Core.Entity.DjiDevice> _deviceRep = deviceRep;
    private readonly SqlSugarRepository<DjiFlightArea> _areaRep = areaRep;
    private readonly DjiFlightAreaRepository _areaRepository = areaRepository;
    private readonly DjiStorageService _storageService = storageService;
    private readonly MqttGatewayPublish _publish = publish;
    private readonly ILogger<DjiFlightAreaService> _logger = logger;

    #region 查询

    /// <summary>飞行区文件分页</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Page")]
    public async Task<SqlSugarPagedList<FlightAreaOutput>> Page(FlightAreaSearchInput input)
    {
        input ??= new FlightAreaSearchInput();

        var query = _areaRep.AsQueryable()
            .WhereIF(!input.WorkspaceId.IsNullOrWhiteSpace(), m => m.WorkspaceId == input.WorkspaceId)
            .WhereIF(!input.DockSn.IsNullOrWhiteSpace(), m => m.DockSn == input.DockSn)
            .WhereIF(input.SyncStatus.HasValue, m => m.SyncStatus == input.SyncStatus)
            .WhereIF(input.OnlyActive == true, m => m.IsActive)
            .WhereIF(!input.Keyword.IsNullOrWhiteSpace(), m =>
                m.FileName != null && m.FileName.Contains(input.Keyword.Trim()))
            .OrderBy(m => m.CreateTime, OrderByType.Desc);

        var paged = await query.Select(m => new FlightAreaOutput
        {
            Id = m.Id,
            WorkspaceId = m.WorkspaceId,
            DockSn = m.DockSn,
            FileName = m.FileName,
            ObjectKey = m.ObjectKey,
            FileSize = m.FileSize,
            Checksum = m.Checksum,
            SyncStatus = m.SyncStatus,
            SyncReason = m.SyncReason,
            IsActive = m.IsActive,
            LastTime = m.LastTime,
            CreateTime = m.CreateTime,
        }).ToPagedListAsync(input.Page, input.PageSize);

        await FillAsync(paged.Items.ToList());
        return paged;
    }

    /// <summary>
    /// 取某机场飞行器与各飞行区的距离快照（控制面板 / 地图叠加用）。
    /// </summary>
    /// <remarks>
    /// 数据来自设备高频推送的 <c>flight_areas_drone_location</c>，本平台只保留每个区域的最新值，
    /// 因此这里返回的行数等于「该机场已启用的区域数」，不会随时间增长。
    /// </remarks>
    [HttpGet]
    [ApiDescriptionSettings(Name = "Locations")]
    public async Task<List<FlightAreaLocationOutput>> Locations([FromQuery] string dockSn)
    {
        if (dockSn.IsNullOrWhiteSpace()) return [];

        var rows = await _areaRepository.GetLocationsAsync(dockSn);
        return rows.Select(m => new FlightAreaLocationOutput
        {
            AreaId = m.AreaId,
            AreaDistance = m.AreaDistance,
            IsInArea = m.IsInArea,
            MinDistance = m.MinDistance,
            EnterCount = m.EnterCount,
            LastEnterTime = m.LastEnterTime,
            LastExitTime = m.LastExitTime,
            LastTime = m.LastTime,
        }).ToList();
    }

    /// <summary>机场下拉</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "DockOptions")]
    public async Task<List<OpsDockOptionOutput>> DockOptions(OpsDockOptionInput input)
    {
        input ??= new OpsDockOptionInput();

        var docks = await _deviceRep.AsQueryable()
            .Where(m => m.Domain == DomainEnum.Dock)
            .WhereIF(!input.WorkspaceId.IsNullOrWhiteSpace(), m => m.WorkspaceId == input.WorkspaceId)
            .OrderBy(m => m.Nick)
            .ToListAsync();

        return docks.Select(m => new OpsDockOptionOutput
        {
            Sn = m.Sn,
            Nick = m.Nick,
            WorkspaceId = m.WorkspaceId,
            IsOnline = m.IsOnline,
            Label = $"{m.Nick ?? m.Sn}（{m.Sn}）",
        }).ToList();
    }

    /// <summary>同步状态字典</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "SyncStatusOptions")]
    public List<OpsDictOptionOutput> SyncStatusOptions()
        => typeof(FlightAreaSyncStatusEnum).GetEnumDescDictionary()
            .OrderBy(m => m.Key)
            .Select(m => new OpsDictOptionOutput { Value = m.Key, Label = m.Value })
            .ToList();

    /// <summary>同步失败原因字典</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "SyncReasonOptions")]
    public List<OpsDictOptionOutput> SyncReasonOptions()
        => typeof(FlightAreaSyncReasonEnum).GetEnumDescDictionary()
            .OrderBy(m => m.Key)
            .Select(m => new OpsDictOptionOutput { Value = m.Key, Label = m.Value })
            .ToList();

    #endregion

    #region 登记 / 下发 / 删除

    /// <summary>
    /// 登记一份飞行区文件（可选立即通知设备同步）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>摘要由服务端从桶里读出来算</b>，不采信调用方传进来的值 —— 摘要是「云端认为该跑哪一版」
    /// 与「设备实际跑到哪一版」唯一的对齐依据，人工填写一旦出错就是一个静默失效的围栏。
    /// 仅在对象存储不可用（读不到内容）时，才退化为使用入参里的 <c>Checksum</c>，并打警告。
    /// </para>
    /// <para>
    /// <b>登记不会自动把新文件置为启用</b>：要等设备同步成功回调才置位，
    /// 避免出现「云端显示已生效、现场还在跑旧围栏」。
    /// </para>
    /// </remarks>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Register")]
    public async Task<FlightAreaOutput> Register([FromBody] FlightAreaRegisterInput input)
    {
        var dock = await ResolveDockAsync(input.DockSn);

        var objectKey = _storageService.ExtractObjectKey(input.ObjectKey);
        if (objectKey.IsNullOrWhiteSpace())
            throw Oops.Oh("无法从所填内容解析出对象存储 Key，请填写 Key 或完整的文件地址");

        // 文件名与 Key 的最后一段保持一致：不一致时设备回传的 file.name 会对不上，
        // 后续按名字的降级匹配就失效了
        var fileName = input.FileName.Trim();
        var keyName = objectKey.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (!keyName.IsNullOrWhiteSpace() && !string.Equals(fileName, keyName, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("登记飞行区文件时文件名与对象 Key 的最后一段不一致（{FileName} vs {KeyName}），以入参文件名为准",
                fileName, keyName);
        }

        var (computed, size) = await _storageService.ComputeFileDigestAsync(objectKey);
        var checksum = computed;
        if (checksum.IsNullOrWhiteSpace())
        {
            checksum = input.Checksum?.Trim();
            if (checksum.IsNullOrWhiteSpace())
            {
                throw Oops.Oh("无法读取对象存储中的文件以计算 SHA256 摘要。" +
                               "请确认 Upload.json 的 OSSProvider 已启用且对象已上传，或在入参中显式提供 Checksum 作为兜底");
            }

            _logger.LogWarning("对象存储未启用或读取失败，飞行区文件摘要改用入参提供值：{Checksum}（机场 {DockSn}）",
                checksum, dock.Sn);
        }
        else if (!input.Checksum.IsNullOrWhiteSpace()
                 && !string.Equals(input.Checksum.Trim(), checksum, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("入参提供的摘要与桶内实际内容不符，已采用服务端计算值。" +
                               "机场 {DockSn}，入参 {Input}，实际 {Computed}",
                dock.Sn, input.Checksum.Trim(), checksum);
        }

        var entity = await _areaRepository.RegisterAsync(dock.Sn, fileName, objectKey, checksum, size > 0 ? size : null);
        if (entity == null) throw Oops.Oh($"登记飞行区文件失败：机场【{dock.Nick ?? dock.Sn}】尚未建档");

        _logger.LogInformation("已登记飞行区文件：机场={DockSn}，文件={FileName}，摘要={Checksum}，是否立即同步={Sync}",
            dock.Sn, fileName, checksum, input.SyncImmediately);

        if (input.SyncImmediately)
            await NotifyDeviceAsync(dock, input.Confirm);

        var output = await ToOutputAsync(entity);
        return output;
    }

    /// <summary>
    /// 通知设备去云端拉取并启用最新的飞行区文件（下发 <c>flight_areas_update</c>）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>这条指令没有入参</b>（协议 <c>data = null</c>），设备收到后会自己去调用
    /// <c>flight_areas_get</c>。因此本接口不关心「下发哪个文件」，只关心「该机场有没有可下发的文件」。
    /// </para>
    /// <para>
    /// <b>没有文件时直接拒绝，不发空指令</b>：设备拿到空列表会清掉本地围栏，
    /// 而调用方的本意通常只是「让设备刷新一下」。真要清空围栏应当显式删除文件记录后再下发。
    /// </para>
    /// </remarks>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Update")]
    public async Task<bool> Update([FromBody] FlightAreaUpdateInput input)
    {
        var dock = await ResolveDockAsync(input.DockSn);
        await NotifyDeviceAsync(dock, input.Confirm);
        return true;
    }

    /// <summary>
    /// 删除飞行区文件记录。
    /// </summary>
    /// <remarks>
    /// <b>只删记录，不删对象存储里的文件</b>：设备可能仍在跑这份围栏，保留原件可以让运维在误删后
    /// 迅速重新登记；桶内清理交给存储的生命周期策略。
    /// </remarks>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Delete")]
    public async Task<int> Delete([FromBody] FlightAreaIdsInput input)
        => await _areaRepository.DeleteAsync(input?.Ids);

    #endregion

    #region 私有实现

    /// <summary>下发 <c>flight_areas_update</c> 通知设备同步</summary>
    private async Task NotifyDeviceAsync(Dji.Core.Entity.DjiDevice dock, bool confirm)
    {
        if (!confirm)
            throw Oops.Oh("该操作会让设备重新加载作业区域，请确认后再执行");

        if (!dock.IsOnline) throw Oops.Oh("机场离线，无法下发飞行区同步指令");

        var files = await _areaRepository.GetByDockAsync(dock.Sn);
        if (files.Count == 0 || files.All(m => m.ObjectKey.IsNullOrWhiteSpace()))
            throw Oops.Oh("该机场尚未登记可用的飞行区文件，下发后设备会清空本地围栏，已阻止本次操作");

        // 防连点：设备解析文件 + 翻转图传链路需要几十秒，期间重复下发会让设备状态错乱
        using var _ = InFlightGuard.Enter($"flightarea|{dock.Sn}", "飞行区同步");

        var request = new CloudMqRequest<object>(TopicMethods.FlightAreasUpdate, dock.Sn);

        int result;
        try
        {
            var reply = await _publish.PublishWithReplyAsync<object, object>(
                Topics.ThingProductServices, request, CommandReplyTimeoutSeconds);

            result = reply?.Data?.Result ?? -1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下发飞行区同步指令无应答：gateway:{Gateway}", dock.Sn);
            throw Oops.Oh("设备未在超时时间内回包（指挥链路可能中断），请确认机场在线后重试");
        }

        if (result != 0)
        {
            _logger.LogWarning("飞行区同步指令被拒：gateway:{Gateway}，result:{Result}", dock.Sn, result);
            throw Oops.Oh($"设备拒绝执行飞行区同步，{(result == -1 ? "未在超时时间内回包" : $"错误码：{result}")}");
        }

        _logger.LogInformation("已下发飞行区同步指令：gateway:{Gateway}（等待设备上报同步进度）", dock.Sn);
    }

    /// <summary>填充派生字段（昵称、大小文本、枚举名）</summary>
    private async Task FillAsync(List<FlightAreaOutput> items)
    {
        if (items.Count == 0) return;

        foreach (var item in items)
        {
            item.SyncStatusName = Desc(item.SyncStatus);
            // 只有同步失败时原因码才有意义，其余状态一律不展示，避免出现「已同步 + 原因：无错误」的噪音
            item.SyncReasonName = item.SyncStatus is FlightAreaSyncStatusEnum.Fail or FlightAreaSyncStatusEnum.SwitchFail
                ? Desc(item.SyncReason)
                : null;
            item.FileSizeText = FormatSize(item.FileSize);
        }

        var dockSns = items.Select(m => m.DockSn).Where(m => !m.IsNullOrWhiteSpace()).Distinct().ToList();
        if (dockSns.Count == 0) return;

        var docks = await _deviceRep.AsQueryable().Where(m => dockSns.Contains(m.Sn)).ToListAsync();
        var nickMap = docks.ToDictionary(m => m.Sn, m => m.Nick);

        foreach (var item in items) item.DockNick = nickMap.GetValueOrDefault(item.DockSn);
    }

    /// <summary>单条转输出并填充派生字段</summary>
    private async Task<FlightAreaOutput> ToOutputAsync(DjiFlightArea entity)
    {
        var output = new FlightAreaOutput
        {
            Id = entity.Id,
            WorkspaceId = entity.WorkspaceId,
            DockSn = entity.DockSn,
            FileName = entity.FileName,
            ObjectKey = entity.ObjectKey,
            FileSize = entity.FileSize,
            Checksum = entity.Checksum,
            SyncStatus = entity.SyncStatus,
            SyncReason = entity.SyncReason,
            IsActive = entity.IsActive,
            LastTime = entity.LastTime,
            CreateTime = entity.CreateTime,
        };

        await FillAsync([output]);
        return output;
    }

    /// <summary>字节数转可读文本</summary>
    private static string FormatSize(long? size)
    {
        if (size is null or <= 0) return "-";

        var value = size.Value;
        if (value < 1024) return $"{value} B";
        if (value < 1024 * 1024) return $"{value / 1024.0:0.#} KB";
        return $"{value / 1024.0 / 1024:0.#} MB";
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
