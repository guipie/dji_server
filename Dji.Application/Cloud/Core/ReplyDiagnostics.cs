// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

namespace Dji.Application.Cloud.Core;

/// <summary>
/// 应答报文收包统计（诊断用）。
/// </summary>
/// <remarks>
/// <para>
/// 设备不回包的故障有两种，但对外表现完全一样（都是「设备未在 N 秒内回包」）：
/// </para>
/// <list type="bullet">
/// <item><b>应答根本没进本进程</b> —— 订阅未生效 / broker 授权拒绝 / 连的不是同一个 broker；</item>
/// <item><b>应答进来了但 bid 对不上</b> —— 设备没有回传请求里的 bid，按 bid 关联永远取不到。</item>
/// </list>
/// <para>
/// 这里记录「收到了多少条应答、最近一条是什么」，让超时异常能直接区分这两种情况，
/// 否则只能靠抓包或翻 broker 日志，定位成本极高。
/// </para>
/// <para>字段均为诊断用途，读写不做强一致保证，允许竞态。</para>
/// </remarks>
internal static class ReplyDiagnostics
{
    private static long _received;
    private static string _lastTopic;
    private static string _lastBid;
    private static long _lastTicks;

    /// <summary>进程启动以来收到的应答报文总数</summary>
    public static long ReceivedCount => Interlocked.Read(ref _received);

    /// <summary>最近一条应答的描述（主题 + bid + 本地接收时间）</summary>
    public static string DescribeLast()
    {
        var topic = Volatile.Read(ref _lastTopic);
        if (string.IsNullOrEmpty(topic)) return "（本次运行尚未收到任何应答报文）";

        var ticks = Interlocked.Read(ref _lastTicks);
        var time = ticks > 0 ? new DateTime(ticks).ToString("HH:mm:ss.fff") : "-";
        return $"{topic} bid:{Volatile.Read(ref _lastBid) ?? "<空>"} 接收于 {time}";
    }

    /// <summary>记录一条应答报文</summary>
    public static void OnReplyReceived(string topic, string bid)
    {
        Interlocked.Increment(ref _received);
        Volatile.Write(ref _lastTopic, topic);
        Volatile.Write(ref _lastBid, bid);
        Interlocked.Exchange(ref _lastTicks, DateTime.Now.Ticks);
    }
}
