// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Core.Enum.DjiEnum.Ops;

namespace Dji.Core.Entity.DjiEntity;

/// <summary>
/// 飞行区 / 禁飞区（运营端在地图上自绘的空域，区别于 <see cref="DjiFlightArea"/> 的「下发文件登记表」）。
/// </summary>
/// <remarks>
/// <para>
/// <b>与 <see cref="DjiFlightArea"/> 的分工必须分清，这两张表解决的是完全不同层次的问题：</b>
/// <list type="bullet">
/// <item><see cref="DjiFlightArea"/> —— 文件登记表。记录「哪台机场同步了哪个 <c>geofence_*.json</c>、
/// 同步到了哪一步」。它<b>不含坐标</b>，几何在对象存储的文件里。</item>
/// <item><c>DjiFlyZone</c> —— 空域定义源。运营人员在地图上画的区域就存在这里，
/// 后续由它<b>生成</b>符合大疆协议的文件，再走登记 + 下发链路推给设备。</item>
/// </list>
/// 简言之：这里是「画板上的原始数据」，那张表是「发出去的成品台账」。有了本表，
/// 运营端才能「保存后再回来编辑」，而不是每次都从头画或从 OBS 里反解文件。
/// </para>
/// <para>
/// 坐标系统一为 <b>WGS84</b>（与设备上报、与大疆协议一致）。
/// 前端若用高德/百度底图需自行做 GCJ02 偏移换算 —— 转换属展示层职责，
/// 落库保持原值，避免多次转换累积误差。
/// </para>
/// </remarks>
[SugarTable(null, "飞行区/禁飞区")]
[SugarIndex("index_DjiFlyZone_Workspace", nameof(WorkspaceId), OrderByType.Asc, nameof(ZoneType), OrderByType.Asc)]
public class DjiFlyZone : EntityWorkspaceBase
{
    /// <summary>区域名称（如「园区东侧作业区」「变电站禁飞区」）</summary>
    [SugarColumn(ColumnDescription = "区域名称", Length = 128, IsNullable = false)]
    public string Name { get; set; }

    /// <summary>
    /// 区域类型：0 作业区（dfence，圈内可飞）/ 1 禁飞区（nfz，圈外可飞）。
    /// </summary>
    [SugarColumn(ColumnDescription = "区域类型")]
    public FlyZoneTypeEnum ZoneType { get; set; } = FlyZoneTypeEnum.CustomFlyZone;

    /// <summary>几何形状：0 多边形 / 1 圆形</summary>
    [SugarColumn(ColumnDescription = "几何形状")]
    public FlyZoneShapeEnum Shape { get; set; } = FlyZoneShapeEnum.Polygon;

    /// <summary>
    /// GeoJSON 的 <c>geometry</c> 片段，原样存串。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 多边形形如 <c>{"type":"Polygon","coordinates":[[[lon,lat],[lon,lat]...]]}</c>，
    /// 圆形形如 <c>{"type":"Point","coordinates":[lon,lat]}</c>（半径单独落在 <see cref="Radius"/>）。
    /// </para>
    /// <para>
    /// <b>为什么整个 geometry 存串而不是拆成坐标明细表</b>：本表的使用方式是「前端一次性拉取某个空间
    /// 下的全部区域直接在地图上渲染」，明细表会引入不必要的 join 与顺序维护；
    /// 而按单个区域检索的需求并不存在。字段用 TEXT 而非 nvarchar —
    /// 一个 255 顶点的多边形序列化后可达 10KB 级。
    /// </para>
    /// </remarks>
    [SugarColumn(ColumnDescription = "几何数据", ColumnDataType = "TEXT", IsNullable = true)]
    public string Geometry { get; set; }

    /// <summary>
    /// 圆形半径（米）。多边形时恒为 0。
    /// </summary>
    /// <remarks>大疆协议要求最小值 10 米，小于该值时设备会拒绝整份文件。</remarks>
    [SugarColumn(ColumnDescription = "半径(米)", IsNullable = true)]
    public double Radius { get; set; }

    /// <summary>垂直下限（米，相对起飞点）。平台侧的辅助信息，不参与下发文件</summary>
    [SugarColumn(ColumnDescription = "下限高(米)", IsNullable = true)]
    public double MinHeight { get; set; }

    /// <summary>垂直上限（米，相对起飞点）。平台侧的辅助信息，不参与下发文件</summary>
    [SugarColumn(ColumnDescription = "上限高(米)", IsNullable = true)]
    public double MaxHeight { get; set; }

    /// <summary>地图上的展示颜色（十六进制，如 <c>#22d3ee</c>）。留空则由前端按类型取默认色</summary>
    [SugarColumn(ColumnDescription = "展示颜色", Length = 16, IsNullable = true)]
    public string Color { get; set; }

    /// <summary>
    /// 是否参与下发。
    /// </summary>
    /// <remarks>
    /// 勾掉只是「暂不下发」，<b>不删除历史</b>：空域常有临时失效的场景（季节性禁飞、施工期限制），
    /// 删了就得重画，而 distinction 只是在导出时过滤掉这一条。
    /// </remarks>
    [SugarColumn(ColumnDescription = "是否启用")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>排序号（小的在上，便于控制压盖顺序）</summary>
    [SugarColumn(ColumnDescription = "排序")]
    public int Sort { get; set; }

    /// <summary>备注（如划定依据、批文号）</summary>
    [SugarColumn(ColumnDescription = "备注", Length = 512, IsNullable = true)]
    public string Remark { get; set; }
}
