// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

namespace Dji.Application.Util;

/// <summary>
/// 设备 SN / MQTT 主题相关扩展。
/// </summary>
[SuppressSniffer]
public static partial class DeviceExtension
{
    /// <summary>大疆机场 SN 长度上限（当前机型为 14 位，如 <c>7CTDM6A00B00AF</c>）</summary>
    private const int DockSnMaxLength = 16;

    /// <summary>
    /// 是否为大疆机场。
    /// </summary>
    /// <remarks>
    /// ⚠️ 基于 SN 长度的判定只是当前机型约定下的兜底手段（机场 14 位、飞行器 20 位）。
    /// 业务代码应优先使用 <c>DjiDevice.Domain</c>；此方法仅在设备尚未建档、
    /// 需要在 MQTT 入口处区分机场与飞行器时使用。新机型若不符合该长度约定需同步调整。
    /// </remarks>
    public static bool IsDock(this string val)
    {
        return !string.IsNullOrEmpty(val) && val.Length <= DockSnMaxLength;
    }

    /// <summary>
    /// 是否为飞行器。
    /// </summary>
    /// <remarks>参见 <see cref="IsDock"/> 的说明。</remarks>
    public static bool IsDrone(this string val)
    {
        return !string.IsNullOrEmpty(val) && val.Length > DockSnMaxLength;
    }

    /// <summary>
    /// 把主题中的 <c>+/</c> 占位替换为指定网关 SN，得到可发布的具体主题。
    /// </summary>
    /// <remarks>
    /// 若 SN 为空会拼出 <c>thing/product//services</c> 这类非法主题，指令将永远无法送达且难以排查，
    /// 因此这里直接抛出由上层记录。
    /// </remarks>
    public static string BindGateway(this string topic, string sn)
    {
        if (sn.IsNullOrWhiteSpace())
            throw new InvalidOperationException($"下发主题 {topic} 时网关 SN 为空，无法构造完整主题");

        return topic.Replace("/+/", $"/{sn}/");
    }
}
