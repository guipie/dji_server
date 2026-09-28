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
using Dji.Core.Enum.DjiEnum.Ops;

namespace Dji.Application.Cloud;

/// <summary>
/// AirSense（周边民航客机感知）上行处理（MQTT）。
/// </summary>
/// <remarks>
/// <para>
/// <b>报文 <c>data</c> 本身是数组，这是本方法唯一容易写错的地方</b>：
/// 其它 events 报文的 <c>data</c> 都是对象，只有 <c>airsense_warning</c> 直接给数组，
/// 一次可同时推送多架民航客机。因此泛型参数是 <c>List&lt;AirSenseWarningItem&gt;</c>；
/// 若误写成单个对象，反序列化会失败（或静默得到 null），表现为「机场明明推了告警、平台一条都没收到」。
/// </para>
/// <para>
/// 只订阅、不提供任何下行指令 —— AirSense 是设备侧的被动感知能力，云端无法开启或关闭它。
/// </para>
/// </remarks>
internal class MqAirSenseService(
    ILogger<MqAirSenseService> logger,
    MqttGatewayPublish gatewayPublish,
    DjiAirSenseRepository airSenseRepository) : BaseModuleService
{
    /// <summary>
    /// 判定「需要提醒运维」的等级阈值（官方口径：≥ 3 建议无人机避让）。
    /// </summary>
    private const int AlertWarningLevel = (int)AirSenseWarningLevelEnum.Level3;

    private readonly ILogger<MqAirSenseService> _logger = logger;
    private readonly MqttGatewayPublish _gatewayPublish = gatewayPublish;
    private readonly DjiAirSenseRepository _airSenseRepository = airSenseRepository;

    /// <summary>
    /// 周边民航客机告警（<c>need_reply = 1</c>）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>顺序是「先落库、再回包」</b>：落库只是一次批量插入，耗时可控；而回包过早会让设备立刻推下一批，
    /// 在高频场景下反而堆起更多待处理任务。
    /// </para>
    /// <para>
    /// <b>落库失败时不做额外补偿</b>：仓储内部已把「机场未建档」等可预期情况降级为日志并返回 0，
    /// 不会抛异常；真正不可预期的异常由 <c>MqttService</c> 的兜底 catch 记录。
    /// 此时设备收不到应答会重发，重发是安全的（每次都插新行，最多产生重复记录），
    /// 比「为了回包而吞掉异常、告警静默丢失」更可接受。
    /// </para>
    /// </remarks>
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.AirsenseWarning)]
    public async Task AirsenseWarningAsync(CloudMqData<List<AirSenseWarningItem>> data)
    {
        var items = data.Data;
        if (items is not { Count: > 0 })
        {
            await _gatewayPublish.PublishAsync(Topics.ThingProductEventsReply, ToEventReply(data));
            return;
        }

        // 只把「建议避让」及以上的目标打成 Warning，其余降为 Information：
        // 等级 0~2 的民航客机日常大量存在，全部按告警级别打日志会把真正要紧的记录淹掉
        var alerts = items.Where(m => (m?.WarningLevel ?? 0) >= AlertWarningLevel).ToList();

        if (alerts.Count > 0)
        {
            _logger.LogWarning("机场 {Gateway} 检出 {Count} 架需避让的民航客机：{Targets}",
                data.Gateway, alerts.Count,
                string.Join("；", alerts.Select(m => $"ICAO={m.Icao} 等级={m.WarningLevel} 水平距离={m.Distance}米 相对高度={m.RelativeAltitude}米")));
        }
        else
        {
            _logger.LogInformation("机场 {Gateway} 上报 {Count} 架民航客机，等级均低于警戒值（最高 {Max}）",
                data.Gateway, items.Count, items.Max(m => m?.WarningLevel ?? 0));
        }

        await _airSenseRepository.InsertAsync(data.Gateway, items, data.TimeStamp);

        await _gatewayPublish.PublishAsync(Topics.ThingProductEventsReply, ToEventReply(data));
    }
}
