// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

namespace Dji.Core.Enum.DjiEnum.Dock;

/// <summary>
/// 机场舱盖状态。
/// </summary>
/// <remarks>
/// 取值与物模型属性 <c>cover_state</c> 一致（OSD 定频上报）。
/// 下发 <c>cover_open</c> / <c>cover_close</c> 前用它做前置判断，
/// 避免对已在目标位置的舱盖重复下发（机场会返回错误）。
/// </remarks>
public enum DockCoverStateEnum
{
    /// <summary>关闭</summary>
    [Description("关闭")]
    Closed = 0,

    /// <summary>打开</summary>
    [Description("打开")]
    Opened = 1,

    /// <summary>半开（运动中被中断，需人工确认）</summary>
    [Description("半开")]
    HalfOpen = 2,

    /// <summary>状态异常</summary>
    [Description("状态异常")]
    Abnormal = 3
}

/// <summary>
/// 机场推杆状态。
/// </summary>
/// <remarks>
/// 取值与物模型属性 <c>putter_state</c> 一致。
/// <b>注意</b>：推杆的开关指令（<c>putter_open</c> / <c>putter_close</c>）只在
/// Dock 1 章节列出，Dock 2 / Dock 3 章节均已移除 —— 新机型推杆由机场自动控制，
/// 因此本项目<b>不提供</b>推杆指令，仅展示状态供运维判断。
/// </remarks>
public enum DockPutterStateEnum
{
    /// <summary>闭合</summary>
    [Description("闭合")]
    Closed = 0,

    /// <summary>展开</summary>
    [Description("展开")]
    Opened = 1,

    /// <summary>半开</summary>
    [Description("半开")]
    HalfOpen = 2,

    /// <summary>状态异常</summary>
    [Description("状态异常")]
    Abnormal = 3
}

/// <summary>
/// 机场工作状态（<c>mode_code</c>）。
/// </summary>
/// <remarks>
/// 这是控制面板最关键的互斥判据：<b>只有 <see cref="Idle"/> 时才允许执行
/// 重启、格式化、固件升级这类会打断作业的操作</b>；
/// <see cref="Working"/> 时下发会被机场以「业务繁忙」拒绝。
/// </remarks>
public enum DockModeCodeEnum
{
    /// <summary>空闲中</summary>
    [Description("空闲中")]
    Idle = 0,

    /// <summary>现场调试</summary>
    [Description("现场调试")]
    OnSiteDebug = 1,

    /// <summary>远程调试</summary>
    [Description("远程调试")]
    RemoteDebug = 2,

    /// <summary>固件升级中</summary>
    [Description("固件升级中")]
    FirmwareUpgrading = 3,

    /// <summary>作业中</summary>
    [Description("作业中")]
    Working = 4,

    /// <summary>待标定</summary>
    [Description("待标定")]
    WaitCalibration = 5
}

/// <summary>
/// 机场任务阶段（<c>flighttask_step_code</c>）。
/// </summary>
/// <remarks>用于在控制面板上区分「机场空闲」与「机场正在为任务做准备」。</remarks>
public enum DockTaskStepEnum
{
    /// <summary>作业准备中</summary>
    [Description("作业准备中")]
    Preparing = 0,

    /// <summary>飞行作业中</summary>
    [Description("飞行作业中")]
    Flying = 1,

    /// <summary>作业后状态恢复</summary>
    [Description("作业后状态恢复")]
    Recovering = 2,

    /// <summary>自定义飞行区更新中</summary>
    [Description("自定义飞行区更新中")]
    FlightAreaUpdating = 3,

    /// <summary>地形障碍物更新中</summary>
    [Description("地形障碍物更新中")]
    TerrainUpdating = 4,

    /// <summary>任务空闲</summary>
    [Description("任务空闲")]
    Idle = 5,

    /// <summary>飞行器异常</summary>
    [Description("飞行器异常")]
    DroneAbnormal = 255,

    /// <summary>未知状态</summary>
    [Description("未知状态")]
    Unknown = 256
}

/// <summary>
/// 开关类属性的通用「开 / 关」取值。
/// </summary>
/// <remarks>
/// 声光报警（<c>alarm_state</c>）、补光灯（<c>supplement_light_state</c>）、
/// 备用电池开关等共用同一套 0/1 语义，故合并为一个枚举。
/// </remarks>
public enum SwitchStateEnum
{
    /// <summary>关闭</summary>
    [Description("关闭")]
    Off = 0,

