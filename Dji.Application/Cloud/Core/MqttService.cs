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
using Dji.Core.Extension;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static SKIT.FlurlHttpClient.Wechat.Api.Models.CgibinTagsMembersGetBlackListResponse.Types;

namespace Dji.Application.Cloud.Core;
/// <summary>
///  MQTT 客户端管理
/// </summary>
// Core/MqttService.cs

internal class MqttService : IMqttService, IDisposable
{
    private readonly IMqttClient _client; // 注意类型是 IMqttClient
    private readonly ILogger<MqttService> _logger;
    private readonly MqttOptions _mqttOptions;
    private readonly Dictionary<string, Delegate> _handlers = new();
    private readonly ModuleManager _moduleManager;
    private readonly SysCacheService _cache;
    private readonly SysOnlineUserService _onlineUserService;
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
    private bool _IsConnectting=false;

    public MqttService(ILogger<MqttService> logger, IMqttClient client, SysCacheService sysCacheService, IOptions<MqttOptions> mqttOptions, ModuleManager moduleManager, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _client = client;
        // 注册连接成功回调
        _client.ConnectedAsync += OnConnectedAsync;
        // 注册消息接收回调
        _client.ApplicationMessageReceivedAsync += OnMqttMessageReceived;
        _client.DisconnectedAsync += OnDisconnected;
        _mqttOptions = mqttOptions.Value;
        _moduleManager = moduleManager;
        _cache = sysCacheService;
        _onlineUserService = scopeFactory.CreateScope().ServiceProvider.GetRequiredService<SysOnlineUserService>();
    }

    public async Task StartAsync()
    {
        try
        {
            _IsConnectting=true;
            var options = new MqttClientOptionsBuilder()
                  .WithClientId(_mqttOptions.ClientId)
                  .WithTcpServer(_mqttOptions.Server, _mqttOptions.Port)
                  .WithCredentials(_mqttOptions.Username, _mqttOptions.Password)
                  .WithCleanSession(true)
                  .Build();
            var result = await _client.ConnectAsync(options, CancellationToken.None);
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("MQTT 已连接到 {0}:{1},Resutlt:{2}", _mqttOptions.Server, _mqttOptions.Port, result);
            _logger.LogInformation("MQTT 已连接到 {Host}:{Port},Resutlt:{result}", _mqttOptions.Server, _mqttOptions.Port, result);

            _IsConnectting = false;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("MQTT 启动失败[{1}],Error:{0}", ex.StackTrace,DateTime.Now.ToString());
            _logger.LogError(ex, "MQTT 启动失败");
            _IsConnectting=false;
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
    public async Task OnDisconnected(MqttClientDisconnectedEventArgs e)
    {
        _logger.LogError("MQTT 断开连接: {Error}", e.Exception);
        if (_client == null || _client.IsConnected == false)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("MQTT 已断开连接，正在尝试重新连接...");
            Thread.Sleep(5 * 1000);
            if (!_IsConnectting)
                await StartAsync();
        }
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
        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

        var cloudMqData = payload.ToObject<CloudMqData<dynamic>>();
        //if (topic == "thing/product/8UUXN5600A07KY/osd") 
        //Console.WriteLine("接受消息，topic:{0},method:{1},gateway:{2}", topic, cloudMqData.Method, ((JObject)cloudMqData.Data)["acc_time"]);
        var matched = _moduleManager._modules
            .Where(s => MqttTopicFilterComparer.Compare(topic, s.Topic) == MqttTopicFilterCompareResult.IsMatch)
            .WhereIF(!cloudMqData.Method.IsNullOrEmpty(), s => s.Method.Equals(cloudMqData.Method))
            .WhereIF(IsSameTopic(topic), s => s.Domain == (MatchSn(topic).IsDrone() ? DomainEnum.Drone : DomainEnum.Dock))
            .ToList();
        if (matched.Count == 0)
            return;
        else if (matched.Count > 1)
        {
            Console.WriteLine($"[MQTT] 订阅了多个重复的方法 {topic}", topic);
            _logger.LogError($"[MQTT] 订阅了多个重复的方法 {topic}", topic);
        }

        foreach (var sub in matched)
        {
            try
            {
                // 构造 CloudMqData<T>
                var cloudDataType = typeof(CloudMqData<>).MakeGenericType(sub.DataType);
                // 替换以下两行：
                // var data = payload.ToObject<ClientErrorData<cloudDataType>>();
                // System.Text.Json.JsonSerializer.Deserialize(payload, cloudDataType, JsonOptions);
                var data = JsonConvert.DeserializeObject(payload, cloudDataType, settings);
                if (cloudMqData.Gateway.IsNullOrEmpty() || IsSameTopic(topic))
                {
                    var index = topic.LastIndexOf('/');
                    var secondIndex = topic[..index].LastIndexOf('/');

                    var sn = topic.Substring(secondIndex + 1, index - secondIndex - 1);
                    if (cloudMqData.Gateway.IsNullOrEmpty())
                    {
                        data.GetType().GetProperty("Gateway").SetValue(data, sn);
                        cloudMqData.Gateway = sn;
                    }
                    if (IsSameTopic(topic))
                        data.GetType().GetProperty("DroneSn").SetValue(data, sn);
                }
                data.GetType().GetProperty("Topic").SetValue(data, topic);

                var onlineDevice = _cache.Get<DjiDevice>(cloudMqData.Gateway.Device());
                if (onlineDevice != null)
                {
                    data.GetType().GetProperty("Ext").SetValue(data, onlineDevice.Nick);
                    if (!onlineDevice.WorkspaceId.IsNullOrWhiteSpace())
                    {
                        cloudMqData.Method = cloudMqData.Method.IsNullOrEmpty() ? (MatchSn(topic).IsDrone() ? "droneOsd" : "dockOsd") : cloudMqData.Method;
                        data.GetType().GetProperty("Method").SetValue(data, cloudMqData.Method);
                        await _onlineUserService.PublicWorkspaceMqMessage(onlineDevice.WorkspaceId, data);
                    }
                }
                var task = (Task)sub.MethodInfo.Invoke(sub.Instance, [data])!;
                if (topic.EndsWith("_reply"))
                {
                    Console.WriteLine($"[MQTT_REPLY] 缓存回复消息,topic: {topic},result:{data.ToJson()}");
                    _cache.Set(cloudMqData.Bid.Reply(), data, TimeSpan.FromSeconds(30));
                }
                await task;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MQTT] 处理失败 {topic}: {ex.InnerException?.Message ?? ex.Message},track:{ex.StackTrace}");
                _logger.LogError($"[MQTT] 处理失败 {topic}: {ex.InnerException?.Message ?? ex.Message},track:{ex.StackTrace}");
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
    private bool IsSameTopic(string topic)
    {
        return topic.EndsWith("/osd") || topic.EndsWith("/state") || topic.EndsWith("/property/set_reply");
    }

    public void Dispose() => _client?.Dispose();
}