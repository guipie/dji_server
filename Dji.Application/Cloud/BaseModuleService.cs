// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Cloud.Entity;
using Dji.Application.Enum;

namespace Dji.Application.Cloud;

[MqttController]
public abstract class BaseModuleService
{
    public CloudMqRequest<MqOutput<T>> ToPublishOutputData<T, F>(T data, CloudMqData<F> from, int result = 0)
    {
        var mqOutput = new MqOutput<T>()
        {
            Output = data,
            Result = result
        };
        return new CloudMqRequest<MqOutput<T>>(from.Method, mqOutput, from.Gateway, from.Tid, from.Bid);
    }
    public CloudMqRequest<MqOutput<object>> ToPublishOutputDataError<F>(CloudMqData<F> from, DjiReplyErrorEnum error)
    {
        var mqOutput = new MqOutput<object>()
        {
            Output = null,
            Result = (int)error
        };
        return new CloudMqRequest<MqOutput<object>>(from.Method, mqOutput, from.Gateway, from.Tid, from.Bid);
    }

    /// <summary>
    /// 构造 <c>events_reply</c> 应答。
    /// </summary>
    /// <remarks>
    /// 协议规定带 <c>need_reply = 1</c> 的上行事件（HMS 告警、媒体回调、飞行区同步进度、AirSense 告警等）
    /// <b>必须应答</b>，否则设备会不断重发同一条报文。
    /// 应答体的结构比 <c>requests_reply</c> 简单得多（只有一个 <c>result</c>，没有 <c>output</c>），
    /// 且要沿用原报文的 <c>tid</c> / <c>bid</c> 以便设备侧做请求应答配对，故单独提供此辅助方法。
    /// </remarks>
    public CloudMqRequest<MqEventReply> ToEventReply<F>(CloudMqData<F> from, int result = 0)
        => new(from.Method, new MqEventReply { Result = result }, from.Gateway, from.Tid, from.Bid);
}
