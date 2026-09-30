// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。


using Dji.Application.Cloud.Entity;
using MQTTnet;
using MQTTnet.Protocol;

namespace Dji.Application.Cloud.Core;

/// <summary>
/// MQTT 下行网关：负责把指令发送到指定网关（机场）的主题上。
/// </summary>
/// <remarks>
/// 上云协议对指令类消息（<c>&lt;sn&gt;/services</c>、<c>&lt;sn&gt;/property/set</c>）要求 QoS 1（至少一次送达）。
/// 历史实现使用 QoS 0，弱网下会静默丢包且平台无从感知，故此处在 <see cref="DefaultQos"/> 统一为 1。
/// </remarks>
public class MqttGatewayPublish(ILogger<MqttGatewayPublish> logger, IMqttClient client, SysCacheService sysCache)
{
    /// <summary>指令类下行统一 QoS 1</summary>
    private const int DefaultQos = 1;

    /// <summary>默认重试次数（不含首次发送）</summary>
    private const int DefaultRetryCount = 2;

    /// <summary>重试退避基数（毫秒），实际间隔按尝试次数线性递增</summary>
    private const int RetryBackoffMs = 500;

    private readonly ILogger _logger = logger;
    private readonly IMqttClient _mqttClient = client;
    private readonly SysCacheService _sysCache = sysCache;

    /// <summary>
    /// 发布消息（自动按网关 SN 补全主题中的 <c>+</c> 通配符）。
    /// </summary>
    /// <returns>0 表示成功；非 0 为 MQTT 原因码；-1 表示异常</returns>
    public async Task<int> PublishAsync<T>(string topic, CloudMqRequest<T> request, int qos = DefaultQos, int retryCount = DefaultRetryCount, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 主题绑定放在 try 之外会因网关 SN 为空而把异常抛给调用方，这里显式校验
        var fullTopic = topic.BindGateway(request.Gateway);
        // 必须用 MqttJson（snake_case）序列化：全局 ToJson() 的 ContractResolver 在 Startup 中被注释，
        // 默认输出 PascalCase（Bid/Tid/Method/Data），DJI 设备按协议只认 snake_case（bid/tid/method/data），
        // 解析不了 PascalCase 时回包的 bid 为空，后端按 bid 关联回包会全部超时。
        var payload = MqttJson.Serialize(request);
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(fullTopic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)qos)
            .Build();

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                var result = await _mqttClient.PublishAsync(message, ct);
                if (result.ReasonCode.ToInt() == 0)
                {
                    _logger.LogInformation("MQTT 下行成功 topic:{Topic} qos:{Qos} payload:{Payload}", fullTopic, qos, payload);
                    return 0;
                }

