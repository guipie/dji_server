// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

namespace Dji.Application.Cloud.Dto.Live;

#region 下行（云 → 机场，thing/product/{sn}/services）

/// <summary>
/// 开始直播（<c>live_start_push</c>）。
/// </summary>
/// <remarks>
/// <para>
/// 这是「云端推流方案」的核心指令：云端把<b>流媒体服务器地址</b>下发给机场，机场把飞行器码流
/// 推到该地址，浏览器再从流媒体服务器拉流。云端全程不碰码流，只做信令。
/// </para>
/// <para>
/// 本项目使用自建 SRS + RTMP，因此 <see cref="UrlType"/> 固定为 1，
/// <see cref="Url"/> 形如 <c>rtmp://host:1935/live/streamName</c>。
/// </para>
/// <para>
/// <b>注意 <c>video_id</c> 是必填</b>：机场要靠它定位「哪台设备的哪个相机的哪路码流」，
/// 该值来自物模型 <c>live_capacity.device_list[].camera_list[].video_list[]</c>，
/// 因此前端必须先拉取直播能力再开播，不能凭约定拼字符串。
/// </para>
/// </remarks>
public class LiveStartPushInput
{
    /// <summary>直播协议类型：0 声网 / 1 RTMP / 3 GB28181 / 4 WebRTC（值必须与协议一致，无 2）</summary>
    public int UrlType { get; set; }

    /// <summary>直播参数（RTMP 时为 <c>rtmp://host:port/app/stream</c>）</summary>
    public string Url { get; set; }

    /// <summary>直播码流标识，格式 <c>{sn}/{camera_index}/{video_index}</c></summary>
    public string VideoId { get; set; }

    /// <summary>直播质量：0 自适应 / 1 流畅 / 2 标清 / 3 高清 / 4 超清</summary>
    public int VideoQuality { get; set; }
}

/// <summary>停止直播（<c>live_stop_push</c>）</summary>
public class LiveStopPushInput
{
    /// <summary>直播码流标识，必须与开播时下发的完全一致</summary>
    public string VideoId { get; set; }
}

/// <summary>设置直播清晰度（<c>live_set_quality</c>）</summary>
public class LiveSetQualityInput
{
    /// <summary>直播码流标识</summary>
    public string VideoId { get; set; }

    /// <summary>直播质量：0 自适应 / 1 流畅 / 2 标清 / 3 高清 / 4 超清</summary>
    public int VideoQuality { get; set; }
}

/// <summary>
/// 切换直播镜头（<c>live_lens_change</c>）。
/// </summary>
/// <remarks>
/// 与「换一路流」（<c>live_stop_push</c> + <c>live_start_push</c>）的区别：
/// 镜头切换是<b>在一路流内</b>换镜头，不断流、地址不变，因此体验更平滑。
/// 但并非所有码流都支持 —— 需先查 <c>live_capacity</c> 里该码流的
/// <c>switchable_video_types</c>，只允许切换到其中的类型，否则机场会拒绝。
/// </remarks>
public class LiveLensChangeInput
{
    /// <summary>镜头类型：normal 默认 / wide 广角 / zoom 变焦 / ir 红外</summary>
    public string VideoType { get; set; }
}

/// <summary>
/// 切换 FPV 相机位置（<c>live_camera_change</c>）。
/// </summary>
/// <remarks>仅机场 2/3 支持舱内 FPV 时有效，座舱摄像头与飞行器摄像头之间的切换。</remarks>
public class LiveCameraChangeInput
{
    /// <summary>直播码流标识</summary>
    public string VideoId { get; set; }

    /// <summary>FPV 位置：0 舱内 / 1 舱外</summary>
    public int CameraPosition { get; set; }
}

#endregion

#region 上行（机场 → 云，thing/product/{sn}/state）

