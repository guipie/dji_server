// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Newtonsoft.Json.Linq;

namespace Dji.Application.Service.DjiWayline.Dto;

/// <summary>
/// 创建/保存航点航线请求
/// </summary>
/// <remarks>
/// 结构与前端 <c>WaylineCreateParams</c> 一一对应；后端据此生成 template.kml 与 waylines.wpml，
/// 并打包为大疆机场可识别的 KMZ 文件。
/// </remarks>
public class CreateWaypointWaylineRequest
{
    /// <summary>航线名称（同一空间内唯一）</summary>
    public string WaylineName { get; set; } = string.Empty;

    /// <summary>模板中文名称，如“航点航线”</summary>
    public string TemplateStr { get; set; }

    /// <summary>模板类型：1=waypoint 2=mapping2d 3=mapping3d 4=mappingStrip</summary>
    public int TemplateType { get; set; } = 1;

    /// <summary>飞行器显示名称</summary>
    public string DroneModel { get; set; }

    /// <summary>设备枚举标识（Domain_Type_SubType）</summary>
    public string DomainTypeSubType { get; set; }

    /// <summary>配件</summary>
    public string Acc { get; set; }

    /// <summary>所属空间；为空时取当前用户的默认空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>任务全局配置</summary>
    public MissionConfig MissionConfig { get; set; } = new();

    /// <summary>航线（模板）配置</summary>
    public Folder Folder { get; set; } = new();

    /// <summary>前端扩展参数（高度模式、定向拍照模式、参考起飞点坐标）</summary>
    public WaylineExt Ext { get; set; } = new();
}

/// <summary>
/// 前端扩展参数
/// </summary>
public class WaylineExt
{
    /// <summary>
    /// 航点高度模式：
    /// hb=海拔高度(EGM96) / xdqfd=相对起飞点高度 / xddm=相对地面高度(AGL)
    /// </summary>
    public string WaylinePointHeightMode { get; set; } = "xdqfd";

    /// <summary>定向拍照模式：normalPhoto / lowLightSmartShooting</summary>
    public string OrientedPhotoMode { get; set; } = "normalPhoto";

    /// <summary>参考起飞点坐标</summary>
    public HomeCoordinate HomeCoordinate { get; set; }
}

/// <summary>
/// 参考起飞点坐标
/// </summary>
public class HomeCoordinate
{
    public double Longitude { get; set; }

    public double Latitude { get; set; }

    public double? Height { get; set; }
}

/// <summary>
/// 任务全局配置（对应 wpml:missionConfig）
/// </summary>
public class MissionConfig
{
    /// <summary>飞向首航点模式：safely（垂直爬升）/ pointToPoint（倾斜爬升）</summary>
    public string FlyToWaylineMode { get; set; } = "safely";

    /// <summary>安全起飞高度（米）</summary>
    public double TakeOffSecurityHeight { get; set; } = 20;

    /// <summary>参考起飞点海拔高度（米）</summary>
    public double TakeOffRefPointAGLHeight { get; set; } = 60;

    /// <summary>全局航线过渡速度（米/秒）</summary>
    public double GlobalTransitionalSpeed { get; set; } = 14;

    /// <summary>结束动作：goHome / noAction / autoLand / gotoFirstWaypoint</summary>
    public string FinishAction { get; set; } = "goHome";

    /// <summary>是否开启航线绕行</summary>
    public bool AutoRerouteInfoVal { get; set; }

    /// <summary>全局返航高度（米），未指定时默认 100</summary>
    public double? GlobalRTHHeight { get; set; }

    /// <summary>失控是否继续执行航线：goContinue / executeLostAction</summary>
    public string ExitOnRCLost { get; set; }

    /// <summary>失控动作：goBack / landing / hover</summary>
    public string ExecuteRCLostAction { get; set; }
}

/// <summary>
/// 航线（模板）配置（对应 template.kml 的 wpml:Folder）
/// </summary>
public class Folder
{
    /// <summary>全局航线飞行速度（米/秒）</summary>
    public double AutoFlightSpeed { get; set; } = 10;

    /// <summary>全局航点转弯模式</summary>
    public string GlobalWaypointTurnMode { get; set; } = "toPointAndStopWithDiscontinuityCurvature";

    /// <summary>云台俯仰角控制模式：manual / usePointSetting</summary>
    public string GimbalPitchMode { get; set; } = "manual";

    /// <summary>全局航线高度（米），未指定时取首个航点高度</summary>
    public double? GlobalHeight { get; set; }

    /// <summary>全局偏航角模式参数</summary>
    public GlobalWaypointHeadingParam GlobalWaypointHeadingParam { get; set; } = new();

    /// <summary>负载设置</summary>
    public PayloadParam PayloadParam { get; set; } = new();

    /// <summary>航点列表</summary>
    public List<Placemark> Placemarks { get; set; } = [];
}

/// <summary>
/// 全局偏航角模式参数
/// </summary>
public class GlobalWaypointHeadingParam
{
    /// <summary>followWayline / manually / fixed / smoothTransition</summary>
    public string WaypointHeadingMode { get; set; } = "followWayline";

    /// <summary>clockwise / counterClockwise / followBadArc</summary>
    public string WaypointHeadingPathMode { get; set; } = "followBadArc";

