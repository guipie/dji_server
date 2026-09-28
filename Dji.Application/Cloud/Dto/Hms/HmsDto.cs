// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

namespace Dji.Application.Cloud.Dto.Hms;

/// <summary>
/// <c>hms</c> 事件报文的 data 部分。
/// </summary>
/// <remarks>
/// <b>这是一份全量快照</b>：<c>list</c> 即设备当前所有活跃告警。
/// 与上一次收到的 list 做差集，消失的那些就是已解除的告警。
/// </remarks>
public class HmsEventData
{
    /// <summary>当前活跃告警列表</summary>
    public List<HmsAlarmItem> List { get; set; } = [];
}

/// <summary>
/// 单条 HMS 告警。
/// </summary>
public class HmsAlarmItem
{
    /// <summary>告警等级（0 通知 / 1 提醒 / 2 警告）</summary>
    public int? Level { get; set; }

    /// <summary>所属模块（0 飞行任务 / 1 设备管理 / 2 媒体 / 3 HMS）</summary>
    public int? Module { get; set; }

    /// <summary>上报时刻是否在空中（0 在地上 / 1 在天上），参与飞行器文案 Key 拼接</summary>
    public int? InTheSky { get; set; }

    /// <summary>是否为及时性告警（1 是，风力减小等条件下会自行消失）</summary>
    public int? Imminent { get; set; }

    /// <summary>告警码（如 <c>0x16100083</c>）</summary>
    public string Code { get; set; }

    /// <summary>设备产品枚举值（<c>{domain}-{type}-{sub_type}</c>，首位 3 为机场、0 为飞行器）</summary>
    public string DeviceType { get; set; }

    /// <summary>告警参数（部件 / 传感器索引，文案占位符需要）</summary>
    public HmsAlarmArgs Args { get; set; }
}

/// <summary>
/// HMS 告警参数。
/// </summary>
/// <remarks>
/// 协议目前只定义了这两个索引，但文案占位符（<c>%battery_index</c> 等）都依赖它们，
/// 因此必须完整保留。为防未来协议扩展出别的键，落库时另存一份 <c>args</c> 原文。
/// </remarks>
public class HmsAlarmArgs
{
    /// <summary>部件索引（0 起）</summary>
    public int? ComponentIndex { get; set; }

    /// <summary>传感器索引（0 起）</summary>
    public int? SensorIndex { get; set; }
}
