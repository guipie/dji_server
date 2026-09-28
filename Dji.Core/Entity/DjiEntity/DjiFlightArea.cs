// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Core.Enum.DjiEnum.Ops;

namespace Dji.Core.Entity.DjiEntity;

/// <summary>
/// 自定义飞行区文件（云端下发给设备、用于圈定可飞行的作业区域）。
/// </summary>
/// <remarks>
/// <para>
/// <b>链路是设备驱动的</b>：云端先在文件存储里准备好 <c>geofence_*.json</c>，然后下发
/// <c>flight_areas_update</c> 通知设备。设备需要时会主动用 <c>flight_areas_get</c>
/// （<b>requests 主题</b>）来要地址，云端回 <c>requests_reply</c> 给出带签名的下载 URL。
/// 设备下载并启用后，持续用 <c>flight_areas_sync_progress</c> 汇报状态。
/// 因此本表既记录「云端发布了哪些文件」，也记录「设备同步到哪一步了」。
/// </para>
/// <para>
/// <b>checksum 是唯一性的关键</b>：文件名会重复使用（<c>geofence_xxx.json</c>），
/// 判断设备上跑的到底是不是最新版，靠的是 <c>SHA256</c> 摘要而不是文件名，故对其建唯一索引。
/// </para>
/// </remarks>
[SugarTable(null, "自定义飞行区文件")]
[SugarIndex("index_DjiFlightArea_Unique", nameof(DockSn), OrderByType.Asc, nameof(Checksum), OrderByType.Asc, true)]
[SugarIndex("index_DjiFlightArea_DockTime", nameof(DockSn), OrderByType.Asc, nameof(CreateTime), OrderByType.Desc)]
public class DjiFlightArea : EntityWorkspaceBase
{
    /// <summary>目标机场 SN</summary>
    [SugarColumn(ColumnDescription = "机场SN", Length = 64, IsNullable = false)]
    public string DockSn { get; set; }

    /// <summary>文件名（协议 <c>file.name</c>，如 <c>geofence_xxx.json</c>）</summary>
    [SugarColumn(ColumnDescription = "文件名", Length = 256, IsNullable = false)]
    public string FileName { get; set; }

    /// <summary>
    /// 对象存储内的完整 Key（如 <c>{工作空间}/flightarea/geofence_xxx.json</c>）。
    /// </summary>
    /// <remarks>
    /// <b>必须存 Key 而不是只存 URL</b>：设备每次来要文件时都要重新签一份<b>未过期</b>的地址，
    /// 而签名需要 Key；只存 URL 就只能把过期地址原样再发一次，设备必然下载失败。
    /// </remarks>
    [SugarColumn(ColumnDescription = "对象Key", Length = 512, IsNullable = true)]
    public string ObjectKey { get; set; }

    /// <summary>最近一次下发给设备的带签名地址（仅用于排查「设备报下载失败」时比对过期时间）</summary>
    [SugarColumn(ColumnDescription = "文件地址", Length = 1024, IsNullable = true)]
    public string Url { get; set; }

    /// <summary>SHA256 摘要（协议 <c>checksum</c>，也用作本表的业务唯一键）</summary>
    [SugarColumn(ColumnDescription = "文件校验和", Length = 128, IsNullable = true)]
    public string Checksum { get; set; }

    /// <summary>文件大小（字节）</summary>
    [SugarColumn(ColumnDescription = "文件大小", IsNullable = true)]
    public long? FileSize { get; set; }

    /// <summary>设备侧同步状态</summary>
    [SugarColumn(ColumnDescription = "同步状态", IsNullable = true)]
    public FlightAreaSyncStatusEnum SyncStatus { get; set; }

    /// <summary>同步失败原因码（仅失败时有意义）</summary>
    [SugarColumn(ColumnDescription = "失败原因码", IsNullable = true)]
    public FlightAreaSyncReasonEnum SyncReason { get; set; }

