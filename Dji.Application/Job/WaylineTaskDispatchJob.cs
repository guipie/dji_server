// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.CloudRepository;
using Dji.Application.Service.DjiWayline;
using Dji.Core.Enum.DjiEnum.Wayline;
using Furion.Schedule;

namespace Dji.Application.Job;

/// <summary>
/// 航线任务调度：驱动定时/条件任务的「执行」指令，并收口超时任务。
/// </summary>
/// <remarks>
/// <para>
/// 为什么需要独立的后台任务：上云协议要求
/// <list type="bullet">
/// <item>定时任务的 <c>flighttask_prepare</c> 最多提前 24 小时下发；</item>
/// <item><c>flighttask_execute</c> 必须在**执行前 2 分钟**才下发；</item>
/// <item>条件任务要等机场上报 <c>flighttask_ready</c> 之后才下发执行指令。</item>
/// </list>
/// 这些等待跨越了 HTTP 请求的生命周期，因此不能在请求线程里 <c>Task.Delay</c>（进程重启即丢失），
/// 必须由可恢复的周期任务扫描数据库驱动。
/// </para>
/// <para>
/// 任务通过 <c>ExecuteSentTime</c> 做幂等标记：本任务每分钟执行一次，若不去重会在一分钟内
/// 连续下发多条 <c>flighttask_execute</c>，机场会全部拒绝并污染任务状态。
/// </para>
/// </remarks>
[JobDetail("job_waylineTask_dispatch", Description = "航线任务调度（定时/条件任务执行 + 超时收口）", GroupName = "dji", Concurrent = false)]
[Minutely(TriggerId = "trigger_waylineTaskDispatch", Description = "驱动航线任务执行", MaxNumberOfRuns = 0, RunOnStart = false)]
public class WaylineTaskDispatchJob : IJob
{
    /// <summary>执行指令提前下发量（毫秒）：协议要求在执行前 2 分钟下发</summary>
    private const long ExecuteLeadMs = 2 * 60 * 1000;

    /// <summary>计划时间过后多久仍未开始执行即判定超时（分钟）</summary>
    private const int TimeoutGraceMinutes = 10;

    private readonly IServiceScope _serviceScope;
    private readonly ILogger<WaylineTaskDispatchJob> _logger;
    private readonly DjiWaylineTaskRepository _taskRepository;

    public WaylineTaskDispatchJob(IServiceScopeFactory scopeFactory)
    {
        _serviceScope = scopeFactory.CreateScope();
        _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<WaylineTaskDispatchJob>>();
        _taskRepository = _serviceScope.ServiceProvider.GetRequiredService<DjiWaylineTaskRepository>();
    }

    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        // 先收口超时任务，再驱动待执行任务：避免已失效的任务继续占用「同一机场仅一个任务」的名额
        await CloseTimeoutTasksAsync(stoppingToken);
        await DispatchPendingTasksAsync(stoppingToken);
    }

    /// <summary>收口长时间停留在「已下发/已就绪」的任务</summary>
    private async Task CloseTimeoutTasksAsync(CancellationToken ct)
    {
        var deadline = DateTime.Now.AddMinutes(-TimeoutGraceMinutes);
        await _taskRepository.CloseTimeoutTasksAsync(deadline, $"机场未在计划时间后 {TimeoutGraceMinutes} 分钟内开始执行，任务已超时");
    }

    /// <summary>驱动定时任务与条件任务的执行指令</summary>
    private async Task DispatchPendingTasksAsync(CancellationToken ct)
    {
        var pending = await _taskRepository.GetPendingExecuteAsync();
        if (pending.Count == 0) return;

        var waylineTaskService = _serviceScope.ServiceProvider.GetRequiredService<DjiWaylineTaskService>();
        var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        foreach (var task in pending)
        {
            if (ct.IsCancellationRequested) return;

            if (!ShouldExecute(task, now)) continue;

            // 先落幂等标记再发送：宁可漏发一次（由超时收口兜底），也不要因重试风暴导致机场连续拒绝
            await _taskRepository.MarkExecuteSentAsync(task.Id);
            await waylineTaskService.TryExecuteAsync(task);
        }
    }

    /// <summary>判断任务此刻是否应下发执行指令</summary>
    private static bool ShouldExecute(DjiWaylineTask task, long now)
    {
        return task.TaskType switch
        {
            // 条件任务：机场已通知满足准备条件
            TaskTypeCodeEnum.Conditional => task.Status == WaylineJobStatus.Ready,
            // 定时任务：到达「执行前 2 分钟」
            TaskTypeCodeEnum.Scheduled => task.ExecuteTime > 0 && now >= task.ExecuteTime - ExecuteLeadMs,
            _ => false,
        };
    }
}
