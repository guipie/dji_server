// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Core.Enum.DjiEnum.Live;

namespace Dji.Core.Entity.DjiEntity;

/// <summary>
/// 直播会话（一次「开播 → 停播」为一个会话）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么要建会话表而不是只存「当前是否在播」</b>：直播地址是按会话动态生成的
/// （SRS 的 stream name 带时间戳，避免上一路流的残留导致播放器拿到旧画面），
/// 且排障时需要知道「谁在什么时候推了哪路流、用什么清晰度、为什么停了」。
/// </para>
/// <para>
/// <b>真值归属</b>：设备侧的播放状态以 OSD <c>live_status</c> 为准，
/// 本表的 <see cref="Status"/> 是平台视角的会话生命周期，两者在
/// <c>LiveStreamStatusEnum</c> 的注释里有说明。
/// </para>
/// <para>
/// <b>同一通道互斥</b>：同一个 <see cref="VideoId"/> 同时只允许一个进行中的会话；
/// 同一机场的并发路数受 <c>live_capacity.coexist_video_number_max</c> 约束，
/// 这两条都在服务层做前置校验，避免机场直接拒绝。
/// </para>
/// </remarks>
[SugarTable(null, "直播会话")]
[SugarIndex("index_DjiLiveStream_VideoId", nameof(VideoId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[SugarIndex("index_DjiLiveStream_DockSn", nameof(DockSn), OrderByType.Asc, nameof(CreateTime), OrderByType.Desc)]
public class DjiLiveStream : EntityWorkspaceBase
{
    /// <summary>回传 / 推流机场 SN（协议 <c>gateway_sn</c>）</summary>
    [SugarColumn(ColumnDescription = "机场SN", Length = 64, IsNullable = true)]
    public string DockSn { get; set; }

    /// <summary>视频源设备 SN（飞行器），取自 <see cref="VideoId"/> 的首段</summary>
    [SugarColumn(ColumnDescription = "视频源SN", Length = 64, IsNullable = true)]
    public string VideoSn { get; set; }

    /// <summary>
    /// 直播码流标识符，格式 <c>{sn}/{camera_index}/{video_index}</c>。
    /// </summary>
    /// <remarks>这是协议里唯一能定位「哪一路流」的标识，停播 / 调清晰度都必须回传它。</remarks>
    [SugarColumn(ColumnDescription = "码流标识", Length = 128, IsNullable = true)]
    public string VideoId { get; set; }

    /// <summary>相机索引，格式 <c>{type-subtype-gimbalindex}</c>，如 39-0-7</summary>
    [SugarColumn(ColumnDescription = "相机索引", Length = 64, IsNullable = true)]
    public string CameraIndex { get; set; }

    /// <summary>码流索引，如 normal-0 / zoom-0</summary>
    [SugarColumn(ColumnDescription = "码流索引", Length = 64, IsNullable = true)]
    public string VideoIndex { get; set; }

    /// <summary>当前镜头类型（normal/wide/zoom/ir）</summary>
    [SugarColumn(ColumnDescription = "镜头类型", Length = 32, IsNullable = true)]
    public string VideoType { get; set; }

    /// <summary>推流协议类型（本项目固定 <see cref="LiveUrlTypeEnum.Rtmp"/>）</summary>
    [SugarColumn(ColumnDescription = "协议类型", IsNullable = true)]
    public LiveUrlTypeEnum UrlType { get; set; }

    /// <summary>清晰度</summary>
    [SugarColumn(ColumnDescription = "清晰度", IsNullable = true)]
    public LiveVideoQualityEnum VideoQuality { get; set; }

    /// <summary>SRS 流名（由机场 SN + 相机索引 + 码流索引 组合并做安全字符处理）</summary>
    [SugarColumn(ColumnDescription = "流名", Length = 128, IsNullable = true)]
    public string StreamName { get; set; }

    /// <summary>下发给机场的推流地址（RTMP，含鉴权参数）</summary>
    [SugarColumn(ColumnDescription = "推流地址", Length = 512, IsNullable = true)]
    public string PushUrl { get; set; }

    /// <summary>前端播放地址（HTTP-FLV，低延迟首选）</summary>
    [SugarColumn(ColumnDescription = "播放地址", Length = 512, IsNullable = true)]
    public string PlayUrl { get; set; }

    /// <summary>前端播放地址备用（HLS，HTTP-FLV 不可用时兜底）</summary>
    [SugarColumn(ColumnDescription = "HLS地址", Length = 512, IsNullable = true)]
    public string HlsUrl { get; set; }

    /// <summary>会话状态</summary>
    [SugarColumn(ColumnDescription = "会话状态", IsNullable = true)]
    public LiveStreamStatusEnum Status { get; set; }

    /// <summary>最近一次指令的返回码（0 表示设备接受）</summary>
    [SugarColumn(ColumnDescription = "最近返回码", IsNullable = true)]
    public int LastResult { get; set; }

    /// <summary>失败原因 / 设备回传的错误描述</summary>
    [SugarColumn(ColumnDescription = "失败原因", Length = 512, IsNullable = true)]
    public string ErrorMessage { get; set; }

    /// <summary>开播时间</summary>
    [SugarColumn(ColumnDescription = "开播时间", IsNullable = true)]
    public DateTime? StartTime { get; set; }

    /// <summary>停播时间</summary>
    [SugarColumn(ColumnDescription = "停播时间", IsNullable = true)]
    public DateTime? StopTime { get; set; }

    /// <summary>最近一次心跳 / 状态刷新时间（由 OSD live_status 驱动，用于识别僵死会话）</summary>
    [SugarColumn(ColumnDescription = "状态刷新时间", IsNullable = true)]
    public DateTime? LastActiveTime { get; set; }

    /// <summary>操作人</summary>
    [SugarColumn(ColumnDescription = "操作人", Length = 64, IsNullable = true)]
    public string OperatorName { get; set; }
}
