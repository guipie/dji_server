// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。



using Dji.Application.Cloud.Entity;
using Dji.Application.Option;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using Newtonsoft.Json.Serialization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Dji.Application.Cloud.Core;
/// <summary>
///  MQTT 客户端管理
/// </summary>
// Core/MqttService.cs

internal class MqttService : IMqttService, IDisposable
{
    private readonly IMqttClient _client; // 注意类型是 IMqttClient
    private readonly ILogger<MqttService> _logger;
    private readonly ITopicRouter _router;
    private readonly MqttOptions _mqttOptions;
    private readonly Dictionary<string, Delegate> _handlers = new();
    private readonly ModuleManager _moduleManager;
    private static readonly JsonSerializerSettings settings = new JsonSerializerSettings
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        },
        // 可选：忽略大小写、处理 null 等
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore
    };

    public bool IsConnected => _client.IsConnected;

    public MqttService(ILogger<MqttService> logger, IMqttClient client, ITopicRouter router, IOptions<MqttOptions> mqttOptions, ModuleManager moduleManager)
    {
        _logger = logger;
        _client = client;
        // 注册连接成功回调
        _client.ConnectedAsync += OnConnectedAsync;
        // 注册消息接收回调
        _client.ApplicationMessageReceivedAsync += OnMqttMessageReceived;
        _router = router;
        _mqttOptions = mqttOptions.Value;
        _moduleManager = moduleManager;
    }

    public async Task StartAsync()
    {
        try
        {
            var options = new MqttClientOptionsBuilder()
                  .WithClientId(_mqttOptions.ClientId)
                  .WithTcpServer(_mqttOptions.Server, _mqttOptions.Port)
                  .WithCredentials(_mqttOptions.Username, _mqttOptions.Password)
                  .WithCleanSession(true)
                  .Build();
            var result = await _client.ConnectAsync(options, CancellationToken.None);
            _logger.LogInformation("MQTT 已连接到 {Host}:{Port},Resutlt:{result}", _mqttOptions.Server, _mqttOptions.Port, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MQTT 启动失败");
            return;
        }
    }

    public async Task StopAsync()
    {
        if (_client.IsConnected)
        {
            var disconnectOptions = new MqttClientDisconnectOptions
            {
                Reason = MqttClientDisconnectOptionsReason.NormalDisconnection
            };
            await _client.DisconnectAsync(disconnectOptions);
        }
    }
    //连接成功后的事件
    public async Task OnConnectedAsync(MqttClientConnectedEventArgs e)
    {
        foreach (var topic in _mqttOptions.SubscribedTopics)
        {
            await SubscribeAsync(topic);
        }
    }
    //断开连接后事件
    public void OnDisconnected(MqttClientDisconnectedEventArgs e)
    {
        _logger.LogError("MQTT 断开连接: {Error}", e.Exception);
    }

    public async Task PublishAsync(string topic, object payload, int qos = 1, CancellationToken ct = default)
    {
        var json = payload.ToJson();
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(json)
            .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)qos)
            .Build();

        await _client.PublishAsync(message, ct);
    }

    public async Task SubscribeAsync(string topic, CancellationToken ct = default)
    {
        if (_client.IsConnected)
        {
            var res = await _client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).Build(), ct);
            _logger.LogDebug("订阅主题: {Topic}", topic);
        }
    }

    public void RegisterHandler<T>(string topicPattern, Func<T, Task> handler) where T : class
    {
        _handlers[topicPattern] = handler;
    }

    private async Task OnMqttMessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload).ToObject<CloudMqData<dynamic>>();
        var matched = _moduleManager._modules
            .Where(s => MqttTopicFilterComparer.Compare(topic, s.Topic) == MqttTopicFilterCompareResult.IsMatch)
            .WhereIF(payload.Method.IsNullOrEmpty(), s => s.Method.Equals(payload.Method))
            .WhereIF(topic.EndsWith("state") || topic.EndsWith("osd"), s => MatchSn(topic).Length > 10 ? s.Type == 1 : s.Type == 2)
            .ToList();
        if (matched.Count == 0)
            return;

        foreach (var sub in matched)
        {
            try
            {
                // 构造 CloudMqData<T>
                var cloudDataType = typeof(CloudMqData<>).MakeGenericType(sub.DataType);
                // 替换以下两行：
                // var data = payload.ToObject<ClientErrorData<cloudDataType>>();
                // System.Text.Json.JsonSerializer.Deserialize(payload, cloudDataType, JsonOptions);
                //var data = JsonConvert.DeserializeObject(payload, cloudDataType, settings);
                // 调用方法
                var task = (Task)sub.MethodInfo.Invoke(sub.Instance, [payload])!;
                await task;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MQTT] 处理失败 {topic}: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
        //await _router.RouteAsync(topic, payload, CancellationToken.None);
    }

    private string MatchSn(string topic, string pattern = @"/([^/]+)/(?:osd|state)")
    {
        Match match = Regex.Match(topic, pattern);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        return "";
    }

    public void Dispose() => _client?.Dispose();
}