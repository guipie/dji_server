// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Service.DjiWayline.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Dji.Application.Service.DjiWayline.Wayline
{
    /// <summary>
    /// 可执行航线文件（waylines.wpml）根对象
    /// </summary>
    [XmlRoot("kml", Namespace = "http://www.opengis.net/kml/2.2")]
    public class WaylinesWpml
    {
        /// <summary>
        /// 航线文档
        /// </summary>
        [XmlElement("Document")]
        public WaylinesDocument Document { get; set; } = new WaylinesDocument();
    }

    /// <summary>
    /// 航线文档（包含任务配置和多条可执行航线）
    /// </summary>
    public class WaylinesDocument
    {
        /// <summary>
        /// 任务全局配置
        /// </summary>
        [XmlElement(Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public MissionConfig MissionConfig { get; set; } = new MissionConfig();

        /// <summary>
        /// 可执行航线列表（每个 Folder 对应一条航线）
        /// </summary>
        [XmlElement("Folder")]
        public Folder Folder { get; set; } = new Folder();
    }

    /// <summary>
    /// 任务全局配置（与 template.kml 类似，但用于执行阶段）
    /// </summary>
    public class WaylineMissionConfig
    {
        /// <summary>
        /// 飞向首航点模式
        /// </summary>
        /// <remarks>
        /// - safely：安全模式（M30/M300）：先升至首航点高度或安全起飞高度，再平飞  
        /// - pointToPoint：倾斜飞行模式：直接斜飞向首航点
        /// </remarks>
        [Required]
        [RegularExpression("^(safely|pointToPoint)$")]
        [XmlElement("flyToWaylineMode", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public string FlyToWaylineMode { get; set; } = "safely";

        /// <summary>
        /// 航线结束动作
        /// </summary>
        /// <remarks>
        /// - goHome：返航  
        /// - noAction：无动作  
        /// - autoLand：原地降落  
        /// - gotoFirstWaypoint：飞回第一个航点
        /// </remarks>
        [Required]
        [RegularExpression("^(goHome|noAction|autoLand|gotoFirstWaypoint)$")]
        [XmlElement("finishAction", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public string FinishAction { get; set; } = "goHome";

        /// <summary>
        /// 遥控器信号丢失时是否继续执行航线
        /// </summary>
        /// <remarks>
        /// - goContinue：继续执行  
        /// - executeLostAction：退出航线并执行失控动作
        /// </remarks>
        [Required]
        [RegularExpression("^(goContinue|executeLostAction)$")]
        [XmlElement("exitOnRCLost", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public string ExitOnRCLost { get; set; } = "goContinue";

        /// <summary>
        /// 失控动作类型（仅当 exitOnRCLost = executeLostAction 时生效）
        /// </summary>
        /// <remarks>
        /// - goBack：返航  
        /// - landing：降落  
        /// - hover：悬停
        /// </remarks>
        [RegularExpression("^(goBack|landing|hover)$")]
        [XmlElement("executeRCLostAction", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public string? ExecuteRCLostAction { get; set; }

        /// <summary>
        /// 安全起飞高度（单位：米）
        /// </summary>
        /// <remarks>
        /// 遥控器场景：[1.2, 1500]；机场场景：[8, 1500]  
        /// 仅在未起飞时生效
        /// </remarks>
        [Range(1.2, 1500.0)]
        [XmlElement("takeOffSecurityHeight", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public double TakeOffSecurityHeight { get; set; } = 20.0;

        /// <summary>
        /// 全局航线过渡速度（单位：m/s）
        /// </summary>
        /// <remarks>范围：[1, 15]</remarks>
        [Range(1.0, 15.0)]
        [XmlElement("globalTransitionalSpeed", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public double GlobalTransitionalSpeed { get; set; } = 10.0;

        /// <summary>
        /// 全局返航高度（单位：米）
        /// </summary>
        /// <remarks>范围：[2, 1500]</remarks>
        [Range(2.0, 1500.0)]
        [XmlElement("globalRTHHeight", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public double GlobalRTHHeight { get; set; } = 100.0;

        /// <summary>
        /// 无人机信息
        /// </summary>
        [Required]
        [XmlElement("droneInfo", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public DroneInfo DroneInfo { get; set; } = new DroneInfo();

        /// <summary>
        /// 载荷信息
        /// </summary>
        [Required]
        [XmlElement("payloadInfo", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public PayloadInfo PayloadInfo { get; set; } = new PayloadInfo();
    }

  


    /// <summary>
    /// 可执行航线（对应一个 Folder）
    /// </summary>
    public class ExecutableWayline
    {
        /// <summary>
        /// 模板 ID（与 template.kml 中的 templateId 关联）
        /// </summary>
        /// <remarks>建议从 0 开始单调递增</remarks>
        [Required]
        [Range(0, 65535)]
        [XmlElement("templateId", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public int TemplateId { get; set; }

        /// <summary>
        /// 执行高度模式
        /// </summary>
        /// <remarks>
        /// - WGS84：椭球高  
        /// - relativeToStartPoint：相对起飞点高度  
        /// - realTimeFollowSurface：实时仿地（仅 M3E/M3T/M3M 支持）
        /// </remarks>
        [Required]
        [RegularExpression("^(WGS84|relativeToStartPoint|realTimeFollowSurface)$")]
        [XmlElement("executeHeightMode", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public string ExecuteHeightMode { get; set; } = "WGS84";

        /// <summary>
        /// 航线 ID（唯一）
        /// </summary>
        [Required]
        [Range(0, 65535)]
        [XmlElement("waylineId", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public int WaylineId { get; set; }

        /// <summary>
        /// 全局航线飞行速度（单位：m/s）
        /// </summary>
        /// <remarks>范围：[1, 15]</remarks>
        [Range(1.0, 15.0)]
        [XmlElement("autoFlightSpeed", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public double AutoFlightSpeed { get; set; } = 10.0;

        /// <summary>
        /// 航点列表
        /// </summary>
        [XmlElement("Placemark")]
        public List<Waypoint> Waypoints { get; set; } = new List<Waypoint>();

        /// <summary>
        /// 航线初始动作组（可选）
        /// </summary>
        [XmlElement("startActionGroup", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public ActionGroup? StartActionGroup { get; set; }
    }

    /// <summary>
    /// 航点（Placemark）
    /// </summary>
    public class Waypoint
    {
        /// <summary>
        /// 航点经纬度坐标（格式：经度,纬度）
        /// </summary>
        [Required]
        [XmlElement("Point")]
        public PointCoordinates Point { get; set; } = new PointCoordinates();

        /// <summary>
        /// 航点序号（从 0 开始，必须连续）
        /// </summary>
        [Required]
        [Range(0, 65535)]
        [XmlElement("index", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public int Index { get; set; }

        /// <summary>
        /// 航点执行高度（单位：米）
        /// </summary>
        /// <remarks>参考平面由 executeHeightMode 决定</remarks>
        [Required]
        [Range(-1000.0, 10000.0)]
        [XmlElement("executeHeight", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public double ExecuteHeight { get; set; }

        /// <summary>
        /// 航点飞行速度（当前点飞向下一点的速度，单位：m/s）
        /// </summary>
        /// <remarks>仅当 useGlobalSpeed=false 时必填</remarks>
        [Range(1.0, 15.0)]
        [XmlElement("waypointSpeed", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public double? WaypointSpeed { get; set; }

        /// <summary>
        /// 航点偏航角参数
        /// </summary>
        /// <remarks>仅当 useGlobalHeadingParam=false 时必填</remarks>
        [XmlElement("waypointHeadingParam", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public WaypointHeadingParam? WaypointHeadingParam { get; set; }

        /// <summary>
        /// 航点转弯参数
        /// </summary>
        /// <remarks>仅当 useGlobalTurnParam=false 时必填</remarks>
        [XmlElement("waypointTurnParam", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public WaypointTurnParam? WaypointTurnParam { get; set; }

        /// <summary>
        /// 是否使用全局速度（默认 true）
        /// </summary>
        [XmlElement("useGlobalSpeed", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public bool UseGlobalSpeed { get; set; } = true;

        /// <summary>
        /// 是否使用全局航向参数（默认 true）
        /// </summary>
        [XmlElement("useGlobalHeadingParam", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public bool UseGlobalHeadingParam { get; set; } = true;

        /// <summary>
        /// 是否使用全局转弯参数（默认 true）
        /// </summary>
        [XmlElement("useGlobalTurnParam", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public bool UseGlobalTurnParam { get; set; } = true;

        /// <summary>
        /// 动作组列表（可选）
        /// </summary>
        [XmlElement("actionGroup", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public List<ActionGroup>? ActionGroups { get; set; }
    }

    /// <summary>
    /// 经纬度坐标
    /// </summary>
    public class PointCoordinates
    {
        /// <summary>
        /// 坐标字符串（格式：经度,纬度）
        /// </summary>
        /// <example>"120.123456,30.123456"</example>
        [XmlText]
        public string Coordinates { get; set; } = "0,0";

        // 辅助属性（非序列化）
        [XmlIgnore]
        public double Longitude => double.Parse(Coordinates.Split(',')[0]);

        [XmlIgnore]
        public double Latitude => double.Parse(Coordinates.Split(',')[1]);
    }

    /// <summary>
    /// 航点偏航角参数
    /// </summary>
    public class WaypointHeadingParam
    {
        /// <summary>
        /// 航点朝向模式
        /// </summary>
        /// <remarks>
        /// - followWayline：沿航线方向  
        /// - fixedAngle：固定角度（需配合 waypointHeadingAngle）  
        /// - poi：朝向兴趣点
        /// </remarks>
        [Required]
        [RegularExpression("^(followWayline|fixedAngle|poi)$")]
        [XmlElement("waypointHeadingMode", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public string WaypointHeadingMode { get; set; } = "followWayline";

        /// <summary>
        /// 固定航向角（单位：度）
        /// </summary>
        /// <remarks>范围：[-180, 180]</remarks>
        [Range(-180.0, 180.0)]
        [XmlElement("waypointHeadingAngle", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public double WaypointHeadingAngle { get; set; } = 0.0;

        /// <summary>
        /// 兴趣点坐标（格式：纬度,经度）
        /// </summary>
        [XmlElement("waypointPoiPoint", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public string? WaypointPoiPoint { get; set; }
    }

    /// <summary>
    /// 航点转弯参数
    /// </summary>
    public class WaypointTurnParam
    {
        /// <summary>
        /// 航点转弯模式
        /// </summary>
        /// <remarks>
        /// - toPointAndStopWithDiscontinuityCurvature：到达后停止，转弯不连续（默认）  
        /// - toPointAndStopWithContinuityCurvature：平滑停止  
        /// - curvedTrajectory：曲线轨迹（不停止）
        /// </remarks>
        [Required]
        [RegularExpression("^(toPointAndStopWithDiscontinuityCurvature|toPointAndStopWithContinuityCurvature|curvedTrajectory)$")]
        [XmlElement("waypointTurnMode", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public string WaypointTurnMode { get; set; } = "toPointAndStopWithDiscontinuityCurvature";

        /// <summary>
        /// 转弯阻尼距离（单位：米）
        /// </summary>
        [Range(0.0, 100.0)]
        [XmlElement("waypointTurnDampingDist", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public double WaypointTurnDampingDist { get; set; } = 0.0;
    }

 
    /// <summary>
    /// 动作触发器
    /// </summary>
    public class ActionTrigger
    {
        /// <summary>
        /// 触发类型
        /// </summary>
        /// <remarks>
        /// - reachPoint：到达航点时触发（最常用）  
        /// - leavePoint：离开航点时触发
        /// </remarks>
        [Required]
        [RegularExpression("^(reachPoint|leavePoint)$")]
        [XmlElement("actionTriggerType", Namespace = "http://www.dji.com/wpmz/1.0.2")]
        public string ActionTriggerType { get; set; } = "reachPoint";
    }

}