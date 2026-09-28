// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

namespace Dji.Core.Enum.DjiEnum.Wayline;

/// <summary>
/// 航线任务状态。
/// </summary>
/// <remarks>
/// 取值与上云协议 <c>flighttask_progress.output.status</c> 的字符串枚举**逐字对齐**，
/// 之所以不落库为 int 枚举：一是协议可能新增状态，字符串原始值不丢信息；二是省去一层
/// 「int ↔ 协议字符串」的双向映射，历史多次因为此类映射表漏项导致状态显示错误。
/// <list type="table">
/// <listheader><term>常量</term><description>含义</description></listheader>
/// <item><term>sent</term><description>已下发（平台在 prepare 成功后的初始态）</description></item>
/// <item><term>ready</term><description>满足 ready_conditions，机场已上报 flighttask_ready（协议本身无此值，属平台内部态）</description></item>
/// <item><term>in_progress</term><description>执行中</description></item>
/// <item><term>paused</term><description>已暂停</description></item>
/// <item><term>ok</term><description>执行成功</description></item>
/// <item><term>canceled</term><description>已取消 / 已终止</description></item>
/// <item><term>failed</term><description>失败</description></item>
/// <item><term>rejected</term><description>被拒绝</description></item>
/// <item><term>timeout</term><description>超时</description></item>
/// <item><term>partially_done</term><description>部分完成</description></item>
/// </list>
/// </remarks>
public static class WaylineJobStatus
{
    /// <summary>已下发（平台初始态）</summary>
    public const string Sent = "sent";

    /// <summary>已满足准备条件（平台内部态，由 flighttask_ready 置位）</summary>
    public const string Ready = "ready";

    /// <summary>执行中</summary>
    public const string InProgress = "in_progress";

    /// <summary>已暂停</summary>
    public const string Paused = "paused";

    /// <summary>执行成功</summary>
    public const string Ok = "ok";

    /// <summary>已取消/已终止</summary>
    public const string Canceled = "canceled";

    /// <summary>失败</summary>
    public const string Failed = "failed";

    /// <summary>被拒绝</summary>
    public const string Rejected = "rejected";

    /// <summary>超时</summary>
    public const string Timeout = "timeout";

    /// <summary>部分完成</summary>
    public const string PartiallyDone = "partially_done";

    private static readonly Dictionary<string, string> Descriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        [Sent] = "已下发",
        [Ready] = "已就绪",
        [InProgress] = "执行中",
        [Paused] = "已暂停",
        [Ok] = "已完成",
        [Canceled] = "已取消",
        [Failed] = "失败",
        [Rejected] = "已拒绝",
        [Timeout] = "超时",
        [PartiallyDone] = "部分完成",
    };

    /// <summary>取状态中文描述；未知状态原样返回，便于排障时不丢信息</summary>
    public static string Describe(string status)
    {
        if (string.IsNullOrWhiteSpace(status)) return "未知";
        return Descriptions.TryGetValue(status, out var text) ? text : status;
    }

    /// <summary>全部状态（供前端渲染筛选项/标签，保证前后端字典一致）</summary>
    public static IReadOnlyDictionary<string, string> All => Descriptions;

    /// <summary>是否已进入终态（不会再收到同任务的进度上报）</summary>
    public static bool IsTerminal(string status)
    {
        return status is Ok or Canceled or Failed or Rejected or Timeout or PartiallyDone;
    }

    /// <summary>是否为「平台仍在跟踪」的活跃态</summary>
    public static bool IsActive(string status)
    {
        return status is Sent or Ready or InProgress or Paused;
    }

    /// <summary>是否为可见任务（非终态）</summary>
    public static bool IsOpen(string status) => !IsTerminal(status);
}
