// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Core.Enum.DjiEnum.Ops;

namespace Dji.Application.Service.DjiOps.Dto;

#region 自定义飞行区文件

/// <summary>自定义飞行区文件查询</summary>
public class FlightAreaSearchInput : BasePageInput
{
    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>机场 SN</summary>
    public string DockSn { get; set; }

    /// <summary>设备侧同步状态</summary>
    public FlightAreaSyncStatusEnum? SyncStatus { get; set; }

    /// <summary>是否只查启用中的版本</summary>
    public bool? OnlyActive { get; set; }

    /// <summary>文件名（模糊）</summary>
    public string Keyword { get; set; }
}

/// <summary>
/// 登记自定义飞行区文件的入参。
/// </summary>
/// <remarks>
/// <para>
/// <b>文件的实际上传走平台既有的上传接口</b>（对象存储直传），本接口只负责「把某个已存在的对象
/// 登记为该机场的飞行区文件」。这样做的原因是飞行区文件必须是<b>合法 JSON 且符合大疆围栏格式</b>，
/// 由平台生成不如让业务方用自己的工具产出后上传。
/// </para>
/// <para>
/// <c>checksum</c> <b>默认由服务端从桶里的真实内容计算</b>，不接受人工填写作为首选值 ——
/// 原因见 <c>DjiStorageService.ComputeFileDigestAsync</c> 的注释：摘要一旦填错，
/// 设备永远同步不上、且两侧日志都不报错。
/// </para>
/// </remarks>
public class FlightAreaRegisterInput
{
    /// <summary>机场 SN</summary>
    [Required(ErrorMessage = "机场 SN 不能为空")]
    public string DockSn { get; set; }

    /// <summary>文件名（如 <c>geofence_xxx.json</c>）</summary>
    [Required(ErrorMessage = "文件名不能为空")]
    public string FileName { get; set; }

    /// <summary>对象存储 Key 或完整 URL（两种都能识别）</summary>
    [Required(ErrorMessage = "对象存储地址不能为空")]
    public string ObjectKey { get; set; }

    /// <summary>
    /// 兜底用的 SHA256 摘要（仅当服务端无法读取对象内容时才使用，例如对象存储未启用）。
    /// </summary>
    /// <remarks>正常情况下留空即可；填了也不会被优先采用，仅在服务端算不出来时生效。</remarks>
    public string Checksum { get; set; }

    /// <summary>登记后是否立即下发 <c>flight_areas_update</c> 通知设备来同步</summary>
    public bool SyncImmediately { get; set; } = true;

    /// <summary>是否已确认（下发指令属于对设备的实际操作，需二次确认）</summary>
    public bool Confirm { get; set; }
}

/// <summary>通知设备同步飞行区（下发 <c>flight_areas_update</c>）的入参</summary>
public class FlightAreaUpdateInput
{
    /// <summary>机场 SN</summary>
    [Required(ErrorMessage = "机场 SN 不能为空")]
    public string DockSn { get; set; }

    /// <summary>是否已确认</summary>
    /// <remarks>
    /// 该指令会让设备<b>重新加载作业区域</b>，过程中飞行器需要开机、图传需要让出链路，
    /// 属于会影响现场作业的操作，因此必须二次确认。
    /// </remarks>
    public bool Confirm { get; set; }
}

/// <summary>飞行区文件批量删除入参</summary>
public class FlightAreaIdsInput
{
    /// <summary>主键集合</summary>
    [Required(ErrorMessage = "请选择要删除的记录")]
    public List<long> Ids { get; set; }
}

/// <summary>飞行区文件输出</summary>
public class FlightAreaOutput
{
    /// <summary>主键</summary>
    public long Id { get; set; }

    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>机场 SN</summary>
    public string DockSn { get; set; }

    /// <summary>机场昵称</summary>
    public string DockNick { get; set; }

    /// <summary>文件名</summary>
    public string FileName { get; set; }

    /// <summary>对象存储 Key</summary>
    public string ObjectKey { get; set; }

    /// <summary>文件大小（字节）</summary>
    public long? FileSize { get; set; }

    /// <summary>文件大小文本</summary>
    public string FileSizeText { get; set; }

    /// <summary>SHA256 摘要</summary>
    public string Checksum { get; set; }

    /// <summary>设备侧同步状态</summary>
    public FlightAreaSyncStatusEnum SyncStatus { get; set; }

    /// <summary>同步状态名称</summary>
    public string SyncStatusName { get; set; }

    /// <summary>同步失败原因码</summary>
    public FlightAreaSyncReasonEnum SyncReason { get; set; }

    /// <summary>同步失败原因说明</summary>
    public string SyncReasonName { get; set; }

    /// <summary>是否为设备当前启用中的版本</summary>
    public bool IsActive { get; set; }

    /// <summary>最近一次同步状态更新时刻</summary>
    public DateTime? LastTime { get; set; }

    /// <summary>登记时间</summary>
    public DateTime? CreateTime { get; set; }
}

#endregion

#region 飞行区距离快照

/// <summary>飞行器与飞行区边界距离快照输出</summary>
public class FlightAreaLocationOutput
{
    /// <summary>区域唯一 ID</summary>
    public string AreaId { get; set; }

    /// <summary>距边界距离（米）</summary>
    public double? AreaDistance { get; set; }

    /// <summary>是否在区域内</summary>
    public bool IsInArea { get; set; }

    /// <summary>历史最近距离（米）</summary>
    public double? MinDistance { get; set; }

    /// <summary>累计进入次数</summary>
    public int EnterCount { get; set; }

    /// <summary>最近一次进入时刻</summary>
    public DateTime? LastEnterTime { get; set; }

    /// <summary>最近一次离开时刻</summary>
    public DateTime? LastExitTime { get; set; }

    /// <summary>最近刷新时刻</summary>
    public DateTime? LastTime { get; set; }
}

#endregion
