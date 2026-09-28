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
using Dji.Application.Service.DjiWayline.Dto;
using Dji.Core.Enum.DjiEnum.Wayline;
using System.Linq;

namespace Dji.Application.Service.DjiWayline;

/// <summary>
/// 航线任务（任务中心）服务。
/// </summary>
/// <remarks>
/// <para>
/// 职责：任务的下发编排、状态查询与控制（暂停/恢复/取消/结束），
/// 是打通「平台 → 机场 → 航线执行 → 实时进度」闭环的唯一业务入口。
/// </para>
/// <para>
/// 协议要点（决定了本类的实现取舍）：
/// <list type="number">
/// <item><c>flighttask_create</c> 已被官方废弃，一律使用 <c>flighttask_prepare</c> + <c>flighttask_execute</c>；</item>
/// <item>立即任务的 <c>execute_time</c> 与机场收到指令的时间差不得超过 30 秒，因此该值由**服务端**取当前时间，
/// 不接受前端传入；</item>
/// <item>定时任务最多提前 24 小时下发 prepare，执行前 2 分钟才下发 execute，因此定时任务由后台任务
/// （<c>WaylineTaskDispatchJob</c>）驱动，而不是在请求线程里等待；</item>
/// <item>同一机场同时只允许一个未结束任务，下发前先做校验，避免机场侧直接报错。</item>
/// </list>
/// </para>
/// </remarks>
[ApiDescriptionSettings(ApplicationConst.GroupName, Order = 100)]
public partial class DjiWaylineTaskService : IDynamicApiController, ITransient
{
    /// <summary>立即任务允许的 execute_time 提前量（毫秒）：给网络留一点余量，同时远小于机场 30 秒容差</summary>
    private const long ImmediateTaskLeadMs = 2000;

    /// <summary>定时任务允许提前下发 prepare 的最大跨度（小时）</summary>
    private const int PrepareLeadHours = 24;

    private readonly SqlSugarRepository<DjiWaylineTask> _taskRep;

    // 注意：命名空间 Dji.Application.Service.DjiDevice 与实体类型 Dji.Core.Entity.DjiDevice 同名，
    // 在本命名空间下裸写 DjiDevice 会被解析成命名空间，故此处必须写全限定名。
    private readonly SqlSugarRepository<Dji.Core.Entity.DjiDevice> _deviceRep;

    private readonly SqlSugarRepository<DjiWaylineEntity> _waylineRep;
    private readonly DjiWaylineTaskRepository _taskRepository;
    private readonly WaylineKmzResolver _kmzResolver;
    private readonly MqttGatewayPublish _publish;
    private readonly UserManager _userManager;
    private readonly ILogger<DjiWaylineTaskService> _logger;

    public DjiWaylineTaskService(
        SqlSugarRepository<DjiWaylineTask> taskRep,
        SqlSugarRepository<Dji.Core.Entity.DjiDevice> deviceRep,
        SqlSugarRepository<DjiWaylineEntity> waylineRep,
        DjiWaylineTaskRepository taskRepository,
        WaylineKmzResolver kmzResolver,
        MqttGatewayPublish publish,
        UserManager userManager,
        ILogger<DjiWaylineTaskService> logger)
    {
        _taskRep = taskRep;
        _deviceRep = deviceRep;
        _waylineRep = waylineRep;
        _taskRepository = taskRepository;
        _kmzResolver = kmzResolver;
        _publish = publish;
        _userManager = userManager;
        _logger = logger;
    }

    #region 查询

