// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

namespace Dji.Core.Enum.DjiEnum.Live;

/// <summary>
/// 直播推流协议类型。
/// </summary>
/// <remarks>
/// <b>取值必须与协议 <c>live_start_push.data.url_type</c> 的枚举整数逐字一致</b>
/// （注意没有 2 —— 官方预留了 RTSP 但当前文档未启用），改动会直接导致机场侧解析失败或推流协议不匹配。
/// 本项目采用<b>自建 SRS + RTMP</b>（见 <c>Dji.json</c> 的 <c>Live</c> 段）。
/// </remarks>
public enum LiveUrlTypeEnum
{
    /// <summary>声网 Agora（公有云，需自建 token 服务）</summary>
    [Description("声网Agora")]
    Agora = 0,

    /// <summary>RTMP（本项目采用）</summary>
    [Description("RTMP")]
    Rtmp = 1,

    /// <summary>GB28181（需已建 28181 下级网关）</summary>
    [Description("GB28181")]
    Gb28181 = 3,

    /// <summary>WebRTC（仅支持 WHIP 信令）</summary>
    [Description("WebRTC")]
    WebRtc = 4
}

/// <summary>
/// 直播清晰度。
/// </summary>
/// <remarks>
/// 取值与协议 <c>video_quality</c> 枚举整数一致。
/// 注意官方文档两处页面对「流畅」的描述不一致（一处 512Kbps、一处 1Mbps），
/// 这里只保留档位名称，把码率交给设备侧决定，避免在云端写死错误参数。
/// </remarks>
public enum LiveVideoQualityEnum
{
    /// <summary>自适应</summary>
    [Description("自适应")]
    Adaptive = 0,

    /// <summary>流畅 960*540</summary>
    [Description("流畅")]
    Smooth = 1,

    /// <summary>标清 1280*720 1Mbps</summary>
    [Description("标清")]
    Standard = 2,

    /// <summary>高清 1280*720 1.5Mbps</summary>
    [Description("高清")]
    Hd = 3,

    /// <summary>超清 1920*1080 3~8Mbps</summary>
    [Description("超清")]
    Ultra = 4
}

/// <summary>
/// 直播镜头类型。
/// </summary>
/// <remarks>
/// 取值与协议 <c>live_lens_change.data.video_type</c> 的字符串**逐字一致**，
/// 同时也在 <c>live_capacity.device_list[].camera_list[].video_list[].video_type</c>
/// 与 OSD <c>live_status[].video_type</c> 中出现 —— 三处共用同一套取值。
/// 注意协议另有 <c>infrared</c> 写法出现在 OSD 中，与 <c>ir</c> 指同一镜头，比对时需归一化。
/// </remarks>
public enum LiveLensTypeEnum
{
    /// <summary>默认镜头（可见光）</summary>
    [Description("默认")]
    Normal = 0,

    /// <summary>广角</summary>
    [Description("广角")]
    Wide = 1,

    /// <summary>变焦</summary>
    [Description("变焦")]
    Zoom = 2,

    /// <summary>红外</summary>
    [Description("红外")]
    Ir = 3
}

/// <summary>
/// 直播会话状态（平台内部态，协议无此字段）。
/// </summary>
/// <remarks>
/// 真值来源是设备上报的 OSD <c>live_status[].status</c>（0 未直播 / 1 在直播），
/// 但「指令已下发、设备尚未推流」这个中间态协议没覆盖，因此这里补一层平台态：
/// 只有 <see cref="Live"/> 表示设备确认在推流，避免前端在设备拒绝时误显示「直播中」。
/// </remarks>
public enum LiveStreamStatusEnum
{
    /// <summary>启动中（指令已下发，等待设备回包 / 开始推流）</summary>
    [Description("启动中")]
    Starting = 0,

    /// <summary>直播中（设备侧 live_status.status = 1）</summary>
    [Description("直播中")]
    Live = 1,

    /// <summary>已停止（用户主动停止或设备侧结束）</summary>
    [Description("已停止")]
    Stopped = 2,

    /// <summary>启动失败（设备拒绝或指令无应答）</summary>
    [Description("失败")]
    Failed = 3
}
