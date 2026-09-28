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
/// 模板文件 template.kml 根对象
/// </summary>
/// <remarks>
/// 结构对齐大疆官方导出的 KMZ 文件：
/// kml(KML 命名空间) → Document(无命名空间) → missionConfig + Folder。
/// </remarks>
[XmlRoot("kml", Namespace = WpmlNamespaces.Kml)]
public class WaylineTemplateKml
{
    /// <summary>文档节点</summary>
    [XmlElement("Document", Namespace = "")]
    public TemplateDocument Document { get; set; } = new();
}

/// <summary>
/// template.kml 的 Document
/// </summary>
public class TemplateDocument
{
    /// <summary>文件创建作者</summary>
    [XmlElement("author", Namespace = WpmlNamespaces.Wpml)]
    public string? Author { get; set; }

    /// <summary>文件创建时间（毫秒级 Unix 时间戳）</summary>
    [XmlElement("createTime", Namespace = WpmlNamespaces.Wpml)]
    public long? CreateTime { get; set; }

    /// <summary>值为空时不输出该元素</summary>
    public bool ShouldSerializeCreateTime() => CreateTime.HasValue;

    /// <summary>文件更新时间（毫秒级 Unix 时间戳）</summary>
    [XmlElement("updateTime", Namespace = WpmlNamespaces.Wpml)]
    public long? UpdateTime { get; set; }

    /// <summary>值为空时不输出该元素</summary>
    public bool ShouldSerializeUpdateTime() => UpdateTime.HasValue;

    /// <summary>任务全局配置</summary>
    [XmlElement("missionConfig", Namespace = WpmlNamespaces.Wpml)]
    public WpmlMissionConfig MissionConfig { get; set; } = new();

    /// <summary>模板信息（本项目一条航线一个 Folder）</summary>
    [XmlElement("Folder", Namespace = "")]
    public TemplateFolder Folder { get; set; } = new();
}

/// <summary>
/// 任务全局配置（template.kml 与 waylines.wpml 共用）
/// </summary>
public class WpmlMissionConfig
{
    /// <summary>飞向首航点模式：safely / pointToPoint</summary>
    [XmlElement("flyToWaylineMode", Namespace = WpmlNamespaces.Wpml)]
    public string FlyToWaylineMode { get; set; } = "safely";

    /// <summary>航线结束动作：goHome / noAction / autoLand / gotoFirstWaypoint</summary>
    [XmlElement("finishAction", Namespace = WpmlNamespaces.Wpml)]
    public string FinishAction { get; set; } = "goHome";

    /// <summary>失控是否继续执行航线：goContinue / executeLostAction</summary>
    [XmlElement("exitOnRCLost", Namespace = WpmlNamespaces.Wpml)]
    public string ExitOnRCLost { get; set; } = "goContinue";

    /// <summary>失控动作类型：goBack / landing / hover（exitOnRCLost=executeLostAction 时必需）</summary>
    [XmlElement("executeRCLostAction", Namespace = WpmlNamespaces.Wpml)]
    public string? ExecuteRCLostAction { get; set; }

    /// <summary>参考起飞点，格式“纬度,经度,高度（椭球高）”</summary>
    [XmlElement("takeOffRefPoint", Namespace = WpmlNamespaces.Wpml)]
    public string? TakeOffRefPoint { get; set; }

    /// <summary>参考起飞点海拔高度（米）</summary>
    [XmlElement("takeOffRefPointAGLHeight", Namespace = WpmlNamespaces.Wpml)]
    public double? TakeOffRefPointAGLHeight { get; set; }

    /// <summary>值为空时不输出该元素</summary>
    public bool ShouldSerializeTakeOffRefPointAGLHeight() => TakeOffRefPointAGLHeight.HasValue;

    /// <summary>安全起飞高度（米）</summary>
    [XmlElement("takeOffSecurityHeight", Namespace = WpmlNamespaces.Wpml)]
    public double TakeOffSecurityHeight { get; set; } = 20;

    /// <summary>全局航线过渡速度（米/秒）</summary>
    [XmlElement("globalTransitionalSpeed", Namespace = WpmlNamespaces.Wpml)]
    public double GlobalTransitionalSpeed { get; set; } = 10;

