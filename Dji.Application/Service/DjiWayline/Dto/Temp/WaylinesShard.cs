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

namespace Dji.Application.Service.DjiWayline.Dto.Temp;

/// <summary>
/// 无人机机型信息
/// </summary>
public class DroneInfo
{
    /// <summary>
    /// 无人机枚举值（如 M30=67）
    /// </summary>
    [Required]
    [Range(0, 65535)]
    [XmlElement("droneEnumValue", Namespace = "http://www.dji.com/wpmz/1.0.2")]
    public int DroneEnumValue { get; set; }

    /// <summary>
    /// 无人机子型号枚举值
    /// </summary>
    [Range(0, 65535)]
    [XmlElement("droneSubEnumValue", Namespace = "http://www.dji.com/wpmz/1.0.2")]
    public int DroneSubEnumValue { get; set; } = 0;
}

/// <summary>
/// 载荷信息
/// </summary>
public class PayloadInfo
{
    /// <summary>
    /// 载荷枚举值（如 P1=52）
    /// </summary>
    [Required]
    [Range(0, 65535)]
    [XmlElement("payloadEnumValue", Namespace = "http://www.dji.com/wpmz/1.0.2")]
    public int PayloadEnumValue { get; set; }

    /// <summary>
    /// 载荷位置索引（从 0 开始）
    /// </summary>
    [Required]
    [Range(0, 10)]
    [XmlElement("payloadPositionIndex", Namespace = "http://www.dji.com/wpmz/1.0.2")]
    public int PayloadPositionIndex { get; set; } = 0;
}

/// <summary>
/// 动作组
/// </summary>
public class ActionGroup
{
    /// <summary>
    /// 动作组 ID
    /// </summary>
    [Required]
    [Range(0, 65535)]
    [XmlElement("actionGroupId", Namespace = "http://www.dji.com/wpmz/1.0.2")]
    public int ActionGroupId { get; set; }

    /// <summary>
    /// 动作组起始航点索引
    /// </summary>
    [Required]
    [Range(0, 65535)]
    [XmlElement("actionGroupStartIndex", Namespace = "http://www.dji.com/wpmz/1.0.2")]
    public int ActionGroupStartIndex { get; set; }

    /// <summary>
    /// 动作组结束航点索引
    /// </summary>
    [Required]
    [Range(0, 65535)]
    [XmlElement("actionGroupEndIndex", Namespace = "http://www.dji.com/wpmz/1.0.2")]
    public int ActionGroupEndIndex { get; set; }

    /// <summary>
    /// 动作组执行模式
    /// </summary>
    /// <remarks>
    /// - sequence：顺序执行  
    /// - parallel：并行执行
    /// </remarks>
    [Required]
    [RegularExpression("^(sequence|parallel)$")]
    [XmlElement("actionGroupMode", Namespace = "http://www.dji.com/wpmz/1.0.2")]
    public string ActionGroupMode { get; set; } = "sequence";

    /// <summary>
    /// 动作触发器
    /// </summary>
    [Required]
    [XmlElement("actionTrigger", Namespace = "http://www.dji.com/wpmz/1.0.2")]
    public ActionTrigger ActionTrigger { get; set; } = new ActionTrigger();

    /// <summary>
    /// 动作列表
    /// </summary>
    [XmlElement("action", Namespace = "http://www.dji.com/wpmz/1.0.2")]
    public List<Action> Actions { get; set; } = new List<Action>();
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
    /// 云台旋转模式
    /// </summary>
    /// <remarks>
    /// - absoluteAngle：绝对角度（默认）  
    /// - relativeAngle：相对角度
    /// </remarks>
    [RegularExpression("^(absoluteAngle|relativeAngle)$")]
    [XmlElement("gimbalRotateMode", Namespace = "http://www.dji.com/wpmz/1.0.2")]
    public string GimbalRotateMode { get; set; } = "absoluteAngle";

    /// <summary>
    /// 是否启用俯仰轴旋转
    /// </summary>
    [XmlElement("gimbalPitchRotateEnable", Namespace = "http://www.dji.com/wpmz/1.0.2")]
    public bool GimbalPitchRotateEnable { get; set; } = false;

    /// <summary>
    /// 俯仰旋转角度（单位：度）
    /// </summary>
    [Range(-90.0, 30.0)]
    [XmlElement("gimbalPitchRotateAngle", Namespace = "http://www.dji.com/wpmz/1.0.2")]
    public double GimbalPitchRotateAngle { get; set; } = 0.0;

    /// <summary>
    /// 是否启用横滚轴旋转
    /// </summary>
    [XmlElement("gimbalRollRotateEnable", Namespace = "http://www.dji.com/wpmz/1.0.2")]
    public bool GimbalRollRotateEnable { get; set; } = false;

    /// <summary>
    /// 横滚旋转角度（单位：度）
    /// </summary>
    [Range(-45.0, 45.0)]
    [XmlElement("gimbalRollRotateAngle", Namespace = "http://www.dji.com/wpmz/1.0.2")]
    public double GimbalRollRotateAngle { get; set; } = 0.0;

    /// <summary>
    /// 是否启用偏航轴旋转
    /// </summary>
    [XmlElement("gimbalYawRotateEnable", Namespace = "http://www.dji.com/wpmz/1.0.2")]
    public bool GimbalYawRotateEnable { get; set; } = false;

    /// <summary>
    /// 偏航旋转角度（单位：度）
    /// </summary>
    [Range(-360.0, 360.0)]
    [XmlElement("gimbalYawRotateAngle", Namespace = "http://www.dji.com/wpmz/1.0.2")]
    public double GimbalYawRotateAngle { get; set; } = 0.0;

    /// <summary>
    /// 是否启用旋转时间控制
    /// </summary>
    [XmlElement("gimbalRotateTimeEnable", Namespace = "http://www.dji.com/wpmz/1.0.2")]
    public bool GimbalRotateTimeEnable { get; set; } = false;

    /// <summary>
    /// 云台旋转时间（单位：秒）
    /// </summary>
    [Range(0.1, 60.0)]
    [XmlElement("gimbalRotateTime", Namespace = "http://www.dji.com/wpmz/1.0.2")]
    public double GimbalRotateTime { get; set; } = 0.0;

    /// <summary>
    /// 文件后缀（仅 takePhoto 有效）
    /// </summary>
    [XmlElement("fileSuffix", Namespace = "http://www.dji.com/wpmz/1.0.2")]
    public string? FileSuffix { get; set; }

    /// <summary>
    /// 载荷位置索引
    /// </summary>
    [Required]
    [Range(0, 10)]
    [XmlElement("payloadPositionIndex", Namespace = "http://www.dji.com/wpmz/1.0.2")]
    public int PayloadPositionIndex { get; set; } = 0;
}