    /// <summary>开启</summary>
    [Description("开启")]
    On = 1
}

/// <summary>
/// 机场空调工作模式（下发参数）。
/// </summary>
/// <remarks>
/// 这是 <c>air_conditioner_mode_switch</c> 的 <c>action</c> 取值，<b>只有 4 个值</b>。
/// 别把上报状态 <c>air_conditioner.air_conditioner_state</c>（0~15，含各种「准备中 / 退出中」过渡态）
/// 与之混用 —— 后者只读，不可直接回填到下发参数里。
/// </remarks>
public enum DockAirConditionerModeEnum
{
    /// <summary>空闲模式（关闭制冷、制热或除湿）</summary>
    [Description("空闲模式")]
    Idle = 0,

    /// <summary>制冷模式</summary>
    [Description("制冷模式")]
    Cooling = 1,

    /// <summary>制热模式</summary>
    [Description("制热模式")]
    Heating = 2,

    /// <summary>除湿模式</summary>
    [Description("除湿模式")]
    Dehumidify = 3
}

/// <summary>
/// 电池运行模式（<c>battery_store_mode</c>）。
/// </summary>
/// <remarks>
/// <b>注意从 1 开始</b>（协议无 0）：
/// 计划模式无任务时电池保持 55%~60%，寿命较长，适合规律作业；
/// 待命模式保持 90%~95%，寿命较短但出勤快，适合应急场景。
/// </remarks>
public enum DockBatteryStoreModeEnum
{
    /// <summary>计划模式（电量保持 55%~60%）</summary>
    [Description("计划模式")]
    Plan = 1,

    /// <summary>待命模式（电量保持 90%~95%）</summary>
    [Description("待命模式")]
    Standby = 2
}

/// <summary>
/// 增强图传链路模式（<c>sdr_workmode_switch.link_workmode</c>）。
/// </summary>
/// <remarks>
/// 切到 4G 增强模式后 SDR 与 4G 会同时工作。
/// 注意：<b>飞行器增强图传开启时无法升级固件</b>，固件升级前会由设备侧主动关闭。
/// </remarks>
public enum SdrLinkWorkmodeEnum
{
    /// <summary>仅使用 SDR</summary>
    [Description("仅使用 SDR")]
    SdrOnly = 0,

    /// <summary>4G 增强模式（SDR 与 4G 同时使用）</summary>
    [Description("4G 增强模式")]
    Enhanced4G = 1
}

/// <summary>
/// eSIM 运营商（<c>esim_operator_switch.esim_operator</c>）。
/// </summary>
/// <remarks>仅 Dock 2 / Dock 3 支持。</remarks>
public enum EsimOperatorEnum
{
    /// <summary>中国移动</summary>
    [Description("中国移动")]
    Mobile = 1,

    /// <summary>中国联通</summary>
    [Description("中国联通")]
    Unicom = 2,

    /// <summary>中国电信</summary>
    [Description("中国电信")]
    Telecom = 3
}

/// <summary>
/// SIM 卡槽（<c>sim_slot_switch.sim_slot</c>）。
/// </summary>
/// <remarks>仅 Dock 2 / Dock 3 支持。</remarks>
public enum SimSlotEnum
{
    /// <summary>实体 SIM 卡</summary>
    [Description("实体 SIM 卡")]
    Physical = 1,

    /// <summary>eSIM</summary>
    [Description("eSIM")]
    Esim = 2
}

/// <summary>
/// 机场设备控制指令 / 任务的执行状态。
/// </summary>
/// <remarks>
/// 取值与协议里 <c>output.status</c> 的字符串枚举一致（机场控制、固件升级、RTK 标定共用）。
/// 平台内部另外补了 <see cref="Sending"/> 表示「指令已下发、尚未收到首个回包」——
/// 这个中间态协议里没有，但前端需要它来区分「没反应」和「设备明确拒绝」。
/// </remarks>
public enum DockTaskStatusEnum
{
    /// <summary>下发中（平台内部态，等待 services_reply）</summary>
    [Description("下发中")]
    Sending = -1,

    /// <summary>已下发</summary>
    [Description("已下发")]
    Sent = 0,

    /// <summary>执行中</summary>
    [Description("执行中")]
    InProgress = 1,

    /// <summary>执行成功</summary>
    [Description("执行成功")]
    Ok = 2,

