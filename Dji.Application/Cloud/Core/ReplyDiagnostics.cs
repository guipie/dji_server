// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按"原样"提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

namespace Dji.Application.Cloud.Core;

/// <summary>
/// MQTT 诊断统计（收包 + 订阅）。
/// </summary>
public static class MqttDiagnostics
{
    // === 所有消息 ===
    private static long _totalMessages;
    private static string _lastTopic;
    private static long _lastMessageTicks;

    // === reply 消息 ===
    private static long _replyMessages;
    private static string _lastReplyTopic;
    private static string _lastReplyBid;
    private static long _lastReplyTicks;

    // === 订阅结果 ===
    private static readonly List<(string Topic, int ResultCode, string Time)> _subscribeResults = new();
    private static readonly HashSet<string> _uniqueTopics = new();
    private static readonly List<string> _uniqueTopicsList = new();

    /// <summary>进程启动以来收到的 MQTT 消息总数</summary>
    public static long TotalMessageCount => Interlocked.Read(ref _totalMessages);

    /// <summary>收到的 reply 消息数</summary>
    public static long ReplyMessageCount => Interlocked.Read(ref _replyMessages);

    /// <summary>最近收到的任意消息的 topic</summary>
    public static string LastTopic => Volatile.Read(ref _lastTopic);

    /// <summary>最近收到的 reply 消息的描述</summary>
    public static string LastReply
    {
        get
        {
            var topic = Volatile.Read(ref _lastReplyTopic);
            if (string.IsNullOrEmpty(topic)) return "（本次运行尚未收到任何应答报文）";
            var ticks = Interlocked.Read(ref _lastReplyTicks);
            var time = ticks > 0 ? new DateTime(ticks).ToString("HH:mm:ss.fff") : "-";
            return $"{topic} bid:{Volatile.Read(ref _lastReplyBid) ?? "<空>"} 接收于 {time}";
        }
    }

    /// <summary>最近一次消息接收时间</summary>
    public static string LastMessageTime
    {
        get
        {
            var ticks = Interlocked.Read(ref _lastMessageTicks);
            return ticks > 0 ? new DateTime(ticks).ToString("HH:mm:ss.fff") : "-";
        }
    }

    /// <summary>实际执行过的订阅结果列表（topic -> suback 结果码）</summary>
    public static IReadOnlyList<string> UniqueTopics
    {
        get { lock (_uniqueTopics) return [.. _uniqueTopicsList]; }
    }

    public static IReadOnlyList<(string Topic, int ResultCode, string Time)> SubscribeResults
    {
        get { lock (_subscribeResults) return [.. _subscribeResults]; }
    }

    /// <summary>记录收到任意一条 MQTT 消息</summary>
    public static void OnMessageReceived(string topic)
    {
        Interlocked.Increment(ref _totalMessages);
        Volatile.Write(ref _lastTopic, topic);
        Interlocked.Exchange(ref _lastMessageTicks, DateTime.Now.Ticks);
        lock (_uniqueTopics)
        {
            if (_uniqueTopics.Add(topic)) _uniqueTopicsList.Add(topic);
        }
    }

    /// <summary>记录收到一条 reply 消息</summary>
    public static void OnReplyReceived(string topic, string bid)
    {
        Interlocked.Increment(ref _replyMessages);
        Volatile.Write(ref _lastReplyTopic, topic);
        Volatile.Write(ref _lastReplyBid, bid);
        Interlocked.Exchange(ref _lastReplyTicks, DateTime.Now.Ticks);
    }

    /// <summary>记录一次订阅尝试的结果</summary>
    public static void OnSubscribeResult(string topic, int resultCode)
    {
        lock (_subscribeResults)
        {
            _subscribeResults.Add((topic, resultCode, DateTime.Now.ToString("HH:mm:ss.fff")));
            if (_subscribeResults.Count > 200) _subscribeResults.RemoveAt(0);
        }
    }
}

/// <summary>旧类名，保留给历史调用点（改为转发到 MqttDiagnostics）</summary>
public static class ReplyDiagnostics
{
    public static long ReceivedCount => MqttDiagnostics.ReplyMessageCount;
    public static string DescribeLast() => MqttDiagnostics.LastReply;

    public static void OnReplyReceived(string topic, string bid)
    {
        MqttDiagnostics.OnReplyReceived(topic, bid);
    }
}