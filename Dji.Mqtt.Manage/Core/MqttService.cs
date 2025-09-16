// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。



using Dji.Mqtt.Manage.Option;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using System.Text.Json;

namespace Dji.Mqtt.Manage.Core;
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

    public bool IsConnected => _client.IsConnected;

    public MqttService(ILogger<MqttService> logger, IMqttClient client, ITopicRouter router, MqttOptions mqttOptions)
    {
        _logger = logger; 
        _client = client;
        // 注册消息接收回调
        _client.ApplicationMessageReceivedAsync += OnMqttMessageReceived;
        _router = router;
        _mqttOptions = mqttOptions;
    }

    public async Task StartAsync()
    {
        var options = new MqttClientOptionsBuilder()
            .WithClientId(_mqttOptions.ClientId)
            .WithTcpServer(_mqttOptions.Server, _mqttOptions.Port)
            .WithCredentials(_mqttOptions.Username, _mqttOptions.Password)
            .WithCleanSession(true)
            .Build();
        await _client.ConnectAsync(options, CancellationToken.None);
        _logger.LogInformation("MQTT 已连接到 {Host}:{Port}", _mqttOptions.Server, _mqttOptions.Port);
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

    public async Task PublishAsync(string topic, object payload, int qos = 1, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(payload);
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
        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

        await _router.RouteAsync(topic, payload, CancellationToken.None);
    }

    private bool IsMatch(string pattern, string topic)
    {
        return pattern.Replace("{product_id}", "*").Replace("{sn}", "*")
               .Split('*').All(part => topic.Contains(part));
    }

    public void Dispose() => _client?.Dispose();
}