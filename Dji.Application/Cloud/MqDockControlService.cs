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
using Dji.Application.Cloud.Dto.Dock;
using Dji.Application.Cloud.Entity;
using Dji.Application.CloudRepository;
using Dji.Core.Enum.DjiEnum.Dock;

namespace Dji.Application.Cloud;

/// <summary>
/// 机场控制指令的执行结果上行处理（MQTT）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么一个方法订阅二十多个指令</b>：机场控制类指令的 <c>events</c> 进度报文
/// <b>结构完全一致</b>（<c>result</c> + <c>output.status</c> + <c>output.progress</c>），
/// 差异只在方法名。若为每条指令写一个处理器，会得到二十多个只做转发的样板方法，
/// 日后协议加一条指令就要跟着加一个方法。因此这里用特性叠加把同一份逻辑复用到底 ——
/// 需要区分指令语义时，直接看报文的 <c>method</c> 字段即可。
/// </para>
/// <para>
/// <b>唯一例外是 RTK 标定</b>：它的回包是按设备逐个给结果的（<c>output.ext.devices[]</c>），
/// 结构不同，因此单独处理。
/// </para>
/// <para>
/// <b>这些报文都带 <c>need_reply</c>，必须应答</b>，否则机场会持续重发同一条进度。
/// </para>
/// </remarks>
internal class MqDockControlService(
    ILogger<MqDockControlService> logger,
    MqttGatewayPublish gatewayPublish,
    DjiDockCommandRepository commandRepository) : BaseModuleService
{
    private readonly ILogger<MqDockControlService> _logger = logger;
    private readonly MqttGatewayPublish _gatewayPublish = gatewayPublish;
    private readonly DjiDockCommandRepository _commandRepository = commandRepository;

    /// <summary>
    /// 机场控制指令的执行进度 / 结果。
    /// </summary>
    /// <remarks>
    /// 覆盖舱盖、充电、飞行器开关机、机场重启、格式化、补光灯、电池保养、空调、声光报警、
    /// 电池运行模式、增强图传、eSIM 相关、远程调试等全部「下发后异步回报」的指令。
    /// </remarks>
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.CoverOpen)]
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.CoverClose)]
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.CoverForceClose)]
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.ChargeOpen)]
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.ChargeClose)]
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.DroneOpen)]
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.DroneClose)]
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.DeviceReboot)]
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.DeviceFormat)]
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.DroneFormat)]
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.SupplementLightOpen)]
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.SupplementLightClose)]
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.BatteryMaintenanceSwitch)]
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.AirConditionerModeSwitch)]
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.AlarmStateSwitch)]
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.BatteryStoreModeSwitch)]
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.SdrWorkmodeSwitch)]
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.EsimActivate)]
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.EsimOperatorSwitch)]
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.SimSlotSwitch)]
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.DebugModeOpen)]
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.DebugModeClose)]
    public async Task ProgressAsync(CloudMqData<DockCommandProgressData> data)
    {
        var output = data.Data?.Output;
        var percent = output?.Progress?.Percent;
        var step = output?.Progress?.StepKey ?? output?.Progress?.CurrentStep;

        _logger.LogInformation("机场 {Gateway} 指令 {Method} 状态：{Status}（{Percent}%，步骤 {Step}）",
            data.Gateway, data.Method, output?.Status ?? "未知", percent?.ToString() ?? "-", step ?? "-");

        await _commandRepository.UpdateProgressAsync(
            data.Bid, data.Gateway, data.Method, output, data.TimeStamp);

        await _gatewayPublish.PublishAsync(Topics.ThingProductEventsReply, ToEventReply(data));
    }

    /// <summary>
    /// RTK 一键标定结果。
    /// </summary>
    /// <remarks>
    /// 外层 <c>result</c> 恒为 0，真正的判据是 <c>output.status</c>：
    /// 所有设备都成功才是 <c>ok</c>，任一设备失败就是 <c>failed</c>。
    /// 单台设备的错误码在 <c>output.ext.devices[].result</c> 里，
    /// 因此失败时把所有非 0 结果拼进错误说明，否则运维只能看到「标定失败」而不知哪台、为什么。
    /// </remarks>
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.RtkCalibration)]
    public async Task RtkCalibrationAsync(CloudMqData<RtkCalibrationResultData> data)
    {
        var output = data.Data?.Output;
        var devices = output?.Ext?.Devices ?? [];

        var failed = devices.Where(m => (m.Result ?? 0) != 0).ToList();
        string error = null;
        if (failed.Count > 0)
        {
            error = string.Join("；", failed.Select(m => $"{m.Sn}（{ModuleName(m.Module)}）失败，错误码 {m.Result}"));
        }

        _logger.LogInformation("机场 {Gateway} RTK 标定结果：{Status}，设备数 {Count}，失败 {Failed}",
            data.Gateway, output?.Status ?? "未知", devices.Count, failed.Count);

        await _commandRepository.UpdateProgressAsync(
            data.Bid, data.Gateway, data.Method,
            new DockCommandOutput
            {
                Status = output?.Status,
                Progress = new DockCommandProgress { Percent = output?.Progress?.Percent },
            },
            data.TimeStamp);

        if (!error.IsNullOrWhiteSpace())
        {
            _logger.LogWarning("RTK 标定存在失败设备：{Error}", error);
        }

        await _gatewayPublish.PublishAsync(Topics.ThingProductEventsReply, ToEventReply(data));
    }

    /// <summary>RTK 标定模块名（协议用字符串表示）</summary>
    private static string ModuleName(string module)
        => module switch
        {
            "3" => "机场",
            "6" => "中继",
            _ => module
        };
}
