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

namespace Dji.Application.Cloud;

/// <summary>
/// 远程日志上传进度上行处理（MQTT）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么要逐文件循环而不是整批处理</b>：一次 <c>fileupload_progress</c> 的
/// <c>output.ext.files[]</c> 可能同时包含机场与飞行器的多个文件，各自有独立进度与状态。
/// 落库是按文件行（唯一索引 = 机场 + 设备 + 模块 + boot_index），因此必须逐个匹配、逐个更新。
/// </para>
/// <para>
/// <b>不回 <c>events_reply</c></b>：官方示例里本报文 <c>need_reply = 0</c>，
/// 与媒体回传的 <c>file_upload_callback</c>（<c>need_reply = 1</c>）不同。
/// 多发一条无用的应答只会干扰设备侧的消息计数。
/// </para>
/// <para>
/// <b>单条记录更新失败不影响其它文件</b>：仓储内部已把「匹配不到」降级为告警日志并返回 null，
/// 因此这里不会因为一个找不到归属的文件而中断整批进度处理。
/// </para>
/// </remarks>
internal class MqLogService(
    ILogger<MqLogService> logger,
    DjiLogRepository logRepository) : BaseModuleService
{
    private readonly ILogger<MqLogService> _logger = logger;
    private readonly DjiLogRepository _logRepository = logRepository;

    /// <summary>日志文件上传进度</summary>
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.FileuploadProgress)]
    public async Task FileuploadProgressAsync(CloudMqData<FileUploadProgressData> data)
    {
        var files = data.Data?.Output?.Ext?.Files;
        if (files is not { Count: > 0 }) return;

        foreach (var file in files)
        {
            var row = await _logRepository.UpdateProgressAsync(data.Gateway, file, data.TimeStamp);
            if (row == null) continue;

            _logger.LogInformation("日志上传进度：{File}（{Device}）{Progress}%，速率 {Rate} B/s，状态 {Status}",
                row.FileName ?? row.ObjectKey ?? $"{row.Module}-{row.BootIndex}",
                row.DeviceSn, file.Progress?.Progress?.ToString() ?? "-",
                file.Progress?.UploadRate?.ToString() ?? "-", row.Status);
        }
    }
}
