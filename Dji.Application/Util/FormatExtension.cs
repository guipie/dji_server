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
/// 通用展示格式化扩展。
/// </summary>
/// <remarks>
/// 字节大小转可读文本此前散落在媒体 / 日志 / 飞行区三个 Service 里各有一份，
/// 且口径已经发生漂移（对 <c>&lt;=0</c> 的处理、单位上限、精度都不一致）。
/// 现统一为这一处，业务代码调用 <see cref="FormatSize(long?)"/> 即可。
/// </remarks>
[SuppressSniffer]
public static class FormatExtension
{
    private static readonly string[] SizeUnits = ["B", "KB", "MB", "GB", "TB"];

    /// <summary>
    /// 字节数转可读文本（自适应单位：B / KB / MB / GB / TB）。
    /// </summary>
    /// <param name="size">字节数；<c>null</c> 或 <c>&lt;=0</c> 返回 "-"</param>
    public static string FormatSize(this long? size)
    {
        if (size is null or <= 0) return "-";

        double value = size.Value;
        var unit = 0;
        while (value >= 1024 && unit < SizeUnits.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit == 0 ? $"{value:0} {SizeUnits[unit]}" : $"{value:0.##} {SizeUnits[unit]}";
    }

    /// <summary>字节数转可读文本（<c>long</c> 重载，内部转为可空后复用同一实现）。</summary>
    public static string FormatSize(this long size) => ((long?)size).FormatSize();
}
