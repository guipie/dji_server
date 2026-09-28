// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

namespace Dji.Core.Enum.DjiEnum.Hms;

/// <summary>
/// HMS 告警等级。
/// </summary>
/// <remarks>
/// 取值与协议 <c>hms</c> 事件 <c>data.list[].level</c> 的整数一致。
/// 官方口径：等级越高越需要关注，<b>2（警告）</b>通常对应影响作业安全的故障。
/// </remarks>
public enum HmsLevelEnum
{
    /// <summary>通知（提示性信息，不影响作业）</summary>
    [Description("通知")]
    Notice = 0,

    /// <summary>提醒（建议关注，一般不影响作业）</summary>
    [Description("提醒")]
    Remind = 1,

    /// <summary>警告（可能影响作业安全，需尽快处理）</summary>
    [Description("警告")]
    Warning = 2
}

/// <summary>
/// HMS 告警所属业务模块。
/// </summary>
/// <remarks>
/// 取值与协议 <c>data.list[].module</c> 一致。用途是把告警分流到不同页面：
/// 飞行任务类归任务中心，媒体类归媒体库，其余归告警中心。
/// </remarks>
public enum HmsModuleEnum
{
    /// <summary>飞行任务</summary>
    [Description("飞行任务")]
    FlightTask = 0,

    /// <summary>设备管理</summary>
    [Description("设备管理")]
    DeviceManage = 1,

    /// <summary>媒体</summary>
    [Description("媒体")]
    Media = 2,

    /// <summary>HMS 本体（绝大多数健康告警都在此模块）</summary>
    [Description("HMS")]
    Hms = 3
}

/// <summary>
/// HMS 告警的活跃状态（平台内部态，协议无此字段）。
/// </summary>
/// <remarks>
/// 协议明确规定 <c>hms</c> 报文是<b>全量快照</b>：上一次有、本次没有 ⇔ 告警已解除。
/// 因此本状态由「比对两次报文」推导，而不是设备直接告知。
/// 之所以不直接删除已解除的记录，是为了保留「曾经发生过什么」以及持续时长，
/// 这对现场排查（尤其是间歇性故障）非常关键。
/// </remarks>
public enum HmsAlarmStatusEnum
{
    /// <summary>已解除（上一次报文里有，本次消失了）</summary>
    [Description("已解除")]
    Recovered = 0,

    /// <summary>活跃（本次报文中仍然存在）</summary>
    [Description("活跃")]
    Active = 1
}
