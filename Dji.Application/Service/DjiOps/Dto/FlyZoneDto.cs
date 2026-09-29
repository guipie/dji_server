// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Core.Enum.DjiEnum.Ops;

namespace Dji.Application.Service.DjiOps.Dto;

#region 飞行区 / 禁飞区（平台自绘）

/// <summary>飞行区查询</summary>
public class FlyZoneSearchInput : BasePageInput
{
    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>区域类型筛选；不传则不过滤</summary>
    public FlyZoneTypeEnum? ZoneType { get; set; }

    /// <summary>几何形状筛选</summary>
    public FlyZoneShapeEnum? Shape { get; set; }

    /// <summary>只看启用中的</summary>
    public bool? IsEnabled { get; set; }

    /// <summary>名称模糊匹配</summary>
    public string Keyword { get; set; }
}

/// <summary>
/// 新增 / 修改时的几何 load。
/// </summary>
/// <remarks>
/// 直接用 GeoJSON 的 <c>geometry</c> 原语，好处是前端 Cesium / mapbox / turf 生态可以直接互通，
/// 后端不需要再引入 NetTopologySuite 这类几何库去做中间转换。
/// </remarks>
public class FlyZoneGeometryInput
{
    /// <summary><c>Polygon</c> 或 <c>Point</c>（圆形时的圆心）</summary>
    public string Type { get; set; }

    /// <summary>
    /// 坐标点列表 —— 多边形与圆形共用同一种形状表示，避免调用方区分嵌套层级。
    /// <list type="bullet">
    /// <item>多边形：<c>[[lon,lat],[lon,lat],...]</c>，至少 3 个点、至多 255 个</item>
    /// <item>圆形：<c>[[lon,lat]]</c>，只取第一个点作为圆心，半径看 <c>Radius</c></item>
    /// </list>
    /// 序列化到库里时再按 GeoJSON 规则还原为 <c>coordinates</c> 的多层结构。
    /// </summary>
    public List<List<double>> Coordinates { get; set; }
}

/// <summary>点结构体，用于「某点是否落在区域内」这类校验的输出</summary>
public class FlyZonePointInput
{
    /// <summary>经度（WGS84）</summary>
    public double Longitude { get; set; }

    /// <summary>纬度（WGS84）</summary>
    public double Latitude { get; set; }
}

/// <summary>新增飞行区</summary>
public class AddFlyZoneInput
{
    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>区域名称</summary>
    [Required(ErrorMessage = "区域名称不能为空")]
    [MaxLength(128, ErrorMessage = "区域名称最长 128 个字符")]
    public string Name { get; set; }

    /// <summary>区域类型</summary>
    public FlyZoneTypeEnum ZoneType { get; set; } = FlyZoneTypeEnum.CustomFlyZone;

    /// <summary>几何形状</summary>
    public FlyZoneShapeEnum Shape { get; set; } = FlyZoneShapeEnum.Polygon;

    /// <summary>几何数据</summary>
    [Required(ErrorMessage = "几何数据不能为空")]
    public FlyZoneGeometryInput Geometry { get; set; }

    /// <summary>圆形半径（米），多边形填 0</summary>
    public double Radius { get; set; }

    /// <summary>下限高（米）</summary>
    public double MinHeight { get; set; }

    /// <summary>上限高（米）</summary>
    public double MaxHeight { get; set; }

    /// <summary>展示颜色（十六进制）</summary>
    public string Color { get; set; }

    /// <summary>是否启用</summary>
    public bool IsEnabled { get; set; } = true;

    public int Sort { get; set; }

    /// <summary>备注</summary>
    public string Remark { get; set; }
}

/// <summary>修改飞行区</summary>
public class UpdateFlyZoneInput
{
    /// <summary>主键</summary>
    [Required(ErrorMessage = "主键不能为空")]
    public long Id { get; set; }

    public string Name { get; set; }

    public FlyZoneTypeEnum ZoneType { get; set; }

    public FlyZoneShapeEnum Shape { get; set; }

    public FlyZoneGeometryInput Geometry { get; set; }

    public double Radius { get; set; }

    public double MinHeight { get; set; }

    public double MaxHeight { get; set; }

    public string Color { get; set; }

    public bool IsEnabled { get; set; }

    public int Sort { get; set; }

    public string Remark { get; set; }
}

/// <summary>批量删除入参</summary>
public class FlyZoneIdsInput
{
    [Required(ErrorMessage = "请选择要删除的区域")]
    public List<long> Ids { get; set; }
}

/// <summary>启停入参</summary>
public class FlyZoneEnableInput
{
    /// <summary>主键集合</summary>
    [Required(ErrorMessage = "请选择要操作的区域")]
    public List<long> Ids { get; set; }

    /// <summary>目标状态</summary>
    public bool IsEnabled { get; set; }
}

/// <summary>飞行区输出</summary>
public class FlyZoneOutput
{
    public long Id { get; set; }

    public string WorkspaceId { get; set; }

    public string Name { get; set; }

    public FlyZoneTypeEnum ZoneType { get; set; }

    /// <summary>类型名称</summary>
    public string ZoneTypeName { get; set; }

    public FlyZoneShapeEnum Shape { get; set; }

    public string ShapeName { get; set; }

    /// <summary>GeoJSON geometry 原串，前端可直接 JSON.parse 丢给地图</summary>
    public string Geometry { get; set; }

    public double Radius { get; set; }

    public double MinHeight { get; set; }

    public double MaxHeight { get; set; }

    public string Color { get; set; }

    public bool IsEnabled { get; set; }

    public int Sort { get; set; }

    public string Remark { get; set; }

    public DateTime? CreateTime { get; set; }

    /// <summary>多边形面积（平方米）；圆形给 0，由前端按半径算或直接用 <see cref="Radius"/></summary>
    public double AreaSquareMeters { get; set; }

    /// <summary>多边形周长（米）</summary>
    public double PerimeterMeters { get; set; }

    /// <summary>顶点数（圆形为 0）</summary>
    public int VertexCount { get; set; }
}

/// <summary>
/// 导出文件的单条校验结果。
/// </summary>
/// <remarks>
/// 大疆对下发文件有多条硬约束，违反任意一条<b>整份文件都会被拒</b>，且设备侧的错误往往含糊。
/// 与其让用户在 Champ 上反复失败，不如在导出时就把「哪块区域、什么问题」讲清楚。
/// </remarks>
public class FlyZoneValidateItem
{
    /// <summary>区域 Id</summary>
    public long ZoneId { get; set; }

    /// <summary>区域名称</summary>
    public string ZoneName { get; set; }

    /// <summary>区域在文件中的 id</summary>
    public string AreaId { get; set; }

    /// <summary>是否通过</summary>
    public bool Valid { get; set; }

    /// <summary>不通过时的说明</summary>
    public string Message { get; set; }
}

/// <summary>导出结果</summary>
public class FlyZoneExportOutput
{
    /// <summary>文件名建议值</summary>
    public string FileName { get; set; }

    /// <summary>符合大疆协议的文件内容（GeoJSON FeatureCollection）</summary>
    public string Content { get; set; }

    /// <summary>实际被导出的区域数量</summary>
    public int Count { get; set; }

    /// <summary>逐区域校验明细</summary>
    public List<FlyZoneValidateItem> Validations { get; set; } = [];
}

#endregion
