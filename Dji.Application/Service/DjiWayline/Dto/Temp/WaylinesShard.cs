// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Dji.Application.Service.DjiWayline.Dto.Temp;

/// <summary>
/// DJI WPML 相关 XML 命名空间
/// </summary>
/// <remarks>
/// 根节点 kml 使用 KML 命名空间；Document/Folder/Placemark/Point/coordinates 等
/// 结构化节点**不使用任何命名空间**（XmlSerializer 会自动输出 xmlns=""），
/// 这与大疆官方导出的 KMZ 文件完全一致；其余业务节点统一使用 wpml 前缀。
/// </remarks>
public static class WpmlNamespaces
{
    /// <summary>KML 根命名空间</summary>
    public const string Kml = "http://www.opengis.net/kml/2.2";

    /// <summary>DJI WPML 命名空间（序列化前缀固定为 wpml）</summary>
    public const string Wpml = "http://www.dji.com/wpmz/1.0.2";

    /// <summary>序列化时注册的命名空间前缀</summary>
    public static XmlSerializerNamespaces Namespaces()
    {
        var ns = new XmlSerializerNamespaces();
        ns.Add("", Kml);
        ns.Add("wpml", Wpml);
        return ns;
    }

    /// <summary>数值统一使用不受区域设置影响的格式，避免出现逗号小数点</summary>
    public static string Num(double value) => value.ToString("0.#########", CultureInfo.InvariantCulture);

    /// <summary>布尔值按大疆规范输出 0/1</summary>
    public static string Bool(bool value) => value ? "1" : "0";
}

/// <summary>
/// 航线绕行配置（仅 M3D / M4D / M4E 系列机型支持）
/// </summary>
public class WpmlAutoRerouteInfo
{
    /// <summary>任务航线绕行：0 不开启，1 开启</summary>
    [XmlElement("missionAutoRerouteMode", Namespace = WpmlNamespaces.Wpml)]
    public int MissionAutoRerouteMode { get; set; }

    /// <summary>过渡段航线绕行：0 不开启，1 开启</summary>
    [XmlElement("transitionalAutoRerouteMode", Namespace = WpmlNamespaces.Wpml)]
    public int TransitionalAutoRerouteMode { get; set; }
}

/// <summary>
/// 无人机机型信息
/// </summary>
public class DroneInfo
{
    /// <summary>无人机主类型枚举值（如 Matrice 4D 系列 = 91）</summary>
    [XmlElement("droneEnumValue", Namespace = WpmlNamespaces.Wpml)]
    public int DroneEnumValue { get; set; }

    /// <summary>无人机子类型枚举值</summary>
    [XmlElement("droneSubEnumValue", Namespace = WpmlNamespaces.Wpml)]
    public int DroneSubEnumValue { get; set; }
}

/// <summary>
/// 负载（相机）信息
/// </summary>
public class PayloadInfo
{
    /// <summary>负载主类型枚举值</summary>
    [XmlElement("payloadEnumValue", Namespace = WpmlNamespaces.Wpml)]
    public int PayloadEnumValue { get; set; }

    /// <summary>负载子类型枚举值</summary>
    [XmlElement("payloadSubEnumValue", Namespace = WpmlNamespaces.Wpml)]
    public int PayloadSubEnumValue { get; set; }

    /// <summary>负载挂载位置索引</summary>
    [XmlElement("payloadPositionIndex", Namespace = WpmlNamespaces.Wpml)]
    public int PayloadPositionIndex { get; set; }
}

/// <summary>
/// 坐标系参数（仅 template.kml 使用）
/// </summary>
public class WaylineCoordinateSysParam
{
    /// <summary>经纬度坐标系，当前固定 WGS84</summary>
    [XmlElement("coordinateMode", Namespace = WpmlNamespaces.Wpml)]
    public string CoordinateMode { get; set; } = "WGS84";

    /// <summary>航点高程参考平面：EGM96 / relativeToStartPoint / aboveGroundLevel</summary>
    [XmlElement("heightMode", Namespace = WpmlNamespaces.Wpml)]
    public string HeightMode { get; set; } = "EGM96";

    /// <summary>经纬度与高度数据源，仅作标记不影响执行</summary>
    [XmlElement("positioningType", Namespace = WpmlNamespaces.Wpml)]
    public string? PositioningType { get; set; }
}