    /// <summary>全局返航高度（米）</summary>
    [XmlElement("globalRTHHeight", Namespace = WpmlNamespaces.Wpml)]
    public double GlobalRTHHeight { get; set; } = 100;

    /// <summary>飞行器机型信息</summary>
    [XmlElement("droneInfo", Namespace = WpmlNamespaces.Wpml)]
    public DroneInfo DroneInfo { get; set; } = new();

    /// <summary>负载机型信息</summary>
    [XmlElement("payloadInfo", Namespace = WpmlNamespaces.Wpml)]
    public PayloadInfo PayloadInfo { get; set; } = new();

    /// <summary>航线绕行配置（仅 M3D / M4D / M4E 系列机型支持，未开启时不输出）</summary>
    [XmlElement("autoRerouteInfo", Namespace = WpmlNamespaces.Wpml)]
    public WpmlAutoRerouteInfo? AutoRerouteInfo { get; set; }
}

/// <summary>
/// template.kml 的 Folder（航点飞行模板）
/// </summary>
public class TemplateFolder
{
    /// <summary>预定义模板类型：waypoint / mapping2d / mapping3d / mappingStrip</summary>
    [XmlElement("templateType", Namespace = WpmlNamespaces.Wpml)]
    public string TemplateType { get; set; } = "waypoint";

    /// <summary>模板 ID，同一 KMZ 内唯一，建议从 0 开始</summary>
    [XmlElement("templateId", Namespace = WpmlNamespaces.Wpml)]
    public int TemplateId { get; set; }

    /// <summary>全局航线飞行速度（米/秒）</summary>
    [XmlElement("autoFlightSpeed", Namespace = WpmlNamespaces.Wpml)]
    public double AutoFlightSpeed { get; set; } = 10;

    /// <summary>全局航点转弯模式</summary>
    [XmlElement("globalWaypointTurnMode", Namespace = WpmlNamespaces.Wpml)]
    public string GlobalWaypointTurnMode { get; set; } = "toPointAndStopWithDiscontinuityCurvature";

    /// <summary>全局航段轨迹是否尽量贴合直线（0/1）</summary>
    [XmlElement("globalUseStraightLine", Namespace = WpmlNamespaces.Wpml)]
    public int GlobalUseStraightLine { get; set; }

    /// <summary>云台俯仰角控制模式：manual / usePointSetting</summary>
    [XmlElement("gimbalPitchMode", Namespace = WpmlNamespaces.Wpml)]
    public string GimbalPitchMode { get; set; } = "manual";

    /// <summary>全局航线高度</summary>
    [XmlElement("globalHeight", Namespace = WpmlNamespaces.Wpml)]
    public double GlobalHeight { get; set; }

    /// <summary>负载设置</summary>
    [XmlElement("payloadParam", Namespace = WpmlNamespaces.Wpml)]
    public TemplatePayloadParam PayloadParam { get; set; } = new();

    /// <summary>全局偏航角模式参数</summary>
    [XmlElement("globalWaypointHeadingParam", Namespace = WpmlNamespaces.Wpml)]
    public WpmlWaypointHeadingParam GlobalWaypointHeadingParam { get; set; } = new();

    /// <summary>坐标系参数</summary>
    [XmlElement("waylineCoordinateSysParam", Namespace = WpmlNamespaces.Wpml)]
    public WaylineCoordinateSysParam WaylineCoordinateSysParam { get; set; } = new();

    /// <summary>航点列表</summary>
    [XmlElement("Placemark", Namespace = "")]
    public List<TemplatePlacemark> Placemarks { get; set; } = [];
}

/// <summary>
/// template.kml 的负载设置
/// </summary>
public class TemplatePayloadParam
{
    /// <summary>图片格式列表，多个以英文逗号分隔，如 “wide,ir”</summary>
    [XmlElement("imageFormat", Namespace = WpmlNamespaces.Wpml)]
    public string? ImageFormat { get; set; }

    /// <summary>负载扫描模式：repetitive / nonRepetitive</summary>
    [XmlElement("scanningMode", Namespace = WpmlNamespaces.Wpml)]
    public string? ScanningMode { get; set; }

    /// <summary>负载挂载位置索引</summary>
    [XmlElement("payloadPositionIndex", Namespace = WpmlNamespaces.Wpml)]
    public int PayloadPositionIndex { get; set; }
}

