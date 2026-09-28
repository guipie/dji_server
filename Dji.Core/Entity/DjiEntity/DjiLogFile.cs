// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Core.Enum.DjiEnum.Ops;

namespace Dji.Core.Entity.DjiEntity;

/// <summary>
/// 设备日志文件（远程日志链路）。
/// </summary>
/// <remarks>
/// <para>
/// <b>三步链路</b>：<c>fileupload_list</c> 先向设备<b>列举</b>可上传的日志索引（<c>boot_index</c> 列表）
/// → 用户勾选后云端用 <c>fileupload_start</c> 把<b>对象存储凭证 + 待传索引</b>一并下发
/// → 设备直传对象存储，并通过 <c>fileupload_progress</c> 持续回报进度。
/// 因此本表的生命周期是「列举出来（待上传）→ 上传中 → 已上传」。
/// </para>
/// <para>
/// <b>为什么用 boot_index 而不是文件名做键</b>：日志文件在设备上按「开机轮次」滚动产生，
/// 文件名由设备自行拼装（含时间戳），只有 <c>boot_index</c> 在同一设备同一模块内稳定唯一。
/// </para>
/// <para>
/// <b>时间单位</b>：列举回包里的 <c>start_time</c> / <c>end_time</c> 单位是<b>秒</b>，
/// 而进度回包里的 <c>finish_time</c> 单位是<b>毫秒</b>，两者不能混用，因此分列存放。
/// </para>
/// </remarks>
[SugarTable(null, "设备日志文件")]
[SugarIndex("index_DjiLogFile_Unique", nameof(DockSn), OrderByType.Asc, nameof(DeviceSn), OrderByType.Asc,
    nameof(Module), OrderByType.Asc, nameof(BootIndex), OrderByType.Asc, true)]
[SugarIndex("index_DjiLogFile_DockTime", nameof(DockSn), OrderByType.Asc, nameof(BeginTime), OrderByType.Desc)]
public class DjiLogFile : EntityWorkspaceBase
{
    /// <summary>网关（机场）SN</summary>
    [SugarColumn(ColumnDescription = "机场SN", Length = 64, IsNullable = false)]
    public string DockSn { get; set; }

    /// <summary>产生该日志的设备 SN（可能是飞行器，也可能是机场）</summary>
    [SugarColumn(ColumnDescription = "设备SN", Length = 64, IsNullable = false)]
    public string DeviceSn { get; set; }

    /// <summary>日志所属模块：0 飞行器 / 3 机场</summary>
    [SugarColumn(ColumnDescription = "所属模块", IsNullable = true)]
    public LogModuleEnum Module { get; set; }

    /// <summary>日志索引（协议 <c>boot_index</c>，设备上按开机轮次编号）</summary>
    [SugarColumn(ColumnDescription = "日志索引", IsNullable = true)]
    public int BootIndex { get; set; }

    /// <summary>日志开始时间（协议 <c>start_time</c>，单位<b>秒</b>）</summary>
    [SugarColumn(ColumnDescription = "开始时间", IsNullable = true)]
    public DateTime? BeginTime { get; set; }

    /// <summary>日志结束时间（协议 <c>end_time</c>，单位<b>秒</b>）</summary>
    [SugarColumn(ColumnDescription = "结束时间", IsNullable = true)]
    public DateTime? EndTime { get; set; }

    /// <summary>文件大小（字节）</summary>
    [SugarColumn(ColumnDescription = "文件大小", IsNullable = true)]
    public long? FileSize { get; set; }

    /// <summary>上传状态</summary>
    [SugarColumn(ColumnDescription = "上传状态", IsNullable = true)]
    public LogUploadStatusEnum Status { get; set; }

    /// <summary>上传进度百分比（0~100）</summary>
    [SugarColumn(ColumnDescription = "进度百分比", IsNullable = true)]
    public int? Progress { get; set; }

    /// <summary>上传速率（字节/秒，协议 <c>upload_rate</c>）</summary>
    [SugarColumn(ColumnDescription = "上传速率", IsNullable = true)]
    public long? UploadRate { get; set; }

    /// <summary>对象存储 Key（上传开始后由云端分配，进度回包里回报）</summary>
    [SugarColumn(ColumnDescription = "对象存储Key", Length = 512, IsNullable = true)]
    public string ObjectKey { get; set; }

    /// <summary>文件指纹（协议 <c>fingerprint</c>，用于校验完整性）</summary>
    [SugarColumn(ColumnDescription = "文件指纹", Length = 128, IsNullable = true)]
    public string Fingerprint { get; set; }

    /// <summary>原始文件名（从对象存储 Key 末段解析）</summary>
    [SugarColumn(ColumnDescription = "文件名", Length = 256, IsNullable = true)]
    public string FileName { get; set; }

    /// <summary>对外访问地址（上传完成后按对象存储配置拼接）</summary>
    [SugarColumn(ColumnDescription = "访问地址", Length = 1024, IsNullable = true)]
    public string Url { get; set; }

    /// <summary>失败原因（协议 <c>progress.result</c> 非 0 时的说明）</summary>
    [SugarColumn(ColumnDescription = "错误信息", Length = 512, IsNullable = true)]
    public string ErrorMessage { get; set; }

    /// <summary>发起人账号</summary>
    [SugarColumn(ColumnDescription = "操作人", Length = 64, IsNullable = true)]
    public string OperatorName { get; set; }

    /// <summary>上传完成时刻（协议 <c>finish_time</c>，单位<b>毫秒</b>转本地时间）</summary>
    [SugarColumn(ColumnDescription = "完成时间", IsNullable = true)]
    public DateTime? FinishTime { get; set; }

    /// <summary>报文时间戳（毫秒，设备时钟）</summary>
    [SugarColumn(ColumnDescription = "上报时间戳", IsNullable = true)]
    public long ReportTimestamp { get; set; }
}
