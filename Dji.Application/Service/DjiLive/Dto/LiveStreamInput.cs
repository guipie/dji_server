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

/// <summary>按机场查询直播通道</summary>
public class LiveChannelInput
{
    /// <summary>机场 SN</summary>
    [Required(ErrorMessage = "机场SN不能为空")]
    public string DockSn { get; set; }
}

/// <summary>
/// 开始直播输入。
/// </summary>
/// <remarks>
/// <b><see cref="VideoId"/> 不接受前端拼字符串</b>：该值必须来自设备上报的
/// <c>live_capacity</c> 通道树，否则机场侧找不到对应码流会直接拒绝。
/// 因此前端流程固定为「先拉通道列表 → 用户选一路 → 开播」。
/// </remarks>
public class StartLiveInput
{
    /// <summary>机场 SN</summary>
    [Required(ErrorMessage = "机场SN不能为空")]
    public string DockSn { get; set; }

    /// <summary>直播码流标识，格式 <c>{sn}/{camera_index}/{video_index}</c></summary>
    [Required(ErrorMessage = "请选择直播通道")]
    public string VideoId { get; set; }

    /// <summary>清晰度：0 自适应 / 1 流畅 / 2 标清 / 3 高清 / 4 超清</summary>
    public LiveVideoQualityEnum VideoQuality { get; set; } = LiveVideoQualityEnum.Adaptive;
}

/// <summary>直播会话操作输入（停播 / 调清晰度 / 切镜头 / 切 FPV）</summary>
public class LiveControlInput : BaseIdInput
{
}

/// <summary>设置清晰度输入</summary>
public class SetLiveQualityInput : BaseIdInput
{
    /// <summary>清晰度：0 自适应 / 1 流畅 / 2 标清 / 3 高清 / 4 超清</summary>
    public LiveVideoQualityEnum VideoQuality { get; set; }
}

/// <summary>
/// 切换镜头输入。
/// </summary>
/// <remarks>
/// 只能切到该码流 <c>switchable_video_types</c> 里声明支持的类型，
/// 越界会被机场拒绝，因此服务层会先做校验再下发。
/// </remarks>
public class SetLiveLensInput : BaseIdInput
{
    /// <summary>镜头类型：normal 默认 / wide 广角 / zoom 变焦 / ir 红外</summary>
    [Required(ErrorMessage = "请选择镜头类型")]
    public string VideoType { get; set; }
}

/// <summary>切换 FPV 相机位置输入</summary>
public class SetLiveCameraInput : BaseIdInput
{
    /// <summary>FPV 位置：0 舱内 / 1 舱外</summary>
    public int CameraPosition { get; set; }
}

/// <summary>直播会话分页查询输入</summary>
public class LiveSessionSearchInput : BasePageInput
{
    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>机场 SN</summary>
    public string DockSn { get; set; }

    /// <summary>会话状态</summary>
    public LiveStreamStatusEnum? Status { get; set; }

    /// <summary>仅看进行中的会话</summary>
    public bool? OnlyActive { get; set; }
}

/// <summary>可直播机场下拉输入</summary>
public class LiveDockOptionInput
{
    /// <summary>工作空间（留空返回全部）</summary>
    public string WorkspaceId { get; set; }
}
