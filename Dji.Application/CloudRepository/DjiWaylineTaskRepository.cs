// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Cloud.Dto.Wayline;
using Dji.Core.Enum.DjiEnum.Wayline;
using System.Linq;

namespace Dji.Application.CloudRepository;

/// <summary>
/// 航线任务仓库：负责任务状态机推进与进度流水落库。
/// </summary>
/// <remarks>
/// <para>
/// 状态来源以机场上报的 <c>flighttask_progress.output.status</c> 为准，不做二次推断，
/// 避免云端「自作聪明」地把设备状态改成与实际不符的值。
/// </para>
/// <para>
/// <b>进度流水去重策略</b>：机场在执行期间约每秒上报一次，若逐条落库，单次 30 分钟任务将产生
/// 约 1800 条高度重复的记录。因此只在「状态 / 步骤 / 航点序号 / 进度值 / 媒体数」发生变化时追加，
/// 既保留完整的语义时间线，又把写入量压到实际状态变化的次数。任务最新态则每条报文都更新。
/// </para>
/// <para>
/// <b>为什么是 public</b>：同目录的其它仓库为 <c>internal</c>，但它们只被 <c>internal</c> 的 MQTT
/// 模块服务消费；本仓库需被公开的动态 API 服务 <c>DjiWaylineTaskService</c> 注入，
/// 若改为 <c>internal</c> 会触发 CS0051（可访问性不一致），故保持 <c>public</c>。
/// </para>
/// </remarks>
public class DjiWaylineTaskRepository(
    SqlSugarRepository<DjiWaylineTask> taskRes,
    SqlSugarRepository<DjiWaylineTaskProgress> progressRes,
    SqlSugarRepository<DjiWaylineEntity> waylineRes,
    DjiMediaRepository mediaRepository,
    ILogger<DjiWaylineTaskRepository> logger) : BaseRepository
{
    private readonly SqlSugarRepository<DjiWaylineTask> _taskRes = taskRes;
    private readonly SqlSugarRepository<DjiWaylineTaskProgress> _progressRes = progressRes;
    private readonly SqlSugarRepository<DjiWaylineEntity> _waylineRes = waylineRes;
    private readonly DjiMediaRepository _mediaRepository = mediaRepository;
    private readonly ILogger<DjiWaylineTaskRepository> _logger = logger;

    private readonly SemaphoreSlim _writeLock = new(1, 1);

    #region 查询

    /// <summary>按计划 ID 取任务</summary>
    public async Task<DjiWaylineTask> GetByFlightIdAsync(string flightId)
    {
        if (flightId.IsNullOrWhiteSpace()) return null;
        return await _taskRes.GetFirstAsync(m => m.FlightId == flightId);
    }

    /// <summary>取某机场最近一次任务（用于 <c>flighttask_progress_get</c>）</summary>
    public async Task<DjiWaylineTask> GetLatestByDockSnAsync(string dockSn)
    {
        if (dockSn.IsNullOrWhiteSpace()) return null;
        return await _taskRes.AsQueryable()
            .Where(m => m.DockSn == dockSn)
            .OrderBy(m => m.CreateTime, OrderByType.Desc)
            .FirstAsync();
    }

    /// <summary>
    /// 该机场是否存在未结束任务。
    /// </summary>
    /// <remarks>
    /// 官方明确：设备正在执行航线任务时再收到执行指令，会拒绝执行并回错误码。
    /// 与其让机场报错，不如在下发前就拦住，给出更明确的中文提示。
    /// </remarks>
    public async Task<DjiWaylineTask> GetActiveTaskByDockSnAsync(string dockSn)
    {
        if (dockSn.IsNullOrWhiteSpace()) return null;

        // 未结束态 = 非终态，即 sent / ready / in_progress / paused
        string[] active = [WaylineJobStatus.Sent, WaylineJobStatus.Ready, WaylineJobStatus.InProgress, WaylineJobStatus.Paused];
        return await _taskRes.AsQueryable()
            .Where(m => m.DockSn == dockSn && active.Contains(m.Status))
            .OrderBy(m => m.CreateTime, OrderByType.Desc)
            .FirstAsync();
    }

    /// <summary>任务进度流水（按时间正序，用于任务详情时间线）</summary>
    public async Task<List<DjiWaylineTaskProgress>> GetProgressAsync(long taskId, int take = 200)
    {
        return await _progressRes.AsQueryable()
            .Where(m => m.TaskId == taskId)
            .OrderBy(m => m.Id, OrderByType.Asc)
            .Take(take)
            .ToListAsync();
    }

    #endregion

    #region 写入

    /// <summary>新建任务（<c>flighttask_prepare</c> 下发成功后落库）</summary>
    public async Task<DjiWaylineTask> InsertAsync(DjiWaylineTask task)
    {
        await _taskRes.InsertAsync(task);

        // 媒体回调与任务落库走两条链路，顺序无保证：回调可能先到，那批媒体的 TaskId / DroneSn 会是空的。
        // 任务一建好就回填，否则「按任务看成果」会缺掉最早上传的那几张（尤其是大文件先传完的情况）。
        if (!task.FlightId.IsNullOrWhiteSpace())
        {
            try
            {
                var rows = await _mediaRepository.BackfillTaskAsync(task.FlightId, task.Id, task.DroneSn);
                if (rows > 0)
                    _logger.LogInformation("回填 {Rows} 条媒体的任务归属，flight_id:{FlightId}", rows, task.FlightId);
            }
            catch (Exception ex)
            {
                // 回填失败不影响任务下发主流程：媒体侧仍可按 flight_id 字符串查到
                _logger.LogWarning(ex, "回填媒体任务归属失败，flight_id:{FlightId}", task.FlightId);
            }
        }

        return task;
    }

    /// <summary>
    /// 依据进度上报推进任务状态并追加流水。
    /// </summary>
    /// <returns>更新后的任务；找不到对应任务时返回 null（不凭空创建）</returns>
    public async Task<DjiWaylineTask> ApplyProgressAsync(string flightId, FlightTaskProgressOutput output, long reportTimestamp)
    {
        if (flightId.IsNullOrWhiteSpace() || output == null) return null;

        // 机场并发上报时保证「读-改-写」串行，避免状态互相覆盖
        await _writeLock.WaitAsync();
        try
        {
            var task = await GetByFlightIdAsync(flightId);
            if (task == null)
            {
                _logger.LogWarning("收到未登记任务的进度上报，已忽略，flight_id:{FlightId}", flightId);
                return null;
            }

            var ext = output.Ext;
            var stepCode = output.Progress?.CurrentStep ?? task.StepCode;
            var percent = output.Progress?.Percent ?? task.ProgressPercent;
            var waypointIndex = ext?.CurrentWaypointIndex ?? task.CurrentWaypointIndex;
            var missionState = ext?.WaylineMissionState ?? task.MissionState;
            var mediaCount = ext?.MediaCount ?? task.MediaCount;
            var breakReason = ext?.BreakPoint?.BreakReason;
            var status = output.Status.IsNullOrWhiteSpace() ? task.Status : output.Status.Trim().ToLowerInvariant();

            var now = DateTime.Now;
            var changed = task.Status != status
                          || task.StepCode != stepCode
                          || task.CurrentWaypointIndex != waypointIndex
                          || task.ProgressPercent != percent
                          || task.MediaCount != mediaCount;

            task.Status = status;
            task.StepCode = stepCode;
            task.StepText = FlightTaskStep.Describe(stepCode);
            task.ProgressPercent = percent;
            task.CurrentWaypointIndex = waypointIndex;
            task.MissionState = missionState;
            task.MediaCount = mediaCount;

            if (ext?.BreakPoint != null) task.BreakPointJson = ext.BreakPoint.ToJson();

            if (breakReason.HasValue && !FlightTaskBreakReason.IsNormal(breakReason))
            {
                task.ErrorCode = breakReason;
                task.ErrorMessage = FlightTaskBreakReason.Describe(breakReason);
            }

            // 首次进入执行态时记录实际开始时间
            if (status == WaylineJobStatus.InProgress && task.BeginTime == null) task.BeginTime = now;

            // 进入终态时收尾（避免重复上报把结束时间不断后移）
            if (WaylineJobStatus.IsTerminal(status) && task.EndTime == null)
            {
                task.EndTime = now;
                if (task.BeginTime == null) task.BeginTime = now;
            }

            await _taskRes.AsUpdateable(task)
                .UpdateColumns(m => new
                {
                    m.Status, m.StepCode, m.StepText, m.ProgressPercent, m.CurrentWaypointIndex,
                    m.MissionState, m.MediaCount, m.BreakPointJson, m.ErrorCode, m.ErrorMessage,
                    m.BeginTime, m.EndTime,
                })
                .ExecuteCommandAsync();

            // 只在语义发生变化时追加流水，避免每秒一条的重复记录
            if (changed)
            {
                await _progressRes.InsertAsync(new DjiWaylineTaskProgress
                {
                    TaskId = task.Id,
                    FlightId = task.FlightId,
                    DockSn = task.DockSn,
                    Status = status,
                    StepCode = stepCode,
                    StepText = task.StepText,
                    ProgressPercent = percent,
                    CurrentWaypointIndex = waypointIndex,
                    MissionState = missionState,
                    MediaCount = mediaCount,
                    BreakPointJson = task.BreakPointJson,
                    ReportTimestamp = reportTimestamp,
                });
            }

            return task;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// 标记任务已满足准备条件（收到 <c>flighttask_ready</c>）。
    /// </summary>
    /// <remarks>条件任务在满足 <c>ready_conditions</c> 后由机场主动通知；只推进「已下发」态，避免覆盖执行中的状态。</remarks>
    public async Task<bool> MarkReadyAsync(string flightId)
    {
        if (flightId.IsNullOrWhiteSpace()) return false;

        var task = await GetByFlightIdAsync(flightId);
        if (task == null) return false;
        if (task.Status != WaylineJobStatus.Sent) return false;

        task.Status = WaylineJobStatus.Ready;
        task.ReadyTime = DateTime.Now;

        return await _taskRes.AsUpdateable(task)
            .UpdateColumns(m => new { m.Status, m.ReadyTime })
            .Where(m => m.FlightId == flightId)
            .ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// 下发指令被机场拒绝时记录失败原因。
    /// </summary>
    /// <remarks>
    /// <c>services_reply.result</c> 非 0 表示机场拒绝执行；此时任务不可能真正飞起来，
    /// 必须落到 failed 并保留错误码，否则前端会一直显示「已下发」而实际什么都没发生。
    /// </remarks>
    public async Task MarkRejectedAsync(string flightId, int errorCode, string stage)
    {
        var task = await GetByFlightIdAsync(flightId);
        if (task == null) return;

        task.Status = WaylineJobStatus.Rejected;
        task.ErrorCode = errorCode;
        task.ErrorMessage = $"{stage} 被机场拒绝（result={errorCode}）";
        task.EndTime ??= DateTime.Now;

        await _taskRes.AsUpdateable(task)
            .UpdateColumns(m => new { m.Status, m.ErrorCode, m.ErrorMessage, m.EndTime })
            .Where(m => m.FlightId == flightId)
            .ExecuteCommandAsync();
    }

    /// <summary>回填执行飞行器（下发时取自机场子设备，非必填）</summary>
    public async Task FillDroneSnAsync(long taskId, string droneSn)
    {
        if (taskId <= 0 || droneSn.IsNullOrWhiteSpace()) return;
        await _taskRes.AsUpdateable(new DjiWaylineTask { Id = taskId, DroneSn = droneSn })
            .UpdateColumns(m => m.DroneSn)
            .Where(m => m.Id == taskId)
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 取出需要后台触发「执行」的定时/条件任务。
    /// </summary>
    /// <remarks>
    /// 过滤条件包含 <c>ExecuteSentTime == null</c>，这是幂等的关键：定时任务每分钟执行一次，
    /// 若不排除已发过执行指令的任务，会在一分钟内连续下发多次 <c>flighttask_execute</c>。
    /// </remarks>
    public async Task<List<DjiWaylineTask>> GetPendingExecuteAsync()
    {
        var taskTypes = new[] { TaskTypeCodeEnum.Scheduled, TaskTypeCodeEnum.Conditional };
        string[] statuses = [WaylineJobStatus.Sent, WaylineJobStatus.Ready];

        return await _taskRes.AsQueryable()
            .Where(m => taskTypes.Contains(m.TaskType) && statuses.Contains(m.Status) && m.ExecuteSentTime == null)
            .OrderBy(m => m.ExecuteTime, OrderByType.Asc)
            .ToListAsync();
    }

    /// <summary>记录执行指令已发送（幂等标记）</summary>
    public async Task MarkExecuteSentAsync(long taskId)
    {
        await _taskRes.AsUpdateable(new DjiWaylineTask { Id = taskId, ExecuteSentTime = DateTime.Now })
            .UpdateColumns(m => m.ExecuteSentTime)
            .Where(m => m.Id == taskId)
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 记录执行指令下发后无应答（机场离线 / 弱网 / 订阅未就绪）。
    /// </summary>
    /// <remarks>
    /// 此处刻意<b>不</b>直接把任务置为 <c>failed</c>：无应答只说明这一次请求没拿到回包，
    /// 机场可能只是短暂掉线，随后仍会自行执行并上报 <c>flighttask_progress</c>。
    /// 若武断判死，反而会把一个正在飞行的任务显示成失败。因此只留痕，最终态交给
    /// <see cref="CloseTimeoutTasksAsync"/> 依据计划时间统一收口。
    /// </remarks>
    public async Task MarkExecuteUnansweredAsync(string flightId)
    {
        if (flightId.IsNullOrWhiteSpace()) return;

        var task = await GetByFlightIdAsync(flightId);
        if (task == null) return;

        task.ErrorMessage = "执行指令下发后机场未应答，请检查机场在线状态";

        await _taskRes.AsUpdateable(task)
            .UpdateColumns(m => m.ErrorMessage)
            .Where(m => m.FlightId == flightId)
            .ExecuteCommandAsync();

        _logger.LogWarning("执行指令无应答，flight_id:{FlightId}，等待超时收口", flightId);
    }

    /// <summary>
    /// 把停留在「已下发/已就绪」且早已过计划时间的任务标记为超时。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 机场可能因为离线、固件异常等原因既不上报进度也不回包，若不收口，前端会一直显示
    /// 「已下发」形成僵尸任务，并长期占用「同一机场仅一个任务」的名额。
    /// </para>
    /// <para>
    /// <b>条件任务会被豁免</b>：条件任务的 <c>ExecuteTime</c> 为 0（表示「无固定执行时间，
    /// 等满足准备条件后再飞」），本方法的 <c>ExecuteTime &gt; 0</c> 条件会将其排除在外。
    /// 这是有意为之——条件任务可能等上数小时甚至数天。若需要中止，请由用户在任务中心手动「取消」。
    /// </para>
    /// </remarks>
    public async Task<int> CloseTimeoutTasksAsync(DateTime deadline, string reason)
    {
        string[] statuses = [WaylineJobStatus.Sent, WaylineJobStatus.Ready];

        // 先把阈值换算成毫秒时间戳再进入表达式树，避免 SqlSugar 翻译不可识别的方法调用
        var deadlineMs = DateTimeUtil.GetUnixTimeStamp(deadline);

        var stale = await _taskRes.AsQueryable()
            .Where(m => statuses.Contains(m.Status) && m.ExecuteTime > 0 && m.ExecuteTime < deadlineMs)
            .ToListAsync();
        if (stale.Count == 0) return 0;

        foreach (var task in stale)
        {
            task.Status = WaylineJobStatus.Timeout;
            task.ErrorMessage = reason;
            task.EndTime ??= DateTime.Now;
        }

        await _taskRes.AsUpdateable(stale)
            .UpdateColumns(m => new { m.Status, m.ErrorMessage, m.EndTime })
            .WhereColumns(m => m.Id)
            .ExecuteCommandAsync();

        _logger.LogWarning("已将 {Count} 个超时任务收口为 timeout", stale.Count);
        return stale.Count;
    }

    /// <summary>取航线实体（下发时用于生成 KMZ 地址与签名）</summary>
    public async Task<DjiWaylineEntity> GetWaylineAsync(long waylineEntityId)
    {
        return await _waylineRes.GetFirstAsync(m => m.Id == waylineEntityId);
    }

    #endregion
}
