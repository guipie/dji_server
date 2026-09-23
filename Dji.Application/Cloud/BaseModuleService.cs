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
}