    /// <summary>失败</summary>
    [Description("失败")]
    Failed = 3,

    /// <summary>取消或终止</summary>
    [Description("取消或终止")]
    Canceled = 4,

    /// <summary>暂停</summary>
    [Description("暂停")]
    Paused = 5,

    /// <summary>拒绝</summary>
    [Description("拒绝")]
    Rejected = 6,

    /// <summary>超时</summary>
    [Description("超时")]
    Timeout = 7
}

/// <summary>
/// 协议 <c>output.status</c> 字符串 ↔ 平台状态的转换。
/// </summary>
/// <remarks>
/// 协议给的是字符串（<c>"in_progress"</c> 等），不是整数，因此无法直接用 <c>Enum.Parse</c>
/// 到 <see cref="DockTaskStatusEnum"/>（后者的数值是平台自定的）。这里集中做映射。
/// </remarks>
public static class DockTaskStatusResolver
{
    /// <summary>把协议字符串转成平台状态；未知值返回 <see cref="DockTaskStatusEnum.InProgress"/>。</summary>
    public static DockTaskStatusEnum Parse(string status)
    {
        return status switch
        {
            "sent" => DockTaskStatusEnum.Sent,
            "in_progress" => DockTaskStatusEnum.InProgress,
            "ok" => DockTaskStatusEnum.Ok,
            "failed" => DockTaskStatusEnum.Failed,
            "canceled" => DockTaskStatusEnum.Canceled,
            "paused" => DockTaskStatusEnum.Paused,
            "rejected" => DockTaskStatusEnum.Rejected,
            "timeout" => DockTaskStatusEnum.Timeout,
            // 设备给了没见过的状态时按「执行中」处理：宁可让前端多等，也不要误判为终态而提前收口
            _ => DockTaskStatusEnum.InProgress
        };
    }

    /// <summary>是否为终态（不会再有后续进度推送）。</summary>
    public static bool IsFinal(this DockTaskStatusEnum status)
        => status is DockTaskStatusEnum.Ok or DockTaskStatusEnum.Failed
            or DockTaskStatusEnum.Canceled or DockTaskStatusEnum.Rejected
            or DockTaskStatusEnum.Timeout;
}

/// <summary>
/// 机场空调工作状态（<c>air_conditioner.air_conditioner_state</c>，<b>只读</b>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>必须与 <see cref="DockAirConditionerModeEnum"/> 分开</b>：后者是下发参数（只有 4 个取值），
/// 本枚举是设备上报的实际状态（16 个取值，含大量过渡态）。
/// 直接把上报值回填进下发参数会踩坑 —— 例如设备报 <c>4</c>（制冷退出模式），
/// 而下发参数里 <c>4</c> 是非法值，机场会直接报错。
/// </para>
/// <para>
/// 过渡态（4~15）说明空调正在切换过程中，此时<b>不要再下发新的模式切换</b>，
/// 应等 <c>air_conditioner.switch_time</c> 倒计时结束。
/// </para>
/// </remarks>
public enum DockAirConditionerStateEnum
{
    /// <summary>空闲模式（无制冷、制热、除湿等）</summary>
    [Description("空闲模式")]
    Idle = 0,

    /// <summary>制冷模式（稳态）</summary>
    [Description("制冷模式")]
    Cooling = 1,

    /// <summary>制热模式（稳态）</summary>
    [Description("制热模式")]
    Heating = 2,

    /// <summary>除湿模式（稳态）</summary>
    [Description("除湿模式")]
    Dehumidify = 3,

    /// <summary>制冷退出模式（过渡态）</summary>
    [Description("制冷退出中")]
    CoolingExiting = 4,

    /// <summary>制热退出模式（过渡态）</summary>
    [Description("制热退出中")]
    HeatingExiting = 5,

    /// <summary>除湿退出模式（过渡态）</summary>
    [Description("除湿退出中")]
    DehumidifyExiting = 6,

    /// <summary>制冷准备模式（过渡态）</summary>
    [Description("制冷准备中")]
    CoolingPreparing = 7,

    /// <summary>制热准备模式（过渡态）</summary>
    [Description("制热准备中")]
    HeatingPreparing = 8,

    /// <summary>除湿准备模式（过渡态）</summary>
    [Description("除湿准备中")]
    DehumidifyPreparing = 9,

    /// <summary>风冷准备中（过渡态，仅 Dock 2 / Dock 3）</summary>
    [Description("风冷准备中")]
    AirCoolingPreparing = 10,

