// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Core.Enum.DjiEnum.Hms;

namespace Dji.Application.Service.DjiHms.Dto;

/// <summary>告警中心查询条件</summary>
public class HmsAlarmSearchInput : BasePageInput
{
    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>机场 SN</summary>
    public string DockSn { get; set; }

    /// <summary>告警等级</summary>
    public HmsLevelEnum? Level { get; set; }

    /// <summary>所属模块</summary>
    public HmsModuleEnum? Module { get; set; }

    /// <summary>
    /// 活跃状态。
    /// </summary>
    /// <remarks>留空表示不限；实际使用时前端默认只查活跃告警（<c>Active</c>）。</remarks>
    public HmsAlarmStatusEnum? Status { get; set; }

    /// <summary>告警码（支持前缀匹配，便于按 <c>0x16</c> 这类码段筛选）</summary>
    public string Code { get; set; }

    /// <summary>关键字（匹配中英文文案）</summary>
    public string Keyword { get; set; }

    /// <summary>是否只看活跃告警（与 <see cref="Status"/> 二选一，便于前端简写）</summary>
    public bool? OnlyActive { get; set; }

    /// <summary>是否包含及时性告警（默认包含；置 false 可只看需要人工处理的）</summary>
    public bool? ExcludeImminent { get; set; }

    /// <summary>时间范围起点（按最近上报时间）</summary>
    public DateTime? StartTime { get; set; }

    /// <summary>时间范围终点</summary>
    public DateTime? EndTime { get; set; }
}

/// <summary>批量操作输入（清除 / 删除告警记录）</summary>
public class HmsAlarmIdsInput
{
    /// <summary>告警记录主键集合</summary>
    public List<long> Ids { get; set; } = [];
}

/// <summary>告警统计条件</summary>
public class HmsStatsInput
{
    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>机场 SN</summary>
    public string DockSn { get; set; }
}

/// <summary>告警列表输出</summary>
public class HmsAlarmOutput
{
    /// <summary>主键</summary>
    public long Id { get; set; }

    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>机场 SN</summary>
    public string DockSn { get; set; }

    /// <summary>机场昵称</summary>
    public string DockNick { get; set; }

    /// <summary>告警来源设备 SN</summary>
    public string DeviceSn { get; set; }

    /// <summary>设备产品枚举值</summary>
    public string DeviceType { get; set; }

    /// <summary>告警码</summary>
    public string Code { get; set; }

    /// <summary>告警等级</summary>
    public HmsLevelEnum Level { get; set; }

    /// <summary>等级名称</summary>
    public string LevelName { get; set; }

    /// <summary>所属模块</summary>
    public HmsModuleEnum Module { get; set; }

    /// <summary>模块名称</summary>
    public string ModuleName { get; set; }

    /// <summary>上报时是否在空中</summary>
    public int InTheSky { get; set; }

    /// <summary>是否为及时性告警</summary>
    public int Imminent { get; set; }

    /// <summary>部件索引（原始值，0 起）</summary>
    public int ComponentIndex { get; set; }

    /// <summary>传感器索引（原始值，0 起）</summary>
    public int SensorIndex { get; set; }

    /// <summary>展示文案（中文优先，缺失回落英文）</summary>
    public string Text { get; set; }

    /// <summary>中文文案</summary>
    public string TextZh { get; set; }

    /// <summary>英文文案</summary>
    public string TextEn { get; set; }

    /// <summary>活跃状态</summary>
    public HmsAlarmStatusEnum Status { get; set; }

    /// <summary>状态名称</summary>
    public string StatusName { get; set; }

    /// <summary>首次出现时间</summary>
    public DateTime? FirstTime { get; set; }

    /// <summary>最近上报时间</summary>
    public DateTime? LastTime { get; set; }

    /// <summary>恢复时间</summary>
    public DateTime? RecoverTime { get; set; }

    /// <summary>持续时间描述（活跃则为「已持续 X」，已恢复则为「共持续 X」）</summary>
    public string DurationText { get; set; }

    /// <summary>报文时间戳</summary>
    public long ReportTimestamp { get; set; }

    /// <summary>落库时间</summary>
    public DateTime? CreateTime { get; set; }
}

/// <summary>告警统计输出</summary>
public class HmsStatsOutput
{
    /// <summary>记录总数</summary>
    public int Total { get; set; }

    /// <summary>当前活跃数</summary>
    public int ActiveCount { get; set; }

    /// <summary>已恢复数</summary>
    public int RecoveredCount { get; set; }

    /// <summary>警告级活跃数（最需要关注）</summary>
    public int WarningCount { get; set; }

    /// <summary>提醒级活跃数</summary>
    public int RemindCount { get; set; }

    /// <summary>通知级活跃数</summary>
    public int NoticeCount { get; set; }

    /// <summary>有活跃告警的机场数</summary>
    public int DockCount { get; set; }

    /// <summary>按机场分布</summary>
    public List<HmsDockStatOutput> ByDock { get; set; } = [];
}

/// <summary>按机场的告警统计</summary>
public class HmsDockStatOutput
{
    /// <summary>机场 SN</summary>
    public string DockSn { get; set; }

    /// <summary>机场昵称</summary>
    public string DockNick { get; set; }

    /// <summary>活跃告警数</summary>
    public int ActiveCount { get; set; }

    /// <summary>其中警告级数量</summary>
    public int WarningCount { get; set; }
}

/// <summary>HMS 机场下拉查询入参</summary>
/// <remarks>
/// 单独定义而不是复用 <c>DjiOps</c> 的同类 DTO：两个模块的 VO 各自演进
/// （HMS 的下拉还带活跃告警数），共用一个入参类型会让耦合方向反过来。
/// </remarks>
public class HmsDockOptionInput
{
    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }
}

/// <summary>机场下拉项</summary>
public class HmsDockOptionOutput
{
    /// <summary>机场 SN</summary>
    public string Sn { get; set; }

    /// <summary>机场昵称</summary>
    public string Nick { get; set; }

    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>是否在线</summary>
    public bool IsOnline { get; set; }

    /// <summary>活跃告警数（下拉框内直接提示风险）</summary>
    public int ActiveCount { get; set; }

    /// <summary>显示文本</summary>
    public string Label { get; set; }
}

/// <summary>字典项（等级 / 模块 / 状态）</summary>
public class HmsDictOptionOutput
{
    /// <summary>值</summary>
    public int Value { get; set; }

    /// <summary>显示文本</summary>
    public string Label { get; set; }
}
