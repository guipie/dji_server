// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using MQTTnet;

namespace Dji.Application.Cloud.Core;

/// <summary>
/// MQTT 客户端核心接口。
/// </summary>
/// <remarks>
/// 上行报文的业务分发由 <see cref="ModuleManager"/> 配合 <c>MqttSubscribeAttribute</c> 完成，
/// 无需在此注册回调（历史上存在的 <c>RegisterHandler</c> 注册后从未被调用，已移除）。
/// </remarks>
internal interface IMqttService
{
    /// <summary>当前是否已连接</summary>
    bool IsConnected { get; }

    /// <summary>启动并连接</summary>
    Task StartAsync();

    /// <summary>断开连接</summary>
    Task StopAsync();

    /// <summary>连接成功回调（内部按配置订阅主题）</summary>
    Task OnConnectedAsync(MqttClientConnectedEventArgs e);

    /// <summary>直接向指定主题发布消息</summary>
    Task PublishAsync(string topic, object payload, int qos = 1, CancellationToken ct = default);

    /// <summary>
    /// 订阅主题。
    /// </summary>
    /// <remarks>
    /// 不返回 bool 是因为必须区分「broker 明确拒绝」（配置问题，重试无意义）
    /// 与「瞬时失败」（可重试）—— 两者在协议层是完全不同的结果码。
    /// </remarks>
    Task<SubscribeOutcome> SubscribeAsync(string topic, CancellationToken ct = default);
}
