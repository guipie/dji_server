// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Cloud.Core;
using Dji.Application.Cloud.Dto.Live;
using Dji.Application.Cloud.Entity;
using Dji.Application.CloudRepository;

namespace Dji.Application.Cloud;

/// <summary>
/// 机场直播状态上行处理（MQTT）。
/// </summary>
/// <remarks>
/// <para>
/// 直播的上行只有一处：物模型<b>状态</b>主题（<c>thing/product/{sn}/state</c>）里的
/// <c>live_capacity</c>（能力树）与 <c>live_status</c>（在播状态）。
/// 下行（start/stop/quality/lens/camera）由 <c>DjiLiveService</c> 负责，两者分层。
/// </para>
/// <para>
/// <b>必须带 <see cref="DomainEnum.Dock"/></b>：<c>MqttService.MatchModules</c> 对
/// <c>/osd</c>、<c>/state</c>、<c>/property/set_reply</c> 这三类主题会强制按
/// 「主题 SN 解析出的领域」过滤处理器，未声明 Domain 的订阅永远匹配不上，
/// 表现为「订阅了但方法从不执行」。这与普通 services/events 主题的匹配规则不同。
/// </para>
/// <para>
/// <b>本处理器不做业务判定</b>：只把快照落库并让仓储去校正会话状态，
/// 因为设备只在状态变化时推送，一次上报不能代表完整现状（见 <c>DjiLiveRepository</c> 注释）。
/// </para>
/// </remarks>
internal class MqLiveService(
    ILogger<MqLiveService> logger,
    DjiLiveRepository liveRepository) : BaseModuleService
{
    private readonly ILogger<MqLiveService> _logger = logger;
    private readonly DjiLiveRepository _liveRepository = liveRepository;

    /// <summary>
    /// 机场物模型状态推送（含直播能力与直播状态）。
    /// </summary>
    /// <remarks>
    /// 机场的 state 是「分多条推送」的，可能一条只带 <c>live_capacity</c>、另一条只带 <c>live_status</c>，
    /// 因此这里把载荷整体交给仓储做<b>逐字段局部更新</b>，不在本层做合并，
    /// 保证「读取 → 合并 → 写回」三步在同一个方法内完成，避免并发下丢更新。
    /// </remarks>
    [MqttSubscribe(Topics.ThingProductState, DomainEnum.Dock)]
    public async Task DockStateAsync(CloudMqData<DockStateData> data)
    {
        if (data?.Data == null || data.Gateway.IsNullOrWhiteSpace()) return;

        var updated = await _liveRepository.UpsertStateAsync(data.Gateway, data.Data, data.TimeStamp);
        if (!updated) return;

        _logger.LogDebug("机场 {Gateway} 状态快照已更新（直播能力:{HasCapacity}，直播状态:{StatusCount} 路，待上传:{Remain}）",
            data.Gateway,
            data.Data.LiveCapacity != null,
            data.Data.LiveStatus?.Count ?? 0,
            data.Data.MediaFileDetail?.RemainUpload);
    }
}
