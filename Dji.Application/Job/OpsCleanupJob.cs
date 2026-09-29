// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.CloudRepository;
using Dji.Schedule;

namespace Dji.Application.Job;

/// <summary>
/// 运维数据收口与保留期清理（P3）。
/// </summary>
/// <remarks>
/// <para>
/// <b>这个任务解决两个不同性质的问题，因此分成了两组</b>：
/// </para>
/// <list type="number">
/// <item>
/// <b>僵死收口（正确性）</b> —— 指令 / 升级 / 日志的终态本来由设备上报驱动，
/// 但设备离线、链路中断时不会上报，记录会永久停在「进行中」。
/// 后果不只是显示不对：<c>DjiOtaService</c> 会因「有进行中的任务」拒绝新建单，
/// 用户从此再也无法升级该机场 —— 只能靠人工改库恢复。因此这类必须收口。
/// </item>
/// <item>
/// <b>保留期清理（容量）</b> —— HMS 已恢复记录、AirSense 告警属于流水，
/// 不清理会无限增长。这类只删「已经很旧且已终态」的数据，删错了不影响业务，
/// 因此保留期取得比较保守。
/// </item>
/// </list>
/// <para>
/// <b>为什么所有子任务都吞异常</b>：一条 SQL 失败（例如某张表在旧库上还没建出来）
/// 不应该让其余五项都不执行。每项各自 try/catch，失败打日志继续。
/// </para>
/// <para>
/// 频率取<b>每分钟</b>：僵死判定本身很快（走状态索引），而指令的「卡住」恰恰是用户最敏感的问题，
/// 等 5 分钟再收口会让用户在这 5 分钟里一直点不动按钮。
/// </para>
/// </remarks>
[JobDetail("job_ops_cleanup", Description = "运维数据收口与保留期清理（指令/升级/日志僵死 + HMS/AirSense 过期）", GroupName = "dji", Concurrent = false)]
[Minutely(TriggerId = "trigger_opsCleanup", Description = "收口僵死运维记录并清理过期数据", MaxNumberOfRuns = 0, RunOnStart = false)]
public class OpsCleanupJob : IJob
{
    /// <summary>指令停在「下发中」的容忍时长（分钟）：正常情况下 services_reply 十几秒内必到</summary>
    private const int CommandStaleMinutes = 5;

    /// <summary>
    /// 升级任务停在「下发中」的容忍时长（分钟）。
    /// </summary>
    /// <remarks>
    /// 比指令长得多：一条 <c>ota_create</c> 可能同时升级机场本体与飞行器，
    /// 设备侧要先做一致性检查再回包，实测慢的时候要一分钟以上。
    /// 注意这里只收口「连第一个进度都没等到」的单子，进入升级中的任务不按时间收口
    /// （下载固件可能几十分钟不刷新百分比）。
    /// </remarks>
    private const int OtaStaleMinutes = 30;

    /// <summary>日志上传停在「上传中」的容忍时长（分钟）：日志动辄几十 MB，弱网下要留足时间</summary>
    private const int LogStaleMinutes = 60;

    /// <summary>HMS 已恢复记录的保留天数</summary>
    private const int HmsRetentionDays = 30;

    /// <summary>AirSense 告警的保留天数</summary>
    private const int AirSenseRetentionDays = 90;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OpsCleanupJob> _logger;

    public OpsCleanupJob(IServiceScopeFactory scopeFactory, ILogger<OpsCleanupJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        // 作用域在本次执行内创建并释放：仓储持有 SqlSugar 客户端，
        // 若在构造函数里创建作用域而不释放，每分钟一次的执行会持续泄漏数据库连接
        using var scope = _scopeFactory.CreateScope();
        var provider = scope.ServiceProvider;

        var commandRepository = provider.GetRequiredService<DjiDockCommandRepository>();
        var otaRepository = provider.GetRequiredService<DjiOtaRepository>();
        var logRepository = provider.GetRequiredService<DjiLogRepository>();
        var hmsRepository = provider.GetRequiredService<DjiHmsRepository>();
        var airSenseRepository = provider.GetRequiredService<DjiAirSenseRepository>();

        await RunAsync("机场控制指令僵死收口",
            "CloseStaleCommands",
            () => commandRepository.CloseStaleAsync(DateTime.Now.AddMinutes(-CommandStaleMinutes)),
            stoppingToken);

        await RunAsync("固件升级僵死收口",
            "CloseStaleOta",
            () => otaRepository.CloseStaleAsync(DateTime.Now.AddMinutes(-OtaStaleMinutes)),
            stoppingToken);

        await RunAsync("日志上传僵死收口",
            "CloseStaleLogs",
            () => logRepository.CloseStaleAsync(DateTime.Now.AddMinutes(-LogStaleMinutes)),
            stoppingToken);

        await RunAsync("HMS 已恢复记录清理",
            "CleanHms",
            () => hmsRepository.CleanRecoveredAsync(DateTime.Now.AddDays(-HmsRetentionDays)),
            stoppingToken);

        await RunAsync("AirSense 历史清理",
            "CleanAirSense",
            () => airSenseRepository.CleanAsync(DateTime.Now.AddDays(-AirSenseRetentionDays)),
            stoppingToken);
    }

    #region 私有实现

    /// <summary>
    /// 执行一项清理并记录结果。
    /// </summary>
    /// <remarks>
    /// 只在<b>真的动了数据</b>时打 Information：这个任务每分钟跑一次，
    /// 若每次都打日志，日志文件会在一天内被「无事发生」的记录淹没，真正的收口动作反而被埋掉。
    /// </remarks>
    private async Task RunAsync(string name, string step, Func<Task<int>> action, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return;

        try
        {
            var count = await action();
            if (count > 0) _logger.LogInformation("[运维清理] {Name}：处理 {Count} 条", name, count);
        }
        catch (Exception ex)
        {
            // 单步失败不影响其余步骤；带 step 便于在日志里精确定位是哪一步
            _logger.LogError(ex, "[运维清理] {Name} 失败（step={Step}）", name, step);
        }
    }

    #endregion
}
