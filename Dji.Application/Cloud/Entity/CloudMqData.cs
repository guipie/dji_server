// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

namespace Dji.Application.Cloud.Entity;

/// <summary>上行报文信封（<c>bid</c>/<c>tid</c>/<c>method</c>/<c>timestamp</c> + <c>data</c>）</summary>
public class CloudMqData<T>
{
    public string Tid { get; set; }
    public string Bid { get; set; }
    public string Method { get; set; }
    public long TimeStamp { get; set; }

    public string Gateway { get; set; }
    public string DroneSn { get; set; }
    public string Topic { get; set; }

    public T Data { get; set; }

    public string Ext { get; set; }
}

/// <summary>
/// 带 <c>result</c> 的应答体。
/// </summary>
/// <remarks>
/// 适用于 <c>requests_reply</c> / <c>services_reply</c>（载荷形如 <c>data: { output, result }</c>）
/// 以及 <c>events_reply</c>（载荷形如 <c>data: { result }</c>，此时不填 <see cref="Output"/>）
/// —— 两者结构一致，用同一个类型即可。
/// </remarks>
public class MqOutput<T>
{
    public T Output { get; set; }
    public int Result { get; set; }
}

/// <summary>
/// 事件应答体（<c>events_reply</c>）。
/// </summary>
/// <remarks>
/// 与 <see cref="MqOutput{T}"/> 的区别：事件应答的载荷只有 <c>result</c>，没有 <c>output</c>，
/// 因此单独定义以避免多输出一个空对象字段。
/// </remarks>
public class MqEventReply
{
    /// <summary>返回码，0 表示成功</summary>
    public int Result { get; set; }
}