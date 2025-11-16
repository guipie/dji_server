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
using System.ComponentModel.DataAnnotations;

namespace Dji.Application.Entity.Dto
{
    /// <summary>
    /// WPML KML 模板根对象
    /// </summary>
    public class WaylineTemplateKml
    {
        /// <summary>
        /// 航线文档信息
        /// </summary>
        [Required]
        public Document Document { get; set; } = new Document();
    }

    /// <summary>
    /// 航线文档
    /// </summary>
    public class Document
    {
        /// <summary>
        /// 文档标题（可选，用于显示）
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// 任务配置信息
        /// </summary>
        [Required]
        public MissionConfig MissionConfig { get; set; } = new MissionConfig();

        /// <summary>
        /// 航线模板列表（至少一个）
        /// </summary>
        [Required]
        public List<WaypointTemplate> Templates { get; set; } = new List<WaypointTemplate>();
    }

    /// <summary>
    /// 航线任务配置信息
    /// </summary>
    public class MissionConfig
    {
        /// <summary>
        /// 飞行模式：飞往航线的方式
        /// </summary>
        /// <remarks>
        /// - safely：安全模式（默认），沿路径平滑飞行，避障生效  
        /// - pointToPoint：点对点模式，直接飞向目标点，可能忽略障碍物
        /// </remarks>
        [Required]
        [RegularExpression("^(safely|pointToPoint)$", ErrorMessage = "FlyToWaylineMode 必须为 'safely' 或 'pointToPoint'")]
        public string FlyToWaylineMode { get; set; } = "safely";

        /// <summary>
        /// 任务结束后的动作
        /// </summary>
        /// <remarks>
        /// - goHome：返航  
        /// - noAction：无动作  
        /// - autoLand：自动降落  
        /// - gotoFirstWaypoint：返回第一个航点
        /// </remarks>
        [Required]
        [RegularExpression("^(goHome|noAction|autoLand|gotoFirstWaypoint)$")]
        public string FinishAction { get; set; } = "goHome";

        /// <summary>
        /// 遥控器信号丢失时的行为
        /// </summary>
        /// <remarks>
        /// - goContinue：继续执行任务  
        /// - executeLostAction：执行预设的失控动作（通常为返航）
        /// </remarks>
        [Required]
        [RegularExpression("^(goContinue|executeLostAction)$")]
        public string ExitOnRCLost { get; set; } = "goContinue";

        /// <summary>
        /// 起飞安全高度（单位：米）
        /// </summary>
        /// <remarks>必须 ≥1 米，建议 ≥20 米以确保安全</remarks>
        [Range(1.0, 500.0, ErrorMessage = "TakeOffSecurityHeight 必须在 1 ~ 500 米之间")]
        public double TakeOffSecurityHeight { get; set; } = 20.0;

        /// <summary>
        /// 全局过渡速度（单位：m/s）
        /// </summary>
        [Range(1.0, 20.0)]
        public double GlobalTransitionalSpeed { get; set; } = 8.0;

        /// <summary>
        /// 全局返航高度（单位：米）
        /// </summary>
        /// <remarks>必须 ≥20 米，确保避开障碍物</remarks>
        [Range(20.0, 500.0)]
        public double GlobalRTHHeight { get; set; } = 100.0;

        /// <summary>
        /// 无人机信息
        /// </summary>
        [Required]
        public DroneInfo DroneInfo { get; set; } = new DroneInfo();

        /// <summary>
        /// 载荷信息
        /// </summary>
        [Required]
        public PayloadInfo PayloadInfo { get; set; } = new PayloadInfo();
    }

    /// <summary>
    /// 无人机信息
    /// </summary>
    public class DroneInfo
    {
        /// <summary>
        /// 无人机型号（如 M300RTK、Mavic3E 等）
        /// </summary>
        [Required]
        public string DroneType { get; set; } = "M300RTK";

        /// <summary>
        /// 固件版本（可选）
        /// </summary>
        public string? FirmwareVersion { get; set; }
    }

    /// <summary>
    /// 载荷信息
    /// </summary>
    public class PayloadInfo
    {
        /// <summary>
        /// 载荷位置索引（从 0 开始）
        /// </summary>
        [Required]
        public int PayloadPositionIndex { get; set; } = 0;

        /// <summary>
        /// 载荷类型（如 P1、Zenmuse H20T、XT2 等）
        /// </summary>
        [Required]
        public string PayloadType { get; set; } = "P1";
    }

