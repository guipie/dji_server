// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Cloud.Core;
using Dji.Application.Cloud.Entity;

namespace Dji.Application.Service.Common;

/// <summary>
/// 机场指令下发（下发 → 等回包 → 取 <c>result</c>）的统一封装。
/// </summary>
/// <remarks>
/// <para>
/// 控制 / 直播 / 日志 / 升级 / 飞行区 / 媒体各 Service 此前各有一份几乎相同的
/// <c>SendAsync</c>：组 <see cref="CloudMqRequest{T}"/> → <see cref="MqttGatewayPublish.PublishWithReplyAsync{T, R}"/>
/// → 取 <c>reply.Data.Result</c> → <c>catch</c> 转 <c>-1</c> → 打日志。这段逻辑的
/// 「超时约定为 -1」「错误码文案」等口径一旦调整就要改遍所有副本。
/// 现收敛为这一处；业务只需传方法名 / 载荷 / 超时 / 动作名，拿到统一的 result 码。
/// </para>
/// <para>
/// <b>返回码约定</b>：<c>0</c> 表示机场已接受（含「回包已按 bid 匹配回来但未带 data/result」的确认态）；
/// 非 0 为错误码；<c>-1</c> 表示超时或异常。
/// </para>
/// </remarks>
public class CommandPublisher(
    MqttGatewayPublish publish,
    ILogger<CommandPublisher> logger) : ITransient
{
    private readonly MqttGatewayPublish _publish = publish;
    private readonly ILogger<CommandPublisher> _logger = logger;

    /// <summary>
    /// 下发一条带载荷的机场指令并等待回包，返回统一 result 码。
    /// </summary>
    /// <typeparam name="T">指令载荷类型（无参指令用 <c>object</c> 并传 <c>null</c>）</typeparam>
    /// <param name="gateway">目标机场 SN</param>
    /// <param name="method">协议方法名</param>
    /// <param name="payload">指令载荷；无参指令传 <c>null</c></param>
    /// <param name="timeoutSeconds">回包等待时长（秒）</param>
    /// <param name="actionName">动作中文名（用于日志与可读报错）</param>
    public async Task<int> SendAsync<T>(string gateway, string method, T payload, int timeoutSeconds, string actionName)
    {
        var request = payload == null
            ? new CloudMqRequest<T>(method, gateway)
            : new CloudMqRequest<T>(method, payload, gateway);

        return await SendRequestAsync(gateway, request, timeoutSeconds, actionName);
    }

    /// <summary>
    /// 下发一个已经组好的请求并等待回包，返回统一 result 码。
    /// </summary>
    /// <remarks>供需要在发之前读取 <c>request.Bid</c> 落库的场景（回包后才有 bid）使用。</remarks>
    public async Task<int> SendRequestAsync<T>(string gateway, CloudMqRequest<T> request, int timeoutSeconds, string actionName)
    {
        try
        {
            var reply = await _publish.PublishWithReplyAsync<T, object>(
                Topics.ThingProductServices, request, timeoutSeconds);

            // 部分指令（如 debug_mode_open/close）机场的 services_reply 只回信封、不带 data/result，
            // 此时 reply.Data 为 null。这类「回包已按 bid 匹配回来」即代表机场已确认收到，
            // 应视为成功（0），而不是误判成 -1 超时；真正执行结果由后续 events 进度驱动。
            // 只有当 data 里明确带了非 0 的 result 时，才按「机场拒绝」处理。
            var result = reply?.Data?.Result ?? 0;
            if (result != 0)
                _logger.LogWarning("机场拒绝{Action}：method:{Method}，gateway:{Gateway}，result:{Result}",
                    actionName, request.Method, gateway, result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Action}指令无应答：method:{Method}，gateway:{Gateway}", actionName, request.Method, gateway);
            return -1;
        }
    }

    /// <summary>
    /// 把 <c>services_reply</c> 的返回码翻译成可读说明。
    /// </summary>
    /// <remarks>
    /// 官方对控制类指令只约定「非 0 代表错误」，未公开逐指令错误码字典；
    /// 超时（平台内部约定为 <c>-1</c>）单独给出明确解释，其余原样带出码值供运维比对。
    /// </remarks>
    public static string DescribeResult(int result)
        => result == -1 ? "设备未在超时时间内回包（指挥链路可能中断）" : $"错误码：{result}";
}
