// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Core.Enum.DjiEnum.Ops;

namespace Dji.Application.Service.DjiOps.Dto;

/// <summary>AirSense 告警查询</summary>
public class AirSenseSearchInput : BasePageInput
{
    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>机场 SN</summary>
    public string DockSn { get; set; }

    /// <summary>告警等级</summary>
    public AirSenseWarningLevelEnum? WarningLevel { get; set; }

    /// <summary>
    /// 是否只查「建议避让」及以上（等级 ≥ 3）。
    /// </summary>
    /// <remarks>
    /// 这是最常用的过滤条件，因此单独给一个开关，而不是让调用方自己传 <c>WarningLevel</c> —— 
    /// 后者只能精确匹配单个等级，而运维关心的是「3 级和 4 级」这一整个区间。
    /// </remarks>
    public bool? OnlyAlert { get; set; }

    /// <summary>ICAO 地址（模糊）</summary>
    public string Keyword { get; set; }

    /// <summary>水平距离上限（米），用于筛出「离得近的」</summary>
    public int? MaxDistance { get; set; }

    /// <summary>上报时间起点</summary>
    public DateTime? StartTime { get; set; }

    /// <summary>上报时间终点</summary>
    public DateTime? EndTime { get; set; }
}

/// <summary>AirSense 统计查询</summary>
public class AirSenseStatsInput
{
    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>机场 SN</summary>
    public string DockSn { get; set; }
}

/// <summary>AirSense 记录批量删除入参</summary>
public class AirSenseIdsInput
{
    /// <summary>主键集合</summary>
    [Required(ErrorMessage = "请选择要删除的记录")]
    public List<long> Ids { get; set; }
}

/// <summary>AirSense 告警输出</summary>
public class AirSenseAlarmOutput
{
    /// <summary>主键</summary>
    public long Id { get; set; }

    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>机场 SN</summary>
    public string DockSn { get; set; }

    /// <summary>机场昵称</summary>
    public string DockNick { get; set; }

    /// <summary>ICAO 地址</summary>
    public string Icao { get; set; }

    /// <summary>告警等级</summary>
    public AirSenseWarningLevelEnum WarningLevel { get; set; }

    /// <summary>告警等级名称</summary>
    public string WarningLevelName { get; set; }

    /// <summary>是否达到「建议避让」等级（≥ 3），前端据此决定是否标红</summary>
    public bool IsAlert { get; set; }

    /// <summary>目标纬度</summary>
    public double? Latitude { get; set; }

    /// <summary>目标经度</summary>
    public double? Longitude { get; set; }

    /// <summary>目标绝对高度（米）</summary>
    public int? Altitude { get; set; }

    /// <summary>绝对高度类型</summary>
    public AirSenseAltitudeTypeEnum AltitudeType { get; set; }

    /// <summary>绝对高度类型名称</summary>
    public string AltitudeTypeName { get; set; }

    /// <summary>目标航向（度）</summary>
    public double? Heading { get; set; }

    /// <summary>相对本机的高度差（米）</summary>
    public int? RelativeAltitude { get; set; }

    /// <summary>相对高度趋势</summary>
    public AirSenseVertTrendEnum VertTrend { get; set; }

    /// <summary>相对高度趋势名称</summary>
    public string VertTrendName { get; set; }

    /// <summary>水平距离（米）</summary>
    public int? Distance { get; set; }

    /// <summary>机场经度（用于地图上画相对方位）</summary>
    public double? DockLongitude { get; set; }

    /// <summary>机场纬度</summary>
    public double? DockLatitude { get; set; }

    /// <summary>报文时间戳（毫秒，设备时钟）</summary>
    public long ReportTimestamp { get; set; }

    /// <summary>落库时间</summary>
    public DateTime? CreateTime { get; set; }
}

/// <summary>AirSense 统计（概览卡片）</summary>
public class AirSenseStatsOutput
{
    /// <summary>记录总数</summary>
    public int Total { get; set; }

    /// <summary>需避让（等级 ≥ 3）的记录数</summary>
    public int AlertCount { get; set; }

    /// <summary>按等级分组的计数</summary>
    public List<AirSenseLevelStatOutput> ByLevel { get; set; } = [];

    /// <summary>涉及的不同 ICAO 目标数（去重）</summary>
    public int TargetCount { get; set; }

    /// <summary>最近一次告警时刻</summary>
    public DateTime? LastTime { get; set; }
}

/// <summary>AirSense 按等级统计</summary>
public class AirSenseLevelStatOutput
{
    /// <summary>等级值</summary>
    public int Level { get; set; }

    /// <summary>等级名称</summary>
    public string LevelName { get; set; }

    /// <summary>数量</summary>
    public int Count { get; set; }
}