    /// <summary>分页查询航线任务</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Page")]
    public async Task<SqlSugarPagedList<DjiWaylineTaskOutput>> Page(DjiWaylineTaskSearchInput input)
    {
        var query = _taskRep.AsQueryable()
            .WhereIF(!input.SearchKey.IsNullOrWhiteSpace(), m => m.JobName.Contains(input.SearchKey.Trim())
                                                                 || m.FlightId.Contains(input.SearchKey.Trim())
                                                                 || m.WaylineName.Contains(input.SearchKey.Trim()))
            .WhereIF(!input.WorkspaceId.IsNullOrWhiteSpace(), m => m.WorkspaceId == input.WorkspaceId)
            .WhereIF(!input.DockSn.IsNullOrWhiteSpace(), m => m.DockSn == input.DockSn)
            .WhereIF(!input.Status.IsNullOrWhiteSpace(), m => m.Status == input.Status)
            .WhereIF(input.TaskType.HasValue, m => m.TaskType == input.TaskType)
            .WhereIF(input.OnlyActive == true, m => m.Status == WaylineJobStatus.Sent
                                                    || m.Status == WaylineJobStatus.Ready
                                                    || m.Status == WaylineJobStatus.InProgress
                                                    || m.Status == WaylineJobStatus.Paused)
            .OrderBy(m => m.CreateTime, OrderByType.Desc);

        var paged = await query.Select(m => new DjiWaylineTaskOutput
        {
            Id = m.Id,
            FlightId = m.FlightId,
            JobName = m.JobName,
            TaskType = m.TaskType,
            ExecuteTime = m.ExecuteTime,
            WorkspaceId = m.WorkspaceId,
            DockSn = m.DockSn,
            DroneSn = m.DroneSn,
            WaylineName = m.WaylineName,
            WaylineId = m.WaylineId,
            Status = m.Status,
            StepCode = m.StepCode,
            StepText = m.StepText,
            ProgressPercent = m.ProgressPercent,
            CurrentWaypointIndex = m.CurrentWaypointIndex,
            MissionState = m.MissionState,
            MediaCount = m.MediaCount,
            ReadyTime = m.ReadyTime,
            BeginTime = m.BeginTime,
            EndTime = m.EndTime,
            ErrorCode = m.ErrorCode,
            ErrorMessage = m.ErrorMessage,
            CreateTime = m.CreateTime,
            CreateUserName = m.CreateUserName,
        }).ToPagedListAsync(input.Page, input.PageSize);

        return await CompleteOutputAsync(paged);
    }

    /// <summary>任务详情</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "Detail")]
    public async Task<DjiWaylineTaskDetailOutput> Detail([FromQuery] QueryByIdWaylineTaskInput input)
    {
        var entity = await GetEntityAsync(input.Id);
        var output = entity.Adapt<DjiWaylineTaskDetailOutput>();
        CompleteOutput(output);
        output.DockNick = await GetDockNickAsync(entity.DockSn);
        return output;
    }

    /// <summary>任务进度流水（任务详情页时间线）</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "Progress")]
    public async Task<List<DjiWaylineTaskProgressOutput>> Progress([FromQuery] WaylineTaskProgressInput input)
    {
        var list = await _taskRepository.GetProgressAsync(input.Id, input.Take <= 0 ? 200 : input.Take);
        return list.Select(m => new DjiWaylineTaskProgressOutput
        {
            Id = m.Id,
            Status = m.Status,
            StatusText = WaylineJobStatus.Describe(m.Status),
            StepCode = m.StepCode,
            StepText = m.StepText,
            ProgressPercent = m.ProgressPercent,
            CurrentWaypointIndex = m.CurrentWaypointIndex,
            MissionState = m.MissionState,
            MediaCount = m.MediaCount,
            BreakPointJson = m.BreakPointJson,
            ReportTimestamp = m.ReportTimestamp,
            CreateTime = m.CreateTime,
        }).ToList();
    }

    /// <summary>查询某机场当前未结束任务（前端执行前预检）</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "ActiveByDock")]
    public async Task<DjiWaylineTaskOutput> ActiveByDock(ActiveWaylineTaskInput input)
    {
        var task = await _taskRepository.GetActiveTaskByDockSnAsync(input.DockSn);
        if (task == null) return null;

        var output = task.Adapt<DjiWaylineTaskOutput>();
        CompleteOutput(output);
        return output;
    }

