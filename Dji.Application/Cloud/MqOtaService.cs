// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Cloud.Core;
using Dji.Application.Cloud.Dto.Ops;
using Dji.Application.Cloud.Entity;
using Dji.Application.CloudRepository;
using Dji.Core.Enum.DjiEnum.Ops;

namespace Dji.Application.Cloud;

/// <summary>
/// 固件升级进度上行处理（MQTT）。
/// </summary>
/// <remarks>
/// <para>
/// <b>只做两件事</b>：把进度写到对应任务行、回 <c>events_reply</c>。
/// 所有业务判断（能不能升级、有哪些前置条件）都在 <c>DjiOtaService</c> 下发前完成 ——
/// 进度处理器处在高频回调路径上，不应再去查库做业务决策。
/// </para>
/// <para>
/// <b>必须应答</b>：<c>ota_progress</c> 带 <c>need_reply</c>，不回包设备会持续重发同一条进度。
/// </para>
/// <para>
/// <b>为什么日志级别用 Information 且打印中文步骤</b>：固件升级是运维最关注的长时间过程，
/// 日志里能看到「下载固件 40%」比看到「in_progress/download_firmware」有用得多。
/// </para>
/// </remarks>
internal class MqOtaService(
    ILogger<MqOtaService> logger,
    MqttGatewayPublish gatewayPublish,
    DjiOtaRepository otaRepository) : BaseModuleService
{
    private readonly ILogger<MqOtaService> _logger = logger;
    private readonly MqttGatewayPublish _gatewayPublish = gatewayPublish;
    private readonly DjiOtaRepository _otaRepository = otaRepository;

    /// <summary>固件升级进度 / 结果</summary>
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.OtaProgress)]
    public async Task OtaProgressAsync(CloudMqData<OtaProgressData> data)
    {
        var output = data.Data?.Output;

        _logger.LogInformation("机场 {Gateway} 固件升级进度：{Status}（{Step} {Percent}%）",
            data.Gateway, output?.Status ?? "未知",
            output?.Progress?.CurrentStep.IsNullOrWhiteSpace() == false
                ? OtaStepResolver.Label(output.Progress.CurrentStep)
                : "步骤未知",
            output?.Progress?.Percent?.ToString() ?? "-");

        await _otaRepository.UpdateProgressAsync(data.Bid, output, data.TimeStamp);

        await _gatewayPublish.PublishAsync(Topics.ThingProductEventsReply, ToEventReply(data));
    }
}