/// <summary>
/// 航点偏航角参数
/// </summary>
public class WpmlWaypointHeadingParam
{
    /// <summary>followWayline / manually / fixed / smoothTransition</summary>
    [XmlElement("waypointHeadingMode", Namespace = WpmlNamespaces.Wpml)]
    public string WaypointHeadingMode { get; set; } = "followWayline";

    /// <summary>目标偏航角，waypointHeadingMode 为 smoothTransition / fixed 时有效</summary>
    [XmlElement("waypointHeadingAngle", Namespace = WpmlNamespaces.Wpml)]
    public double? WaypointHeadingAngle { get; set; }

    /// <summary>值为空时不输出该元素</summary>
    public bool ShouldSerializeWaypointHeadingAngle() => WaypointHeadingAngle.HasValue;

    /// <summary>朝向的兴趣点，格式“纬度,经度,高度”</summary>
    [XmlElement("waypointPoiPoint", Namespace = WpmlNamespaces.Wpml)]
    public string? WaypointPoiPoint { get; set; }

    /// <summary>clockwise / counterClockwise / followBadArc</summary>
    [XmlElement("waypointHeadingPathMode", Namespace = WpmlNamespaces.Wpml)]
    public string WaypointHeadingPathMode { get; set; } = "followBadArc";
}

/// <summary>
/// 航点转弯参数
/// </summary>
public class WpmlWaypointTurnParam
{
    /// <summary>航点转弯模式</summary>
    [XmlElement("waypointTurnMode", Namespace = WpmlNamespaces.Wpml)]
    public string WaypointTurnMode { get; set; } = "toPointAndStopWithDiscontinuityCurvature";

    /// <summary>航点转弯截距（米）</summary>
    [XmlElement("waypointTurnDampingDist", Namespace = WpmlNamespaces.Wpml)]
    public double WaypointTurnDampingDist { get; set; }
}

/// <summary>
/// 航点云台角度参数（仅 waylines.wpml 使用）
/// </summary>
public class WpmlWaypointGimbalHeadingParam
{
    /// <summary>云台俯仰角</summary>
    [XmlElement("waypointGimbalPitchAngle", Namespace = WpmlNamespaces.Wpml)]
    public double WaypointGimbalPitchAngle { get; set; }

    /// <summary>云台偏航角</summary>
    [XmlElement("waypointGimbalYawAngle", Namespace = WpmlNamespaces.Wpml)]
    public double WaypointGimbalYawAngle { get; set; }
}

/// <summary>
/// 航点经纬度坐标（KMZ 中仅使用“经度,纬度”两个分量）
/// </summary>
public class PointCoordinates
{
    /// <summary>坐标字符串，格式：经度,纬度</summary>
    [XmlElement("coordinates", Namespace = "")]
    public string Coordinates { get; set; } = "0,0";

    /// <summary>经度（非序列化）</summary>
    [XmlIgnore]
    public double Longitude => Parse(0);

    /// <summary>纬度（非序列化）</summary>
    [XmlIgnore]
    public double Latitude => Parse(1);