    /// <summary>
    /// 航线模板定义
    /// </summary>
    public class WaypointTemplate
    {
        /// <summary>
        /// 预定义模板类型
        /// </summary>
        /// <remarks>
        /// 模板为用户提供了快速生成航线的方案。用户填充模板元素，再导入大疆支持客户端（如 DJI Pilot），
        /// 即可快速生成可执行的测绘/巡检航线。
        /// <para>取值定义：</para>
        /// <list type="bullet">
        ///   <item><description><c>waypoint</c>：航点飞行</description></item>
        ///   <item><description><c>mapping2d</c>：建图航拍（正射影像）</description></item>
        ///   <item><description><c>mapping3d</c>：倾斜摄影（多角度建模）</description></item>
        ///   <item><description><c>mappingStrip</c>：航带飞行（长条带状区域）</description></item>
        /// </list>
        /// </remarks>
        [Required]
        [RegularExpression("^(waypoint|mapping2d|mapping3d|mappingStrip)$",
            ErrorMessage = "TemplateType 必须为 waypoint / mapping2d / mapping3d / mappingStrip")]
        public string TemplateType { get; set; } = "waypoint";

        /// <summary>
        /// 坐标系参数
        /// </summary>
        [Required]
        public WaylineCoordinateSysParam CoordinateSysParam { get; set; } = new WaylineCoordinateSysParam();

        /// <summary>
        /// 自动飞行速度（单位：m/s）
        /// </summary>
        [Range(0.1, 15.0, ErrorMessage = "AutoFlightSpeed 必须在 0.1 ~ 15 m/s 之间")]
        public double AutoFlightSpeed { get; set; } = 7.0;

        /// <summary>
        /// 云台俯仰控制模式
        /// </summary>
        /// <remarks>
        /// - manual：手动指定角度（由每个航点的 GimbalPitchAngle 决定）  
        /// - usePointSetting：使用航点设置（同 manual，但语义更明确）
        /// </remarks>
        [Required]
        [RegularExpression("^(manual|usePointSetting)$")]
        public string GimbalPitchMode { get; set; } = "usePointSetting";

        /// <summary>
        /// 全局航向参数（可选）
        /// </summary>
        public GlobalWaypointHeadingParam? GlobalWaypointHeadingParam { get; set; }

        /// <summary>
        /// 全局转弯模式
        /// </summary>
        /// <remarks>
        /// - toPointAndStopWithDiscontinuityCurvature：到达航点后停止，转弯不连续（默认）  
        /// - toPointAndStopWithContinuityCurvature：平滑停止  
        /// - curvedTrajectory：曲线轨迹（不停止）
        /// </remarks>
        [Required]
        [RegularExpression("^(toPointAndStopWithDiscontinuityCurvature|toPointAndStopWithContinuityCurvature|curvedTrajectory)$")]
        public string GlobalWaypointTurnMode { get; set; } = "toPointAndStopWithDiscontinuityCurvature";

        /// <summary>
        /// 航点列表
        /// </summary>
        [Required]
        public List<Placemark> Placemarks { get; set; } = new List<Placemark>();
    }

    /// <summary>
    /// 坐标系参数
    /// </summary>
    public class WaylineCoordinateSysParam
    {
        /// <summary>
        /// 坐标系类型
        /// </summary>
        /// <remarks>
        /// - WGS84：经纬度坐标系（十进制度）  
        /// - UTM：通用横轴墨卡托投影（较少用）
        /// </remarks>
        [Required]
        [RegularExpression("^(WGS84|UTM)$")]
        public string CoordinateSystem { get; set; } = "WGS84";

        /// <summary>
        /// 高度参考系
        /// </summary>
        /// <remarks>
        /// - WGS84：椭球高  
        /// - AGL：离地高度（Above Ground Level）
        /// </remarks>
        [Required]
        [RegularExpression("^(WGS84|AGL)$")]
        public string HeightReference { get; set; } = "WGS84";
    }

    /// <summary>
    /// 全局航向参数
    /// </summary>
    public class GlobalWaypointHeadingParam
    {
        /// <summary>
        /// 航点朝向模式
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        ///   <item><description><c>followWayline</c>：沿航线方向自动调整机头朝向（默认）</description></item>
        ///   <item><description><c>fixedAngle</c>：固定角度，需配合 WaypointHeadingAngle 使用</description></item>
        ///   <item><description><c>poi</c>：朝向兴趣点（POI），需提供 WaypointPoiPoint 坐标</description></item>
        /// </list>
        /// </remarks>
        [Required]
        [RegularExpression("^(followWayline|fixedAngle|poi)$",
            ErrorMessage = "WaypointHeadingMode 必须为 followWayline / fixedAngle / poi")]
        public string WaypointHeadingMode { get; set; } = "followWayline";