                _logger.LogWarning("MQTT 下行失败（第 {Attempt} 次）topic:{Topic} reason:{Reason}", attempt + 1, fullTopic, result.ReasonCode);
                if (attempt >= retryCount) return result.ReasonCode.ToInt();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MQTT 下行异常（第 {Attempt} 次）topic:{Topic} payload:{Payload}", attempt + 1, fullTopic, payload);
                if (attempt >= retryCount) return -1;
            }

            // 线性退避：历史实现是无间隔连发，弱网下反而加剧拥塞
            await Task.Delay(RetryBackoffMs * (attempt + 1), ct);
        }
    }

    /// <summary>
    /// 发布消息并等待设备回包（按 Bid 关联）。
    /// </summary>
    /// <remarks>
    /// 每次调用都会刷新 Bid/Tid：MQTT 回包靠 Bid 关联，若复用同一个请求对象而不换 Bid，
    /// 会读到上一轮遗留的缓存回包。（历史实现的三元条件写反，Bid 为空时反而保持不变。）
    /// </remarks>
    public async Task<CloudMqData<MqOutput<R>>> PublishWithReplyAsync<T, R>(string topic, CloudMqRequest<T> request, int timeoutSeconds = 10)
        => await WaitReplyAsync<MqOutput<R>, T>(topic, request, timeoutSeconds);

    /// <summary>
    /// 发布消息并等待设备回包，<b>不做 <c>{ output, result }</c> 包装约定</b>，直接按调用方给的类型解析报文 <c>data</c>。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>services_reply</c> 的 <c>data</c> 绝大多数是 <c>{ output, result }</c> 两段式（用上面那个重载即可）。
    /// 但个别方法的 <c>data</c> <b>直接平铺业务字段</b> —— 典型是 <c>fileupload_list</c>：
    /// 它的 <c>data</c> 是 <c>{ files: [...], result: 0 }</c>，没有 <c>output</c>。
    /// 若用 <c>MqOutput&lt;R&gt;</c> 去接，业务字段会落在 <c>output</c> 之外被静默丢弃
    /// （全局序列化配置是 <c>MissingMemberHandling.Ignore</c>，不报错、只是「拿到的全是 null」）。
    /// </para>
    /// <para>因此为这类方法提供一条「不猜结构」的通道。</para>
    /// </remarks>
    public async Task<CloudMqData<TData>> PublishWithReplyDataAsync<T, TData>(string topic, CloudMqRequest<T> request, int timeoutSeconds = 10)
        => await WaitReplyAsync<TData, T>(topic, request, timeoutSeconds);

    /// <summary>公共的「下发 + 按 bid 轮询回包」实现</summary>
    /// <remarks>
    /// 回包在 <see cref="MqttService"/> 收包时以「原始 JSON 字符串」存入缓存（缓存键 = <c>bid</c>），
    /// 这里必须先把字符串取出来、再用与收包侧同一套 <see cref="MqttJson"/> 配置反序列化。
    ///
    /// 不能直接 <c>_sysCache.Get&lt;CloudMqData&lt;TData&gt;&gt;(key)</c>：NewLife 缓存的泛型 <c>Get&lt;T&gt;</c>
    /// 只在「值本身就是 T」或「简单类型可转换」时成功，把 string 转成 <c>CloudMqData&lt;T&gt;</c> 这类复杂类型会静默
    /// 返回 <c>null</c>（不抛异常），表现为「设备明明回包了却一直等到超时」。
    /// 另外上云协议字段是 snake_case，只有 Newtonsoft + SnakeCaseNamingStrategy 才能正确映射。
    /// </remarks>
    private async Task<CloudMqData<TData>> WaitReplyAsync<TData, T>(string topic, CloudMqRequest<T> request, int timeoutSeconds)
    {
        ArgumentNullException.ThrowIfNull(request);

        request.Bid = Guid.NewGuid().ToString();
        request.Tid = Guid.NewGuid().ToString();
        request.Timestamp = DateTimeUtil.ToUnixTimestampByMilliseconds(DateTime.Now);

        var replyKey = request.Bid.Reply();
        var fullTopic = topic.BindGateway(request.Gateway);

        // bid 是本次请求与回包的唯一关联键：必须打进日志，否则无法与 mqtt 抓包/mqttx 里的回包对照
        var repliesBefore = ReplyDiagnostics.ReceivedCount;
        _logger.LogInformation("MQTT 下发并等待回包 topic:{Topic} bid:{Bid}", fullTopic, request.Bid);

        var publishResult = await PublishAsync(topic, request);
        if (publishResult != 0)
        {
            // 不直接抛错：部分 broker 在「主题当前无订阅者」时也会返回非 0，此时仍可能收到回包
            _logger.LogWarning("MQTT 下发返回非 0（{Reason}），继续等待回包 topic:{Topic} bid:{Bid}", publishResult, fullTopic, request.Bid);
        }

        for (var waited = 0; waited < timeoutSeconds; waited++)
        {
            await Task.Delay(1000);

            var payload = _sysCache.Get<string>(replyKey);
            if (payload.IsNullOrWhiteSpace()) continue;

            try
            {
                var reply = MqttJson.Deserialize<CloudMqData<TData>>(payload);
                // 用模式匹配而非 ?. ：TData 是开放泛型参数（可能是值类型），空传播运算符在泛型参数上会报 CS8978
                if (reply is not null && reply.Data is not null)
                {
                    _logger.LogInformation("MQTT 回包匹配成功 topic:{Topic} bid:{Bid} 耗时:{Seconds}秒", fullTopic, request.Bid, waited + 1);
                    return reply;
                }
            }
            catch (Exception ex)
            {
                // 单个坏报文不应打断整个等待流程，记录后继续等（超时仍会抛出统一的友好异常）
                _logger.LogError(ex, "回包反序列化失败 topic:{Topic} payload:{Payload}", fullTopic, payload);
            }
        }

        // 超时时把「应答有没有进到本进程」一并打出来：
        // 收到 0 条 ⇒ 故障在订阅/broker 侧；收到若干条但 bid 对不上 ⇒ 故障在关联键
        var received = ReplyDiagnostics.ReceivedCount - repliesBefore;
        _logger.LogError("等待回包超时 topic:{Topic} bid:{Bid} 下发返回码:{Code} 等待期间收到应答:{Received}条 最近一条:{Last}",
            fullTopic, request.Bid, publishResult, received, ReplyDiagnostics.DescribeLast());

        throw Oops.Oh($"设备未在 {timeoutSeconds} 秒内回包，topic:{fullTopic}，bid:{request.Bid}"
                      + $"（下发返回码:{publishResult}，等待期间收到 {received} 条应答报文）");
    }
}
