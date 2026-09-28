// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

namespace Dji.Core.Enum.DjiEnum.Media;

/// <summary>
/// 媒体文件类型。
/// </summary>
/// <remarks>
/// <para>
/// <b>协议侧没有这个字段</b>，取值由云端按文件名后缀推断（见 <see cref="MediaFileTypeResolver"/>）。
/// 之所以落库而不是每次临时算：媒体列表要按类型筛选并做分类统计，落到列上才能走索引。
/// </para>
/// <para>
/// 后三种是航线作业的<b>副产品</b>，容易被忽略但排查 RTK 精度问题时会用到：
/// 机场 2 较新固件会把飞行器 SD 卡里的 PPK 文件与 RTCM 原始数据一并回传
/// （详见官方「机场 - 媒体管理」文档）。
/// </para>
/// </remarks>
public enum MediaFileTypeEnum
{
    /// <summary>未知类型</summary>
    [Description("未知")]
    Unknown = 0,

    /// <summary>图片（jpg/jpeg/png/dng/tif 等）</summary>
    [Description("图片")]
    Image = 1,

    /// <summary>视频（mp4/mov/mkv/avi 等）</summary>
    [Description("视频")]
    Video = 2,

    /// <summary>PPK 后处理数据（obs/rtk/mrk/nav）</summary>
    [Description("PPK数据")]
    Ppk = 3,

    /// <summary>RTCM 原始观测数据（dat）</summary>
    [Description("RTCM数据")]
    Rtcm = 4,

    /// <summary>其他附件（日志、分屏等未归类文件）</summary>
    [Description("其他")]
    Other = 9
}

/// <summary>
/// 按文件名 / 后缀推断 <see cref="MediaFileTypeEnum"/>。
/// </summary>
/// <remarks>
/// 单独抽出来是为了让「协议文件 → 类型」这条规则只有一个实现处：
/// 上行落下时写入列，历史数据回填、前端兜底展示都复用同一份规则。
/// </remarks>
public static class MediaFileTypeResolver
{
    private static readonly HashSet<string> ImageSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".tif", ".tiff",
        // 大疆红外相机的原始格式
        ".dng", ".raw", ".rw2"
    };

    private static readonly HashSet<string> VideoSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mov", ".mkv", ".avi", ".m4v", ".ts"
    };

    private static readonly HashSet<string> PpkSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        ".obs", ".rtk", ".mrk", ".nav"
    };

    private static readonly HashSet<string> RtcmSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        ".dat"
    };

    /// <summary>
    /// 解析文件类型。后缀为空时退化为按文件名整个匹配，仍无法判定则返回 <see cref="MediaFileTypeEnum.Unknown"/>。
    /// </summary>
    public static MediaFileTypeEnum Resolve(string fileNameOrSuffix)
    {
        if (string.IsNullOrWhiteSpace(fileNameOrSuffix)) return MediaFileTypeEnum.Unknown;

        var suffix = fileNameOrSuffix.Trim();
        var dot = suffix.LastIndexOf('.');
        // 传进来的可能只是后缀（.mp4），也可能是完整文件名（DJI_0001.MP4）
        suffix = dot >= 0 ? suffix[dot..] : "." + suffix;

        if (ImageSuffixes.Contains(suffix)) return MediaFileTypeEnum.Image;
        if (VideoSuffixes.Contains(suffix)) return MediaFileTypeEnum.Video;
        if (PpkSuffixes.Contains(suffix)) return MediaFileTypeEnum.Ppk;
        if (RtcmSuffixes.Contains(suffix)) return MediaFileTypeEnum.Rtcm;
        return MediaFileTypeEnum.Unknown;
    }

    /// <summary>该类型是否可在浏览器内直接预览（图片/视频）</summary>
    public static bool IsPreviewable(MediaFileTypeEnum type)
        => type is MediaFileTypeEnum.Image or MediaFileTypeEnum.Video;
}