    private double Parse(int index)
    {
        var parts = (Coordinates ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (index >= parts.Length) return 0;
        return double.TryParse(parts[index], NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }

    /// <summary>由“经度,纬度[,高度]”构造，自动丢弃高度分量</summary>
    public static PointCoordinates From(string? lonLatHeight)
    {
        var parts = (lonLatHeight ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries);
        var lon = parts.Length > 0 ? parts[0].Trim() : "0";
        var lat = parts.Length > 1 ? parts[1].Trim() : "0";
        return new PointCoordinates { Coordinates = $"{lon},{lat}" };
    }
}

/// <summary>
/// 动作触发器
/// </summary>
public class WpmlActionTrigger
{
    /// <summary>reachPoint / betweenAdjacentPoints / multipleTiming / multipleDistance</summary>
    [XmlElement("actionTriggerType", Namespace = WpmlNamespaces.Wpml)]
    public string ActionTriggerType { get; set; } = "reachPoint";

    /// <summary>间隔时间（秒）或间隔距离（米），multipleTiming / multipleDistance 时必填</summary>
    [XmlElement("actionTriggerParam", Namespace = WpmlNamespaces.Wpml)]
    public double? ActionTriggerParam { get; set; }

    /// <summary>值为空时不输出该元素</summary>
    public bool ShouldSerializeActionTriggerParam() => ActionTriggerParam.HasValue;
}

/// <summary>
/// 单个动作
/// </summary>
public class WpmlAction
{
    /// <summary>动作 ID，同一动作组内唯一</summary>
    [XmlElement("actionId", Namespace = WpmlNamespaces.Wpml)]
    public int ActionId { get; set; }

    /// <summary>动作执行器类型，如 takePhoto / gimbalRotate / hover</summary>
    [XmlElement("actionActuatorFunc", Namespace = WpmlNamespaces.Wpml)]
    public string ActionActuatorFunc { get; set; } = string.Empty;

    /// <summary>动作参数</summary>
    [XmlElement("actionActuatorFuncParam", Namespace = WpmlNamespaces.Wpml)]
    public ActionActuatorFuncParam? ActionActuatorFuncParam { get; set; }
}

/// <summary>
/// 动作参数
/// </summary>
/// <remarks>
/// 不同动作的参数集合差异极大（拍照、变焦、云台旋转、悬停……），且后续大疆还会新增字段。
/// 因此这里按“有序键值对”保存，序列化时原样输出为 &lt;wpml:key&gt;value&lt;/wpml:key&gt;，
/// 保证前端配置了什么就能原样落盘，不会因为后端枚举不全而丢字段。
/// </remarks>
public class ActionActuatorFuncParam : IXmlSerializable
{
    private readonly List<KeyValuePair<string, string>> _items = [];

    /// <summary>参数键值对（保持添加顺序）</summary>
    public IReadOnlyList<KeyValuePair<string, string>> Items => _items;

    /// <summary>追加一个参数（值为 null 时忽略）</summary>
    public void Add(string name, string? value)
    {
        if (string.IsNullOrEmpty(name) || value == null) return;
        _items.Add(new KeyValuePair<string, string>(name, value));
    }

    /// <summary>追加一个字符串参数</summary>
    public void AddString(string name, string? value) => Add(name, value);

    /// <summary>追加一个数值参数</summary>
    public void AddNumber(string name, double? value)
    {
        if (value.HasValue) Add(name, WpmlNamespaces.Num(value.Value));
    }

    /// <summary>按大疆规范追加一个布尔参数（输出 0/1）</summary>
    public void AddBool(string name, bool? value)
    {
        if (value.HasValue) Add(name, WpmlNamespaces.Bool(value.Value));
    }

    /// <summary>是否没有任何参数</summary>
    public bool IsEmpty => _items.Count == 0;

    /// <inheritdoc />
    public XmlSchema? GetSchema() => null;

    /// <inheritdoc />
    public void WriteXml(XmlWriter writer)
    {
        foreach (var item in _items)
            writer.WriteElementString(item.Key, WpmlNamespaces.Wpml, item.Value);
    }

    /// <inheritdoc />
    public void ReadXml(XmlReader reader)
    {
        var isEmpty = reader.IsEmptyElement;
        reader.ReadStartElement();
        if (isEmpty) return;
        while (reader.NodeType != XmlNodeType.EndElement && reader.NodeType != XmlNodeType.None)
        {
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName != "actionActuatorFuncParam")
            {
                var name = reader.LocalName;
                var value = reader.ReadElementContentAsString();
                Add(name, value);
            }
            else
            {
                reader.Read();
            }
        }
        if (reader.NodeType == XmlNodeType.EndElement) reader.ReadEndElement();
    }
}

/// <summary>
/// 动作组
/// </summary>
public class WpmlActionGroup
{
    /// <summary>动作组 ID</summary>
    [XmlElement("actionGroupId", Namespace = WpmlNamespaces.Wpml)]
    public int ActionGroupId { get; set; }

    /// <summary>动作组起始航点索引</summary>
    [XmlElement("actionGroupStartIndex", Namespace = WpmlNamespaces.Wpml)]
    public int ActionGroupStartIndex { get; set; }

    /// <summary>动作组结束航点索引</summary>
    [XmlElement("actionGroupEndIndex", Namespace = WpmlNamespaces.Wpml)]
    public int ActionGroupEndIndex { get; set; }

    /// <summary>sequence（顺序执行）/ parallel（并行执行）</summary>
    [XmlElement("actionGroupMode", Namespace = WpmlNamespaces.Wpml)]
    public string ActionGroupMode { get; set; } = "sequence";

    /// <summary>动作触发器</summary>
    [XmlElement("actionTrigger", Namespace = WpmlNamespaces.Wpml)]
    public WpmlActionTrigger ActionTrigger { get; set; } = new();

    /// <summary>动作列表</summary>
    [XmlElement("action", Namespace = WpmlNamespaces.Wpml)]
    public List<WpmlAction> Actions { get; set; } = [];
}
