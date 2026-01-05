// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Dji.Application.Service.DjiWayline.Dto;


/// <summary>
/// 任务配置
/// </summary>
[XmlRoot("missionConfig", Namespace = "http://www.dji.com/wpmz/1.0.6")]
public class MissionConfig
{
    /// <summary>
    /// 飞向首航点模式
    /// </summary>
    /// <remarks>
    /// - safely：安全模式（M300）飞行器起飞，上升至首航点高度，再平飞至首航点。如果首航点低于起飞点，则起飞后平飞至首航点上方再下降。（M30）飞行器起飞，上升至首航点高度，再平飞至首航点。如果首航点低于"安全起飞高度"，则起飞至"安全起飞高度"后，平飞至首航点上方再下降。注意"安全起飞高度"仅在飞行器未起飞时生效。
    /// - pointToPoint：倾斜飞行模式（M300）飞行器起飞后，倾斜飞到首航点。 （M30）飞行器起飞至"安全起飞高度"，再倾斜爬升至首航点。如果首航点高度低于"安全起飞高度"，则先平飞后下降。
    /// </remarks>
    [Required]
    [RegularExpression("^(safely|pointToPoint)$")]
    [XmlElement("flyToWaylineMode", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public string FlyToWaylineMode { get; set; } = "safely";

    /// <summary>
    /// 航线结束动作
    /// </summary>
    /// <remarks>
    /// - goHome：飞行器完成航线任务后，退出航线模式并返航。
    /// - noAction：飞行器完成航线任务后，退出航线模式。
    /// - autoLand：飞行器完成航线任务后，退出航线模式并原地降落。
    /// - gotoFirstWaypoint：飞行器完成航线任务后，立即飞向航线起始点，到达后退出航线模式。
    /// * 注：以上动作执行过程，若飞行器退出了航线模式且进入失控状态，则会优先执行失控动作。
    /// </remarks>
    [Required]
    [RegularExpression("^(goHome|noAction|autoLand|gotoFirstWaypoint)$")]
    [XmlElement("finishAction", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public string FinishAction { get; set; } = "goHome";

    /// <summary>
    /// 失控是否继续执行航线
    /// </summary>
    /// <remarks>
    /// - goContinue：继续执行
    /// - executeLostAction：退出航线，执行失控动作
    /// </remarks>
    [Required]
    [RegularExpression("^(goContinue|executeLostAction)$")]
    [XmlElement("exitOnRCLost", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public string ExitOnRCLost { get; set; } = "goContinue";

    /// <summary>
    /// 失控动作类型
    /// </summary>
    /// <remarks>
    /// - goBack：返航。飞行器从失控位置飞向起飞点
    /// - landing：降落。飞行器从失控位置原地降落
    /// - hover：悬停。飞行器从失控位置悬停
    /// </remarks>
    [Required]
    [RegularExpression("^(goBack|landing|hover)$")]
    [XmlElement("executeRCLostAction", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public string ExecuteRCLostAction { get; set; } = "hover";

    /// <summary>
    /// 安全起飞高度
    /// </summary>
    /// <remarks>
    /// 遥控器场景 [1.2,1500]，机场场景 [8,1500] （高度模式：相对起飞点高度）
    /// * 注：飞行器起飞后，先爬升至该高度，再根据"飞向首航点模式"的设置飞至首航点。该元素仅在飞行器未起飞时生效。
    /// </remarks>
    [Required]
    [Range(1.2, 1500)]
    [XmlElement("takeOffSecurityHeight", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public double TakeOffSecurityHeight { get; set; } = 20;

    /// <summary>
    /// 全局航线过渡速度
    /// </summary>
    /// <remarks>
    /// 飞行器飞往每条航线首航点的速度。航线任务中断时，飞行器从当前位置恢复至断点的速度。
    /// </remarks>
    [Required]
    [Range(1, 15)]
    [XmlElement("globalTransitionalSpeed", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public double GlobalTransitionalSpeed { get; set; } = 10;

    /// <summary>
    /// 全局返航高度
    /// </summary>
    /// <remarks>
    /// 飞行器返航时，先爬升至该高度，再进行返航
    /// </remarks>
    [Required]
    [Range(2, 1500)]
    [XmlElement("globalRTHHeight", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public double GlobalRTHHeight { get; set; } = 100;

    /// <summary>
    /// 飞行器机型信息
    /// </summary>
    [XmlElement("droneInfo", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public DroneInfo DroneInfo { get; set; } = new DroneInfo();

    /// <summary>
    /// 航线绕行信息
    /// </summary>
    [XmlElement("autoRerouteInfo", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public AutoRerouteInfo AutoRerouteInfo { get; set; } = new AutoRerouteInfo();

    /// <summary>
    /// 航线避限飞区模式
    /// </summary>
    [XmlElement("waylineAvoidLimitAreaMode", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public int WaylineAvoidLimitAreaMode { get; set; } = 0;

    /// <summary>
    /// 负载机型信息
    /// </summary>
    [XmlElement("payloadInfo", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public PayloadInfo PayloadInfo { get; set; } = new PayloadInfo();
}

/// <summary>
/// 飞行器机型信息
/// </summary>
public class DroneInfo
{
    /// <summary>
    /// 飞行器机型主类型
    /// </summary>
    /// <remarks>
    /// 飞行器主类型枚举值请参考[产品支持](https://developer.dji.com/doc/cloud-api-tutorial/cn/overview/product-support.html)页面中的飞行器/遥控器/机场枚举值中的主类型(type)
    /// </remarks>
    [Required]
    [XmlElement("droneEnumValue", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public int DroneEnumValue { get; set; } = 100;

    /// <summary>
    /// 飞行器机型子类型
    /// </summary>
    /// <remarks>
    /// 飞行器子类型枚举值请参考[产品支持](https://developer.dji.com/doc/cloud-api-tutorial/cn/overview/product-support.html)页面中的飞行器/遥控器/机场枚举值中的子类型(sub_type)
    /// </remarks>
    [Required]
    [XmlElement("droneSubEnumValue", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public int DroneSubEnumValue { get; set; } = 0;
}

/// <summary>
/// 负载机型信息
/// </summary>
public class PayloadInfo
{
    /// <summary>
    /// 负载机型主类型
    /// </summary>
    /// <remarks>
    /// 负载机型类型枚举值请参考[产品支持](https://developer.dji.com/doc/cloud-api-tutorial/cn/overview/product-support.html)页面中的相机枚举值中type-subtype-gimbalindex中的type字段
    /// </remarks>
    [Required]
    [XmlElement("payloadEnumValue", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public int PayloadEnumValue { get; set; } = 98;

    /// <summary>
    /// 负载机型子类型
    /// </summary>
    /// <remarks>
    /// 负载机型子类型枚举值请参考[产品支持](https://developer.dji.com/doc/cloud-api-tutorial/cn/overview/product-support.html)页面中的相机枚举值中type-subtype-gimbalindex中的subtype字段
    /// </remarks>
    [Required]
    [XmlElement("payloadSubEnumValue", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public int PayloadSubEnumValue { get; set; } = 0;

    /// <summary>
    /// 负载挂载位置
    /// </summary>
    /// <remarks>
    /// 负载挂载位置枚举值请参考[产品支持](https://developer.dji.com/doc/cloud-api-tutorial/cn/overview/product-support.html)页面中的相机枚举值中type-subtype-gimbalindex中的gimbalindex字段
    /// </remarks>
    [Required]
    [XmlElement("payloadPositionIndex", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public int PayloadPositionIndex { get; set; } = 0;
}

/// <summary>
/// 航线绕行信息
/// </summary>
public class AutoRerouteInfo
{
    /// <summary>
    /// 过渡航线自动绕行模式
    /// </summary>
    [XmlElement("transitionalAutoRerouteMode", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public int TransitionalAutoRerouteMode { get; set; } = 1;

    /// <summary>
    /// 任务航线自动绕行模式
    /// </summary>
    [XmlElement("missionAutoRerouteMode", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public int MissionAutoRerouteMode { get; set; } = 1;
}

/// <summary>
/// 航线信息
/// </summary>
[XmlRoot("Folder", Namespace = "http://www.opengis.net/kml/2.2")]
public class Folder
{
    /// <summary>
    /// 模板ID
    /// </summary>
    /// <remarks>
    /// 在一个kmz文件内该ID唯一。建议从0开始单调连续递增。在template.kml和waylines.wpml文件中，将使用该id将模板与所生成的可执行航线进行关联。
    /// </remarks>
    [Required]
    [Range(0, 65535)]
    [XmlElement("templateId", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public int TemplateId { get; set; } = 0;

    /// <summary>
    /// 执行高度模式
    /// </summary>
    /// <remarks>
    /// WGS84：椭球高模式
    /// relativeToStartPoint：相对起飞点高度模式
    /// realTimeFollowSurface: 使用实时仿地模式，仅支持M3E/M3T/M3M
    /// </remarks>
    [Required]
    [RegularExpression("^(WGS84|relativeToStartPoint|realTimeFollowSurface)$")]
    [XmlElement("executeHeightMode", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public string ExecuteHeightMode { get; set; } = "WGS84";

    /// <summary>
    /// 航线ID
    /// </summary>
    /// <remarks>
    /// 在一条航线中该ID唯一。建议从0开始单调连续递增。
    /// </remarks>
    [Required]
    [Range(0, 65535)]
    [XmlElement("waylineId", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public int WaylineId { get; set; } = 0;

    /// <summary>
    /// 全局航线飞行速度
    /// </summary>
    /// <remarks>
    /// 该元素定义了此模板生成的整段航线中，飞行器的目标飞行速度。如果额外定义了某航点的该元素，则局部定义会覆盖全局定义。
    /// </remarks>
    [Required]
    [Range(1, 15)]
    [XmlElement("autoFlightSpeed", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public double AutoFlightSpeed { get; set; } = 10;

    /// <summary>
    /// 实时仿地模式是否根据FOV
    /// </summary>
    [XmlElement("realTimeFollowSurfaceByFov", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public int RealTimeFollowSurfaceByFov { get; set; } = 0;

    /// <summary>
    /// 距离
    /// </summary>
    [XmlElement("distance", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public double Distance { get; set; } = 2735.49487304688;

    /// <summary>
    /// 持续时间
    /// </summary>
    [XmlElement("duration", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public double Duration { get; set; } = 352.969497680664;

    /// <summary>
    /// 航点信息
    /// </summary>
    [XmlElement("Placemark", Namespace = "http://www.opengis.net/kml/2.2")]
    public Placemark Placemark { get; set; } = new Placemark();
}

/// <summary>
/// 航点信息
/// </summary>
public class Placemark
{
    /// <summary>
    /// 航点经纬度
    /// </summary>
    [XmlElement("Point", Namespace = "http://www.opengis.net/kml/2.2")]
    public Point Point { get; set; } = new Point();

    /// <summary>
    /// 航点序号
    /// </summary>
    /// <remarks>
    /// 在一条航线内该ID唯一。该序号必须从0开始单调连续递增。
    /// </remarks>
    [Required]
    [Range(0, 65535)]
    [XmlElement("index", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public int Index { get; set; } = 0;

    /// <summary>
    /// 航点执行高度
    /// </summary>
    /// <remarks>
    /// 该元素仅在waylines.wpml中使用。具体高程参考平面在"wpml:executeHeightMode"中声明。
    /// </remarks>
    [Required]
    [XmlElement("executeHeight", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public double ExecuteHeight { get; set; } = 100;

    /// <summary>
    /// 航点飞行速度，当前航点飞向下一个航点的速度
    /// </summary>
    /// <remarks>
    /// 当且仅当"wpml:useGlobalSpeed"为"0"时必需
    /// </remarks>
    [Required]
    [Range(1, 15)]
    [XmlElement("waypointSpeed", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public double WaypointSpeed { get; set; } = 10;

    /// <summary>
    /// 偏航角模式参数
    /// </summary>
    /// <remarks>
    /// 当且仅当"wpml:useGlobalHeadingParam"为"0"时必需
    /// </remarks>
    [XmlElement("waypointHeadingParam", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public WaypointHeadingParam WaypointHeadingParam { get; set; } = new WaypointHeadingParam();

    /// <summary>
    /// 航点类型（航点转弯模式）
    /// </summary>
    /// <remarks>
    /// 当且仅当"wpml:useGlobalTurnParam"为"0"时必需
    /// </remarks>
    [XmlElement("waypointTurnParam", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public WaypointTurnParam WaypointTurnParam { get; set; } = new WaypointTurnParam();

    /// <summary>
    /// 该航段是否贴合直线
    /// </summary>
    /// <remarks>
    /// 当且仅当"wpml:waypointTurnParam"内"waypointTurnMode"被设置为"toPointAndStopWithContinuityCurvature"或"toPointAndPassWithContinuityCurvature"时必需。如果此元素被设置，则局部定义会覆盖全局定义。
    /// </remarks>
    [XmlElement("useStraightLine", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public int UseStraightLine { get; set; } = 1;

    /// <summary>
    /// 航点初始动作
    /// </summary>
    [XmlElement("actionGroup", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public ActionGroup ActionGroup { get; set; } = new ActionGroup();

    /// <summary>
    /// 是否危险点
    /// </summary>
    [XmlElement("isRisky", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public int IsRisky { get; set; } = 0;

    /// <summary>
    /// 航点工作类型
    /// </summary>
    [XmlElement("waypointWorkType", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public int WaypointWorkType { get; set; } = 0;
}

/// <summary>
/// 航点经纬度
/// </summary>
public class Point
{
    /// <summary>
    /// 经纬度坐标
    /// </summary>
    [XmlElement("coordinates", Namespace = "http://www.opengis.net/kml/2.2")]
    public string Coordinates { get; set; } = "114.375413283,30.662208389";
}

/// <summary>
/// 偏航角模式参数
/// </summary>
public class WaypointHeadingParam
{
    /// <summary>
    /// 偏航角模式
    /// </summary>
    [XmlElement("waypointHeadingMode", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public string WaypointHeadingMode { get; set; } = "followWayline";

    /// <summary>
    /// 偏航角
    /// </summary>
    [XmlElement("waypointHeadingAngle", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public double WaypointHeadingAngle { get; set; } = 0;

    /// <summary>
    /// POI点
    /// </summary>
    [XmlElement("waypointPoiPoint", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public string WaypointPoiPoint { get; set; } = "0.000000,0.000000,0.000000";

    /// <summary>
    /// 偏航角角度是否启用
    /// </summary>
    [XmlElement("waypointHeadingAngleEnable", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public int WaypointHeadingAngleEnable { get; set; } = 0;

    /// <summary>
    /// 航道路径模式
    /// </summary>
    [XmlElement("waypointHeadingPathMode", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public string WaypointHeadingPathMode { get; set; } = "followBadArc";

    /// <summary>
    /// POI索引
    /// </summary>
    [XmlElement("waypointHeadingPoiIndex", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public int WaypointHeadingPoiIndex { get; set; } = 0;
}

/// <summary>
/// 航点类型（航点转弯模式）
/// </summary>
public class WaypointTurnParam
{
    /// <summary>
    /// 航点转弯模式
    /// </summary>
    [XmlElement("waypointTurnMode", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public string WaypointTurnMode { get; set; } = "toPointAndStopWithDiscontinuityCurvature";

    /// <summary>
    /// 转弯阻尼距离
    /// </summary>
    [XmlElement("waypointTurnDampingDist", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public double WaypointTurnDampingDist { get; set; } = 0;
}

/// <summary>
/// 航点初始动作
/// </summary>
/// <remarks>
/// 该元素用于规划一系列初始动作，在航线开始前执行。航线中断恢复时，先执行初始动作，再执行航点动作。
/// </remarks>
public class ActionGroup
{
    /// <summary>
    /// 动作组ID
    /// </summary>
    [XmlElement("actionGroupId", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public int ActionGroupId { get; set; } = 0;

    /// <summary>
    /// 动作组起始索引
    /// </summary>
    [XmlElement("actionGroupStartIndex", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public int ActionGroupStartIndex { get; set; } = 0;

    /// <summary>
    /// 动作组结束索引
    /// </summary>
    [XmlElement("actionGroupEndIndex", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public int ActionGroupEndIndex { get; set; } = 0;

    /// <summary>
    /// 动作组模式
    /// </summary>
    [XmlElement("actionGroupMode", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public string ActionGroupMode { get; set; } = "sequence";

    /// <summary>
    /// 动作触发条件
    /// </summary>
    [XmlElement("actionTrigger", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public ActionTrigger ActionTrigger { get; set; } = new ActionTrigger();

    /// <summary>
    /// 动作列表
    /// </summary>
    [XmlElement("action", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public List<Action> Actions { get; set; } = new List<Action>();
}

/// <summary>
/// 动作触发条件
/// </summary>
public class ActionTrigger
{
    /// <summary>
    /// 触发类型
    /// </summary>
    [XmlElement("actionTriggerType", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public string ActionTriggerType { get; set; } = "reachPoint";
}

/// <summary>
/// 动作
/// </summary>
public class Action
{
    /// <summary>
    /// 动作ID
    /// </summary>
    [XmlElement("actionId", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public int ActionId { get; set; } = 0;

    /// <summary>
    /// 动作执行器功能
    /// </summary>
    [XmlElement("actionActuatorFunc", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public string ActionActuatorFunc { get; set; } = "rotateYaw";

    /// <summary>
    /// 动作执行器功能参数
    /// </summary>
    [XmlElement("actionActuatorFuncParam", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public ActionActuatorFuncParam ActionActuatorFuncParam { get; set; } = new ActionActuatorFuncParam();
}

/// <summary>
/// 动作执行器功能参数
/// </summary>
public class ActionActuatorFuncParam
{
    /// <summary>
    /// 飞行器目标偏航角（相对于地理北）
    /// </summary>
    [XmlElement("aircraftHeading", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public double AircraftHeading { get; set; } = 0;

    /// <summary>
    /// 飞行路径模式
    /// </summary>
    [XmlElement("aircraftPathMode", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public string AircraftPathMode { get; set; } = "counterClockwise";

    /// <summary>
    /// 云台俯仰角度
    /// </summary>
    [XmlElement("gimbalPitchRotateAngle", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public double GimbalPitchRotateAngle { get; set; } = 0;

    /// <summary>
    /// 云台旋转角度
    /// </summary>
    [XmlElement("gimbalYawRotateAngle", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public double GimbalYawRotateAngle { get; set; } = 0;

    /// <summary>
    /// 云台旋转时间
    /// </summary>
    [XmlElement("gimbalRotateTime", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public double GimbalRotateTime { get; set; } = 0;

    /// <summary>
    /// 变焦焦距
    /// </summary>
    [XmlElement("focalLength", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public double FocalLength { get; set; } = 24;

    /// <summary>
    /// 是否使用焦距因子
    /// </summary>
    [XmlElement("isUseFocalFactor", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public int IsUseFocalFactor { get; set; } = 0;

    /// <summary>
    /// 负载挂载位置
    /// </summary>
    [XmlElement("payloadPositionIndex", Namespace = "http://www.dji.com/wpmz/1.0.6")]
    public int PayloadPositionIndex { get; set; } = 0;
}
