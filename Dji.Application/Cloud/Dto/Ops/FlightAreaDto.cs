// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

namespace Dji.Application.Cloud.Dto.Ops;

#region 自定义飞行区

/// <summary>
/// <c>flight_areas_get</c> 的应答载荷（走 <c>requests_reply</c>）。
/// </summary>
/// <remarks>
/// <para>
/// 这是<b>设备驱动</b>的链路：云端把飞行区文件放进对象存储后下发 <c>flight_areas_update</c> 通知，
/// 设备需要时<b>主动</b>用 <c>flight_areas_get</c> 来要地址，云端此时才回本结构。
/// 因此处理器必须随取随签 —— 设备可能在任何时刻（甚至几小时后）来要，预生成并缓存的地址早就过期了。
/// </para>
/// <para>
/// 结构上是标准的 <c>{ output, result }</c> 两段式，可直接配合 <c>ToPublishOutputData</c> 使用。
/// </para>
/// </remarks>
public class FlightAreaGetOutput
{
    /// <summary>文件列表；<b>没有任何自定义飞行区时必须回空数组</b>，不能回 null</summary>
    /// <remarks>
    /// 空数组与「字段缺失」对设备是不同的语义：空数组表示「云端确实没有配置飞行区」，
    /// 设备会据此清掉本地已有的作业区域；而字段缺失只会被当作解析失败（对应 <c>reason = 2</c>）。
    /// </remarks>
    public List<FlightAreaGetFile> Files { get; set; } = [];
}

/// <summary>单个自定义飞行区文件</summary>
public class FlightAreaGetFile
{
    /// <summary>文件名（如 <c>geofence_xxx.json</c>）</summary>
    public string Name { get; set; }

    /// <summary>带签名的下载地址</summary>
    public string Url { get; set; }

    /// <summary>文件 SHA256 摘要（文件内容摘要，不是 URL 签名）</summary>
    public string Checksum { get; set; }

    /// <summary>文件大小（字节）</summary>
    public long? Size { get; set; }
}

/// <summary>
/// <c>flight_areas_sync_progress</c> 的上行载荷（<c>events</c>）。
/// </summary>
/// <remarks>
/// <c>need_reply = 1</c>，<b>必须回 <c>events_reply</c></b>，否则设备会不停重发。
/// </remarks>
public class FlightAreaSyncProgressData
{
    /// <summary>同步状态字符串（<c>wait_sync</c> / <c>synchronizing</c> / <c>synchronized</c> / <c>fail</c> / <c>switch_fail</c>）</summary>
    public string Status { get; set; }

    /// <summary>失败原因码（仅 <c>fail</c> 时有意义）</summary>
    public int? Reason { get; set; }

    /// <summary>涉及的文件</summary>
    public FlightAreaSyncFile File { get; set; }
}

/// <summary>同步进度里携带的文件标识</summary>
/// <remarks>
/// <b>只给 name 与 checksum，不给 URL</b>：云端要靠 <c>checksum</c> 才能确定设备到底在同步哪一份文件
/// —— 文件名会被重复使用（每次更新都叫 <c>geofence_xxx.json</c>），只有摘要不会重复。
/// </remarks>
public class FlightAreaSyncFile
{
    /// <summary>文件名</summary>
    public string Name { get; set; }

    /// <summary>文件 SHA256 摘要</summary>
    public string Checksum { get; set; }
}

/// <summary>
/// <c>flight_areas_drone_location</c> 的上行载荷（<c>events</c>）。
/// </summary>
/// <remarks>
/// <c>need_reply = 0</c>，<b>不需要回包</b>，且是高频推送（飞行中可能每秒数次）——
/// 处理器里不能写同步阻塞的 I/O，也不能按流水落库。
/// </remarks>
public class FlightAreaDroneLocationData
{
    /// <summary>飞行器与各飞行区的距离信息（本次上报送全部已启用区域）</summary>
    public List<FlightAreaDroneLocation> DroneLocations { get; set; } = [];
}

/// <summary>飞行器相对某个自定义飞行区的位置</summary>
public class FlightAreaDroneLocation
{
    /// <summary>区域唯一 ID（对应飞行区文件内的区域定义）</summary>
    public string AreaId { get; set; }

    /// <summary>
    /// 飞行器距该区域边界的距离（米）。
    /// </summary>
    /// <remarks>
    /// <b>官方文档没有说明「在区内」时这个值是正还是负</b>，也没有说明它是到最近边的垂距还是到中心的距离。
    /// 因此本平台<b>不对符号与几何语义做任何假设</b>：判「在不在区内」一律以 <see cref="IsInArea"/> 为准，
    /// 这个数值只取绝对值用于「贴近边界」的排序与展示。
    /// </remarks>
    public double? AreaDistance { get; set; }

    /// <summary>飞行器当前是否在该区域内</summary>
    public bool IsInArea { get; set; }
}

#endregion

#region AirSense（感知民航客机）

/// <summary>
/// <c>airsense_warning</c> 的上行元素（<c>events</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>该报文的 <c>data</c> 本身是数组</b>，一次可推送多架民航客机，因此订阅方法签名应为
/// <c>CloudMqData&lt;List&lt;AirSenseWarningItem&gt;&gt;</c>；落库时展开成多行。
/// </para>
/// <para>
/// <c>need_reply = 1</c>，必须回 <c>events_reply</c>。但<b>是否落库不应影响回包</b>：漏存一条告警
/// 是本平台的问题，让民航客机告警被反复重发只会干扰链路。
/// </para>
/// </remarks>
public class AirSenseWarningItem
{
    /// <summary>民航客机的 ICAO 地址（如 <c>B-5931</c>）</summary>
    public string Icao { get; set; }

    /// <summary>告警等级（0 无危险 ~ 4 等级四，<b>≥ 3 建议避让</b>）</summary>
    public int? WarningLevel { get; set; }

    /// <summary>目标纬度（WGS84，南纬为负，6 位小数）</summary>
    public double? Latitude { get; set; }

    /// <summary>目标经度（WGS84，西经为负，6 位小数）</summary>
    public double? Longitude { get; set; }

    /// <summary>目标绝对高度（米）</summary>
    public int? Altitude { get; set; }

    /// <summary>绝对高度的参考基准（0 椭球高 / 1 海拔高）</summary>
    public int? AltitudeType { get; set; }

    /// <summary>目标航向（度，0 正北 / 90 正东）</summary>
    public double? Heading { get; set; }

    /// <summary>目标相对本机的垂直高度差（米）</summary>
    public int? RelativeAltitude { get; set; }

    /// <summary>相对高度的变化趋势（0 不变 / 1 上升 / 2 下降）</summary>
    public int? VertTrend { get; set; }

    /// <summary>目标与本机的水平距离（米）</summary>
    public int? Distance { get; set; }
}

#endregion
