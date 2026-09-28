// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Core.Enum.DjiEnum.Hms;

namespace Dji.Core.Entity.DjiEntity;

/// <summary>
/// HMS 健康告警（设备自诊断结果）。
/// </summary>
/// <remarks>
/// <para>
/// <b>写入来源</b>：机场上行 <c>thing/product/{sn}/events</c> 的 <c>hms</c>，由 <c>MqHmsService</c> 落库。
/// </para>
/// <para>
/// <b>核心语义：报文的 <c>data.list</c> 是「当前全部活跃告警」的全量快照</b>
/// （官方原文：「若上一次 HMS 上报中的告警，在本次 HMS 上报中消失了，则说明该告警解除了」）。
/// 因此落库不是简单的追加，而是三件事：
/// ① 本次出现的 upsert 为 <see cref="HmsAlarmStatusEnum.Active"/> 并刷新 <see cref="LastTime"/>；
/// ② 该机场名下「活跃但本次未出现」的置为 <see cref="HmsAlarmStatusEnum.Recovered"/> 并记录恢复时刻；
/// ③ 已恢复的记录<b>不删除</b> —— 间歇性故障的现场排查高度依赖「它曾经来过、持续了多久」。
/// </para>
/// <para>
/// <b>为什么唯一键要带上 component_index / sensor_index</b>：同一个告警码会在不同部件上同时出现
/// （例如左右两块电池温度过高、前后两根充电杆异常），只按 <c>code</c> 去重会丢信息。
/// 协议里这两个值藏在 <c>args</c> 对象中，这里拆成独立列，既保证唯一性也便于按部件筛选。
/// </para>
/// </remarks>
[SugarTable(null, "HMS健康告警")]
[SugarIndex("index_DjiHmsAlarm_Unique", nameof(DockSn), OrderByType.Asc, nameof(Code), OrderByType.Asc,
    nameof(DeviceType), OrderByType.Asc, nameof(ComponentIndex), OrderByType.Asc, nameof(SensorIndex), OrderByType.Asc, true)]
[SugarIndex("index_DjiHmsAlarm_DockStatus", nameof(DockSn), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[SugarIndex("index_DjiHmsAlarm_LevelTime", nameof(Level), OrderByType.Asc, nameof(LastTime), OrderByType.Desc)]
public class DjiHmsAlarm : EntityWorkspaceBase
{
    /// <summary>网关（机场）SN</summary>
    [SugarColumn(ColumnDescription = "机场SN", Length = 64, IsNullable = false)]
    public string DockSn { get; set; }

    /// <summary>告警来源设备的产品枚举值（协议 <c>device_type</c>，如 <c>0-67-0</c>）</summary>
    /// <remarks><c>{domain}-{type}-{sub_type}</c>，首位为 3 表示机场本体、0 表示飞行器。</remarks>
    [SugarColumn(ColumnDescription = "设备型号", Length = 32, IsNullable = true)]
    public string DeviceType { get; set; }

    /// <summary>告警来源设备 SN（协议未随报文给出，由 <see cref="DeviceType"/> 与拓扑关系推断，可能为空）</summary>
    [SugarColumn(ColumnDescription = "设备SN", Length = 64, IsNullable = true)]
    public string DeviceSn { get; set; }

    /// <summary>告警码（协议 <c>code</c>，形如 <c>0x16100083</c>，是查询文案的 Key 的核心部分）</summary>
    [SugarColumn(ColumnDescription = "告警码", Length = 32, IsNullable = false)]
    public string Code { get; set; }

    /// <summary>告警等级</summary>
    [SugarColumn(ColumnDescription = "告警等级", IsNullable = true)]
    public HmsLevelEnum Level { get; set; }

    /// <summary>业务模块。飞行任务 / 媒体类的告警用于在对应页面做交叉提示</summary>
    [SugarColumn(ColumnDescription = "所属模块", IsNullable = true)]
    public HmsModuleEnum Module { get; set; }

    /// <summary>上报时刻设备是否在空中（0 在地上 / 1 在天上）</summary>
    /// <remarks>这个值参与飞行器告警文案 Key 的拼接（见 <c>HmsTextService</c>），不能丢。</remarks>
    [SugarColumn(ColumnDescription = "是否在飞", IsNullable = true)]
    public int InTheSky { get; set; }

    /// <summary>
    /// 是否为「及时性」告警（1 是 / 0 否）。
    /// </summary>
    /// <remarks>
    /// 官方举例：风过大属于及时性告警，风力减小后会<b>自动消失</b>。
    /// 这类告警不需要运维介入，前端默认折叠，避免淹没真正需要处理的故障。
    /// </remarks>
    [SugarColumn(ColumnDescription = "是否及时性", IsNullable = true)]
    public int Imminent { get; set; }

    /// <summary>部件索引（协议 <c>args.component_index</c>，从 0 开始，展示时 +1）</summary>
    [SugarColumn(ColumnDescription = "部件索引", IsNullable = true)]
    public int ComponentIndex { get; set; }

    /// <summary>传感器索引（协议 <c>args.sensor_index</c>，从 0 开始，展示时 +1）</summary>
    [SugarColumn(ColumnDescription = "传感器索引", IsNullable = true)]
    public int SensorIndex { get; set; }

    /// <summary><c>args</c> 对象原文。协议未来新增占位参数时无需改表</summary>
    [SugarColumn(ColumnDescription = "参数原文", ColumnDataType = "TEXT", IsNullable = true)]
    public string ArgsJson { get; set; }

    /// <summary>中文告警文案（由 <c>hms.json</c> 查表并回填占位符得到；查不到时为空）</summary>
    [SugarColumn(ColumnDescription = "中文文案", Length = 1024, IsNullable = true)]
    public string TextZh { get; set; }

    /// <summary>英文告警文案（同 <see cref="TextZh"/>）</summary>
    [SugarColumn(ColumnDescription = "英文文案", Length = 1024, IsNullable = true)]
    public string TextEn { get; set; }

    /// <summary>活跃状态（由两次报文比对推导，协议本身不给）</summary>
    [SugarColumn(ColumnDescription = "告警状态", IsNullable = true)]
    public HmsAlarmStatusEnum Status { get; set; }

    /// <summary>首次出现时间（该告警第一次被上报的时刻）</summary>
    [SugarColumn(ColumnDescription = "首次出现", IsNullable = true)]
    public DateTime? FirstTime { get; set; }

    /// <summary>最近一次上报时间（每次仍然活跃都会刷新，用于判断告警是否还在持续）</summary>
    [SugarColumn(ColumnDescription = "最近上报", IsNullable = true)]
    public DateTime? LastTime { get; set; }

    /// <summary>恢复时间（本次快照中消失的时刻；仍活跃时为空）</summary>
    [SugarColumn(ColumnDescription = "恢复时间", IsNullable = true)]
    public DateTime? RecoverTime { get; set; }

    /// <summary>报文时间戳（毫秒，设备时钟）</summary>
    [SugarColumn(ColumnDescription = "上报时间戳", IsNullable = true)]
    public long ReportTimestamp { get; set; }
}
