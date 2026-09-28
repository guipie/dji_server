// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Core.Enum.DjiEnum.Ops;

namespace Dji.Core.Entity.DjiEntity;

/// <summary>
/// AirSense 告警（周边民航客机感知）。
/// </summary>
/// <remarks>
/// <para>
/// <b>写入来源</b>：机场上行 <c>thing/product/{sn}/events</c> 的 <c>airsense_warning</c>。
/// <b>注意该报文的 <c>data</c> 是数组而非对象</b> —— 一次可同时推送多架飞机，落库要展开成多行。
/// </para>
/// <para>
/// <b>这是「流水表」而非「快照表」</b>：与自定义飞行区距离不同，飞机是从外部闯入的第三方目标，
/// 事后复盘「当时有几架民航飞机在附近、最近多远」正是安全审计最需要的证据，因此每次都留一行。
/// 表增长由业务低峰期的清理任务控制（默认保留 90 天）。
/// </para>
/// <para>
/// <b>等级语义</b>：官方建议 <c>warning_level &gt;= 3</c> 时无人机主动避让，
/// 前端据此决定是否升级为红色强提醒。
/// </para>
/// </remarks>
[SugarTable(null, "AirSense告警")]
[SugarIndex("index_DjiAirSense_DockTime", nameof(DockSn), OrderByType.Asc, nameof(ReportTimestamp), OrderByType.Desc)]
[SugarIndex("index_DjiAirSense_Level", nameof(WarningLevel), OrderByType.Asc, nameof(CreateTime), OrderByType.Desc)]
public class DjiAirSenseAlarm : EntityWorkspaceBase
{
    /// <summary>机场 SN</summary>
    [SugarColumn(ColumnDescription = "机场SN", Length = 64, IsNullable = false)]
    public string DockSn { get; set; }

    /// <summary>民航客机 ICAO 地址（协议 <c>icao</c>，如 <c>B-5931</c>）</summary>
    [SugarColumn(ColumnDescription = "ICAO地址", Length = 32, IsNullable = true)]
    public string Icao { get; set; }

    /// <summary>告警等级（0 无危险 ~ 4 等级四；&gt;= 3 建议避让）</summary>
    [SugarColumn(ColumnDescription = "告警等级", IsNullable = true)]
    public AirSenseWarningLevelEnum WarningLevel { get; set; }

    /// <summary>目标纬度（WGS84）</summary>
    [SugarColumn(ColumnDescription = "纬度", IsNullable = true)]
    public double? Latitude { get; set; }

    /// <summary>目标经度（WGS84）</summary>
    [SugarColumn(ColumnDescription = "经度", IsNullable = true)]
    public double? Longitude { get; set; }

    /// <summary>目标绝对高度（米）</summary>
    [SugarColumn(ColumnDescription = "绝对高度", IsNullable = true)]
    public int? Altitude { get; set; }

    /// <summary>绝对高度的类型（椭球高 / 海拔高）</summary>
    [SugarColumn(ColumnDescription = "高度类型", IsNullable = true)]
    public AirSenseAltitudeTypeEnum AltitudeType { get; set; }

    /// <summary>目标航向（度，0 正北 / 90 正东）</summary>
    [SugarColumn(ColumnDescription = "航向", IsNullable = true)]
    public double? Heading { get; set; }

    /// <summary>目标相对本机的垂直高度差（米，正数表示高于本机）</summary>
    [SugarColumn(ColumnDescription = "相对高度", IsNullable = true)]
    public int? RelativeAltitude { get; set; }

    /// <summary>相对高度的变化趋势（上升 / 下降 / 不变）</summary>
    [SugarColumn(ColumnDescription = "高度趋势", IsNullable = true)]
    public AirSenseVertTrendEnum VertTrend { get; set; }

    /// <summary>目标与本机的水平距离（米）</summary>
    [SugarColumn(ColumnDescription = "水平距离", IsNullable = true)]
    public int? Distance { get; set; }

    /// <summary>本机（机场）当时的经度，用于事后在地图上复现「谁在谁的哪一侧」</summary>
    /// <remarks>
    /// 取的是<b>机场</b>坐标而非飞行器坐标：协议不带本机位置，而本平台没有单独存飞行器 OSD 的实时位置。
    /// 作业时飞行器通常在机场数公里内，用机场坐标画相对方位误差可接受；列名如实标注为 Dock 以免误用。
    /// </remarks>
    [SugarColumn(ColumnDescription = "机场经度", IsNullable = true)]
    public double? DockLongitude { get; set; }

    /// <summary>本机（机场）当时的纬度</summary>
    [SugarColumn(ColumnDescription = "机场纬度", IsNullable = true)]
    public double? DockLatitude { get; set; }

    /// <summary>报文时间戳（毫秒，设备时钟）</summary>
    [SugarColumn(ColumnDescription = "上报时间戳", IsNullable = true)]
    public long ReportTimestamp { get; set; }
}
