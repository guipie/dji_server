// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

namespace Dji.Core.Entity.DjiEntity;

/// <summary>
/// 机场状态快照（每机场一行，设备上报状态变化时局部覆盖）。
/// </summary>
/// <remarks>
/// <para>
/// <b>数据来源</b>：物模型<b>状态</b>主题 <c>thing/product/{sn}/state</c>。
/// 这是唯一携带 <c>live_capacity</c> / <c>live_status</c> 的报文（OSD 定频报文里没有），
/// 且设备<b>只在变化时推送</b>，因此必须落库 —— 靠订阅回调的瞬时值无法支撑页面刷新。
/// </para>
/// <para>
/// <b>关键约束：机场的 state 是「分多条推送」的</b>（官方「设备属性推送」章节明确说明）。
/// 同一次状态变化可能拆成「只有 live_capacity 的一条」「只有 live_status 的一条」。
/// 因此每列都必须允许为空，写入时<b>逐字段判空后局部更新</b>，
/// 绝不能用一条报文的完整 DTO 覆盖整行 —— 否则会把上一次拿到的 live_capacity 冲成 null。
/// </para>
/// <para>
/// <b>为什么存 JSON 而不拆子表</b>：<c>live_capacity</c> 是三层嵌套（设备 → 相机 → 码流）的
/// 只读能力描述，云端从不按子结构查询，只整份下发给前端渲染通道树。拆表会引入 3 张表与级联维护成本，
/// 收益为零。若将来要做「跨机场码流检索」，那时再拆不迟。
/// </para>
/// <para>
/// <b>与 DjiLiveStream 的分工</b>：本表是设备<b>声明</b>的能力与在播状态（原始真值），
/// <see cref="DjiLiveStream"/> 是平台<b>发起的</b>会话（含推流地址、操作人）。两者互为校验：
/// 若会话是「直播中」但 <see cref="LiveStatusJson"/> 里没有对应 video_id，说明设备侧已断流。
/// </para>
/// </remarks>
[SugarTable(null, "机场状态快照")]
[SugarIndex("index_DjiDockState_DockSn", nameof(DockSn), OrderByType.Asc, true)]
public class DjiDockState : EntityAppBase
{
    /// <summary>机场 SN</summary>
    [SugarColumn(ColumnDescription = "机场SN", Length = 64, IsNullable = false)]
    public string DockSn { get; set; }

    /// <summary>可选择推流的码流总数（<c>live_capacity.available_video_number</c>）</summary>
    [SugarColumn(ColumnDescription = "可选码流数", IsNullable = true)]
    public int? AvailableVideoNumber { get; set; }

    /// <summary>可同时推流的最大码流数（<c>live_capacity.coexist_video_number_max</c>），开播前置校验要用</summary>
    [SugarColumn(ColumnDescription = "最大并发路数", IsNullable = true)]
    public int? CoexistVideoNumberMax { get; set; }

    /// <summary>完整直播能力树 JSON（协议 <c>live_capacity</c> 原文，含 device_list/camera_list/video_list）</summary>
    [SugarColumn(ColumnDescription = "直播能力树", ColumnDataType = "TEXT", IsNullable = true)]
    public string CapacityJson { get; set; }

    /// <summary>当前整体直播状态 JSON（协议 <c>live_status</c> 数组原文）</summary>
    [SugarColumn(ColumnDescription = "直播状态", ColumnDataType = "TEXT", IsNullable = true)]
    public string LiveStatusJson { get; set; }

    /// <summary>待上传媒体文件数量（<c>media_file_detail.remain_upload</c>）</summary>
    [SugarColumn(ColumnDescription = "待上传媒体数", IsNullable = true)]
    public int? RemainUpload { get; set; }

    /// <summary>设备上报该状态的时间（取协议 <c>timestamp</c>，毫秒）</summary>
    [SugarColumn(ColumnDescription = "上报时间戳", IsNullable = true)]
    public long ReportTimestamp { get; set; }

    /// <summary>
    /// 本地接收时间（云端收到该 state 报文并落库的时刻）。
    /// </summary>
    /// <remarks>
    /// 刻意不叫 <c>UpdateTime</c>：基类 <c>EntityBase.UpdateTime</c> 带
    /// <c>IsOnlyIgnoreInsert</c>（插入时不写入），而快照表恰恰需要「首次上报即记录接收时间」，
    /// 复用它会导致首行的时间为空，同时造成成员隐藏（CS0114）。故独立命名。
    /// </remarks>
    [SugarColumn(ColumnDescription = "接收时间", IsNullable = true)]
    public DateTime? ReceivedTime { get; set; }

