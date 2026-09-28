// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Core.Enum.DjiEnum.Dock;

namespace Dji.Application.Service.DjiDock.Dto;

/// <summary>
/// 机场控制指令的统一下发入参。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么用「统一入口」而不是一个指令一个接口</b>：控制面板上一排按钮背后是同一种链路
/// （下发 <c>services</c> → 收 <c>services_reply</c> → 收 <c>events</c> 进度），
/// 差异只有方法名与少量参数。用一个入口可以让前端只对接一个接口，
/// 日后协议新增指令时后端加一行元数据即可，无需动前端 API 层。
/// </para>
/// <para>
/// <b>参数按需取用</b>：开关类指令看 <see cref="Action"/>，图传看 <see cref="LinkWorkmode"/>，
/// eSIM 相关看 <see cref="Imei"/> / <see cref="DeviceType"/> 等。多余字段被忽略，
/// 不做「必须为空」这类苛刻校验 —— 前端复用一个表单时传多了很正常。
/// </para>
/// </remarks>
public class DockCommandExecuteInput
{
    /// <summary>目标机场 SN</summary>
    [Required(ErrorMessage = "机场 SN 不能为空")]
    public string DockSn { get; set; }

    /// <summary>协议方法名（如 <c>cover_open</c>），须在服务端白名单内</summary>
    [Required(ErrorMessage = "指令不能为空")]
    public string Method { get; set; }

    /// <summary>
    /// 通用操作值。
    /// </summary>
    /// <remarks>
    /// 复用于三种语义：电池保养/声光报警（0 关 1 开）、空调模式（0 空闲 1 制冷 2 制热 3 除湿）、
    /// 电池运行模式（1 计划 2 待命）。取值合法性由各指令的元数据校验。
    /// </remarks>
    public int? Action { get; set; }

    /// <summary>图传链路模式（0 仅 SDR / 1 4G 增强），<c>sdr_workmode_switch</c> 用</summary>
    public int? LinkWorkmode { get; set; }

    /// <summary>Dongle IMEI，eSIM 相关指令用于标识操作对象</summary>
    public string Imei { get; set; }

    /// <summary>eSIM 目标设备类型（<c>dock</c> / <c>drone</c>）</summary>
    public string DeviceType { get; set; }

    /// <summary>SIM 卡槽（1 实体卡 / 2 eSIM）</summary>
    public int? SimSlot { get; set; }

    /// <summary>eSIM 运营商（1 移动 / 2 联通 / 3 电信）</summary>
    public int? EsimOperator { get; set; }

    /// <summary>
    /// 高危操作的确认标记。
    /// </summary>
    /// <remarks>
    /// 重启、格式化这类不可逆操作为 <c>false</c> 时服务端直接拒绝，强制前端先弹确认框。
    /// 这道校验放在服务端而不是只靠前端弹窗：前端可以被绕过（直接调接口），
    /// 而「格式化把机场存储清空」是不可挽回的。
    /// </remarks>
    public bool Confirm { get; set; }
}

/// <summary>RTK 一键标定入参（需提供标定点的经纬高）</summary>
public class DockRtkCalibrationInput
{
    /// <summary>目标机场 SN</summary>
    [Required(ErrorMessage = "机场 SN 不能为空")]
    public string DockSn { get; set; }

    /// <summary>标定点经度</summary>
    [Required(ErrorMessage = "经度不能为空")]
    public double Longitude { get; set; }

    /// <summary>标定点纬度</summary>
    [Required(ErrorMessage = "纬度不能为空")]
    public double Latitude { get; set; }

    /// <summary>标定点椭球高度（米）</summary>
    [Required(ErrorMessage = "高度不能为空")]
    public double Height { get; set; }

    /// <summary>确认标记（标定会重置机场 RTK，需前端二次确认）</summary>
    public bool Confirm { get; set; }
}

/// <summary>机场控制指令记录查询</summary>
public class DockCommandSearchInput : BasePageInput
{
    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>机场 SN</summary>
    public string DockSn { get; set; }

    /// <summary>协议方法名</summary>
    public string Method { get; set; }

    /// <summary>执行状态</summary>
    public DockTaskStatusEnum? Status { get; set; }

    /// <summary>时间范围起点</summary>
    public DateTime? StartTime { get; set; }

    /// <summary>时间范围终点</summary>
    public DateTime? EndTime { get; set; }
}

/// <summary>机场下拉查询</summary>
public class DockOptionInput
{
    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }
}

/// <summary>机场控制面板状态输出</summary>
public class DockStateOutput
{
    /// <summary>机场 SN</summary>
    public string DockSn { get; set; }

