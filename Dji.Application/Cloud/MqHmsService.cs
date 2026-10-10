// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Cloud.Core;
using Dji.Application.Cloud.Dto.Hms;
using Dji.Application.Cloud.Entity;
using Dji.Application.CloudRepository;

namespace Dji.Application.Cloud;

/// <summary>
/// HMS 健康告警上行处理（MQTT）。
/// </summary>
/// <remarks>
/// 整个 HMS 功能只有这一条上行消息，云端不需要下发任何指令，因此实现非常薄：
/// 只做落库（由仓储完成状态演算），协议未要求回包。
/// </remarks>
internal class MqHmsService(
    ILogger<MqHmsService> logger,
    DjiHmsRepository hmsRepository)
{
    private readonly ILogger<MqHmsService> _logger = logger;
    private readonly DjiHmsRepository _hmsRepository = hmsRepository;

    /// <summary>
    /// 设备健康告警上报。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>报文是全量快照，空列表也是有效信息</b>（表示所有告警已恢复），
    /// 因此即使 <c>list</c> 为空也必须走一次同步，否则「告警全部解除」这一状态变化会丢失。
    /// </para>
    /// <para>
    /// <b>不回 <c>events_reply</c></b>：官方协议里 HMS 报文没有 <c>need_reply</c> 字段
    /// （字段只有 <c>bid / data / tid / timestamp / method</c>），无需应答；
    /// 与媒体回调 / 飞行区同步进度等「带 <c>need_reply = 1</c>」的事件不同。
    /// 回一条多余的 <c>events_reply</c> 只会持续占用下行主题。
    /// </para>
    /// </remarks>
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.Hms)]
    public async Task HmsAsync(CloudMqData<HmsEventData> data)
    {
        var list = data.Data?.List ?? [];

        var added = await _hmsRepository.SyncAsync(data.Gateway, list, data.TimeStamp);

        if (list.Count == 0)
            _logger.LogInformation("机场 {Gateway} 上报 HMS 空快照，所有告警视为已恢复", data.Gateway);
        else
            _logger.LogInformation("机场 {Gateway} 上报 HMS 告警 {Count} 条（新增 {Added} 条）",
                data.Gateway, list.Count, added < 0 ? 0 : added);
    }
}