    /// <summary>风冷中（稳态，仅 Dock 2 / Dock 3）</summary>
    [Description("风冷中")]
    AirCooling = 11,

    /// <summary>风冷退出中（过渡态，仅 Dock 2 / Dock 3）</summary>
    [Description("风冷退出中")]
    AirCoolingExiting = 12,

    /// <summary>除雾准备中（过渡态，仅 Dock 2 / Dock 3）</summary>
    [Description("除雾准备中")]
    DefogPreparing = 13,

    /// <summary>除雾中（稳态，仅 Dock 2 / Dock 3）</summary>
    [Description("除雾中")]
    Defogging = 14,

    /// <summary>除雾退出中（过渡态，仅 Dock 2 / Dock 3）</summary>
    [Description("除雾退出中")]
    DefogExiting = 15
}

/// <summary>
/// 飞行器是否在舱（<c>drone_in_dock</c>）。
/// </summary>
/// <remarks>
/// 强制关闭舱盖（<c>cover_force_close</c>）的唯一前置条件：只有在<b>舱外</b>时才允许强制关舱盖，
/// 否则可能夹伤桨叶。
/// </remarks>
public enum DroneInDockEnum
{
    /// <summary>舱外</summary>
    [Description("舱外")]
    Outside = 0,

    /// <summary>舱内</summary>
    [Description("舱内")]
    Inside = 1
}

/// <summary>
/// 飞行器充电状态（<c>drone_charge_state.state</c>）。
/// </summary>
public enum DroneChargeStateEnum
{
    /// <summary>空闲（未充电）</summary>
    [Description("空闲")]
    Idle = 0,

    /// <summary>充电中</summary>
    [Description("充电中")]
    Charging = 1
}

/// <summary>
/// 机场固件升级状态（<c>firmware_upgrade_status</c>，只读）。
/// </summary>
/// <remarks>
/// 与固件升级任务的状态不是一回事：这里只表示「当前是否处于升级过程中」，
/// 且是 OSD 定频上报的。升级任务本身的进度见 <c>dji_ota_task</c>。
/// </remarks>
public enum DockFirmwareUpgradeStatusEnum
{
    /// <summary>未升级</summary>
    [Description("未升级")]
    NotUpgrading = 0,

    /// <summary>升级中</summary>
    [Description("升级中")]
    Upgrading = 1
}

/// <summary>
/// 机场静音模式（<c>silent_mode</c>）。
/// </summary>
/// <remarks>
/// 开启静音会带来三个副作用（官方提示）：风扇转速降低导致制冷能力下降、
/// 蜂鸣器关闭（关舱盖时需注意周边安全）、待机指示灯熄灭。
/// </remarks>
public enum DockSilentModeEnum
{
    /// <summary>非静音模式</summary>
    [Description("非静音模式")]
    Off = 0,

    /// <summary>静音模式</summary>
    [Description("静音模式")]
    On = 1
}

/// <summary>
/// 降雨量等级（<c>rainfall</c>）。
/// </summary>
public enum DockRainfallEnum
{
    /// <summary>无雨</summary>
    [Description("无雨")]
    None = 0,

    /// <summary>小雨</summary>
    [Description("小雨")]
    Light = 1,

    /// <summary>中雨</summary>
    [Description("中雨")]
    Moderate = 2,

    /// <summary>大雨</summary>
    [Description("大雨")]
    Heavy = 3
}

/// <summary>
/// 机场控制指令的风险等级（平台内部态，用于前端二次确认）。
/// </summary>
/// <remarks>
/// 只有 <see cref="Dangerous"/> 与 <see cref="Caution"/> 会在前端触发二次确认弹窗。
/// 之所以在<b>服务端</b>而不是前端定义风险等级：风险判断属于业务规则，
/// 前端各页面（控制面板、批量运维）都要用，集中一处可避免各页面口径不一致。
/// </remarks>
public enum DockCommandRiskEnum
{
    /// <summary>普通操作（开合舱盖、开关补光灯等，可随时执行）</summary>
    [Description("普通")]
    Normal = 0,

    /// <summary>需谨慎（会中断潜在作业，如机场重启、强制关舱盖）</summary>
    [Description("需谨慎")]
    Caution = 1,

    /// <summary>危险且不可逆（格式化存储，数据无法找回）</summary>
    [Description("危险")]
    Dangerous = 2
}
