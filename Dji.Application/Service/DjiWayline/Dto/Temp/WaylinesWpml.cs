// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using System.Xml.Serialization;

namespace Dji.Application.Service.DjiWayline.Dto.Temp;

/// <summary>
/// 可执行航线文件 waylines.wpml 根对象
/// </summary>
/// <remarks>
/// 结构对齐大疆官方导出的 KMZ 文件：
/// kml(KML 命名空间) → Document(无命名空间) → missionConfig + Folder。
/// </remarks>
[XmlRoot("kml", Namespace = WpmlNamespaces.Kml)]
public class WaylinesWpml
{
    /// <summary>文档节点</summary>
    [XmlElement("Document", Namespace = "")]
    public WaylinesDocument Document { get; set; } = new();
}

/// <summary>
/// waylines.wpml 的 Document
/// </summary>
public class WaylinesDocument
{
    /// <summary>任务全局配置</summary>
    [XmlElement("missionConfig", Namespace = WpmlNamespaces.Wpml)]
    public WpmlMissionConfig MissionConfig { get; set; } = new();

    /// <summary>可执行航线（一条航线对应一个 Folder）</summary>
    [XmlElement("Folder", Namespace = "")]
    public WaylinesFolder Folder { get; set; } = new();
}

/// <summary>
/// waylines.wpml 的可执行航线
/// </summary>
public class WaylinesFolder
{
    /// <summary>模板 ID，与 template.kml 中的 templateId 关联</summary>
    [XmlElement("templateId", Namespace = WpmlNamespaces.Wpml)]
    public int TemplateId { get; set; }

    /// <summary>全局航线飞行速度（米/秒）</summary>
    [XmlElement("autoFlightSpeed", Namespace = WpmlNamespaces.Wpml)]
    public double AutoFlightSpeed { get; set; } = 10;

    /// <summary>执行高度模式：WGS84 / relativeToStartPoint / realTimeFollowSurface</summary>
    [XmlElement("executeHeightMode", Namespace = WpmlNamespaces.Wpml)]
    public string ExecuteHeightMode { get; set; } = "WGS84";

    /// <summary>航线 ID，一条航线内唯一</summary>
    [XmlElement("waylineId", Namespace = WpmlNamespaces.Wpml)]
    public int WaylineId { get; set; }

    /// <summary>航线总距离（米）</summary>
    [XmlElement("distance", Namespace = WpmlNamespaces.Wpml)]
    public double? Distance { get; set; }

    /// <summary>值为空时不输出该元素</summary>
    public bool ShouldSerializeDistance() => Distance.HasValue;

    /// <summary>航线预计总时长（秒）</summary>
    [XmlElement("duration", Namespace = WpmlNamespaces.Wpml)]
    public double? Duration { get; set; }

    /// <summary>值为空时不输出该元素</summary>
    public bool ShouldSerializeDuration() => Duration.HasValue;

    /// <summary>航点列表</summary>
    [XmlElement("Placemark", Namespace = "")]
    public List<WaylinesPlacemark> Placemarks { get; set; } = [];
}

/// <summary>
/// waylines.wpml 的航点
/// </summary>
public class WaylinesPlacemark
{
    /// <summary>航点序号，从 0 开始单调连续递增</summary>
    [XmlElement("index", Namespace = WpmlNamespaces.Wpml)]
    public int Index { get; set; }

    /// <summary>航点执行高度（米），参考平面由 executeHeightMode 决定</summary>
    [XmlElement("executeHeight", Namespace = WpmlNamespaces.Wpml)]
    public double ExecuteHeight { get; set; }

    /// <summary>航点飞行速度，当前航点飞向下一个航点的速度（米/秒）</summary>
    [XmlElement("waypointSpeed", Namespace = WpmlNamespaces.Wpml)]
    public double? WaypointSpeed { get; set; }

    /// <summary>值为空时不输出该元素</summary>
    public bool ShouldSerializeWaypointSpeed() => WaypointSpeed.HasValue;

    /// <summary>航点偏航角参数</summary>
    [XmlElement("waypointHeadingParam", Namespace = WpmlNamespaces.Wpml)]
    public WpmlWaypointHeadingParam? WaypointHeadingParam { get; set; }

    /// <summary>航点转弯参数</summary>
    [XmlElement("waypointTurnParam", Namespace = WpmlNamespaces.Wpml)]
    public WpmlWaypointTurnParam? WaypointTurnParam { get; set; }

    /// <summary>航点云台角度参数</summary>
    [XmlElement("waypointGimbalHeadingParam", Namespace = WpmlNamespaces.Wpml)]
    public WpmlWaypointGimbalHeadingParam? WaypointGimbalHeadingParam { get; set; }

    /// <summary>该航段是否贴合直线（0/1）</summary>
    [XmlElement("useStraightLine", Namespace = WpmlNamespaces.Wpml)]
    public int? UseStraightLine { get; set; }

    /// <summary>值为空时不输出该元素</summary>
    public bool ShouldSerializeUseStraightLine() => UseStraightLine.HasValue;

    /// <summary>是否危险点（0/1）</summary>
    [XmlElement("isRisky", Namespace = WpmlNamespaces.Wpml)]
    public int IsRisky { get; set; }

    /// <summary>航点工作类型（0：正常）</summary>
    [XmlElement("waypointWorkType", Namespace = WpmlNamespaces.Wpml)]
    public int WaypointWorkType { get; set; }

    /// <summary>航点坐标</summary>
    [XmlElement("Point", Namespace = "")]
    public PointCoordinates Point { get; set; } = new();

    /// <summary>航点动作组</summary>
    [XmlElement("actionGroup", Namespace = WpmlNamespaces.Wpml)]
    public List<WpmlActionGroup> ActionGroups { get; set; } = [];
}