    /// <summary>是否为设备当前启用中的版本（同步成功时置位，同一机场只应有一份为 true）</summary>
    [SugarColumn(ColumnDescription = "是否启用中", IsNullable = true)]
    public bool IsActive { get; set; }

    /// <summary>最近一次同步状态更新时刻</summary>
    [SugarColumn(ColumnDescription = "最近同步时间", IsNullable = true)]
    public DateTime? LastTime { get; set; }

    /// <summary>报文时间戳（毫秒，设备时钟）</summary>
    [SugarColumn(ColumnDescription = "上报时间戳", IsNullable = true)]
    public long ReportTimestamp { get; set; }
}

/// <summary>
/// 飞行器与自定义飞行区边界的位置快照（每机场每个区域一行）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么存「快照」而不存「流水」</b>：<c>flight_areas_drone_location</c> 是
/// <c>need_reply = 0</c> 的高频推送（飞行中可能每秒数次），存流水会让表在几天内膨胀到千万级，
/// 而这些历史距离数据没有任何回溯价值 —— 真正要留痕的是「什么时候进入过作业区域」。
/// 因此本表只保留每个区域的最新值，并在<b>越界状态发生翻转</b>时额外累加
/// <see cref="EnterCount"/> / 刷新 <see cref="LastEnterTime"/>，同时由服务层打一条结构化告警日志。
/// </para>
/// </remarks>
[SugarTable(null, "飞行区距离快照")]
[SugarIndex("index_DjiFlightAreaLocation_Unique", nameof(DockSn), OrderByType.Asc, nameof(AreaId), OrderByType.Asc, true)]
public class DjiFlightAreaLocation : EntityWorkspaceBase
{
    /// <summary>机场 SN</summary>
    [SugarColumn(ColumnDescription = "机场SN", Length = 64, IsNullable = false)]
    public string DockSn { get; set; }

    /// <summary>区域唯一 ID（协议 <c>area_id</c>，对应飞行区文件内的区域）</summary>
    [SugarColumn(ColumnDescription = "区域ID", Length = 64, IsNullable = false)]
    public string AreaId { get; set; }

    /// <summary>飞行器距离该区域边界的距离（米，协议 <c>area_distance</c>）</summary>
    [SugarColumn(ColumnDescription = "距边界距离", IsNullable = true)]
    public double? AreaDistance { get; set; }

    /// <summary>飞行器当前是否在自定义飞行区内</summary>
    [SugarColumn(ColumnDescription = "是否在区内", IsNullable = true)]
    public bool IsInArea { get; set; }

    /// <summary>距边界最近的一次距离（历史最小值，用于评估「贴近边界」风险）</summary>
    [SugarColumn(ColumnDescription = "历史最近距离", IsNullable = true)]
    public double? MinDistance { get; set; }

    /// <summary>累计进入该区域的次数（每次「区外 → 区内」翻转累加 1）</summary>
    /// <remarks>
    /// 只统计翻转次数而不是每条报文加一：设备在边界附近抖动时会连续推很多条 <c>is_in_area = true</c>，
    /// 按报文计数会把一次进入算成几十次。
    /// </remarks>
    [SugarColumn(ColumnDescription = "进入次数", IsNullable = true)]
    public int EnterCount { get; set; }

    /// <summary>最近一次进入该区域的时刻</summary>
    [SugarColumn(ColumnDescription = "最近进入时间", IsNullable = true)]
    public DateTime? LastEnterTime { get; set; }

    /// <summary>最近一次离开该区域的时刻</summary>
    [SugarColumn(ColumnDescription = "最近离开时间", IsNullable = true)]
    public DateTime? LastExitTime { get; set; }

    /// <summary>最近更新时间</summary>
    [SugarColumn(ColumnDescription = "更新时间", IsNullable = true)]
    public DateTime? LastTime { get; set; }

    /// <summary>报文时间戳（毫秒，设备时钟）</summary>
    [SugarColumn(ColumnDescription = "上报时间戳", IsNullable = true)]
    public long ReportTimestamp { get; set; }
}
