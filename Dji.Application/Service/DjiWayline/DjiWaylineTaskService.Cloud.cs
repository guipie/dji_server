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
using Dji.Application.Service.DjiWayline.Dto;
using Dji.Core.Enum.DjiEnum.Wayline;

namespace Dji.Application.Service.DjiWayline;

/// <summary>
/// 航线任务服务（MQTT 下行部分）。
/// </summary>
/// <remarks>
/// <para>
/// 所有下行指令统一走 <c>thing/product/{sn}/services</c> 主题并要求设备应答，
/// 因此这里只用 <see cref="MqttGatewayPublish.PublishWithReplyAsync{T,R}"/>，
/// 不用「发完即忘」的 PublishAsync —— 否则机场拒绝执行时平台会一直显示「已下发」。
/// </para>
/// <para>
/// 与上行处理器（<c>MqWaylineService</c>）分层：本类只发指令、推进本地任务状态，
/// 不解析进度报文；状态的真值来源始终是机场上报的 <c>flighttask_progress</c>。
/// </para>
/// </remarks>
public partial class DjiWaylineTaskService
{
    /// <summary>下发任务指令的等待时长（机场需下载 KMZ 并做飞前检查，给足时间）</summary>
    private const int CommandReplyTimeoutSeconds = 30;

    /// <summary>返航高度下界（协议约束）</summary>
    private const int RthAltitudeMin = 20;

    /// <summary>返航高度上界（协议约束）</summary>
    private const int RthAltitudeMax = 1500;

    /// <summary>下发航线任务（<c>flighttask_prepare</c>）</summary>
    /// <returns>0 表示机场接受；非 0 为错误码；-1 表示超时/异常</returns>
    private async Task<int> SendPrepareAsync(DjiWaylineTask task, FlightTaskFile file, CreateWaylineTaskInput input)
    {
        var payload = new FlightTaskPrepareInput
        {
            FlightId = task.FlightId,
            // 条件任务没有固定执行时间，留空而不是发 0，避免机场把 0 当作 1970 年
            ExecuteTime = task.ExecuteTime > 0 ? task.ExecuteTime : null,
            TaskType = (int)task.TaskType,
            File = file,
            ReadyConditions = input.ReadyConditions,
            ExecutableConditions = input.ExecutableConditions,
            BreakPoint = null,
            RthAltitude = task.RthAltitude,
            // 机场当前只支持「预设高度」模式，固定 1
            RthMode = task.RthAltitude is > 0 ? 1 : null,
            OutOfControlAction = task.OutOfControlAction ?? 0,
            ExitWaylineWhenRcLost = input.ExitWaylineWhenRcLost ?? 0,
            // 官方推荐默认使用高精度 RTK 任务
            WaylinePrecisionType = input.WaylinePrecisionType ?? 1,
            SimulateMission = input.SimulateMission,
            FlightSafetyAdvanceCheck = input.FlightSafetyAdvanceCheck,
        };

        var request = new CloudMqRequest<FlightTaskPrepareInput>(TopicMethods.FlightTaskPrepare, payload, task.DockSn);
        return await PublishWithReplyAsync(request, "下发任务");
    }

    /// <summary>执行航线任务（<c>flighttask_execute</c>）</summary>
    private async Task<int> SendExecuteAsync(string flightId, string dockSn)
    {
        var request = new CloudMqRequest<FlightTaskExecuteInput>(
            TopicMethods.FlightTaskExecute,
            new FlightTaskExecuteInput { FlightId = flightId },
            dockSn);
        return await PublishWithReplyAsync(request, "执行任务");
    }

    /// <summary>
    /// 由后台调度任务触发执行（定时任务 / 条件任务就绪）。
    /// </summary>
    /// <remarks>
    /// 与前台 <see cref="Execute"/> 的区别：这里不抛异常、不做状态前置校验
    /// （调用方已按数据库状态筛选过），失败时直接把任务收口为 rejected/failed，
    /// 保证后台线程不会因一次失败而中断整轮调度。
    /// </remarks>
    internal async Task TryExecuteAsync(DjiWaylineTask task)
    {
        if (WaylineJobStatus.IsTerminal(task.Status)) return;

        var result = await SendExecuteAsync(task.FlightId, task.DockSn);
        if (result == 0) return;

        if (result == -1)
        {
            // 无应答：可能是机场离线或弱网，先记录原因，由超时收口统一判定终态
            await _taskRepository.MarkExecuteUnansweredAsync(task.FlightId);
            return;
        }

        await _taskRepository.MarkRejectedAsync(task.FlightId, result, "执行任务(flighttask_execute)");
    }

    /// <summary>下发无载荷的控制指令（暂停 / 恢复）</summary>
    private async Task<bool> SendSimpleCommandAsync(string dockSn, string flightId, string method, string actionName)
    {
        // 协议中 pause / recovery 的 data 为空对象，这里仍带上 flight_id，
        // 兼容按 flight_id 区分任务的固件版本（多收到一个未知字段设备会忽略）。
        var request = new CloudMqRequest<object>(method, new { FlightId = flightId }, dockSn);
        return await PublishWithReplyAsync(request, actionName) == 0;
    }

    /// <summary>
    /// 发送需要回包的指令并解析 <c>services_reply.result</c>。
    /// </summary>
    /// <remarks>
    /// 设备未在超时内回包时 <see cref="MqttGatewayPublish.PublishWithReplyAsync{T,R}"/> 会抛异常，
    /// 这里统一转为 -1，让调用方按「失败」处理而不是把异常抛给前端（前端只需看到可读的中文提示）。
    /// </remarks>
    private async Task<int> PublishWithReplyAsync<T>(CloudMqRequest<T> request, string actionName)
    {
        try
        {
            var reply = await _publish.PublishWithReplyAsync<T, object>(Topics.ThingProductServices, request, CommandReplyTimeoutSeconds);
            var result = reply?.Data?.Result ?? -1;

            if (result != 0)
                _logger.LogWarning("机场拒绝航线任务指令：{Action}，method:{Method}，gateway:{Gateway}，result:{Result}",
                    actionName, request.Method, request.Gateway, result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "航线任务指令无应答：{Action}，method:{Method}，gateway:{Gateway}",
                actionName, request.Method, request.Gateway);
            return -1;
        }
    }
}