        /// <summary>
        /// 固定航向角（仅当 WaypointHeadingMode = fixedAngle 时有效）
        /// </summary>
        /// <remarks>
        /// 单位：度，范围 [-180, 180]  
        /// 正北为 0°，顺时针为正（东为 90°，南为 180°/-180°，西为 -90°）
        /// </remarks>
        [Range(-180.0, 180.0, ErrorMessage = "WaypointHeadingAngle 必须在 -180 到 180 度之间")]
        public double WaypointHeadingAngle { get; set; } = 0.0;

        /// <summary>
        /// 兴趣点坐标（仅当 WaypointHeadingMode = poi 时必填）
        /// </summary>
        /// <remarks>
        /// 格式："{latitude},{longitude}"，例如 "30.123456,120.123456"
        /// </remarks>
        public string? WaypointPoiPoint { get; set; }

        /// <summary>
        /// 航向路径模式（可选）
        /// </summary>
        public string? WaypointHeadingPathMode { get; set; }
    }

    /// <summary>
    /// 航点（Placemark）定义
    /// </summary>
    public class Placemark
    {
        /// <summary>
        /// 航点索引（从 0 开始，必须连续）
        /// </summary>
        [Required]
        public int Index { get; set; }

        /// <summary>
        /// 经度（十进制度）
        /// </summary>
        [Range(-180.0, 180.0, ErrorMessage = "经度必须在 -180 到 180 之间")]
        public double Longitude { get; set; }

        /// <summary>
        /// 纬度（十进制度）
        /// </summary>
        [Range(-90.0, 90.0, ErrorMessage = "纬度必须在 -90 到 90 之间")]
        public double Latitude { get; set; }

        /// <summary>
        /// 椭球高（单位：米，WGS84 椭球面起算）
        /// </summary>
        [Range(-1000.0, 10000.0)]
        public double EllipsoidHeight { get; set; }

        /// <summary>
        /// 相对起飞点高度（单位：米）
        /// </summary>
        [Range(0.0, 500.0)]
        public double Height { get; set; }

        /// <summary>
        /// 云台俯仰角（单位：度）
        /// </summary>
        /// <remarks>
        /// 范围通常为 [-90, 30]：  
        /// -90° 为垂直向下，0° 为水平，30° 为略微上仰（部分机型支持）
        /// </remarks>
        [Required]
        [Range(-90.0, 30.0, ErrorMessage = "GimbalPitchAngle 必须在 -90 到 30 度之间")]
        public double GimbalPitchAngle { get; set; } = 0.0;

        /// <summary>
        /// 动作列表（可选）
        /// </summary>
        public List<Action>? Actions { get; set; }
    }

    /// <summary>
    /// 航点动作
    /// </summary>
    public class Action
    {
        /// <summary>
        /// 动作唯一 ID
        /// </summary>
        [Required]
        public int ActionId { get; set; }

        /// <summary>
        /// 动作执行器功能类型
        /// </summary>
        /// <remarks>
        /// - takePhoto：拍照  
        /// - startRecord：开始录像  
        /// - stopRecord：停止录像  
        /// - gimbalRotate：云台旋转  
        /// - hover：悬停
        /// </remarks>
        [Required]
        [RegularExpression("^(takePhoto|startRecord|stopRecord|gimbalRotate|hover)$")]
        public string ActionActuatorFunc { get; set; } = "takePhoto";

        /// <summary>
        /// 动作参数
        /// </summary>
        [Required]
        public ActionActuatorFuncParam ActionActuatorFuncParam { get; set; } = new ActionActuatorFuncParam();
    }

    /// <summary>
    /// 动作执行器参数
    /// </summary>
    public class ActionActuatorFuncParam
    {
        /// <summary>
        /// 云台俯仰旋转角度（单位：度）
        /// </summary>
        /// <remarks>仅当 ActionActuatorFunc = gimbalRotate 时有效</remarks>
        [Range(-90.0, 30.0)]
        public double? GimbalPitchRotateAngle { get; set; }

        /// <summary>
        /// 云台横滚旋转角度（单位：度）
        /// </summary>
        [Range(-45.0, 45.0)]
        public double? GimbalRollRotateAngle { get; set; }

        /// <summary>
        /// 云台偏航旋转角度（单位：度）
        /// </summary>
        [Range(-360.0, 360.0)]
        public double? GimbalYawRotateAngle { get; set; }

        /// <summary>
        /// 云台旋转时间（单位：秒）
        /// </summary>
        [Range(0.1, 60.0)]
        public double? GimbalRotateTime { get; set; }

        /// <summary>
        /// 载荷位置索引（从 0 开始）
        /// </summary>
        [Required]
        public int PayloadPositionIndex { get; set; } = 0;
    }
}