    /// <summary>机场昵称</summary>
    public string DockNick { get; set; }

    /// <summary>机型</summary>
    public string Model { get; set; }

    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>是否在线（依据设备表在线标记）</summary>
    public bool IsOnline { get; set; }

    /// <summary>机场状态码</summary>
    public int? ModeCode { get; set; }

    /// <summary>机场状态名称</summary>
    public string ModeName { get; set; }

    /// <summary>是否处于空闲（可执行重启、格式化等高危操作）</summary>
    public bool IsIdle { get; set; }

    /// <summary>任务阶段码</summary>
    public int? FlighttaskStepCode { get; set; }

    /// <summary>任务阶段名称</summary>
    public string FlighttaskStepName { get; set; }

    /// <summary>舱盖状态</summary>
    public int? CoverState { get; set; }

    /// <summary>舱盖状态名称</summary>
    public string CoverStateName { get; set; }

    /// <summary>推杆状态（Dock 2/3 无推杆指令，仅展示）</summary>
    public int? PutterState { get; set; }

    /// <summary>推杆状态名称</summary>
    public string PutterStateName { get; set; }

    /// <summary>飞行器是否在舱</summary>
    public int? DroneInDock { get; set; }

    /// <summary>飞行器在舱名称</summary>
    public string DroneInDockName { get; set; }

    /// <summary>补光灯状态</summary>
    public int? SupplementLightState { get; set; }

    /// <summary>声光报警状态</summary>
    public int? AlarmState { get; set; }

    /// <summary>电池运行模式</summary>
    public int? BatteryStoreMode { get; set; }

    /// <summary>电池运行模式名称</summary>
    public string BatteryStoreModeName { get; set; }

    /// <summary>空调状态</summary>
    public int? AirConditionerState { get; set; }

    /// <summary>空调状态名称</summary>
    public string AirConditionerStateName { get; set; }

    /// <summary>空调可切换等待时间（秒）</summary>
    public int? AirConditionerSwitchTime { get; set; }

    /// <summary>飞行器充电状态</summary>
    public int? DroneChargeState { get; set; }

    /// <summary>飞行器电量百分比</summary>
    public int? DroneChargePercent { get; set; }

    /// <summary>急停按钮状态</summary>
    public int? EmergencyStopState { get; set; }

    /// <summary>静音模式</summary>
    public int? SilentMode { get; set; }

    /// <summary>图传链路模式</summary>
    public int? SdrLinkWorkmode { get; set; }

    /// <summary>图传链路模式名称</summary>
    public string SdrLinkWorkmodeName { get; set; }

    /// <summary>固件版本</summary>
    public string FirmwareVersion { get; set; }

    /// <summary>固件升级状态</summary>
    public int? FirmwareUpgradeStatus { get; set; }

    /// <summary>累计作业次数</summary>
    public int? JobNumber { get; set; }

    /// <summary>累计运行时长（秒）</summary>
    public long? AccTime { get; set; }

    /// <summary>存储总容量（KB）</summary>
    public long? StorageTotal { get; set; }

    /// <summary>存储已用（KB）</summary>
    public long? StorageUsed { get; set; }

    /// <summary>舱内温度（℃）</summary>
    public double? Temperature { get; set; }

    /// <summary>舱内湿度（%RH）</summary>
    public double? Humidity { get; set; }

    /// <summary>环境温度（℃）</summary>
    public double? EnvironmentTemperature { get; set; }

    /// <summary>风速（m/s）</summary>
    public double? WindSpeed { get; set; }

    /// <summary>降雨量等级</summary>
    public int? Rainfall { get; set; }

    /// <summary>市电电压（V）</summary>
    public int? ElectricSupplyVoltage { get; set; }

    /// <summary>经度</summary>
    public double? Longitude { get; set; }

    /// <summary>纬度</summary>
    public double? Latitude { get; set; }

    /// <summary>活跃告警数（来自 HMS，控制面板直接提示风险）</summary>
    public int ActiveAlarmCount { get; set; }

    /// <summary>其中警告级告警数</summary>
    public int WarningAlarmCount { get; set; }

    /// <summary>OSD 最后接收时间（用于判断数据是否新鲜）</summary>
    public DateTime? OsdReceivedTime { get; set; }

    /// <summary>正在执行中的指令</summary>
    public List<DockCommandRecordOutput> RunningCommands { get; set; } = [];
}

/// <summary>机场可控操作项（前端据此渲染按钮，避免在页面上硬编码规则）</summary>
public class DockActionOptionOutput
{
    /// <summary>协议方法名</summary>
    public string Method { get; set; }