    /// <summary>任务状态字典（前端筛选项与标签）</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "StatusOptions")]
    public List<WaylineJobStatusOption> StatusOptions()
    {
        return WaylineJobStatus.All
            .Select(m => new WaylineJobStatusOption
            {
                Value = m.Key,
                Label = m.Value,
                IsTerminal = WaylineJobStatus.IsTerminal(m.Key),
            })
            .ToList();
    }

    /// <summary>执行步骤字典（前端展示步骤含义）</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "StepOptions")]
    public List<FlightTaskStepOption> StepOptions()
    {
        return FlightTaskStep.All
            .OrderBy(m => m.Key)
            .Select(m => new FlightTaskStepOption { Value = m.Key, Label = m.Value })
            .ToList();
    }

    /// <summary>可选执行机场（下发任务时的机场下拉，含在线与占用状态）</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "DockOptions")]
    public async Task<List<DockOptionOutput>> DockOptions([FromQuery] string workspaceId)
    {
        var docks = await _deviceRep.AsQueryable()
            .Where(m => m.Domain == DomainEnum.Dock)
            .WhereIF(!workspaceId.IsNullOrWhiteSpace(), m => m.WorkspaceId == workspaceId)
            .OrderBy(m => m.Sn)
            .ToListAsync();
        if (docks.Count == 0) return [];

        // 一次性取出这些机场的未结束任务，避免逐个机场查库产生 N+1
        var dockSns = docks.Select(m => m.Sn).ToList();
        string[] active =
        [
            WaylineJobStatus.Sent, WaylineJobStatus.Ready,
            WaylineJobStatus.InProgress, WaylineJobStatus.Paused,
        ];
        var busyTasks = await _taskRep.AsQueryable()
            .Where(m => dockSns.Contains(m.DockSn) && active.Contains(m.Status))
            .ToListAsync();

        return docks.Select(dock =>
        {
            var busy = busyTasks.FirstOrDefault(m => m.DockSn == dock.Sn);
            return new DockOptionOutput
            {
                Sn = dock.Sn,
                Nick = dock.Nick,
                Model = dock.Model,
                WorkspaceId = dock.WorkspaceId,
                IsOnline = dock.IsOnline,
                Busy = busy != null,
                BusyStatus = busy == null ? null : WaylineJobStatus.Describe(busy.Status),
                CanDispatch = dock.IsOnline && busy == null,
            };
        }).ToList();
    }

    #endregion

    #region 下发与控制

