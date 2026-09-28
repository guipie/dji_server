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
        var payload = request.ToJson();
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
    private async Task<CloudMqData<TData>> WaitReplyAsync<TData, T>(string topic, CloudMqRequest<T> request, int timeoutSeconds)
    {
        ArgumentNullException.ThrowIfNull(request);

        request.Bid = Guid.NewGuid().ToString();
        request.Tid = Guid.NewGuid().ToString();
        request.Timestamp = DateTimeUtil.ToUnixTimestampByMilliseconds(DateTime.Now);

        var replyKey = request.Bid.Reply();
        await PublishAsync(topic, request);

        for (var waited = 0; waited < timeoutSeconds; waited++)
        {
            await Task.Delay(1000);
            // 用模式匹配而非 ?. ：TData 是开放泛型参数（可能是值类型），空传播运算符在泛型参数上会报 CS8978
            var reply = _sysCache.Get<CloudMqData<TData>>(replyKey);
            if (reply is not null && reply.Data is not null) return reply;
        }

        throw Oops.Oh($"设备未在 {timeoutSeconds} 秒内回包，topic:{request.Gateway}");
    }
}