/// <summary>
/// template.kml 的航点
/// </summary>
public class TemplatePlacemark
{
    /// <summary>航点序号，从 0 开始单调连续递增</summary>
    [XmlElement("index", Namespace = WpmlNamespaces.Wpml)]
    public int Index { get; set; }

    /// <summary>是否危险点（0/1）</summary>
    [XmlElement("isRisky", Namespace = WpmlNamespaces.Wpml)]
    public int IsRisky { get; set; }

    /// <summary>是否使用全局高度（0/1）</summary>
    [XmlElement("useGlobalHeight", Namespace = WpmlNamespaces.Wpml)]
    public int UseGlobalHeight { get; set; } = 1;

    /// <summary>航点高度（WGS84 椭球高）</summary>
    [XmlElement("ellipsoidHeight", Namespace = WpmlNamespaces.Wpml)]
    public double? EllipsoidHeight { get; set; }

    /// <summary>值为空时不输出该元素</summary>
    public bool ShouldSerializeEllipsoidHeight() => EllipsoidHeight.HasValue;

    /// <summary>航点高度（EGM96 海拔高 / 相对起飞点高度 / AGL 相对地面高度）</summary>
    [XmlElement("height", Namespace = WpmlNamespaces.Wpml)]
    public double? Height { get; set; }

    /// <summary>值为空时不输出该元素</summary>
    public bool ShouldSerializeHeight() => Height.HasValue;

    /// <summary>航点飞行速度（米/秒），useGlobalSpeed=0 时必需</summary>
    [XmlElement("waypointSpeed", Namespace = WpmlNamespaces.Wpml)]
    public double? WaypointSpeed { get; set; }

    /// <summary>值为空时不输出该元素</summary>
    public bool ShouldSerializeWaypointSpeed() => WaypointSpeed.HasValue;

    /// <summary>是否使用全局飞行速度（0/1）</summary>
    [XmlElement("useGlobalSpeed", Namespace = WpmlNamespaces.Wpml)]
    public int UseGlobalSpeed { get; set; } = 1;

    /// <summary>是否使用全局偏航角参数（0/1）</summary>
    [XmlElement("useGlobalHeadingParam", Namespace = WpmlNamespaces.Wpml)]
    public int UseGlobalHeadingParam { get; set; } = 1;

    /// <summary>是否使用全局转弯参数（0/1）</summary>
    [XmlElement("useGlobalTurnParam", Namespace = WpmlNamespaces.Wpml)]
    public int UseGlobalTurnParam { get; set; } = 1;

    /// <summary>航点云台俯仰角，gimbalPitchMode=usePointSetting 时必需</summary>
    [XmlElement("gimbalPitchAngle", Namespace = WpmlNamespaces.Wpml)]
    public double? GimbalPitchAngle { get; set; }

    /// <summary>值为空时不输出该元素</summary>
    public bool ShouldSerializeGimbalPitchAngle() => GimbalPitchAngle.HasValue;

    /// <summary>该航段是否贴合直线（0/1）</summary>
    [XmlElement("useStraightLine", Namespace = WpmlNamespaces.Wpml)]
    public int? UseStraightLine { get; set; }

    /// <summary>值为空时不输出该元素</summary>
    public bool ShouldSerializeUseStraightLine() => UseStraightLine.HasValue;

    /// <summary>航点偏航角参数，useGlobalHeadingParam=0 时必需</summary>
    [XmlElement("waypointHeadingParam", Namespace = WpmlNamespaces.Wpml)]
    public WpmlWaypointHeadingParam? WaypointHeadingParam { get; set; }

    /// <summary>航点转弯参数，useGlobalTurnParam=0 时必需</summary>
    [XmlElement("waypointTurnParam", Namespace = WpmlNamespaces.Wpml)]
    public WpmlWaypointTurnParam? WaypointTurnParam { get; set; }

    /// <summary>航点坐标</summary>
    [XmlElement("Point", Namespace = "")]
    public PointCoordinates Point { get; set; } = new();

    /// <summary>航点动作组</summary>
    [XmlElement("actionGroup", Namespace = WpmlNamespaces.Wpml)]
    public List<WpmlActionGroup> ActionGroups { get; set; } = [];
}
