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
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Dji.Application.Service.DjiWayline.Dto;

public class CreateWaypointWaylineRequest
{
    [JsonPropertyName("waylineName")]
    public string WaylineName { get; set; } = string.Empty;

    [JsonPropertyName("domainTypeSubType")]
    public string DomainTypeSubType { get; set; }

    [JsonPropertyName("acc")]
    public string Acc { get; set; }

    [JsonPropertyName("templateType")]
    public int TemplateType { get; set; }

    [JsonPropertyName("templateStr")]
    public string TemplateStr { get; set; } = string.Empty;

    [JsonPropertyName("missionConfig")]
    public MissionConfig MissionConfig { get; set; } = new();

    [JsonPropertyName("folder")]
    public Folder Folder { get; set; } = new();
}

public class MissionConfig
{
    [JsonPropertyName("flyToWaylineMode")]
    public string FlyToWaylineMode { get; set; } = string.Empty;

    [JsonPropertyName("takeOffSecurityHeight")]
    public float TakeOffSecurityHeight { get; set; } = 100; // 注意：JSON 中是字符串 "100"

    [JsonPropertyName("takeOffRefPointAGLHeight")]
    public int TakeOffRefPointAGLHeight { get; set; }

    [JsonPropertyName("globalTransitionalSpeed")]
    public int GlobalTransitionalSpeed { get; set; }

    [JsonPropertyName("finishAction")]
    public string FinishAction { get; set; } = string.Empty;

    [JsonPropertyName("autoRerouteInfoVal")]
    public bool AutoRerouteInfoVal { get; set; }
}

public class Folder
{
    [JsonPropertyName("autoFlightSpeed")]
    public int AutoFlightSpeed { get; set; }

    [JsonPropertyName("globalWaypointTurnMode")]
    public string GlobalWaypointTurnMode { get; set; } = string.Empty;

    [JsonPropertyName("gimbalPitchMode")]
    public string GimbalPitchMode { get; set; } = string.Empty;

    [JsonPropertyName("globalWaypointHeadingParam")]
    public GlobalWaypointHeadingParam GlobalWaypointHeadingParam { get; set; } = new();

    [JsonPropertyName("payloadParam")]
    public PayloadParam PayloadParam { get; set; } = new();

    [JsonPropertyName("placemarks")]
    public List<Placemark> Placemarks { get; set; } = new();
}

public class GlobalWaypointHeadingParam
{
    [JsonPropertyName("waypointHeadingMode")]
    public string WaypointHeadingMode { get; set; } = string.Empty;

    [JsonPropertyName("waypointHeadingPathMode")]
    public string WaypointHeadingPathMode { get; set; } = string.Empty;
}

public class PayloadParam
{
    [JsonPropertyName("imageFormat")]
    public List<string> ImageFormat { get; set; } = new();
}

public class Placemark
{
    [JsonPropertyName("point")]
    public string Point { get; set; } = string.Empty;

    [JsonPropertyName("executeHeight")]
    public int ExecuteHeight { get; set; }

    [JsonPropertyName("num")]
    public int? Num { get; set; } // 可能不存在

    [JsonPropertyName("actionsGroup")]
    public List<ActionGroup> ActionsGroup { get; set; } = new();

    [JsonPropertyName("guid")]
    public string? Guid { get; set; }
}

public class ActionGroup
{
    [JsonPropertyName("actionTrigger")]
    public ActionTrigger? ActionTrigger { get; set; }

    [JsonPropertyName("actionId")]
    public int ActionId { get; set; }

    [JsonPropertyName("actionActuatorFunc")]
    public string ActionActuatorFunc { get; set; } = string.Empty;

    [JsonPropertyName("actionValue")]
    public string ActionValue { get; set; } = string.Empty;

    [JsonPropertyName("actionActuatorFuncParam")]
    public ActionActuatorFuncParam? ActionActuatorFuncParam { get; set; }
}

public class ActionTrigger
{
    [JsonPropertyName("_custom")]
    public CustomWrapper? Custom { get; set; }

    // 兼容非 _custom 结构（如 panoShot 中的直接 actionTriggerType）
    [JsonPropertyName("actionTriggerType")]
    public string? ActionTriggerType { get; set; }
}

public class CustomWrapper
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("stateTypeName")]
    public string StateTypeName { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public TriggerValue Value { get; set; } = new();
}

public class TriggerValue
{
    [JsonPropertyName("actionTriggerType")]
    public string ActionTriggerType { get; set; } = string.Empty;

    [JsonPropertyName("actionTriggerParam")]
    public object? ActionTriggerParam { get; set; } // 可能是 int 或其他，用 object 更安全

    // 以下字段按需添加（因为不同 action 类型包含不同字段）
    [JsonPropertyName("payloadLensIndex")]
    public string? PayloadLensIndex { get; set; }

    [JsonPropertyName("fileSuffix")]
    public string? FileSuffix { get; set; }

    [JsonPropertyName("useGlobalPayloadLensIndex")]
    public bool? UseGlobalPayloadLensIndex { get; set; }

    [JsonPropertyName("focalLength")]
    public int? FocalLength { get; set; }

    [JsonPropertyName("aircraftHeading")]
    public int? AircraftHeading { get; set; }

    [JsonPropertyName("aircraftPathMode")]
    public string? AircraftPathMode { get; set; }

    [JsonPropertyName("gimbalHeadingYawBase")]
    public int? GimbalHeadingYawBase { get; set; }

    [JsonPropertyName("gimbalRotateMode")]
    public string? GimbalRotateMode { get; set; }

    [JsonPropertyName("gimbalPitchRotateEnable")]
    public bool? GimbalPitchRotateEnable { get; set; }

    [JsonPropertyName("gimbalPitchRotateAngle")]
    public int? GimbalPitchRotateAngle { get; set; }

    [JsonPropertyName("gimbalRollRotateEnable")]
    public bool? GimbalRollRotateEnable { get; set; }

    [JsonPropertyName("gimbalRollRotateAngle")]
    public int? GimbalRollRotateAngle { get; set; }

    [JsonPropertyName("gimbalYawRotateEnable")]
    public bool? GimbalYawRotateEnable { get; set; }

    [JsonPropertyName("gimbalYawRotateAngle")]
    public int? GimbalYawRotateAngle { get; set; }

    [JsonPropertyName("gimbalRotateTimeEnable")]
    public bool? GimbalRotateTimeEnable { get; set; }

    [JsonPropertyName("gimbalRotateTime")]
    public int? GimbalRotateTime { get; set; }

    [JsonPropertyName("hoverTime")]
    public int? HoverTime { get; set; }

    [JsonPropertyName("directoryName")]
    public string? DirectoryName { get; set; }
}

public class ActionActuatorFuncParam
{
    [JsonPropertyName("_custom")]
    public CustomWrapper Custom { get; set; } = new();
}