    /// <summary>
    /// 下发航线任务（prepare + execute）。
    /// </summary>
    /// <remarks>
    /// 立即任务：prepare 与 execute 连续下发；
    /// 定时任务：此处只下发 prepare，execute 由后台任务在「执行前 2 分钟」触发；
    /// 条件任务：此处下发 prepare 并等待机场 <c>flighttask_ready</c>，就绪后由后台任务触发 execute。
    /// </remarks>
    /// <returns>任务主键</returns>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Create")]
    public async Task<long> Create(CreateWaylineTaskInput input)
    {
        if (input == null) throw Oops.Oh("请求参数不能为空");

        var (wayline, file) = await ResolveDispatchSourceAsync(input.WaylineEntityId);
        var dock = await ResolveDockAsync(input.DockSn);

        var active = await _taskRepository.GetActiveTaskByDockSnAsync(dock.Sn);
        if (active != null)
            throw Oops.Oh($"机场【{dock.Nick ?? dock.Sn}】当前有未结束任务（{WaylineJobStatus.Describe(active.Status)}），请先结束该任务后再下发");

        if (input.TaskType == TaskTypeCodeEnum.Conditional && input.ReadyConditions == null)
            throw Oops.Oh("条件任务必须填写准备条件（电量 / 可执行时段）");

        var rthAltitude = ResolveRthAltitude(input.RthAltitude, wayline.GlobalRTHHeight);
        var executeTime = ResolveExecuteTime(input);

        var task = new DjiWaylineTask
        {
            WorkspaceId = dock.WorkspaceId ?? wayline.WorkspaceId,
            FlightId = Guid.NewGuid().ToString("N"),
            TaskType = input.TaskType,
            ExecuteTime = executeTime,
            JobName = string.IsNullOrWhiteSpace(input.JobName) ? wayline.WaylineName : input.JobName.Trim(),
            DockSn = dock.Sn,
            DroneSn = await GetDockDroneSnAsync(dock.Sn),
            WaylineEntityId = wayline.Id,
            WaylineId = wayline.WaylineId,
            WaylineName = wayline.WaylineName,
            KmzFileName = wayline.KmzFileName,
            Fingerprint = file.Fingerprint,
            Status = WaylineJobStatus.Sent,
            StepCode = 0,
            StepText = FlightTaskStep.Describe(0),
            RthAltitude = rthAltitude,
            OutOfControlAction = input.OutOfControlAction,
        };

        await _taskRepository.InsertAsync(task);

        var prepareResult = await SendPrepareAsync(task, file, input);
        if (prepareResult != 0)
        {
            await _taskRepository.MarkRejectedAsync(task.FlightId, prepareResult, "下发任务(flighttask_prepare)");
            throw Oops.Oh($"机场拒绝下发任务，错误码：{prepareResult}");
        }

        // 条件任务需等机场上报 flighttask_ready；定时任务需等到执行前 2 分钟，两者都由后台任务驱动
        if (input.TaskType != TaskTypeCodeEnum.Immediate) return task.Id;

        var executeResult = await SendExecuteAsync(task.FlightId, dock.Sn);
        if (executeResult != 0)
        {
            await _taskRepository.MarkRejectedAsync(task.FlightId, executeResult, "执行任务(flighttask_execute)");
            throw Oops.Oh($"机场拒绝执行任务，错误码：{executeResult}");
        }

        return task.Id;
    }

    /// <summary>执行任务（用于条件任务就绪后手动/由后台触发执行，也可对「已下发」态补发执行指令）</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Execute")]
    public async Task<bool> Execute(WaylineTaskControlInput input)
    {
        var task = await GetEntityAsync(input.Id);
        EnsureControllable(task, [WaylineJobStatus.Sent, WaylineJobStatus.Ready]);

        var result = await SendExecuteAsync(task.FlightId, task.DockSn);
        if (result != 0)
        {
            await _taskRepository.MarkRejectedAsync(task.FlightId, result, "执行任务(flighttask_execute)");
            throw Oops.Oh($"机场拒绝执行任务，错误码：{result}");
        }

        return true;
    }

    /// <summary>暂停任务（仅执行中可暂停）</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Pause")]
    public async Task<bool> Pause(WaylineTaskControlInput input)
    {
        var task = await GetEntityAsync(input.Id);
        EnsureControllable(task, [WaylineJobStatus.InProgress]);
        return await SendSimpleCommandAsync(task.DockSn, task.FlightId, TopicMethods.FlightTaskPause, "暂停");
    }

    /// <summary>恢复任务（仅暂停态可恢复）</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Recovery")]
    public async Task<bool> Recovery(WaylineTaskControlInput input)
    {
        var task = await GetEntityAsync(input.Id);
        EnsureControllable(task, [WaylineJobStatus.Paused]);
        return await SendSimpleCommandAsync(task.DockSn, task.FlightId, TopicMethods.FlightTaskRecovery, "恢复");
    }

    /// <summary>取消任务（适用于已下发/已就绪/执行中，机场随后会回 removed/canceled）</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Undo")]
    public async Task<bool> Undo(WaylineTaskControlInput input)
    {
        var task = await GetEntityAsync(input.Id);
        if (WaylineJobStatus.IsTerminal(task.Status)) throw Oops.Oh($"任务已{WaylineJobStatus.Describe(task.Status)}，无需取消");

        var request = new CloudMqRequest<FlightTaskUndoInput>(
            TopicMethods.FlightTaskUndo,
            new FlightTaskUndoInput { FlightIds = [task.FlightId] },
            task.DockSn);
        var reply = await PublishWithReplyAsync(request, "取消");
        return reply == 0;
    }