    #region 以下字段来自 OSD 定频报文（thing/product/{sn}/osd）

    /// <summary>
    /// 完整 OSD 报文原文（JSON）。
    /// </summary>
    /// <remarks>
    /// OSD 字段有上百个（固件版本、搜星、环境、电气、备份电池、保养……），
    /// 其中绝大多数只用于展示、从不参与查询。与其把它们全部展开成列（表宽失控、每次改协议都要加列），
    /// 不如把原文整份存下来，由前端按需读取；下面只把<b>控制面板与运维判断真正要用</b>的字段抽成列。
    /// </remarks>
    [SugarColumn(ColumnDescription = "OSD原文", ColumnDataType = "TEXT", IsNullable = true)]
    public string OsdJson { get; set; }

    /// <summary>机场工作状态（<c>mode_code</c>）：判断能否执行重启/格式化/升级的关键互斥量</summary>
    [SugarColumn(ColumnDescription = "机场状态码", IsNullable = true)]
    public int? ModeCode { get; set; }

    /// <summary>机场任务阶段（<c>flighttask_step_code</c>）</summary>
    [SugarColumn(ColumnDescription = "任务阶段码", IsNullable = true)]
    public int? FlighttaskStepCode { get; set; }

    /// <summary>舱盖状态（<c>cover_state</c>）</summary>
    [SugarColumn(ColumnDescription = "舱盖状态", IsNullable = true)]
    public int? CoverState { get; set; }

    /// <summary>推杆状态（<c>putter_state</c>）。Dock 2/3 无推杆指令，仅作运维参考</summary>
    [SugarColumn(ColumnDescription = "推杆状态", IsNullable = true)]
    public int? PutterState { get; set; }

    /// <summary>飞行器是否在舱（<c>drone_in_dock</c>）：强制关舱盖前必须先确认为 0</summary>
    [SugarColumn(ColumnDescription = "飞行器在舱", IsNullable = true)]
    public int? DroneInDock { get; set; }

    /// <summary>补光灯状态（<c>supplement_light_state</c>）</summary>
    [SugarColumn(ColumnDescription = "补光灯状态", IsNullable = true)]
    public int? SupplementLightState { get; set; }

    /// <summary>电池运行模式（<c>battery_store_mode</c>）</summary>
    [SugarColumn(ColumnDescription = "电池运行模式", IsNullable = true)]
    public int? BatteryStoreMode { get; set; }

    /// <summary>声光报警状态（<c>alarm_state</c>）</summary>
    [SugarColumn(ColumnDescription = "声光报警状态", IsNullable = true)]
    public int? AlarmState { get; set; }

    /// <summary>空调工作状态（<c>air_conditioner.air_conditioner_state</c>，0~15 含过渡态）</summary>
    [SugarColumn(ColumnDescription = "空调状态", IsNullable = true)]
    public int? AirConditionerState { get; set; }

    /// <summary>空调剩余可切换等待时间（秒，<c>air_conditioner.switch_time</c>）</summary>
    [SugarColumn(ColumnDescription = "空调切换等待", IsNullable = true)]
    public int? AirConditionerSwitchTime { get; set; }

    /// <summary>飞行器充电状态（<c>drone_charge_state.state</c>，0 空闲 / 1 充电中）</summary>
    [SugarColumn(ColumnDescription = "充电状态", IsNullable = true)]
    public int? DroneChargeState { get; set; }

    /// <summary>飞行器电池电量百分比（<c>drone_charge_state.capacity_percent</c>）</summary>
    [SugarColumn(ColumnDescription = "电量百分比", IsNullable = true)]
    public int? DroneChargePercent { get; set; }

    /// <summary>紧急停止按钮状态（<c>emergency_stop_state</c>）：按下时一切自动动作都会中止</summary>
    [SugarColumn(ColumnDescription = "急停按钮", IsNullable = true)]
    public int? EmergencyStopState { get; set; }

    /// <summary>静音模式（<c>silent_mode</c>）</summary>
    [SugarColumn(ColumnDescription = "静音模式", IsNullable = true)]
    public int? SilentMode { get; set; }

    /// <summary>图传链路模式（<c>wireless_link.link_workmode</c>，0 仅 SDR / 1 4G 融合）</summary>
    [SugarColumn(ColumnDescription = "图传模式", IsNullable = true)]
    public int? SdrLinkWorkmode { get; set; }