    /// <summary>目标偏航角</summary>
    public double? WaypointHeadingAngle { get; set; }

    /// <summary>朝向兴趣点，格式“纬度,经度,高度”</summary>
    public string WaypointPoiPoint { get; set; }
}

/// <summary>
/// 负载参数（对应 wpml:payloadParam）
/// </summary>
public class PayloadParam
{
    /// <summary>图片格式，如 wide,ir,zoom</summary>
    public List<string> ImageFormat { get; set; } = [];

    /// <summary>负载扫描模式：repetitive / nonRepetitive</summary>
    public string ScanningMode { get; set; } = "repetitive";

    /// <summary>负载挂载位置索引</summary>
    public int? PayloadPositionIndex { get; set; }
}

/// <summary>
/// 航点
/// </summary>
public class Placemark
{
    /// <summary>前端生成的航点唯一标识，仅用于前端列表 key，不写入 KMZ</summary>
    public string Guid { get; set; }

    /// <summary>航点坐标，格式“经度,纬度,高度”</summary>
    public string Point { get; set; } = string.Empty;

    /// <summary>航点高度（按高度模式解释）</summary>
    public double ExecuteHeight { get; set; }

    /// <summary>是否使用全局高度</summary>
    public bool? UseGlobalHeight { get; set; }

    /// <summary>WGS84 椭球高，与 executeHeight 同时存在时优先使用</summary>
    public double? EllipsoidHeight { get; set; }

    /// <summary>是否使用全局飞行速度</summary>
    public bool? UseGlobalSpeed { get; set; }

    /// <summary>航点飞行速度（米/秒），useGlobalSpeed=false 时必需</summary>
    public double? FlyToPointSpeed { get; set; }

    /// <summary>是否使用全局转弯参数</summary>
    public bool UseGlobalTurnParam { get; set; } = true;

    /// <summary>转弯参数，useGlobalTurnParam=false 时必需</summary>
    public WaypointTurnParam WaypointTurnParam { get; set; }

    /// <summary>是否使用全局偏航角参数</summary>
    public bool UseGlobalHeadingParam { get; set; } = true;

    /// <summary>偏航角参数，useGlobalHeadingParam=false 时必需</summary>
    public WaypointHeadingParam WaypointHeadingParam { get; set; }

    /// <summary>云台俯仰角，gimbalPitchMode=usePointSetting 时必需</summary>
    public double? GimbalPitchAngle { get; set; }

    /// <summary>航段是否贴合直线</summary>
    public bool? UseStraightLine { get; set; }

    /// <summary>是否危险点</summary>
    public bool? IsRisky { get; set; }

    /// <summary>航点工作类型（0=正常）</summary>
    public int? WaypointWorkType { get; set; }

    /// <summary>航点动作列表</summary>
    public List<ActionGroup> ActionsGroup { get; set; } = [];
}

/// <summary>
/// 航点转弯参数
/// </summary>
public class WaypointTurnParam
{
    /// <summary>航点转弯模式</summary>
    public string WaypointTurnMode { get; set; }

    /// <summary>航点转弯截距（米）</summary>
    public double? WaypointTurnDampingDist { get; set; }
}

/// <summary>
/// 航点偏航角参数
/// </summary>
public class WaypointHeadingParam
{
    /// <summary>followWayline / manually / fixed / smoothTransition</summary>
    public string WaypointHeadingMode { get; set; }

    /// <summary>目标偏航角，smoothTransition / fixed 时必需</summary>
    public double? WaypointHeadingAngle { get; set; }

    /// <summary>朝向兴趣点，格式“纬度,经度,高度”</summary>
    public string WaypointPoiPoint { get; set; }

    /// <summary>clockwise / counterClockwise / followBadArc</summary>
    public string WaypointHeadingPathMode { get; set; }
}

/// <summary>
/// 航点动作（前端一个航点的每个动作为一条记录）
/// </summary>
/// <remarks>
/// <see cref="ActionActuatorFuncParam"/> 采用弱类型对象：不同动作的参数集合差异极大，
/// 且大疆会持续新增字段，因此这里保留前端提交的原始键值对，
/// 生成 KMZ 时逐个输出为 &lt;wpml:key&gt;value&lt;/wpml:key&gt;，避免因后端枚举不全而丢字段。
/// </remarks>
public class ActionGroup
{
    /// <summary>动作触发器</summary>
    public ActionTrigger ActionTrigger { get; set; }

    /// <summary>动作ID，同一动作组内唯一</summary>
    public int ActionId { get; set; }

    /// <summary>动作执行器类型，如 takePhoto / gimbalRotate / hover</summary>
    public string ActionActuatorFunc { get; set; } = string.Empty;

    /// <summary>前端动作标识（如 takePhotoMultipleTiming），仅用于前端展示</summary>
    public string ActionValue { get; set; }

    /// <summary>动作参数，键名即 WPML 元素名</summary>
    public JObject ActionActuatorFuncParam { get; set; }
}

/// <summary>
/// 动作触发器
/// </summary>
public class ActionTrigger
{
    /// <summary>reachPoint / betweenAdjacentPoints / multipleTiming / multipleDistance</summary>
    public string ActionTriggerType { get; set; } = "reachPoint";

    /// <summary>间隔时间（秒）或间隔距离（米）</summary>
    public double? ActionTriggerParam { get; set; }
}
