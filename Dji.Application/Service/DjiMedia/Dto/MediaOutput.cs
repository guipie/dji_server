// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
//
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Core.Enum.DjiEnum.Media;

namespace Dji.Application.Service.DjiMedia.Dto;

/// <summary>媒体列表项</summary>
public class DjiMediaOutput
{
    public long Id { get; set; }
    public string WorkspaceId { get; set; }

    /// <summary>对象存储 Key（唯一标识，前端去重用）</summary>
    public string ObjectKey { get; set; }

    /// <summary>文件名</summary>
    public string FileName { get; set; }

    /// <summary>文件后缀</summary>
    public string Suffix { get; set; }

    /// <summary>媒体类型</summary>
    public MediaFileTypeEnum FileType { get; set; }

    /// <summary>媒体类型可读名</summary>
    public string FileTypeName { get; set; }

    /// <summary>是否可预览（图片 / 视频）</summary>
    public bool Previewable { get; set; }

    /// <summary>文件大小（字节）</summary>
    public long FileSize { get; set; }

    /// <summary>文件大小可读文本（如 12.3 MB）</summary>
    public string SizeText { get; set; }

    /// <summary>访问地址</summary>
    public string Url { get; set; }

    /// <summary>回传机场 SN</summary>
    public string DockSn { get; set; }

    /// <summary>机场别名</summary>
    public string DockNick { get; set; }

    /// <summary>拍摄飞行器 SN</summary>
    public string DroneSn { get; set; }

    /// <summary>所属任务 ID（协议 flight_id）</summary>
    public string FlightId { get; set; }

    /// <summary>所属任务名称</summary>
    public string FlightJobName { get; set; }

    /// <summary>是否原图</summary>
    public bool IsOriginal { get; set; }

    /// <summary>文件组 ID</summary>
    public string FileGroupId { get; set; }

    /// <summary>拍摄时间</summary>
    public DateTime? MediaCreateTime { get; set; }

    /// <summary>拍摄经度（WGS84）</summary>
    public double? Longitude { get; set; }

    /// <summary>拍摄纬度（WGS84）</summary>
    public double? Latitude { get; set; }

    /// <summary>绝对高度（米）</summary>
    public double? AbsoluteAltitude { get; set; }

    /// <summary>相对高度（米）</summary>
    public double? RelativeAltitude { get; set; }

    /// <summary>云台偏航角（度）</summary>
    public double? GimbalYawDegree { get; set; }

    /// <summary>回传时间</summary>
    public DateTime? CreateTime { get; set; }
}

/// <summary>媒体详情（含任务与机场上下文）</summary>
public class DjiMediaDetailOutput : DjiMediaOutput
{
    /// <summary>对象存储桶</summary>
    public string Bucket { get; set; }

    /// <summary>业务路径</summary>
    public string BizPath { get; set; }

    /// <summary>飞行器型号枚举值</summary>
    public string DroneModelKey { get; set; }

    /// <summary>负载型号枚举值</summary>
    public string PayloadModelKey { get; set; }

    /// <summary>本地任务主键</summary>
    public long? TaskId { get; set; }

    /// <summary>回传报文时间戳（毫秒）</summary>
    public long ReportTimestamp { get; set; }
}

/// <summary>媒体类型统计</summary>
public class MediaTypeStatOutput
{
    /// <summary>媒体类型</summary>
    public MediaFileTypeEnum FileType { get; set; }

    /// <summary>类型可读名</summary>
    public string FileTypeName { get; set; }

    /// <summary>数量</summary>
    public int Count { get; set; }

    /// <summary>总大小（字节）</summary>
    public long TotalSize { get; set; }
}

/// <summary>媒体总览统计</summary>
public class MediaStatsOutput
{
    /// <summary>媒体总数</summary>
    public int TotalCount { get; set; }

    /// <summary>媒体总大小（字节）</summary>
    public long TotalSize { get; set; }

    /// <summary>图片数量</summary>
    public int ImageCount { get; set; }

    /// <summary>视频数量</summary>
    public int VideoCount { get; set; }

    /// <summary>分类型统计</summary>
    public List<MediaTypeStatOutput> Types { get; set; } = [];
}

/// <summary>有媒体的任务（前端筛选下拉用）</summary>
public class MediaTaskOptionOutput
{
    /// <summary>协议 flight_id</summary>
    public string FlightId { get; set; }

    /// <summary>任务名称</summary>
    public string JobName { get; set; }

    /// <summary>机场 SN</summary>
    public string DockSn { get; set; }

    /// <summary>媒体数量</summary>
    public int MediaCount { get; set; }

    /// <summary>展示名</summary>
    public string Label { get; set; }
}

/// <summary>媒体类型字典项</summary>
public class MediaTypeOptionOutput
{
    public int Value { get; set; }
    public string Label { get; set; }
}

/// <summary>
/// 机场下拉项（媒体筛选用）。
/// </summary>
/// <remarks>
/// 只列出 <c>Domain = Dock</c> 的设备；带上 <see cref="MediaCount"/> 是为了让用户
/// 在筛选前就知道「哪些机场真的回传过媒体」，避免点进空列表反复排查。
/// </remarks>
public class MediaDockOptionOutput
{
    /// <summary>机场 SN</summary>
    public string Sn { get; set; }

    /// <summary>机场别名</summary>
    public string Nick { get; set; }

    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>是否在线</summary>
    public bool IsOnline { get; set; }

    /// <summary>该机场已回传的媒体数量</summary>
    public int MediaCount { get; set; }

    /// <summary>展示名（别名优先）</summary>
    public string Label { get; set; }
}