    /// <summary>机场固件版本（<c>firmware_version</c>）</summary>
    [SugarColumn(ColumnDescription = "固件版本", Length = 64, IsNullable = true)]
    public string FirmwareVersion { get; set; }

    /// <summary>固件升级状态（<c>firmware_upgrade_status</c>，0 未升级 / 1 升级中）</summary>
    [SugarColumn(ColumnDescription = "固件升级状态", IsNullable = true)]
    public int? FirmwareUpgradeStatus { get; set; }

    /// <summary>子设备（飞行器）状态原文 JSON（对频状态、开机状态）</summary>
    [SugarColumn(ColumnDescription = "子设备状态", ColumnDataType = "TEXT", IsNullable = true)]
    public string SubDeviceJson { get; set; }

    /// <summary>搜星状态原文 JSON（标定 / 收敛 / 档位 / 星数）</summary>
    [SugarColumn(ColumnDescription = "搜星状态", ColumnDataType = "TEXT", IsNullable = true)]
    public string PositionStateJson { get; set; }

    /// <summary>网络状态原文 JSON（类型 / 质量 / 速率）</summary>
    [SugarColumn(ColumnDescription = "网络状态", ColumnDataType = "TEXT", IsNullable = true)]
    public string NetworkStateJson { get; set; }

    /// <summary>机场累计作业次数</summary>
    [SugarColumn(ColumnDescription = "累计作业次数", IsNullable = true)]
    public int? JobNumber { get; set; }

    /// <summary>机场累计运行时长（秒）</summary>
    [SugarColumn(ColumnDescription = "累计运行时长", IsNullable = true)]
    public long? AccTime { get; set; }

    /// <summary>存储总容量（KB）</summary>
    [SugarColumn(ColumnDescription = "存储总容量", IsNullable = true)]
    public long? StorageTotal { get; set; }

    /// <summary>存储已用容量（KB）</summary>
    [SugarColumn(ColumnDescription = "存储已用", IsNullable = true)]
    public long? StorageUsed { get; set; }

    /// <summary>舱内温度（℃）</summary>
    [SugarColumn(ColumnDescription = "舱内温度", IsNullable = true)]
    public double? Temperature { get; set; }

    /// <summary>舱内湿度（%RH）</summary>
    [SugarColumn(ColumnDescription = "舱内湿度", IsNullable = true)]
    public double? Humidity { get; set; }

    /// <summary>环境温度（℃）</summary>
    [SugarColumn(ColumnDescription = "环境温度", IsNullable = true)]
    public double? EnvironmentTemperature { get; set; }

    /// <summary>风速（m/s）</summary>
    [SugarColumn(ColumnDescription = "风速", IsNullable = true)]
    public double? WindSpeed { get; set; }

    /// <summary>降雨量等级（0 无雨 ~ 3 大雨）</summary>
    [SugarColumn(ColumnDescription = "降雨量", IsNullable = true)]
    public int? Rainfall { get; set; }

    /// <summary>市电电压（V）</summary>
    [SugarColumn(ColumnDescription = "市电电压", IsNullable = true)]
    public int? ElectricSupplyVoltage { get; set; }

    /// <summary>工作电压（mV）</summary>
    [SugarColumn(ColumnDescription = "工作电压", IsNullable = true)]
    public int? WorkingVoltage { get; set; }

    /// <summary>工作电流（mA）</summary>
    [SugarColumn(ColumnDescription = "工作电流", IsNullable = true)]
    public double? WorkingCurrent { get; set; }

    /// <summary>机场经度（WGS84）</summary>
    [SugarColumn(ColumnDescription = "经度", IsNullable = true)]
    public double? Longitude { get; set; }

    /// <summary>机场纬度（WGS84）</summary>
    [SugarColumn(ColumnDescription = "纬度", IsNullable = true)]
    public double? Latitude { get; set; }

    /// <summary>机场椭球高度（米）</summary>
    [SugarColumn(ColumnDescription = "高度", IsNullable = true)]
    public double? Height { get; set; }

    /// <summary>OSD 报文时间戳（毫秒，设备时钟）</summary>
    [SugarColumn(ColumnDescription = "OSD时间戳", IsNullable = true)]
    public long OsdTimestamp { get; set; }

    /// <summary>OSD 落库时刻</summary>
    [SugarColumn(ColumnDescription = "OSD接收时间", IsNullable = true)]
    public DateTime? OsdReceivedTime { get; set; }

    #endregion
}
