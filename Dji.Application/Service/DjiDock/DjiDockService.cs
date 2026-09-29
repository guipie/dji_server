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
using Dji.Application.Service.Common;
using Dji.Application.Service.DjiDock.Dto;
using Dji.Core.Enum.DjiEnum.Dock;
using Dji.Core.Enum.DjiEnum.Hms;
using Dji.Core.Enum.DjiEnum.Ops;
using Dji.JsonSerialization;

namespace Dji.Application.Service.DjiDock;

/// <summary>
/// 机场远程控制服务（运维控制面板）。
/// </summary>
/// <remarks>
/// <para>
/// <b>职责边界</b>：本服务只负责「读快照 + 校验 + 下发 + 落一条指令记录」。
/// 设备随后推来的 <c>events</c> 进度由 <c>MqDockControlService</c> 落库到同一行
/// （<c>dji_dock_command</c>，以协议 <c>bid</c> 归并），因此本服务<b>不</b>轮询进度、
/// 也<b>不</b>自己维护指令状态机。
/// </para>
/// <para>
/// <b>为什么把「能不能点」放在服务端算</b>：控制面板上二十来个按钮背后是同一套互斥规则
/// （机场作业中不能开舱盖、飞行器在舱内不能强制关盖、固件升级中什么都不能做……）。
/// 这些规则若只在前端写，会上演「前端拦住了、接口没拦住」的经典事故 ——
/// 前端可以被绕过。因此 <see cref="Actions"/> 返回的是<b>服务端判定结果</b>，
/// 前端只负责渲染；<see cref="Execute"/> 下发前会<b>再判一次</b>，两手都不落空。
/// </para>
/// <para>
/// <b>机型差异</b>：本项目只对接 DJI Dock 2 / Dock 3。推杆指令（<c>putter_open</c>）
/// 只在 Dock 1 章节存在，因此不提供；eSIM 系列指令仅 Dock 2/3 支持，正常开放；
/// RTK 一键标定官方只在 Dock 2 章节列出，故对 Dock 3 直接判为不可用。
/// </para>
/// </remarks>
[ApiDescriptionSettings(ApplicationConst.GroupName, Order = 140)]
public class DjiDockService(
    SqlSugarRepository<Dji.Core.Entity.DjiDevice> deviceRep,
    SqlSugarRepository<DjiDockCommand> commandRep,
    DjiDockStateRepository dockStateRepository,
    DjiDockCommandRepository commandRepository,
    DjiHmsRepository hmsRepository,
    MqttGatewayPublish publish,
    UserManager userManager,
    ILogger<DjiDockService> logger) : IDynamicApiController, ITransient
{
    /// <summary>控制指令回包等待时长（机场要在本地执行动作，比普通查询慢）</summary>
    private const int CommandReplyTimeoutSeconds = 15;

    private readonly SqlSugarRepository<Dji.Core.Entity.DjiDevice> _deviceRep = deviceRep;
    private readonly SqlSugarRepository<DjiDockCommand> _commandRep = commandRep;
    private readonly DjiDockStateRepository _dockStateRepository = dockStateRepository;
    private readonly DjiDockCommandRepository _commandRepository = commandRepository;
    private readonly DjiHmsRepository _hmsRepository = hmsRepository;
    private readonly MqttGatewayPublish _publish = publish;
    private readonly UserManager _userManager = userManager;
    private readonly ILogger<DjiDockService> _logger = logger;

    #region 枚举与分组常量

    private const string GroupCover = "舱盖";
    private const string GroupPower = "供电";
    private const string GroupDrone = "飞行器";
    private const string GroupEnv = "环境";
    private const string GroupLink = "通信";
    private const string GroupMaintain = "维护";
    private const string GroupDanger = "危险";

    #endregion

    #region 指令字典

    /// <summary>
    /// 平台支持的机场控制指令（唯一的指令字典）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>新增一条指令只需要在这里加一行</b>：<see cref="Actions"/> 会自动把它渲染成按钮，
    /// <see cref="Execute"/> 会自动放行，参数校验按 <see cref="ActionMeta.Kind"/> 分支处理。
    /// 不需要改前端、不需要加接口。
    /// </para>
    /// <para>
    /// 顺序即前端展示顺序：按「日常操作 → 环境调节 → 通信 → 维护 → 危险」排列，危险的放最后。
    /// </para>
    /// </remarks>
    private static readonly List<ActionMeta> ActionMetas =
    [
        // ── 舱盖 ──
        new(TopicMethods.CoverOpen, "打开舱盖", GroupCover, DockCommandRiskEnum.Normal, ParamKind.None,
            RequireIdle: true, Current: s => s.CoverState,
            Rule: s => s.CoverState == (int)DockCoverStateEnum.Opened ? "舱盖已处于打开状态" : null),
        new(TopicMethods.CoverClose, "关闭舱盖", GroupCover, DockCommandRiskEnum.Normal, ParamKind.None,
            RequireIdle: true, Current: s => s.CoverState,
            Rule: s => s.CoverState == (int)DockCoverStateEnum.Closed ? "舱盖已处于关闭状态" : null),
        new(TopicMethods.CoverForceClose, "强制关闭舱盖", GroupCover, DockCommandRiskEnum.Caution, ParamKind.None,
            Current: s => s.CoverState,
            Rule: s => s.DroneInDock == null
                ? "尚未获取到「飞行器是否在舱」的状态，无法确认能否强制关舱盖"
                : s.DroneInDock == (int)DroneInDockEnum.Inside
                    ? "飞行器在舱内，禁止强制关闭舱盖（可能夹伤桨叶）"
                    : null),

        // ── 供电 ──
        new(TopicMethods.ChargeOpen, "打开充电", GroupPower, DockCommandRiskEnum.Normal, ParamKind.None,
            RequireIdle: true, Current: s => s.DroneChargeState,
            Rule: s => s.DroneChargeState == (int)DroneChargeStateEnum.Charging ? "飞行器正在充电" : null),
        new(TopicMethods.ChargeClose, "关闭充电", GroupPower, DockCommandRiskEnum.Normal, ParamKind.None,
            RequireIdle: true, Current: s => s.DroneChargeState,
            Rule: s => s.DroneChargeState == (int)DroneChargeStateEnum.Idle ? "飞行器当前未在充电" : null),

        // ── 飞行器 ──
        new(TopicMethods.DroneOpen, "飞行器开机", GroupDrone, DockCommandRiskEnum.Caution, ParamKind.None,
            RequireIdle: true),
        new(TopicMethods.DroneClose, "飞行器关机", GroupDrone, DockCommandRiskEnum.Caution, ParamKind.None,
            RequireIdle: true),

        // ── 环境 ──
        new(TopicMethods.AirConditionerModeSwitch, "空调工作模式", GroupEnv, DockCommandRiskEnum.Normal,
            ParamKind.AirConditioner, Current: s => s.AirConditionerState,
            Rule: s => s.AirConditionerSwitchTime > 0
                ? $"空调正在切换中，请等待 {s.AirConditionerSwitchTime} 秒后再操作"
                : null),
        new(TopicMethods.SupplementLightOpen, "打开补光灯", GroupEnv, DockCommandRiskEnum.Normal, ParamKind.None,
            Current: s => s.SupplementLightState,
            Rule: s => s.SupplementLightState == (int)SwitchStateEnum.On ? "补光灯已开启" : null),
        new(TopicMethods.SupplementLightClose, "关闭补光灯", GroupEnv, DockCommandRiskEnum.Normal, ParamKind.None,
            Current: s => s.SupplementLightState,
            Rule: s => s.SupplementLightState == (int)SwitchStateEnum.Off ? "补光灯已关闭" : null),
        new(TopicMethods.AlarmStateSwitch, "声光报警", GroupEnv, DockCommandRiskEnum.Caution,
            ParamKind.Switch, Current: s => s.AlarmState),
        new(TopicMethods.BatteryMaintenanceSwitch, "电池保养", GroupEnv, DockCommandRiskEnum.Caution,
            ParamKind.Switch),
        new(TopicMethods.BatteryStoreModeSwitch, "电池运行模式", GroupEnv, DockCommandRiskEnum.Caution,
            ParamKind.BatteryStore, Current: s => s.BatteryStoreMode),

        // ── 通信 ──
        new(TopicMethods.SdrWorkmodeSwitch, "增强图传", GroupLink, DockCommandRiskEnum.Normal,
            ParamKind.LinkWorkmode, Current: s => s.SdrLinkWorkmode),
        new(TopicMethods.EsimActivate, "eSIM 激活", GroupLink, DockCommandRiskEnum.Caution, ParamKind.Esim),
        new(TopicMethods.SimSlotSwitch, "SIM 卡槽切换", GroupLink, DockCommandRiskEnum.Caution, ParamKind.SimSlot),
        new(TopicMethods.EsimOperatorSwitch, "eSIM 运营商切换", GroupLink, DockCommandRiskEnum.Caution,
            ParamKind.EsimOperator),

        // ── 维护 ──
        new(TopicMethods.DebugModeOpen, "开启远程调试", GroupMaintain, DockCommandRiskEnum.Caution, ParamKind.None,
            RequireIdle: true),
        new(TopicMethods.DebugModeClose, "关闭远程调试", GroupMaintain, DockCommandRiskEnum.Normal, ParamKind.None),
        new(TopicMethods.RtkCalibration, "一键标定", GroupMaintain, DockCommandRiskEnum.Caution, ParamKind.Rtk,
            Api: nameof(RtkCalibration), RequireIdle: true,
            // 官方文档仅在「机场 2」章节列出该指令；机型未知时不做拦截，交给设备判断
            Rule: s => s.Model == "Dock3"
                ? "该机型暂不支持一键标定（官方文档仅在机场 2 章节提供该指令）"
                : null),

        // ── 危险（放最后，避免误点） ──
        new(TopicMethods.DeviceReboot, "机场重启", GroupDanger, DockCommandRiskEnum.Dangerous, ParamKind.None,
            RequireIdle: true),
        new(TopicMethods.DeviceFormat, "机场数据格式化", GroupDanger, DockCommandRiskEnum.Dangerous, ParamKind.None,
            RequireIdle: true),
        new(TopicMethods.DroneFormat, "飞行器数据格式化", GroupDanger, DockCommandRiskEnum.Dangerous, ParamKind.None,
            RequireIdle: true),
    ];

    #endregion

    #region 查询

    /// <summary>机场控制面板状态（主视图）</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "State")]
    public async Task<DockStateOutput> State([FromQuery] string dockSn)
    {
        var dock = await ResolveDockAsync(dockSn);
        return await BuildStateAsync(dock);
    }

    /// <summary>
    /// 可取的操作列表（前端据此渲染按钮，服务端算好可用性与禁用原因）。
    /// </summary>
    /// <remarks>传 <paramref name="dockSn"/> 时会结合该机场的实时状态计算；不传则返回「全部可选」的模板。</remarks>
    [HttpGet]
    [ApiDescriptionSettings(Name = "Actions")]
    public async Task<List<DockActionOptionOutput>> Actions([FromQuery] string dockSn)
    {
        DockStateOutput state = null;
        HashSet<string> runningMethods = [];

        if (!dockSn.IsNullOrWhiteSpace())
        {
            var dock = await ResolveDockAsync(dockSn);
            state = await BuildStateAsync(dock);
            runningMethods = (await _commandRepository.GetRunningAsync(dock.Sn))
                .Select(m => m.Method).ToHashSet();
        }

        return ActionMetas.Select(m =>
        {
            var reason = state == null ? null : Evaluate(m, state, runningMethods);

            return new DockActionOptionOutput
            {
                Method = m.Method,
                Name = m.Name,
                Group = m.Group,
                RiskLevel = m.Risk,
                RiskName = Desc(m.Risk),
                NeedConfirm = m.Risk != DockCommandRiskEnum.Normal,
                Available = reason == null,
                DisabledReason = reason,
                Api = m.Api,
                CurrentValue = state == null ? null : m.Current?.Invoke(state),
                RequiredFields = FieldsOf(m),
            };
        }).ToList();
    }

    /// <summary>指令记录分页</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Page")]
    public async Task<SqlSugarPagedList<DockCommandRecordOutput>> Page(DockCommandSearchInput input)
    {
        input ??= new DockCommandSearchInput();

        var query = _commandRep.AsQueryable()
            .WhereIF(!input.WorkspaceId.IsNullOrWhiteSpace(), m => m.WorkspaceId == input.WorkspaceId)
            .WhereIF(!input.DockSn.IsNullOrWhiteSpace(), m => m.DockSn == input.DockSn)
            .WhereIF(!input.Method.IsNullOrWhiteSpace(), m => m.Method == input.Method)
            .WhereIF(input.Status.HasValue, m => m.Status == input.Status)
            .WhereIF(input.StartTime.HasValue, m => m.CreateTime >= input.StartTime)
            .WhereIF(input.EndTime.HasValue, m => m.CreateTime <= input.EndTime)
            .OrderBy(m => m.CreateTime, OrderByType.Desc);

        var paged = await query.Select(m => new DockCommandRecordOutput
        {
            Id = m.Id,
            DockSn = m.DockSn,
            Method = m.Method,
            ActionName = m.ActionName,
            Bid = m.Bid,
            RiskLevel = m.RiskLevel,
            Status = m.Status,
            Percent = m.Percent,
            StepKey = m.StepKey,
            Result = m.Result,
            ErrorMessage = m.ErrorMessage,
            OperatorName = m.OperatorName,
            CreateTime = m.CreateTime,
            FinishTime = m.FinishTime,
        }).ToPagedListAsync(input.Page, input.PageSize);

        await FillRecordsAsync(paged.Items.ToList());
        return paged;
    }

    /// <summary>某机场最近的指令记录（控制面板活动日志）</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "Recent")]
    public async Task<List<DockCommandRecordOutput>> Recent([FromQuery] string dockSn, [FromQuery] int limit = 20)
    {
        var list = await _commandRepository.GetRecentAsync(dockSn, limit <= 0 ? 20 : limit);
        var output = list.Select(ToRecordOutput).ToList();
        await FillRecordsAsync(output);
        return output;
    }

    /// <summary>机场下拉（含状态与是否有在途指令）</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "DockOptions")]
    public async Task<List<DockOptionOutput>> DockOptions(DockOptionInput input)
    {
        input ??= new DockOptionInput();

        var docks = await _deviceRep.AsQueryable()
            .Where(m => m.Domain == DomainEnum.Dock)
            .WhereIF(!input.WorkspaceId.IsNullOrWhiteSpace(), m => m.WorkspaceId == input.WorkspaceId)
            .OrderBy(m => m.Nick)
            .ToListAsync();

        var dockSns = docks.Select(m => m.Sn).ToList();
        var states = await _dockStateRepository.GetByDockSnsAsync(dockSns);
        var stateMap = states.ToDictionary(m => m.DockSn);

        var runningDocks = await _commandRep.AsQueryable()
            .Where(m => dockSns.Contains(m.DockSn)
                && m.Status != DockTaskStatusEnum.Ok && m.Status != DockTaskStatusEnum.Failed
                && m.Status != DockTaskStatusEnum.Canceled && m.Status != DockTaskStatusEnum.Rejected
                && m.Status != DockTaskStatusEnum.Timeout)
            .Select(m => m.DockSn)
            .ToListAsync();

        return docks.Select(m =>
        {
            var modeCode = stateMap.GetValueOrDefault(m.Sn)?.ModeCode;
            var label = modeCode.HasValue ? Name(modeCode, typeof(DockModeCodeEnum)) : null;
            return new DockOptionOutput
            {
                Sn = m.Sn,
                Nick = m.Nick,
                WorkspaceId = m.WorkspaceId,
                IsOnline = m.IsOnline,
                ModeCode = modeCode,
                ModeName = label,
                HasRunningCommand = runningDocks.Contains(m.Sn),
                Label = m.Nick.IsNullOrWhiteSpace() ? m.Sn : $"{m.Nick}（{m.Sn}）",
            };
        }).ToList();
    }

    /// <summary>指令状态字典</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "StatusOptions")]
    public List<DockDictOptionOutput> StatusOptions()
        => typeof(DockTaskStatusEnum).GetEnumDescDictionary()
            .OrderBy(m => m.Key)
            .Select(m => new DockDictOptionOutput { Value = m.Key, Label = m.Value })
            .ToList();

    /// <summary>风险等级字典</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "RiskOptions")]
    public List<DockDictOptionOutput> RiskOptions()
        => typeof(DockCommandRiskEnum).GetEnumDescDictionary()
            .OrderBy(m => m.Key)
            .Select(m => new DockDictOptionOutput { Value = m.Key, Label = m.Value })
            .ToList();

    /// <summary>机场状态字典</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "ModeOptions")]
    public List<DockDictOptionOutput> ModeOptions()
        => typeof(DockModeCodeEnum).GetEnumDescDictionary()
            .OrderBy(m => m.Key)
            .Select(m => new DockDictOptionOutput { Value = m.Key, Label = m.Value })
            .ToList();

    #endregion

    #region 下发

    /// <summary>
    /// 下发机场控制指令（统一入口）。
    /// </summary>
    /// <remarks>
    /// <para>执行顺序（任一步失败即中止，不会发出半成品指令）：</para>
    /// <list type="number">
    /// <item>方法名必须在服务端字典内，且不是需要额外表单的特殊指令；</item>
    /// <item>机场必须已建档、属于机场类型、已绑定工作空间；</item>
    /// <item>按实时状态判定可用性（离线 / 急停 / 升级中 / 作业中 / 同类指令在途）；</item>
    /// <item>风险等级高于普通时必须有 <c>Confirm</c>；</item>
    /// <item>参数范围校验；</item>
    /// <item>下发并等待 <c>services_reply</c>；</item>
    /// <item><b>回包之后</b>落一条指令记录（<c>bid</c> 由发送方生成，事前拿不到）。</item>
    /// </list>
    /// <para>
    /// 机场拒绝（<c>result != 0</c>）时会抛错，但<b>记录已经落库</b> ——
    /// 被拒绝的操作同样需要留痕，否则「谁点过格式化」就查不到了。
    /// </para>
    /// </remarks>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Execute")]
    public async Task<DockCommandRecordOutput> Execute([FromBody] DockCommandExecuteInput input)
    {
        var meta = ActionMetas.FirstOrDefault(m => m.Method == input?.Method)
                   ?? throw Oops.Oh($"不支持的机场指令：{input?.Method}");

        if (meta.Api != nameof(Execute))
            throw Oops.Oh($"【{meta.Name}】需要额外填写参数，请通过 {meta.Api} 接口下发");

        var dock = await ResolveDockAsync(input.DockSn);
        var state = await BuildStateAsync(dock);
        var runningMethods = (await _commandRepository.GetRunningAsync(dock.Sn))
            .Select(m => m.Method).ToHashSet();

        var reason = Evaluate(meta, state, runningMethods);
        if (reason != null) throw Oops.Oh(reason);

        if (meta.Risk != DockCommandRiskEnum.Normal && !input.Confirm)
            throw Oops.Oh($"【{meta.Name}】属于「{Desc(meta.Risk)}」操作，请确认后再下发");

        var payload = BuildPayload(meta, input);

        return await DispatchAsync(dock, meta, payload,
            payload == null ? null : JSON.Serialize(payload));
    }

    /// <summary>
    /// RTK 一键标定。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么单独一个接口</b>：其它指令的 <c>data</c> 都只有一两个枚举字段，
    /// 而标定要传一个<b>设备数组</b>（每个元素含 SN、模块、经纬高），表单形态完全不同。
    /// </para>
    /// <para>
    /// <b>标定设备怎么来</b>：机场本体（<c>module = "3"</c>）必然包含；
    /// 再补上该机场下已建档的中继设备（<c>Domain = Repeater</c>，<c>module = "6"</c>）。
    /// 只有机场本体时也照常下发 —— 官方的 <c>devices</c> 数组允许多元素，单元素同样合法。
    /// </para>
    /// <para>
    /// 所有设备共用同一组坐标：实际部署中「标定点」就是一个已知经纬高的空地点位，
    /// 机场与中继各自量取的不是同一点，但云端只需给出待标定的目标值。
    /// </para>
    /// </remarks>
    [HttpPost]
    [ApiDescriptionSettings(Name = "RtkCalibration")]
    public async Task<DockCommandRecordOutput> RtkCalibration([FromBody] DockRtkCalibrationInput input)
    {
        var meta = ActionMetas.First(m => m.Method == TopicMethods.RtkCalibration);

        var dock = await ResolveDockAsync(input.DockSn);
        var state = await BuildStateAsync(dock);
        var runningMethods = (await _commandRepository.GetRunningAsync(dock.Sn))
            .Select(m => m.Method).ToHashSet();

        var reason = Evaluate(meta, state, runningMethods);
        if (reason != null) throw Oops.Oh(reason);

        if (!input.Confirm)
            throw Oops.Oh("标定会重置机场的 RTK 基准，请确认后再下发");

        if (input.Longitude is < -180 or > 180 || input.Latitude is < -90 or > 90)
            throw Oops.Oh("标定坐标超出合法范围，请检查经纬度");

        var point = new RtkCalibrationPoint
        {
            Longitude = input.Longitude,
            Latitude = input.Latitude,
            Height = input.Height,
        };

        var payload = new RtkCalibrationPayload();
        payload.Devices.Add(new RtkCalibrationDeviceInput
        {
            Sn = dock.Sn,
            Module = ((int)RtkCalibrationModuleEnum.Dock).ToString(),
            Data = point,
        });

        var relays = await _deviceRep.AsQueryable()
            .Where(m => m.ParentSn == dock.Sn && m.Domain == DomainEnum.Repeater)
            .ToListAsync();

        foreach (var relay in relays)
        {
            payload.Devices.Add(new RtkCalibrationDeviceInput
            {
                Sn = relay.Sn,
                Module = ((int)RtkCalibrationModuleEnum.Relay).ToString(),
                Data = point,
            });
        }

        return await DispatchAsync(dock, meta, payload, JSON.Serialize(payload));
    }

    #endregion

    #region 私有实现 —— 下发

    /// <summary>
    /// 真正的下发与落库（<see cref="Execute"/> 与 <see cref="RtkCalibration"/> 共用）。
    /// </summary>
    private async Task<DockCommandRecordOutput> DispatchAsync<T>(
        Dji.Core.Entity.DjiDevice dock, ActionMeta meta, T payload, string payloadJson)
        where T : class
    {
        // 防连点：从「开始下发」到「记录落库」之间的十几秒空窗里，数据库里还没有这条记录
        using var _ = InFlightGuard.Enter($"{dock.Sn}|{meta.Method}", meta.Name);

        var request = payload == null
            ? new CloudMqRequest<T>(meta.Method, dock.Sn)
            : new CloudMqRequest<T>(meta.Method, payload, dock.Sn);

        int result;
        try
        {
            var reply = await _publish.PublishWithReplyAsync<T, object>(
                Topics.ThingProductServices, request, CommandReplyTimeoutSeconds);
            result = reply?.Data?.Result ?? -1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "机场指令无应答：method:{Method}，gateway:{Gateway}", meta.Method, dock.Sn);
            result = -1;
        }

        if (result != 0)
        {
            _logger.LogWarning("机场拒绝指令：method:{Method}，gateway:{Gateway}，result:{Result}",
                meta.Method, dock.Sn, result);
        }

        var record = await _commandRepository.CreateAsync(new DjiDockCommand
        {
            WorkspaceId = dock.WorkspaceId,
            DockSn = dock.Sn,
            Method = meta.Method,
            ActionName = meta.Name,
            // 协议要求 bid 唯一且由发送方生成，生成发生在发送方法内部，只能回包后取
            Bid = request.Bid,
            RiskLevel = meta.Risk,
            Status = result == 0 ? DockTaskStatusEnum.Sent : DockTaskStatusEnum.Failed,
            PayloadJson = payloadJson,
            Result = result,
            ErrorMessage = result == 0 ? null : DescribeResult(result),
            OperatorId = _userManager.UserId,
            OperatorName = _userManager.RealName ?? _userManager.Account,
            CreateTime = DateTime.Now,
            FinishTime = result == 0 ? null : DateTime.Now,
        });

        if (result != 0)
            throw Oops.Oh($"机场拒绝执行【{meta.Name}】，{DescribeResult(result)}");

        _logger.LogInformation("已下发机场指令：method:{Method}，gateway:{Gateway}，bid:{Bid}，operator:{Operator}",
            meta.Method, dock.Sn, request.Bid, record.OperatorName);

        var output = ToRecordOutput(record);
        output.DockNick = dock.Nick;
        return output;
    }

    /// <summary>
    /// 把 <c>services_reply</c> 的返回码翻译成可读说明。
    /// </summary>
    /// <remarks>
    /// 官方对控制类指令的 <c>result</c> 只约定「非 0 代表错误」，<b>没有给出逐指令的错误码字典</b>
    /// （错误码由设备侧定义、且随固件变化）。因此这里不做猜测式翻译，
    /// 只在超时（平台内部约定为 -1）时给出明确解释，其余原样带出码值供运维比对。
    /// </remarks>
    private static string DescribeResult(int result)
        => result == -1 ? "设备未在超时时间内回包（指挥链路可能中断）" : $"错误码：{result}";

    /// <summary>组装无参 / 带参指令的下发载荷；无参指令返回 null（协议要求 <c>data = null</c>）</summary>
    private static DockCommandPayload BuildPayload(ActionMeta meta, DockCommandExecuteInput input)
    {
        switch (meta.Kind)
        {
            case ParamKind.None:
                return null;

            case ParamKind.Switch:
                RequireIn(input.Action, 0, 1, "操作值", meta.Name);
                return new DockCommandPayload { Action = input.Action };

            case ParamKind.AirConditioner:
                RequireIn(input.Action, 0, 3, "空调模式", meta.Name);
                return new DockCommandPayload { Action = input.Action };

            case ParamKind.BatteryStore:
                // 协议无 0，只有 1 计划 / 2 待命
                RequireIn(input.Action, 1, 2, "电池运行模式", meta.Name);
                return new DockCommandPayload { Action = input.Action };

            case ParamKind.LinkWorkmode:
                RequireIn(input.LinkWorkmode, 0, 1, "图传模式", meta.Name);
                return new DockCommandPayload { LinkWorkmode = input.LinkWorkmode };

            case ParamKind.Esim:
                RequireEsim(input);
                return new DockCommandPayload
                {
                    Imei = input.Imei.Trim(),
                    DeviceType = input.DeviceType.Trim().ToLowerInvariant(),
                };

            case ParamKind.SimSlot:
                RequireEsim(input);
                RequireIn(input.SimSlot, 1, 2, "SIM 卡槽", meta.Name);
                return new DockCommandPayload
                {
                    Imei = input.Imei.Trim(),
                    DeviceType = input.DeviceType.Trim().ToLowerInvariant(),
                    SimSlot = input.SimSlot,
                };

            case ParamKind.EsimOperator:
                RequireEsim(input);
                RequireIn(input.EsimOperator, 1, 3, "运营商", meta.Name);
                return new DockCommandPayload
                {
                    Imei = input.Imei.Trim(),
                    DeviceType = input.DeviceType.Trim().ToLowerInvariant(),
                    EsimOperator = input.EsimOperator,
                };

            default:
                return null;
        }
    }

    /// <summary>eSIM 类指令的共同必填项校验</summary>
    private static void RequireEsim(DockCommandExecuteInput input)
    {
        if (input.Imei.IsNullOrWhiteSpace())
            throw Oops.Oh("eSIM 相关指令必须提供 Dongle 的 IMEI（可在机场状态中查看）");

        if (input.DeviceType.IsNullOrWhiteSpace()
            || input.DeviceType.Trim().ToLowerInvariant() is not ("dock" or "drone"))
        {
            throw Oops.Oh("请指定目标设备类型：dock（机场）或 drone（飞行器）");
        }
    }

    /// <summary>闭区间取值校验</summary>
    private static void RequireIn(int? value, int min, int max, string fieldName, string actionName)
    {
        if (!value.HasValue) throw Oops.Oh($"【{actionName}】请指定{fieldName}");

        if (value.Value < min || value.Value > max)
            throw Oops.Oh($"【{actionName}】的{fieldName}取值非法：{value.Value}，应在 {min}~{max} 之间");
    }

    #endregion

    #region 私有实现 —— 校验

    /// <summary>
    /// 判断一条指令当前能否下发；返回 <c>null</c> 表示可以。
    /// </summary>
    /// <remarks>
    /// 判定顺序即优先级：先排除「物理上根本不能发」的原因（离线、急停、升级中），
    /// 再说「发了会打断作业」，最后才是个别指令自己的专属规则。
    /// 这样用户看到的禁用原因是<b>最根本的那一个</b>，而不是一堆并列的红字。
    /// </remarks>
    private static string Evaluate(ActionMeta meta, DockStateOutput state, HashSet<string> runningMethods)
    {
        if (!state.IsOnline) return "机场离线，无法下发指令";

        if (state.EmergencyStopState == (int)SwitchStateEnum.On)
            return "机场急停按钮已按下，请先复位后再操作";

        if (state.ModeCode == (int)DockModeCodeEnum.FirmwareUpgrading)
            return "机场正在固件升级，升级完成前不可下发任何指令";

        if (meta.RequireIdle && state.ModeCode != (int)DockModeCodeEnum.Idle)
            return $"机场当前处于「{state.ModeName ?? "非空闲"}」，该操作会打断作业，请等待机场空闲后再试";

        if (runningMethods.Contains(meta.Method))
            return "该指令正在执行中，请等待完成后再试";

        return meta.Rule?.Invoke(state);
    }

    #endregion

    #region 私有实现 —— 组装输出

    /// <summary>组装机场控制面板状态（快照 + 告警计数 + 在途指令）</summary>
    private async Task<DockStateOutput> BuildStateAsync(Dji.Core.Entity.DjiDevice dock)
    {
        var state = await _dockStateRepository.GetAsync(dock.Sn);
        var output = MapState(dock, state);

        var alarms = await _hmsRepository.GetActiveAsync(dock.Sn);
        output.ActiveAlarmCount = alarms.Count;
        output.WarningAlarmCount = alarms.Count(m => m.Level == HmsLevelEnum.Warning);

        var running = await _commandRepository.GetRunningAsync(dock.Sn);
        output.RunningCommands = running.Select(ToRecordOutput).ToList();

        return output;
    }

    /// <summary>
    /// 把「设备表 + 快照表」拼成控制面板视图。
    /// </summary>
    /// <remarks>
    /// 快照可能整行为空（新接入的机场还没上报过 OSD），此时只返回设备侧的基础信息，
    /// 各个状态列留空 —— 前端据 <c>OsdReceivedTime</c> 为空即可提示「尚未上报状态」。
    /// </remarks>
    private static DockStateOutput MapState(Dji.Core.Entity.DjiDevice dock, DjiDockState state)
    {
        var output = new DockStateOutput
        {
            DockSn = dock.Sn,
            DockNick = dock.Nick,
            Model = dock.Model,
            WorkspaceId = dock.WorkspaceId,
            IsOnline = dock.IsOnline,
        };

        if (state == null) return output;

        output.ModeCode = state.ModeCode;
        output.ModeName = Name(state.ModeCode, typeof(DockModeCodeEnum));
        output.IsIdle = state.ModeCode == (int)DockModeCodeEnum.Idle;

        output.FlighttaskStepCode = state.FlighttaskStepCode;
        output.FlighttaskStepName = Name(state.FlighttaskStepCode, typeof(DockTaskStepEnum));

        output.CoverState = state.CoverState;
        output.CoverStateName = Name(state.CoverState, typeof(DockCoverStateEnum));

        output.PutterState = state.PutterState;
        output.PutterStateName = Name(state.PutterState, typeof(DockPutterStateEnum));

        output.DroneInDock = state.DroneInDock;
        output.DroneInDockName = Name(state.DroneInDock, typeof(DroneInDockEnum));

        output.SupplementLightState = state.SupplementLightState;
        output.AlarmState = state.AlarmState;

        output.BatteryStoreMode = state.BatteryStoreMode;
        output.BatteryStoreModeName = Name(state.BatteryStoreMode, typeof(DockBatteryStoreModeEnum));

        output.AirConditionerState = state.AirConditionerState;
        // 注意用「状态」枚举而不是「下发模式」枚举：上报值含 4~15 的过渡态
        output.AirConditionerStateName = Name(state.AirConditionerState, typeof(DockAirConditionerStateEnum));
        output.AirConditionerSwitchTime = state.AirConditionerSwitchTime;

        output.DroneChargeState = state.DroneChargeState;
        output.DroneChargePercent = state.DroneChargePercent;
        output.EmergencyStopState = state.EmergencyStopState;
        output.SilentMode = state.SilentMode;

        output.SdrLinkWorkmode = state.SdrLinkWorkmode;
        output.SdrLinkWorkmodeName = Name(state.SdrLinkWorkmode, typeof(SdrLinkWorkmodeEnum));

        output.FirmwareVersion = state.FirmwareVersion;
        output.FirmwareUpgradeStatus = state.FirmwareUpgradeStatus;

        output.JobNumber = state.JobNumber;
        output.AccTime = state.AccTime;
        output.StorageTotal = state.StorageTotal;
        output.StorageUsed = state.StorageUsed;

        output.Temperature = state.Temperature;
        output.Humidity = state.Humidity;
        output.EnvironmentTemperature = state.EnvironmentTemperature;
        output.WindSpeed = state.WindSpeed;
        output.Rainfall = state.Rainfall;
        output.ElectricSupplyVoltage = state.ElectricSupplyVoltage;

        output.Longitude = state.Longitude;
        output.Latitude = state.Latitude;
        output.OsdReceivedTime = state.OsdReceivedTime;

        return output;
    }

    /// <summary>补齐指令记录的可读字段（机场昵称、枚举名、是否进行中）</summary>
    private async Task FillRecordsAsync(List<DockCommandRecordOutput> items)
    {
        if (items.Count == 0) return;

        foreach (var item in items)
        {
            item.RiskName = Desc(item.RiskLevel);
            item.StatusName = Desc(item.Status);
            item.IsRunning = !item.Status.IsFinal();
        }

        var dockSns = items.Select(m => m.DockSn).Where(m => !m.IsNullOrWhiteSpace()).Distinct().ToList();
        if (dockSns.Count == 0) return;

        var docks = await _deviceRep.AsQueryable().Where(m => dockSns.Contains(m.Sn)).ToListAsync();
        var nickMap = docks.ToDictionary(m => m.Sn, m => m.Nick);
        foreach (var item in items) item.DockNick = nickMap.GetValueOrDefault(item.DockSn);
    }

    private static DockCommandRecordOutput ToRecordOutput(DjiDockCommand entity) => new()
    {
        Id = entity.Id,
        DockSn = entity.DockSn,
        Method = entity.Method,
        ActionName = entity.ActionName,
        Bid = entity.Bid,
        RiskLevel = entity.RiskLevel,
        Status = entity.Status,
        Percent = entity.Percent,
        StepKey = entity.StepKey,
        Result = entity.Result,
        ErrorMessage = entity.ErrorMessage,
        OperatorName = entity.OperatorName,
        CreateTime = entity.CreateTime,
        FinishTime = entity.FinishTime,
        RiskName = Desc(entity.RiskLevel),
        StatusName = Desc(entity.Status),
        IsRunning = !entity.Status.IsFinal(),
    };

    private async Task<Dji.Core.Entity.DjiDevice> ResolveDockAsync(string dockSn)
    {
        if (dockSn.IsNullOrWhiteSpace()) throw Oops.Oh("机场不能为空");

        var dock = await _deviceRep.GetFirstAsync(m => m.Sn == dockSn) ?? throw Oops.Oh($"未找到机场：{dockSn}");
        if (dock.Domain != DomainEnum.Dock) throw Oops.Oh($"{dockSn} 不是机场设备");
        if (dock.WorkspaceId.IsNullOrWhiteSpace()) throw Oops.Oh($"机场【{dock.Nick ?? dock.Sn}】尚未绑定工作空间");

        return dock;
    }

    #endregion

    #region 私有实现 —— 小工具

    /// <summary>取枚举描述；未知取值返回 null 而不是数字串，便于前端判断「设备给了没见过的值」</summary>
    private static string Name(int? value, Type enumType)
    {
        if (!value.HasValue) return null;
        return enumType.GetEnumDescDictionary().TryGetValue(value.Value, out var label) ? label : null;
    }

    /// <summary>取枚举的 <c>[Description]</c>；显式写全限定名以避免与 NewLife 的同名扩展二义</summary>
    private static string Desc<T>(T value) where T : struct, System.Enum
        => Dji.Core.EnumExtension.GetDescription(value);

    /// <summary>把指令所需的额外参数名转成前端可直接识别的 camelCase 列表</summary>
    private static List<string> FieldsOf(ActionMeta meta) => meta.Kind switch
    {
        ParamKind.Switch or ParamKind.AirConditioner or ParamKind.BatteryStore => ["action"],
        ParamKind.LinkWorkmode => ["linkWorkmode"],
        ParamKind.Esim => ["imei", "deviceType"],
        ParamKind.SimSlot => ["imei", "deviceType", "simSlot"],
        ParamKind.EsimOperator => ["imei", "deviceType", "esimOperator"],
        ParamKind.Rtk => ["longitude", "latitude", "height"],
        _ => [],
    };

    #endregion

    #region 内部类型

    /// <summary>带参指令的参数形态（决定前端要展示哪些输入项、下发时如何校验）</summary>
    private enum ParamKind
    {
        /// <summary>无参（协议 <c>data = null</c>）</summary>
        None,

        /// <summary>开关（<c>action</c> 0 关 / 1 开）</summary>
        Switch,

        /// <summary>空调模式（<c>action</c> 0~3）</summary>
        AirConditioner,

        /// <summary>电池运行模式（<c>action</c> 1 计划 / 2 待命）</summary>
        BatteryStore,

        /// <summary>增强图传（<c>link_workmode</c> 0 仅 SDR / 1 4G 增强）</summary>
        LinkWorkmode,

        /// <summary>eSIM 激活（<c>imei</c> + <c>device_type</c>）</summary>
        Esim,

        /// <summary>SIM 卡槽切换（<c>imei</c> + <c>device_type</c> + <c>sim_slot</c>）</summary>
        SimSlot,

        /// <summary>eSIM 运营商切换（<c>imei</c> + <c>device_type</c> + <c>esim_operator</c>）</summary>
        EsimOperator,

        /// <summary>RTK 标定（走独立接口，参数是设备数组）</summary>
        Rtk,
    }

    /// <summary>
    /// 一条可控指令的元数据。
    /// </summary>
    /// <param name="Method">协议方法名</param>
    /// <param name="Name">操作中文名</param>
    /// <param name="Group">前端分组</param>
    /// <param name="Risk">风险等级</param>
    /// <param name="Kind">参数形态</param>
    /// <param name="RequireIdle">是否要求机场空闲（true 表示会打断作业）</param>
    /// <param name="Api">命中的接口名（特殊指令走独立接口）</param>
    /// <param name="Current">取当前值（用于前端回显）</param>
    /// <param name="Rule">专属可用性规则，返回禁用原因或 null</param>
    private sealed record ActionMeta(
        string Method,
        string Name,
        string Group,
        DockCommandRiskEnum Risk,
        ParamKind Kind,
        bool RequireIdle = false,
        string Api = "Execute",
        Func<DockStateOutput, int?> Current = null,
        Func<DockStateOutput, string> Rule = null);

    #endregion
}
