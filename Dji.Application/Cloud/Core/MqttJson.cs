// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dji.Application.Cloud.Core;

/// <summary>
/// MQTT 报文序列化配置（单一下沉点，收发两侧共用，避免出现两套命名策略导致的字段丢失）。
/// </summary>
internal static class MqttJson
{
    /// <summary>上云协议使用 <c>snake_case</c> 字段命名，如 <c>drone_sn</c>、<c>file_id</c>。</summary>
    public static readonly JsonSerializerSettings Settings = new()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy(),
        },
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore,
    };

    /// <summary>把原始报文反序列化为指定类型</summary>
    public static T Deserialize<T>(string payload)
    {
        return string.IsNullOrWhiteSpace(payload)
            ? default
            : JsonConvert.DeserializeObject<T>(payload, Settings);
    }

    /// <summary>把原始报文反序列化为运行时类型（用于 <c>CloudMqData&lt;T&gt;</c> 的动态构造）</summary>
    public static object Deserialize(string payload, Type type)
    {
        return string.IsNullOrWhiteSpace(payload)
            ? null
            : JsonConvert.DeserializeObject(payload, type, Settings);
    }
}
