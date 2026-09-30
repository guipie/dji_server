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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Dji.Application.Cloud.Core;

/// <summary>
/// MQTT 客户端：负责连接、订阅、上行业务消息的解析与分发。
/// </summary>
/// <remarks>
/// 分发流程：<c>报文 → 解析 → 匹配订阅方法（按 Topic/Method/Domain）→ 注入信封字段 → 可选推送前端 → 执行处理器</c>。
/// 回复报文（主题以 <c>_reply</c> 结尾）会按 <c>bid</c> 缓存原始 JSON，供 <see cref="MqttGatewayPublish.PublishWithReplyAsync{T, R}"/> 取用。
/// </remarks>
internal class MqttService : IMqttService, IDisposable
{
    /// <summary>回复报文的缓存时长</summary>
    private static readonly TimeSpan ReplyCacheTtl = TimeSpan.FromSeconds(30);

    /// <summary>断线重连前的等待时长</summary>
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);

    /// <summary>从主题中提取设备 SN，如 <c>thing/product/{sn}/osd</c></summary>
    private const string SnPattern = @"/([^/]+)/(?:osd|state)";

    private readonly IMqttClient _client;
    private readonly ILogger<MqttService> _logger;
    private readonly MqttOptions _mqttOptions;
    private readonly ModuleManager _moduleManager;
    private readonly SysCacheService _cache;
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>串行化连接与重连，避免断线回调并发触发多次连接</summary>
    private readonly SemaphoreSlim _connectLock = new(1, 1);

    /// <summary>订阅重试的串行锁（同一时刻只跑一轮）</summary>
    private readonly SemaphoreSlim _subscribeRetryLock = new(1, 1);

    /// <summary>订阅重试间隔</summary>
    private static readonly TimeSpan SubscribeRetryInterval = TimeSpan.FromSeconds(60);

    /// <summary>待重试订阅的主题（仅「瞬时失败」，不含 broker 授权拒绝）</summary>
    private readonly List<string> _retryTopics = [];

    private Timer _subscribeRetryTimer;

    private volatile bool _isConnecting;

    public MqttService(
        ILogger<MqttService> logger,
        IMqttClient client,
        SysCacheService sysCacheService,
        IOptions<MqttOptions> mqttOptions,
        ModuleManager moduleManager,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _client = client;
        _client.ConnectedAsync += OnConnectedAsync;
        _client.ApplicationMessageReceivedAsync += OnMqttMessageReceived;
        _client.DisconnectedAsync += OnDisconnected;
        _mqttOptions = mqttOptions.Value;
        _moduleManager = moduleManager;
        _cache = sysCacheService;
        _scopeFactory = scopeFactory;
    }

    public bool IsConnected => _client.IsConnected;

    public async Task StartAsync()
    {
        await _connectLock.WaitAsync();
        try
        {
            if (_client.IsConnected) return;

            _isConnecting = true;
            var options = new MqttClientOptionsBuilder()
                .WithClientId(_mqttOptions.ClientId)
                .WithTcpServer(_mqttOptions.Server, _mqttOptions.Port)
                .WithCredentials(_mqttOptions.Username, _mqttOptions.Password)
                .WithCleanSession(_mqttOptions.CleanSession)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(_mqttOptions.KeepAliveSeconds))
                .Build();

            var result = await _client.ConnectAsync(options, CancellationToken.None);

            // CONNACK 被拒时 ConnectAsync 同样是正常返回（不抛异常），必须自己检查结果码。
            // 历史实现无条件打印「已连接」，于是「被 broker 拒绝 + 一条主题都订不上」
            // 在日志上与「连接正常」完全一样，表现为「设备明明在线却收不到任何指令回执」。
            if (result.ResultCode != MqttClientConnectResultCode.Success)
            {
                _logger.LogError("MQTT 连接被拒绝 {Host}:{Port} 结果码:{Code}({Reason})，{Delay} 秒后重试；本次不会执行主题订阅",
                    _mqttOptions.Server, _mqttOptions.Port, (int)result.ResultCode, result.ResultCode, ReconnectDelay.TotalSeconds);
                return;
            }

            _logger.LogInformation("MQTT 已连接 {Host}:{Port}，结果:{Result}", _mqttOptions.Server, _mqttOptions.Port, result.ResultCode);

            // 关键修复：MQTTnet 5.x 在断线重连场景下，ConnectedAsync 事件不一定会被触发
            // （特别是在 OnDisconnected → StartAsync → ConnectAsync 的重连路径中）。
            // 如果只依赖 ConnectedAsync 事件来订阅，断线重连后主题订阅会永久丢失，
            // 表现为 OSD 能收到（EMQX 内部缓存推送）但 reply 收不到（一次性消息错过即丢失）。
            // 因此在 StartAsync 连接成功后直接调用 OnConnectedAsync，确保每次连接都执行订阅。
            await OnConnectedAsync(new MqttClientConnectedEventArgs(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MQTT 连接失败，服务端:{Host}:{Port}", _mqttOptions.Server, _mqttOptions.Port);
        }
        finally
        {
            _isConnecting = false;
            _connectLock.Release();
        }
    }

    public async Task StopAsync()
    {
        if (!_client.IsConnected) return;

        await _client.DisconnectAsync(new MqttClientDisconnectOptions
        {
            Reason = MqttClientDisconnectOptionsReason.NormalDisconnection,
        });
    }

    /// <summary>连接成功后按配置订阅全部主题</summary>
    /// <remarks>
    /// 逐个主题独立处理：某个主题订阅失败（broker 授权拒绝等）不应连带跳过其余主题。
    /// 历史实现的循环里一旦抛异常，后面的主题就再也订不上了，且没有任何日志。
    /// </remarks>
    public async Task OnConnectedAsync(MqttClientConnectedEventArgs e)
    {
        var topics = _mqttOptions.SubscribedTopics;
        _logger.LogInformation("MQTT 连接成功，开始订阅 {Count} 个主题", topics.Count);

        var denied = new List<string>();
        var failed = new List<string>();
        foreach (var topic in topics)
        {
            var outcome = await SubscribeAsync(topic);
            if (outcome == SubscribeOutcome.Denied) denied.Add(topic);
            else if (outcome == SubscribeOutcome.Failed) failed.Add(topic);
        }

        if (denied.Count > 0)
        {
            // 授权拒绝重试没有意义，必须人工改 broker 配置，所以单独出一条明确的错误日志
            _logger.LogError("MQTT 有 {Count} 个主题被 broker 拒绝订阅：{Topics}。"
                             + "这些主题的消息不会进入本服务（可用 EMQX 控制台「客户端 → 订阅」核对），请检查 broker 的授权（ACL/authorization）配置",
                denied.Count, string.Join("、", denied));
        }

        // 瞬时失败（超时 / 异常）安排后台重试：否则一次网络抖动会让某个主题永久失联到下次重启
        if (failed.Count > 0) ScheduleSubscribeRetry(failed);

        if (denied.Count == 0 && failed.Count == 0)
        {
            _logger.LogInformation("MQTT 全部 {Count} 个主题订阅成功", topics.Count);
        }
    }

    /// <summary>断线回调：延迟后重连</summary>
    private async Task OnDisconnected(MqttClientDisconnectedEventArgs e)
    {
        _logger.LogError(e.Exception, "MQTT 连接已断开");

        // 历史实现用 Thread.Sleep 阻塞线程池线程，这里改为异步等待
        await Task.Delay(ReconnectDelay);

        if (!_isConnecting && !_client.IsConnected)
        {
            await StartAsync();
        }
    }

    public async Task PublishAsync(string topic, object payload, int qos = 1, CancellationToken ct = default)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload.ToJson())
            .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)qos)
            .Build();

        await _client.PublishAsync(message, ct);
    }

    /// <summary>
    /// 订阅主题，并检查 SUBACK 结果码。
    /// </summary>
    /// <remarks>
    /// broker 授权拒绝时返回的是 SUBACK 里的失败码（如 135 未授权、162 不支持通配符），
    /// <b>MQTTnet 不会抛异常</b>；历史实现没有检查结果码，导致「订阅被拒」和「订阅成功」
    /// 在日志上完全一样，只能表现为「设备明明回了包却一直超时」。
    /// </remarks>
    /// <returns>订阅结果；<see cref="SubscribeOutcome.Denied"/> 表示 broker 明确拒绝</returns>
    public async Task<SubscribeOutcome> SubscribeAsync(string topic, CancellationToken ct = default)
    {
        if (!_client.IsConnected)
        {
            _logger.LogWarning("MQTT 未连接，无法订阅 topic:{Topic}", topic);
            return SubscribeOutcome.Failed;
        }

        try
        {
            var result = await _client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).Build(), ct);
            var items = result?.Items ?? [];

            // 结果码 > 2 即为明确拒绝（授权、通配符不支持等），属于配置问题，重试没有意义
            foreach (var item in items)
            {
                // 记录订阅结果（包括被拒绝的，方便诊断 ACL 问题）
                MqttDiagnostics.OnSubscribeResult(item.TopicFilter.Topic, (int)item.ResultCode);
            }

            foreach (var item in items.Where(m => IsSubscribeDenied(m.ResultCode)))
            {
                _logger.LogError("MQTT 订阅被 broker 拒绝 topic:{Topic} 结果码:{Code}({Reason})，请检查 broker 的授权（ACL）配置",
                    item.TopicFilter, (int)item.ResultCode, DescribeSubscribeCode((int)item.ResultCode));
            }
            if (items.Any(m => IsSubscribeDenied(m.ResultCode))) return SubscribeOutcome.Denied;

            var granted = items.Where(m => (int)m.ResultCode <= 2).Select(m => (int)m.ResultCode).ToList();
            if (granted.Count == 0)
            {
                // 一个结果码都没拿到属于协议异常，归为可重试
                _logger.LogWarning("MQTT 订阅未取得结果 topic:{Topic}，将安排重试", topic);
                return SubscribeOutcome.Failed;
            }

            _logger.LogInformation("MQTT 已订阅 topic:{Topic} 授予QoS:{Qos}", topic, string.Join("/", granted));
            return SubscribeOutcome.Granted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MQTT 订阅异常 topic:{Topic}，将安排重试", topic);
            return SubscribeOutcome.Failed;
        }
    }

    /// <summary>SUBACK 结果码 > 2 表示 broker 明确拒绝（非授予 QoS0/1/2）</summary>
    private static bool IsSubscribeDenied(MqttClientSubscribeResultCode code) => (int)code > 2;

    /// <summary>把可重试的订阅失败登记到后台重试队列</summary>
    /// <remarks>
    /// 没有这一步的话，一次网络抖动导致的订阅失败会让该主题<b>永久失联到下次重启</b> ——
    /// 表现和「设备不回包」一模一样，但没人知道订阅早就掉了。
    /// </remarks>
    private void ScheduleSubscribeRetry(IEnumerable<string> topics)
    {
        var added = new List<string>();
        lock (_retryTopics)
        {
            foreach (var topic in topics)
            {
                if (_retryTopics.Contains(topic)) continue;
                _retryTopics.Add(topic);
                added.Add(topic);
            }
        }

        if (added.Count == 0) return;

        _subscribeRetryTimer ??= new Timer(OnSubscribeRetryTick, null, SubscribeRetryInterval, SubscribeRetryInterval);
        _logger.LogWarning("MQTT 订阅失败的主题已进入后台重试（每 {Seconds} 秒一次）：{Topics}",
            SubscribeRetryInterval.TotalSeconds, string.Join("、", added));
    }

    /// <summary>后台重试订阅；Timer 回调要求返回 void，故用 async void + 全量 try/catch 兜底</summary>
    private async void OnSubscribeRetryTick(object state)
    {
        if (!await _subscribeRetryLock.WaitAsync(0)) return;

        try
        {
            if (!_client.IsConnected) return;

            string[] pending;
            lock (_retryTopics) pending = [.. _retryTopics];
            if (pending.Length == 0) return;

            foreach (var topic in pending)
            {
                var outcome = await SubscribeAsync(topic);
                if (outcome == SubscribeOutcome.Failed) continue;

                // 成功、或已被 broker 明确拒绝（拒绝时上面已打过错误日志），都无需再重试
                lock (_retryTopics) _retryTopics.Remove(topic);
                if (outcome == SubscribeOutcome.Granted)
                    _logger.LogInformation("MQTT 订阅重试成功 topic:{Topic}", topic);
            }

            lock (_retryTopics)
            {
                if (_retryTopics.Count > 0) return;
            }

            _subscribeRetryTimer?.Dispose();
            _subscribeRetryTimer = null;
            _logger.LogInformation("MQTT 订阅重试已全部完成，停止重试定时器");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MQTT 订阅重试异常");
        }
        finally
        {
            _subscribeRetryLock.Release();
        }
    }

    /// <summary>把 SUBACK 结果码翻译成可读原因</summary>
    private static string DescribeSubscribeCode(int code) => code switch
    {
        128 => "未指定错误（broker 拒绝）",
        131 => "实现相关错误",
        135 => "未授权（broker ACL 拒绝）",
        143 => "主题过滤器非法",
        145 => "报文标识符被占用",
        151 => "超出配额",
        158 => "不支持共享订阅",
        161 => "不支持订阅标识符",
        162 => "不支持通配符订阅",
        _ => "未知",
    };

    /// <summary>
    /// 匹配出的订阅方法必须唯一；重复订阅属于配置错误，只执行优先级最高的一条。
    /// </summary>
    /// <remarks>
    /// 历史实现在 <c>matched.Count &gt; 1</c> 时仅打印日志、随后仍遍历执行全部处理器，
    /// 会造成同一报文被重复处理（例如机场 OSD 同时进入飞行器处理器并写坏设备记录）。
    /// </remarks>
    private IReadOnlyList<SubscriptionModel> MatchModules(string topic, string method)
    {
        var isSameTopic = IsSameTopic(topic);
        var domain = isSameTopic ? ResolveDomain(MatchSn(topic)) : null;

        var matched = _moduleManager._modules
            .Where(s => MqttTopicFilterComparer.Compare(topic, s.Topic) == MqttTopicFilterCompareResult.IsMatch)
            .WhereIF(!method.IsNullOrEmpty(), s => s.Method.Equals(method))
            .WhereIF(isSameTopic, s => s.Domain == domain)
            .ToList();

        if (matched.Count <= 1) return matched;

        var chosen = matched.FirstOrDefault(m => m.Domain != null) ?? matched[0];
        _logger.LogError("[MQTT] 主题 {Topic} 方法 {Method} 匹配到 {Count} 个处理器（{Handlers}），仅执行 {Chosen}",
            topic, method, matched.Count,
            string.Join(", ", matched.Select(m => $"{m.DeclaringType.Name}.{m.MethodInfo.Name}")),
            $"{chosen.DeclaringType.Name}.{chosen.MethodInfo.Name}");

        return [chosen];
    }

    private async Task OnMqttMessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        if (topic.EndsWith("reply")) {
            _logger.LogWarning("reply:消息" + Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
        }
        MqttDiagnostics.OnMessageReceived(topic);
        _logger.LogInformation("[MQTT-RX] topic:{Topic}", topic);
        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
        if (payload.IsNullOrWhiteSpace()) return;

        CloudMqData<dynamic> cloudMqData;
        try
        {
            cloudMqData = MqttJson.Deserialize<CloudMqData<dynamic>>(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MQTT] 报文解析失败 topic:{Topic}", topic);
            return;
        }

        if (cloudMqData == null) return;
        cloudMqData.Topic = topic;

        // 先按 bid 缓存回复原文，供 PublishWithReplyAsync 取用。
        // 这里刻意缓存原始 JSON 而非强类型对象：调用方期望的输出类型各不相同（MqOutput<T> 中的 T 不同），
        // 缓存原文可以让任意类型的等待方自行反序列化，也避免因缺少处理器而漏缓存。
        if (topic.EndsWith("_reply"))
        {
            if (cloudMqData.Bid.IsNullOrWhiteSpace())
            {
                // 没有 bid 就永远无法与请求关联，属协议异常，必须显式报警而不是静默丢弃
                _logger.LogWarning("[MQTT] 收到应答报文但没有 bid，无法与请求关联 topic:{Topic} method:{Method}", topic, cloudMqData.Method);
            }
            else
            {
                var replyKey = cloudMqData.Bid.Reply();
                var cached = _cache.Set(replyKey, payload, ReplyCacheTtl);

                ReplyDiagnostics.OnReplyReceived(topic, cloudMqData.Bid);
                _logger.LogInformation("[MQTT] 收到应答 topic:{Topic} bid:{Bid} method:{Method} 写入缓存:{Cached}",
                    topic, cloudMqData.Bid, cloudMqData.Method, cached ? "成功" : "失败");
            }
        }

        var matched = MatchModules(topic, cloudMqData.Method);
        if (matched.Count == 0) return;

        // 机场/飞行器的 OSD 报文主题里是设备 SN，但载荷的 gateway 字段可能为空（同一网关的子设备），
        // 统一在进入处理器前补全信封字段。
        var sn = MatchSn(topic);
        if (cloudMqData.Gateway.IsNullOrWhiteSpace() && !sn.IsNullOrWhiteSpace() && !IsSameTopic(topic))
        {
            cloudMqData.Gateway = sn;
        }

        foreach (var sub in matched)
        {
            try
            {
                var data = MqttJson.Deserialize(payload, typeof(CloudMqData<>).MakeGenericType(sub.DataType));
                if (data == null) continue;

                await PrepareForDispatchAsync(data, cloudMqData, topic, sn);

                await (Task)sub.MethodInfo.Invoke(sub.Instance, [data])!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MQTT] 处理失败 topic:{Topic} handler:{Handler}", topic, sub.MethodInfo.Name);
            }
        }
    }

    /// <summary>
    /// 注入信封字段（Topic / Gateway / DroneSn / Method / Ext），并把报文推送给对应工作空间内的在线用户。
    /// </summary>
    /// <remarks>
    /// OSD 报文统一补一个 <c>dockOsd</c> / <c>droneOsd</c> 的 method，前端据此选择处理器。
    /// 推送环节使用短生命周期作用域解析 <see cref="SysOnlineUserService"/>（该类为 Transient）。
    /// </remarks>
    private async Task PrepareForDispatchAsync(object data, CloudMqData<dynamic> raw, string topic, string sn)
    {
        var type = data.GetType();
        var isSameTopic = IsSameTopic(topic);

        // 机场自身的 OSD/状态报文载荷里不带 gateway，此时网关就是主题中的设备 SN；
        // 飞行器的报文载荷里 gateway 是所属机场 SN，不能覆盖。
        if (raw.Gateway.IsNullOrWhiteSpace() && !sn.IsNullOrWhiteSpace())
        {
            raw.Gateway = sn;
        }

        SetProperty(type, data, "Topic", topic);
        SetProperty(type, data, "Gateway", raw.Gateway);

        if (isSameTopic && !sn.IsNullOrWhiteSpace())
        {
            SetProperty(type, data, "DroneSn", sn);
        }

        var method = !raw.Method.IsNullOrEmpty()
            ? raw.Method
            : isSameTopic ? (ResolveDomain(sn) == DomainEnum.Drone ? "droneOsd" : "dockOsd") : null;
        SetProperty(type, data, "Method", method);

        if (raw.Gateway.IsNullOrWhiteSpace()) return;

        var device = _cache.Get<DjiDevice>(raw.Gateway.Device());
        SetProperty(type, data, "Ext", device?.Nick);

        if (device == null || device.WorkspaceId.IsNullOrWhiteSpace()) return;

        using var scope = _scopeFactory.CreateScope();
        var onlineUserService = scope.ServiceProvider.GetRequiredService<SysOnlineUserService>();
        await onlineUserService.PublicWorkspaceMqMessage(device.WorkspaceId, data);
    }

    /// <summary>反射写入信封字段；字段不存在时静默跳过</summary>
    private static void SetProperty(Type type, object instance, string name, object value)
    {
        if (string.IsNullOrEmpty(name)) return;

        var property = type.GetProperty(name);
        if (property is null || !property.CanWrite) return;

        // 值为空时不要覆盖 DTO 已有的默认值
        if (value is null || (value is string text && text.Length == 0)) return;

        property.SetValue(instance, value);
    }

    /// <summary>判断主题是否为“设备自身状态”类（需要按机场/飞行器区分处理器）</summary>
    private static bool IsSameTopic(string topic)
    {
        return topic.EndsWith("/osd") || topic.EndsWith("/state") || topic.EndsWith("/property/set_reply");
    }

    /// <summary>从主题中解析设备 SN</summary>
    private static string MatchSn(string topic, string pattern = SnPattern)
    {
        var match = Regex.Match(topic, pattern);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    /// <summary>
    /// 判定该 SN 属于机场还是飞行器。
    /// </summary>
    /// <remarks>
    /// 优先取设备档案中的领域（最可靠）；档案尚未建立时退化为 SN 长度约定，见 <see cref="DeviceExtension.IsDrone"/>。
    /// </remarks>
    private DomainEnum? ResolveDomain(string sn)
    {
        if (sn.IsNullOrWhiteSpace()) return null;

        var device = _cache.Get<DjiDevice>(sn.Device());
        return device?.Domain ?? (sn.IsDrone() ? DomainEnum.Drone : DomainEnum.Dock);
    }

    public void Dispose()
    {
        _client.ConnectedAsync -= OnConnectedAsync;
        _client.ApplicationMessageReceivedAsync -= OnMqttMessageReceived;
        _client.DisconnectedAsync -= OnDisconnected;
        _connectLock.Dispose();
        _subscribeRetryTimer?.Dispose();
        _subscribeRetryLock.Dispose();
        _client?.Dispose();
    }
}

/// <summary>主题订阅结果</summary>
internal enum SubscribeOutcome
{
    /// <summary>订阅成功（授予 QoS0/1/2）</summary>
    Granted,

    /// <summary>broker 明确拒绝（授权、过滤器非法等），属于配置问题，重试无意义</summary>
    Denied,

    /// <summary>瞬时失败（未连接 / 超时 / 协议异常），可后台重试</summary>
    Failed,
}