/// <summary>
/// 机场物模型状态（<c>state</c> 主题）中本平台关心的部分。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么用「局部 DTO」而不是完整状态结构</b>：<c>state</c> 报文包含几十个字段
/// （累计作业次数、维保状态、图传链路、网络状态……），每个还可能各自变化；
/// 本平台只用到直播能力、直播状态与媒体待上传数量，声明这三项即可让反序列化忽略其余字段，
/// 既避免维护一整份状态模型，也避免设备新增字段时（前向兼容）出问题。
/// </para>
/// <para>
/// <b>关键：机场的 <c>state</c> 是「分多条推送」的</b>（官方「设备属性推送」章节明确说明）。
/// 同一次状态变化可能拆成「只有 live_capacity 的一条」「只有 live_status 的一条」，
/// 因此处理时必须<b>逐字段判空后局部更新</b>，绝不能用一条报文的完整 DTO 覆盖整行，
/// 否则会把上一次拿到的 live_capacity 冲成 null。
/// </para>
/// </remarks>
public class DockStateData
{
    /// <summary>网关设备直播能力（仅状态变化时推送）</summary>
    public LiveCapacityPayload LiveCapacity { get; set; }

    /// <summary>网关当前整体直播状态推送（仅状态变化时推送）</summary>
    public List<LiveStatusItem> LiveStatus { get; set; }

    /// <summary>媒体文件上传细节（含待上传数量）</summary>
    public MediaFileDetailPayload MediaFileDetail { get; set; }
}

/// <summary>
/// 直播能力树（<c>live_capacity</c>）。
/// </summary>
/// <remarks>
/// 三层结构：设备（飞行器）→ 相机 → 码流。
/// <c>available_video_number</c> 是「可选几路」，<c>coexist_video_number_max</c> 是「能同时推几路」——
/// 后者才是开播前的并发校验依据，两者容易混用。
/// </remarks>
public class LiveCapacityPayload
{
    /// <summary>可选择推流的码流数量</summary>
    public int AvailableVideoNumber { get; set; }

    /// <summary>可同时推流的最大码流数量</summary>
    public int CoexistVideoNumberMax { get; set; }

    /// <summary>可选择的视频设备源（设备层，如飞行器）</summary>
    public List<LiveCapacityDevice> DeviceList { get; set; }
}

/// <summary>直播能力：设备层</summary>
public class LiveCapacityDevice
{
    /// <summary>飞行器等视频源设备序列号</summary>
    public string Sn { get; set; }

    /// <summary>该设备可被选择推流的码流数</summary>
    public int AvailableVideoNumber { get; set; }

    /// <summary>该设备可同时被推流的码流数</summary>
    public int CoexistVideoNumberMax { get; set; }

    /// <summary>该设备上的相机列表</summary>
    public List<LiveCapacityCamera> CameraList { get; set; }
}

/// <summary>直播能力：相机层</summary>
public class LiveCapacityCamera
{
    /// <summary>相机索引，格式 <c>{type-subtype-gimbalindex}</c></summary>
    public string CameraIndex { get; set; }

    /// <summary>该相机级别的视频源可被选择推流的码流数</summary>
    public int AvailableVideoNumber { get; set; }

    /// <summary>该相机级别的视频源可同时被推流的码流数</summary>
    public int CoexistVideoNumberMax { get; set; }

    /// <summary>该相机可选择的码流列表</summary>
    public List<LiveCapacityVideo> VideoList { get; set; }
}

/// <summary>直播能力：码流层</summary>
public class LiveCapacityVideo
{
    /// <summary>码流索引（如 normal-0 / zoom-0）</summary>
    public string VideoIndex { get; set; }

    /// <summary>码流类型</summary>
    public string VideoType { get; set; }

    /// <summary>该码流支持切换的镜头类型列表，<c>live_lens_change</c> 只能切到其中的值</summary>
    public List<string> SwitchableVideoTypes { get; set; }
}

/// <summary>单路直播状态（<c>live_status</c> 数组元素）</summary>
public class LiveStatusItem
{
    /// <summary>直播码流标识符，格式 <c>{sn}/{camera_index}/{video_index}</c></summary>
    public string VideoId { get; set; }

    /// <summary>视频镜头类型（normal/wide/zoom/infrared 等）</summary>
    public string VideoType { get; set; }

    /// <summary>直播码流质量：0 自适应 / 1 流畅 / 2 标清 / 3 高清 / 4 超清</summary>
    public int VideoQuality { get; set; }

    /// <summary>直播状态：0 未直播 / 1 在直播</summary>
    public int Status { get; set; }

    /// <summary>错误码</summary>
    public int ErrorStatus { get; set; }
}

/// <summary>媒体文件上传细节（<c>media_file_detail</c>）</summary>
public class MediaFileDetailPayload
{
    /// <summary>待上传数量</summary>
    public int RemainUpload { get; set; }
}

#endregion
