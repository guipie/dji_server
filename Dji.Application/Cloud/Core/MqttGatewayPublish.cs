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
public class MqttGatewayPublish(ILogger<MqttGatewayPublish> logger, IMqttClient client, SysCacheService sysCache)
{
    private const int DEFAULT_QOS = 0;
    public static readonly int DEFAULT_RETRY_COUNT = 2;
    public static readonly int DEFAULT_RETRY_TIMEOUT = 3000;

    private readonly ILogger _logger = logger;
    private readonly IMqttClient _mqttClient = client;
    private readonly SysCacheService _sysCache = sysCache;

    public async Task<int> PublishAsync<T>(string topic, CloudMqRequest<T> request, int qos = DEFAULT_QOS, int publishCount = 0, CancellationToken ct = default)
    {
        try
        {


            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic.BindGateway(request.Gateway))
                .WithPayload(request.ToJson())
            .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)qos)
                .Build();
            var result = await _mqttClient.PublishAsync(message, ct);
            if (result.ReasonCode != 0 && publishCount > 0)
            {
                var time = 0;
                while (time++ < (publishCount > 10 ? 10 : publishCount))
                {
                    if (result.ReasonCode != 0)
                        result = await _mqttClient.PublishAsync(message, ct);
                }
            }
            Console.WriteLine("send topic: {0},result:{1}, payload: {2}", topic, result.ToJson(), request.ToJson());
            _logger.LogInformation("send topic: {Topic}, payload: {Payload}", topic, request.ToJson());
            return result.ReasonCode.ToInt();
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to publish the message. {0}", request.ToString());
            _logger.LogError(e, "Failed to publish the message. {Request}", request.ToString());
            return -1;
        }
    }
    public async Task<int> PublishAsync<T>(string topic, CloudMqRequest<T> request)
    {
        return await PublishAsync(topic.BindGateway(request.Gateway), request, DEFAULT_QOS);
    }


    public async Task<CloudMqData<MqOutput<R>>> PublishWithReplyAsync<T,R>(string topic, CloudMqRequest<T> request, int timeout = 10)
    {
        _logger.LogInformation("send topic: {Topic}, payload: {Payload}", topic, request.ToJson());

        var time = 0;
        request.Bid = request.Bid.IsNullOrWhiteSpace() ? request.Bid : Guid.NewGuid().ToString();
        request.Tid = request.Bid.IsNullOrWhiteSpace() ? request.Tid : Guid.NewGuid().ToString();
        var result = await PublishAsync(topic.BindGateway(request.Gateway), request, 0, 0);
        while (time++ <= timeout)
        {
            Console.WriteLine("waiting reply... {0}s,reply data:{1}", time, _sysCache.Get<CloudMqData<MqOutput<R>>>(request.Bid.Reply()));
            var replyData = _sysCache.Get<CloudMqData<MqOutput<R>>>(request.Bid.Reply());
            await Task.Delay(1000);
            if (replyData == null || replyData.Data == null) continue;
            return replyData;
        }
        throw Oops.Oh("send topic fail..");
    }
}