    /// <summary>操作名称</summary>
    public string Name { get; set; }

    /// <summary>分组（舱盖 / 供电 / 飞行器 / 环境 / 通信 / 维护 / 危险）</summary>
    public string Group { get; set; }

    /// <summary>风险等级</summary>
    public DockCommandRiskEnum RiskLevel { get; set; }

    /// <summary>风险等级名称</summary>
    public string RiskName { get; set; }

    /// <summary>是否需要二次确认（风险等级高于普通时为 true）</summary>
    public bool NeedConfirm { get; set; }

    /// <summary>当前是否可执行</summary>
    public bool Available { get; set; }

    /// <summary>不可执行的原因（可直接展示给用户）</summary>
    public string DisabledReason { get; set; }

    /// <summary>
    /// 该操作对应的接口名（前端据此决定把动作派发到哪个方法）。
    /// </summary>
    /// <remarks>
    /// 绝大多数指令走统一的 <c>Execute</c>；RTK 标定因为要额外填经纬高，单独走 <c>RtkCalibration</c>。
    /// 把它显式暴露出来，前端就不需要在前端代码里维护一份「哪些指令是特殊的」的判断。
    /// </remarks>
    public string Api { get; set; } = "Execute";

    /// <summary>
    /// 该操作对应的当前值（用于前端回显 / 高亮当前档位）。
    /// </summary>
    /// <remarks>
    /// 例如空调当前处于制冷模式时返回 1，前端可直接把「制冷」按钮标记为选中。
    /// 无对应状态的指令（如重启、格式化）为 null。
    /// </remarks>
    public int? CurrentValue { get; set; }

    /// <summary>需要用户额外填写的参数名（如 <c>action</c> / <c>imei</c>），为空表示无参</summary>
    public List<string> RequiredFields { get; set; } = [];
}

/// <summary>
/// 机场指令记录输出。
/// </summary>
/// <remarks>
/// 刻意不叫 <c>DockCommandOutput</c>：协议层（<c>Dji.Application.Cloud.Dto.Dock</c>）
/// 已有同名类型表示「<c>events</c> 进度报文的 output 结构」，两者会被同时引入同一文件而冲突。
/// 这里表示的是<b>落库后的指令记录</b>，语义本就不同，分开命名反而更清楚。
/// </remarks>
public class DockCommandRecordOutput
{
    /// <summary>主键</summary>
    public long Id { get; set; }

    /// <summary>机场 SN</summary>
    public string DockSn { get; set; }

    /// <summary>机场昵称</summary>
    public string DockNick { get; set; }

    /// <summary>协议方法名</summary>
    public string Method { get; set; }

    /// <summary>操作名称</summary>
    public string ActionName { get; set; }

    /// <summary>业务 ID</summary>
    public string Bid { get; set; }

    /// <summary>风险等级</summary>
    public DockCommandRiskEnum RiskLevel { get; set; }

    /// <summary>风险等级名称</summary>
    public string RiskName { get; set; }

    /// <summary>执行状态</summary>
    public DockTaskStatusEnum Status { get; set; }

    /// <summary>状态名称</summary>
    public string StatusName { get; set; }

    /// <summary>是否进行中</summary>
    public bool IsRunning { get; set; }

    /// <summary>进度百分比</summary>
    public int? Percent { get; set; }

    /// <summary>当前步骤</summary>
    public string StepKey { get; set; }

    /// <summary>返回码</summary>
    public int? Result { get; set; }

    /// <summary>错误信息</summary>
    public string ErrorMessage { get; set; }

    /// <summary>操作人</summary>
    public string OperatorName { get; set; }

    /// <summary>下发时间</summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>结束时间</summary>
    public DateTime? FinishTime { get; set; }
}

/// <summary>机场下拉项</summary>
public class DockOptionOutput
{
    /// <summary>机场 SN</summary>
    public string Sn { get; set; }

    /// <summary>机场昵称</summary>
    public string Nick { get; set; }

    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>是否在线</summary>
    public bool IsOnline { get; set; }

    /// <summary>机场状态码</summary>
    public int? ModeCode { get; set; }

    /// <summary>机场状态名称</summary>
    public string ModeName { get; set; }

    /// <summary>是否正在执行指令</summary>
    public bool HasRunningCommand { get; set; }

    /// <summary>显示文本</summary>
    public string Label { get; set; }
}

/// <summary>通用枚举字典项（状态 / 风险等级等下拉用）</summary>
public class DockDictOptionOutput
{
    /// <summary>取值</summary>
    public int Value { get; set; }

    /// <summary>显示文本</summary>
    public string Label { get; set; }
}
