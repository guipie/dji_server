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
using Dji.Application.Option;
using Dji.Core.Enum.DjiEnum.Live;
using Dji.Schedule;
using Microsoft.Extensions.Options;

namespace Dji.Application.Job;

/// <summary>
/// 直播会话收口：清理「启动中」僵死会话与超时直播。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么必须有这个任务</b>：直播会话的终态本来由设备上报的 <c>live_status</c> 驱动，
/// 但有两种情况设备永远不会上报，会话会永久卡在进行中并占用并发名额：
/// <list type="number">
/// <item><b>启动无响应</b> —— 指令下发后机场离线 / 图传被占用 / 编码器异常，
/// 既不回包也不推流（此时 <c>StartTime</c> 为空，不算「已开播」）；</item>
/// <item><b>停播未上报</b> —— 推流中途断网，设备侧已断但云端不知情（典型「幽灵直播」）。</item>
/// </list>
/// </para>
/// <para>
/// <b>与超时收口的区别</b>：本任务处理的是<b>资源占用</b>问题（会话占名额），
/// 而不是业务正确性问题 —— 因此对「已开播超时」会先尝试下发 <c>live_stop_push</c> 真正停止推流，
/// 再收口状态；对「启动无响应」只收口本地状态（推流本就没起来，无需再发指令）。
/// </para>
/// </remarks>
[JobDetail("job_liveSession_cleanup", Description = "直播会话收口（启动僵死 + 超时停播）", GroupName = "dji", Concurrent = false)]
[Minutely(TriggerId = "trigger_liveSessionCleanup", Description = "收口僵死与超时的直播会话", MaxNumberOfRuns = 0, RunOnStart = false)]
public class LiveSessionCleanupJob : IJob
{
    /// <summary>「启动中」允许的最长停留时长（分钟），超过即判定为启动失败</summary>
    private const int StartingGraceMinutes = 5;

    private readonly IServiceScope _serviceScope;
    private readonly ILogger<LiveSessionCleanupJob> _logger;
    private readonly DjiLiveRepository _liveRepository;
    private readonly MqttGatewayPublish _publish;
    private readonly LiveOptions _liveOptions;

    public LiveSessionCleanupJob(IServiceScopeFactory scopeFactory)
    {
        _serviceScope = scopeFactory.CreateScope();
        _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<LiveSessionCleanupJob>>();
        _liveRepository = _serviceScope.ServiceProvider.GetRequiredService<DjiLiveRepository>();
        _publish = _serviceScope.ServiceProvider.GetRequiredService<MqttGatewayPublish>();
        _liveOptions = _serviceScope.ServiceProvider.GetRequiredService<IOptions<DjiOptions>>().Value.Live ?? new LiveOptions();
    }

    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        await CloseStaleStartingAsync(stoppingToken);
        await CloseTimeoutLiveAsync(stoppingToken);
    }

    /// <summary>收口长时间停留在「启动中」的会话</summary>
    private async Task CloseStaleStartingAsync(CancellationToken ct)
    {
        var deadline = DateTime.Now.AddMinutes(-StartingGraceMinutes);
        var stale = await _liveRepository.GetStaleStartingSessionsAsync(deadline);
        if (stale.Count == 0) return;

        foreach (var session in stale)
        {
            if (ct.IsCancellationRequested) return;

            // 启动阶段失败不补发停播指令：推流本就没建立，再发指令只会让机场回错误码
            await _liveRepository.UpdateSessionStatusAsync(session.Id, LiveStreamStatusEnum.Failed, -1,
                $"机场在 {StartingGraceMinutes} 分钟内未确认开播，已自动收口（请检查机场在线状态与图传占用情况）");

            _logger.LogWarning("直播会话启动超时已收口，机场:{DockSn}，通道:{VideoId}", session.DockSn, session.VideoId);
        }
    }

    /// <summary>收口已开播超过上限时长的会话，并真正停止推流</summary>
    private async Task CloseTimeoutLiveAsync(CancellationToken ct)
    {
        if (_liveOptions.MaxSessionMinutes <= 0) return;

        var deadline = DateTime.Now.AddMinutes(-_liveOptions.MaxSessionMinutes);
        var timeouts = await _liveRepository.GetTimeoutSessionsAsync(deadline);
        if (timeouts.Count == 0) return;

        foreach (var session in timeouts)
        {
            if (ct.IsCancellationRequested) return;

            // 先真正停掉推流，再收口状态；否则本地显示已停止、设备仍在推流
            try
            {
                var request = new CloudMqRequest<LiveStopPushInput>(TopicMethods.LiveStopPush,
                    new LiveStopPushInput { VideoId = session.VideoId }, session.DockSn);
                await _publish.PublishAsync(Topics.ThingProductServices, request);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "自动停播指令下发失败，机场:{DockSn}，通道:{VideoId}", session.DockSn, session.VideoId);
            }

            await _liveRepository.UpdateSessionStatusAsync(session.Id, LiveStreamStatusEnum.Stopped, 0,
                $"已达单次直播时长上限（{_liveOptions.MaxSessionMinutes} 分钟），已自动停播");

            _logger.LogInformation("直播已达时长上限并自动停播，机场:{DockSn}，通道:{VideoId}",
                session.DockSn, session.VideoId);
        }
    }
}
