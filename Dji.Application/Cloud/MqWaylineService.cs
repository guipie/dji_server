// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Cloud.Core;
using Dji.Application.Cloud.Dto.Wayline;
using Dji.Application.Cloud.Entity;
using Dji.Application.CloudRepository;
using Dji.Application.Option;
using Dji.Application.Service.DjiWayline;
using Microsoft.Extensions.Options;

namespace Dji.Application.Cloud;

/// <summary>
/// 机场航线任务上行处理（MQTT）。
/// </summary>
/// <remarks>
/// <para>
/// 本类只做「解析报文 → 调用仓储/解析器 → 回包」，不写 SQL，也不构造下行任务指令
/// （下发由 <c>DjiWaylineTaskService</c> 负责），以保持「上行与下行职责分离」。
/// </para>
/// <para>
/// 订阅方法必须与方法名**一一对应**：<c>MqttService</c> 在匹配到多个处理器时只会执行其中一条并打印错误日志，
/// 因此重复订阅同一 (Topic, Method) 会导致其中一条静默失效。
/// </para>
/// </remarks>
internal class MqWaylineService(
    ILogger<MqWaylineService> logger,
    MqttGatewayPublish gatewayPublish,
    DjiWaylineTaskRepository taskRepository,
    WaylineKmzResolver kmzResolver,
    IOptions<DjiOptions> options) : BaseModuleService
{
    private readonly ILogger<MqWaylineService> _logger = logger;
    private readonly MqttGatewayPublish _gatewayPublish = gatewayPublish;
    private readonly DjiWaylineTaskRepository _taskRepository = taskRepository;
    private readonly WaylineKmzResolver _kmzResolver = kmzResolver;
    private readonly DjiOptions _options = options.Value;

    #region 航线任务进度 / 就绪

    /// <summary>
    /// 上报航线任务进度。
    /// </summary>
    /// <remarks>
    /// 这是整条任务链路的**信息总线**：状态、执行步骤、当前航点、媒体数量、断点信息都由此上报。
    /// 落库后由 <c>MqttService</c> 统一推送给所属工作空间的在线用户，前端任务中心据此实时刷新。
    /// </remarks>
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.FlightTaskProgress)]
    public async Task FlightTaskProgressAsync(CloudMqData<MqOutput<FlightTaskProgressOutput>> data)
    {
        // 协议把 flight_id 放在 output.ext 内（见官方 flighttask_progress 示例报文）
        var output = data.Data?.Output;
        var flightId = output?.Ext?.FlightId;
        if (output == null || flightId.IsNullOrWhiteSpace())
        {
            _logger.LogWarning("航线任务进度报文缺少 flight_id，已忽略，gateway:{Gateway}", data.Gateway);
            await ReplyEventAsync(data);
            return;
        }

        await _taskRepository.ApplyProgressAsync(flightId, output, data.TimeStamp);
        await ReplyEventAsync(data);
    }

    /// <summary>
    /// 通知任务满足准备条件（仅条件任务 <c>task_type=2</c> 会收到）。
    /// </summary>
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.FlightTaskReady)]
    public async Task FlightTaskReadyAsync(CloudMqData<FlightTaskReadyOutput> data)
    {
        var flightIds = data.Data?.FlightIds;
        if (flightIds is not { Count: > 0 })
        {
            await ReplyEventAsync(data);
            return;
        }

        _logger.LogInformation("机场 {Gateway} 通知任务就绪：{FlightIds}", data.Gateway, string.Join(",", flightIds));
        foreach (var flightId in flightIds) await _taskRepository.MarkReadyAsync(flightId);

        await ReplyEventAsync(data);
    }

    #endregion

    #region 设备主动请求

    /// <summary>
    /// 机场请求云端配置（App 凭证 + NTP 服务器）。
    /// </summary>
    /// <remarks>
    /// 必须回复：机场启动时靠它取 NTP 地址完成校时，而航线任务对时间差极其敏感
    /// （立即任务要求与 <c>execute_time</c> 相差不超过 30 秒），校时失败会直接导致任务被拒绝。
    /// </remarks>
    [MqttSubscribe(Topics.ThingProductRequests, TopicMethods.Config)]
    public async Task ConfigAsync(CloudMqData<CloudConfigRequest> data)
    {
        _logger.LogInformation("机场 {Gateway} 请求云端配置，scope:{Scope}, type:{Type}",
            data.Gateway, data.Data?.ConfigScope, data.Data?.ConfigType);

        if (_options.NtpServerHost.IsNullOrWhiteSpace())
        {
            _logger.LogWarning("未配置 Dji.NtpServerHost，机场将无法通过本平台校时，请尽快在 Dji.json 中补齐");
        }

        var reply = new CloudConfigReply
        {
            AppId = _options.AppId,
            AppKey = _options.AppKey,
            AppLicense = _options.AppLicense,
            NtpServerHost = _options.NtpServerHost,
            NtpServerPort = _options.NtpServerPort,
        };

        await _gatewayPublish.PublishAsync(Topics.ThingProductRequestsReply, ToPublishOutputData(reply, data));
    }

    /// <summary>
    /// 机场重取任务资源（KMZ 下载地址与签名）。
    /// </summary>
    /// <remarks>
    /// 机场在「航线文件失效 / 下载失败 / 断点续飞重新取文件」时会主动索取，
    /// 云端必须给出与 <c>flighttask_prepare</c> 完全一致的 <c>file</c>，否则机场校验签名失败。
    /// </remarks>
    [MqttSubscribe(Topics.ThingProductRequests, TopicMethods.FlightTaskResourceGet)]
    public async Task FlightTaskResourceGetAsync(CloudMqData<FlightTaskResourceGetInput> data)
    {
        var flightId = data.Data?.FlightId;
        _logger.LogInformation("机场 {Gateway} 请求任务资源，flight_id:{FlightId}", data.Gateway, flightId);

        var task = await _taskRepository.GetByFlightIdAsync(flightId);
        var wayline = task?.WaylineEntityId is > 0
            ? await _taskRepository.GetWaylineAsync(task.WaylineEntityId.Value)
            : null;

        var file = await _kmzResolver.ResolveAsync(wayline);
        if (file == null)
        {
            await _gatewayPublish.PublishAsync(Topics.ThingProductRequestsReply,
                ToPublishOutputDataError(data, Enum.DjiReplyErrorEnum.UNKNOWN_ERROR));
            return;
        }

        await _gatewayPublish.PublishAsync(Topics.ThingProductRequestsReply,
            ToPublishOutputData(new FlightTaskResourceGetOutput { File = file }, data));
    }

    /// <summary>
    /// 查询目标机场的当前任务（多机场/蛙跳任务使用）。
    /// </summary>
    [MqttSubscribe(Topics.ThingProductRequests, TopicMethods.FlightTaskProgressGet)]
    public async Task FlightTaskProgressGetAsync(CloudMqData<FlightTaskProgressGetInput> data)
    {
        var targetSn = data.Data?.Sn.IsNullOrWhiteSpace() == false ? data.Data.Sn : data.Gateway;
        var task = await _taskRepository.GetLatestByDockSnAsync(targetSn);

        await _gatewayPublish.PublishAsync(Topics.ThingProductRequestsReply,
            ToPublishOutputData(new FlightTaskProgressGetOutput { FlightId = task?.FlightId }, data));
    }

    #endregion

    #region 私有实现

    /// <summary>
    /// 回复事件（<c>events_reply</c>）。
    /// </summary>
    /// <remarks>
    /// 协议要求云端对设备上报的事件给出应答；不回包虽不影响设备继续上报，
    /// 但在弱网下会加剧设备侧重发，且不利于抓包排障，因此统一回 <c>result: 0</c>。
    /// </remarks>
    private async Task ReplyEventAsync<T>(CloudMqData<T> from)
    {
        var reply = new CloudMqRequest<MqEventReply>(from.Method, new MqEventReply { Result = 0 }, from.Gateway, from.Tid, from.Bid);
        await _gatewayPublish.PublishAsync(Topics.ThingProductEventsReply, reply);
    }

    #endregion
}
