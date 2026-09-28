// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Core.Enum.DjiEnum.Live;

namespace Dji.Application.Service.DjiLive.Dto;

/// <summary>
/// 一路可直播的通道。
/// </summary>
/// <remarks>
/// 由设备上报的 <c>live_capacity</c> 三层树（设备 → 相机 → 码流）拍平而来，
/// 并叠加 <c>live_status</c> 的实时在播信息。前端用它渲染通道选择器。
/// </remarks>
public class LiveChannelOutput
{
    /// <summary>视频源设备 SN（飞行器）</summary>
    public string Sn { get; set; }

    /// <summary>相机索引，格式 <c>{type-subtype-gimbalindex}</c></summary>
    public string CameraIndex { get; set; }

    /// <summary>码流索引（如 normal-0 / zoom-0）</summary>
    public string VideoIndex { get; set; }

    /// <summary>完整码流标识 <c>{sn}/{camera_index}/{video_index}</c>，开播时必须原样回传</summary>
    public string VideoId { get; set; }

    /// <summary>码流类型</summary>
    public string VideoType { get; set; }

    /// <summary>该码流支持切换的镜头类型</summary>
    public List<string> SwitchableVideoTypes { get; set; } = [];

    /// <summary>当前是否正在推流（来自 <c>live_status</c>）</summary>
    public bool IsLive { get; set; }

    /// <summary>当前清晰度</summary>
    public int VideoQuality { get; set; }

    /// <summary>设备侧错误码（非 0 表示该路流有异常）</summary>
    public int ErrorStatus { get; set; }

    /// <summary>关联的本地会话主键（在播时用于停播 / 调参）</summary>
    public long? SessionId { get; set; }

    /// <summary>可读的通道名称，便于前端直接展示</summary>
    public string ChannelName { get; set; }
}

/// <summary>
/// 机场直播状态（页面主视图）。
/// </summary>
public class LiveDockStateOutput
{
    /// <summary>机场 SN</summary>
    public string DockSn { get; set; }

    /// <summary>机场别名</summary>
    public string DockNick { get; set; }

    /// <summary>机场是否在线</summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// 服务端是否已配置直播（<c>Dji.Live.Enabled</c> 且推流地址非空）。
    /// </summary>
    /// <remarks>
    /// 前端据此提前禁用开播按钮，避免用户点了才被拒绝 —— 未配置时下发无效地址只会让机场报错。
    /// </remarks>
    public bool LiveEnabled { get; set; }

    /// <summary>未启用直播时的提示文案</summary>
    public string DisabledReason { get; set; }

    /// <summary>设备是否已上报直播能力；未上报时无法开播</summary>
    public bool HasCapacity { get; set; }

    /// <summary>可选择的码流总数</summary>
    public int AvailableVideoNumber { get; set; }

    /// <summary>可同时推流的最大路数</summary>
    public int CoexistVideoNumberMax { get; set; }

    /// <summary>当前在播路数</summary>
    public int LiveCount { get; set; }

    /// <summary>待上传媒体数（<c>media_file_detail.remain_upload</c>）</summary>
    public int RemainUpload { get; set; }

    /// <summary>可直播通道列表</summary>
    public List<LiveChannelOutput> Channels { get; set; } = [];

    /// <summary>快照时间（设备上报时间）</summary>
    public DateTime? UpdateTime { get; set; }
}

/// <summary>直播会话输出</summary>
public class LiveSessionOutput
{
    public long Id { get; set; }
    public string WorkspaceId { get; set; }
    public string DockSn { get; set; }

    /// <summary>机场别名（列表展示用）</summary>
    public string DockNick { get; set; }

    public string VideoSn { get; set; }
    public string VideoId { get; set; }
    public string CameraIndex { get; set; }
    public string VideoIndex { get; set; }
    public string VideoType { get; set; }

    /// <summary>镜头类型可读名</summary>
    public string VideoTypeName { get; set; }

    public LiveUrlTypeEnum UrlType { get; set; }

    /// <summary>协议类型可读名</summary>
    public string UrlTypeName { get; set; }

    public LiveVideoQualityEnum VideoQuality { get; set; }

    /// <summary>清晰度可读名</summary>
    public string VideoQualityName { get; set; }

    public string StreamName { get; set; }

    /// <summary>推流地址（RTMP）</summary>
    public string PushUrl { get; set; }

    /// <summary>播放地址（HTTP-FLV，低延迟首选）</summary>
    public string PlayUrl { get; set; }

    /// <summary>播放地址（HLS，兜底）</summary>
    public string HlsUrl { get; set; }

    public LiveStreamStatusEnum Status { get; set; }

    /// <summary>会话状态可读名</summary>
    public string StatusName { get; set; }

    public int LastResult { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? StopTime { get; set; }
    public DateTime? LastActiveTime { get; set; }
    public string OperatorName { get; set; }
    public DateTime? CreateTime { get; set; }
}

/// <summary>可直播机场下拉项</summary>
public class LiveDockOptionOutput
{
    /// <summary>机场 SN</summary>
    public string Sn { get; set; }

    /// <summary>机场别名</summary>
    public string Nick { get; set; }

    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>是否在线</summary>
    public bool IsOnline { get; set; }

    /// <summary>是否有在途会话（用于在前端提示「该机场已有直播」）</summary>
    public bool HasActiveSession { get; set; }

    /// <summary>展示名（别名优先）</summary>
    public string Label { get; set; }
}

/// <summary>清晰度字典项</summary>
public class LiveQualityOption
{
    /// <summary>协议取值</summary>
    public int Value { get; set; }

    /// <summary>显示名</summary>
    public string Label { get; set; }
}

/// <summary>镜头字典项</summary>
public class LiveLensOption
{
    /// <summary>协议取值（normal/wide/zoom/ir）</summary>
    public string Value { get; set; }

    /// <summary>显示名</summary>
    public string Label { get; set; }
}

/// <summary>会话状态字典项</summary>
public class LiveStatusOption
{
    public int Value { get; set; }
    public string Label { get; set; }
}
