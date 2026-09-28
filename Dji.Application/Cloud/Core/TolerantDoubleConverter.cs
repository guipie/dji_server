// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Newtonsoft.Json;
using System.Globalization;

namespace Dji.Application.Cloud.Core;

/// <summary>
/// 宽容的浮点数转换器：数字、数字字符串、空串、null 都能落到 <c>double?</c>。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要它</b>：大疆协议文档里同一个字段的「类型」与「示例」经常不一致 ——
/// 例如媒体元数据的 <c>gimbal_yaw_degree</c> 标注 float，示例却给字符串 <c>"-91.40"</c>；
/// 不同固件版本也确实两种形式都在用。默认的 Newtonsoft 反序列化遇到
/// <c>""</c> 或 <c>"N/A"</c> 这类非数值内容会直接抛异常，而异常会中断<b>整条报文</b>的解析，
/// 导致一个无关字段把整条媒体回调丢掉（该报文 <c>need_reply=1</c>，机场还会不断重发）。
/// </para>
/// <para>
/// 因此这里把「无法识别的单个数值」降级为 <c>null</c>，让报文其余部分正常落地。
/// </para>
/// </remarks>
public class TolerantDoubleConverter : JsonConverter
{
    /// <summary>允许承接的类型（含可空与非可空 double）</summary>
    public override bool CanConvert(Type objectType)
    {
        var type = Nullable.GetUnderlyingType(objectType) ?? objectType;
        return type == typeof(double) || type == typeof(float);
    }

    public override bool CanRead => true;

    /// <summary>只接管读取，写出仍走默认逻辑（避免改变下行报文的数值格式）</summary>
    public override bool CanWrite => false;

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var isNullable = Nullable.GetUnderlyingType(objectType) != null;
        var underlying = Nullable.GetUnderlyingType(objectType) ?? objectType;

        switch (reader.TokenType)
        {
            case JsonToken.Null:
            case JsonToken.Undefined:
                return isNullable ? null : (underlying == typeof(float) ? 0f : 0d);

            case JsonToken.Integer:
            case JsonToken.Float:
                return Convert.ChangeType(reader.Value, underlying, CultureInfo.InvariantCulture);

            case JsonToken.String:
                // 空串 / "null" / "N/A" 一律视为无值，不抛异常
                var text = (reader.Value as string)?.Trim();
                if (string.IsNullOrEmpty(text)) return isNullable ? null : (underlying == typeof(float) ? 0f : 0d);

                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                    return underlying == typeof(float) ? (float)parsed : parsed;

                return isNullable ? null : (underlying == typeof(float) ? 0f : 0d);

            default:
                // 数组 / 对象等完全不符合预期的形态：跳过该值，保持报文其余部分可用
                reader.Skip();
                return isNullable ? null : (underlying == typeof(float) ? 0f : 0d);
        }
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        => throw new NotSupportedException("该转换器仅用于读取协议报文。");
}