    /// <summary>结束任务（机场降落并退出工作模式）</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Stop")]
    public async Task<bool> Stop(StopWaylineTaskInput input)
    {
        var task = await GetEntityAsync(input.Id);
        if (WaylineJobStatus.IsTerminal(task.Status)) throw Oops.Oh($"任务已{WaylineJobStatus.Describe(task.Status)}，无需结束");

        var request = new CloudMqRequest<FlightTaskStopInput>(
            TopicMethods.FlightTaskStop,
            new FlightTaskStopInput { FlightId = task.FlightId, Reason = input.Reason },
            task.DockSn);
        var reply = await PublishWithReplyAsync(request, "结束");
        return reply == 0;
    }

    #endregion

    #region 私有实现

    private async Task<DjiWaylineTask> GetEntityAsync(long id)
    {
        return await _taskRep.GetFirstAsync(m => m.Id == id) ?? throw Oops.Oh(ErrorCodeEnum.D1002);
    }

    /// <summary>校验并取出航线与其 KMZ 资源</summary>
    private async Task<(DjiWaylineEntity Wayline, FlightTaskFile File)> ResolveDispatchSourceAsync(long waylineEntityId)
    {
        var wayline = await _waylineRep.GetFirstAsync(m => m.Id == waylineEntityId)
                      ?? throw Oops.Oh(ErrorCodeEnum.D1002);

        if (wayline.WaylineParamJson.IsNullOrWhiteSpace())
            throw Oops.Oh("该航线缺少航线参数，请先重新编辑保存以生成 KMZ");

        var file = await _kmzResolver.ResolveAsync(wayline)
                   ?? throw Oops.Oh("无法解析航线 KMZ 下载地址或签名，请检查文件服务配置（Dji.FileBaseUrl）后重试");

        return (wayline, file);
    }

    /// <summary>校验机场</summary>
    private async Task<Dji.Core.Entity.DjiDevice> ResolveDockAsync(string dockSn)
    {
        if (dockSn.IsNullOrWhiteSpace()) throw Oops.Oh("执行机场不能为空");

        var dock = await _deviceRep.GetFirstAsync(m => m.Sn == dockSn)
                   ?? throw Oops.Oh($"未找到机场：{dockSn}");

        if (dock.Domain != DomainEnum.Dock) throw Oops.Oh($"{dockSn} 不是机场设备，无法执行航线任务");
        if (dock.WorkspaceId.IsNullOrWhiteSpace()) throw Oops.Oh($"机场【{dock.Nick ?? dock.Sn}】尚未绑定工作空间，请先在设备管理中完成绑定");

        return dock;
    }

    /// <summary>
    /// 计算协议要求的 <c>execute_time</c>。
    /// </summary>
    /// <remarks>
    /// 立即任务由服务端取当前时间：机场侧对立即任务限制 30 秒误差，前端传入的「计划时间」极易超差。
    /// </remarks>
    private static long ResolveExecuteTime(CreateWaylineTaskInput input)
    {
        if (input.TaskType == TaskTypeCodeEnum.Immediate)
            return DateTimeOffset.Now.ToUnixTimeMilliseconds() + ImmediateTaskLeadMs;

        if (input.ExecuteTime is null)
        {
            if (input.TaskType == TaskTypeCodeEnum.Scheduled) throw Oops.Oh("定时任务必须指定计划执行时间");

            // 条件任务 execute_time 可选，用 0 表示「无固定时间」，由 ready 事件驱动
            return 0;
        }

        var target = new DateTimeOffset(input.ExecuteTime.Value).ToUnixTimeMilliseconds();
        if (target <= DateTimeOffset.Now.ToUnixTimeMilliseconds())
            throw Oops.Oh("计划执行时间必须晚于当前时间");

        if (input.TaskType == TaskTypeCodeEnum.Scheduled
            && target - DateTimeOffset.Now.ToUnixTimeMilliseconds() > TimeSpan.FromHours(PrepareLeadHours).TotalMilliseconds)
            throw Oops.Oh($"定时任务的计划执行时间最多提前 {PrepareLeadHours} 小时下发；更远的任务请稍后再创建");

        return target;
    }

    /// <summary>
    /// 计算返航高度。
    /// </summary>
    /// <remarks>
    /// 未显式指定时沿用航线自身的「全局返航高度」；两者都不可用时返回 null（协议中该字段可选），
    /// 而不是塞一个 0 —— 0 不在协议允许的 [20, 1500] 区间内，会被机场判为非法参数。
    /// </remarks>
    private static int? ResolveRthAltitude(int? input, double waylineRthHeight)
    {
        var value = input is > 0 ? input.Value : (int)Math.Round(waylineRthHeight);
        if (value <= 0) return null;

        if (value < RthAltitudeMin || value > RthAltitudeMax)
            throw Oops.Oh($"返航高度需在 {RthAltitudeMin}~{RthAltitudeMax} 米之间，当前：{value}");

        return value;
    }

    /// <summary>取机场下的飞行器 SN（每个机场当期只挂一架飞行器）</summary>
    private async Task<string> GetDockDroneSnAsync(string dockSn)
    {
        var drone = await _deviceRep.AsQueryable()
            .Where(m => m.ParentSn == dockSn)
            .OrderBy(m => m.Id, OrderByType.Asc)
            .FirstAsync();
        return drone?.Sn;
    }

    private async Task<string> GetDockNickAsync(string dockSn)
    {
        if (dockSn.IsNullOrWhiteSpace()) return null;
        return await _deviceRep.AsQueryable().Where(m => m.Sn == dockSn).Select(m => m.Nick).FirstAsync();
    }

    /// <summary>控制指令的前置状态校验</summary>
    private static void EnsureControllable(DjiWaylineTask task, string[] allowed)
    {
        if (!allowed.Contains(task.Status))
            throw Oops.Oh($"任务当前状态为「{WaylineJobStatus.Describe(task.Status)}」，不支持该操作");
    }

    /// <summary>补齐列表/详情中由状态派生的字段</summary>
    private static void CompleteOutput(DjiWaylineTaskOutput output)
    {
        if (output == null) return;
        output.StatusText = WaylineJobStatus.Describe(output.Status);
        output.StepText = FlightTaskStep.Describe(output.StepCode);
        output.IsActive = WaylineJobStatus.IsActive(output.Status);
    }

    /// <summary>分页结果补齐派生字段</summary>
    private async Task<SqlSugarPagedList<DjiWaylineTaskOutput>> CompleteOutputAsync(SqlSugarPagedList<DjiWaylineTaskOutput> paged)
    {
        if (paged?.Items == null) return paged;

        foreach (var item in paged.Items) CompleteOutput(item);

        // 机场昵称按页内去重查询，避免逐行查库
        var dockSns = paged.Items.Select(m => m.DockSn).Where(m => !m.IsNullOrWhiteSpace()).Distinct().ToList();
        if (dockSns.Count == 0) return paged;

        var docks = await _deviceRep.AsQueryable()
            .Where(m => dockSns.Contains(m.Sn))
            .Select(m => new { m.Sn, m.Nick })
            .ToListAsync();
        var nickMap = docks.ToDictionary(m => m.Sn, m => m.Nick);

        foreach (var item in paged.Items)
            if (!item.DockSn.IsNullOrWhiteSpace()) item.DockNick = nickMap.GetValueOrDefault(item.DockSn);

        return paged;
    }

    #endregion
}
