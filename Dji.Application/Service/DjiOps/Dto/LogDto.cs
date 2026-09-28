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

/// <summary>
/// 列举设备可上传日志的入参。
/// </summary>
/// <remarks>
/// 列举是「按模块问设备」的：机场日志与飞行器日志分属不同模块，设备只会返回被问到的模块。
/// 不传 <see cref="Modules"/> 时两个模块都问 —— 运维通常并不关心日志到底存在哪一侧。
/// </remarks>
public class LogListInput
{
    /// <summary>机场 SN</summary>
    [Required(ErrorMessage = "机场 SN 不能为空")]
    public string DockSn { get; set; }

    /// <summary>要列举的模块（0 飞行器 / 3 机场），留空表示全部</summary>
    public List<LogModuleEnum> Modules { get; set; }
}

/// <summary>日志文件查询</summary>
public class LogFileSearchInput : BasePageInput
{
    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>机场 SN</summary>
    public string DockSn { get; set; }

    /// <summary>设备 SN</summary>
    public string DeviceSn { get; set; }

    /// <summary>所属模块</summary>
    public LogModuleEnum? Module { get; set; }

    /// <summary>上传状态</summary>
    public LogUploadStatusEnum? Status { get; set; }

    /// <summary>文件名（模糊）</summary>
    public string Keyword { get; set; }

    /// <summary>日志产生时间起点</summary>
    public DateTime? StartTime { get; set; }

    /// <summary>日志产生时间终点</summary>
    public DateTime? EndTime { get; set; }
}

/// <summary>发起日志上传的入参</summary>
public class LogUploadStartInput
{
    /// <summary>机场 SN</summary>
    [Required(ErrorMessage = "机场 SN 不能为空")]
    public string DockSn { get; set; }

    /// <summary>要上传的日志记录主键集合</summary>
    [Required(ErrorMessage = "请至少选择一条日志")]
    public List<long> Ids { get; set; }
}

/// <summary>取消日志上传的入参</summary>
public class LogCancelInput
{
    /// <summary>机场 SN</summary>
    [Required(ErrorMessage = "机场 SN 不能为空")]
    public string DockSn { get; set; }

    /// <summary>要取消的模块（协议只支持按模块取消，不支持按文件取消）</summary>
    [Required(ErrorMessage = "请至少选择一个模块")]
    public List<LogModuleEnum> Modules { get; set; }
}

/// <summary>日志记录批量删除入参</summary>
public class LogIdsInput
{
    /// <summary>主键集合</summary>
    [Required(ErrorMessage = "请选择要删除的记录")]
    public List<long> Ids { get; set; }
}

/// <summary>日志文件输出</summary>
public class LogFileOutput
{
    /// <summary>主键</summary>
    public long Id { get; set; }

    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>机场 SN</summary>
    public string DockSn { get; set; }

    /// <summary>机场昵称</summary>
    public string DockNick { get; set; }

    /// <summary>设备 SN</summary>
    public string DeviceSn { get; set; }

    /// <summary>设备名称</summary>
    public string DeviceName { get; set; }

    /// <summary>所属模块</summary>
    public LogModuleEnum Module { get; set; }

    /// <summary>所属模块名称</summary>
    public string ModuleName { get; set; }

    /// <summary>日志索引</summary>
    public int BootIndex { get; set; }

    /// <summary>日志开始时间</summary>
    public DateTime? BeginTime { get; set; }

    /// <summary>日志结束时间</summary>
    public DateTime? EndTime { get; set; }

    /// <summary>文件大小（字节）</summary>
    public long? FileSize { get; set; }

    /// <summary>文件大小文本</summary>
    public string FileSizeText { get; set; }

    /// <summary>上传状态</summary>
    public LogUploadStatusEnum Status { get; set; }

    /// <summary>上传状态名称</summary>
    public string StatusName { get; set; }

    /// <summary>是否仍在进行中</summary>
    public bool IsRunning { get; set; }

    /// <summary>进度百分比</summary>
    public int? Progress { get; set; }

    /// <summary>上传速率（字节/秒）</summary>
    public long? UploadRate { get; set; }

    /// <summary>对象存储 Key</summary>
    public string ObjectKey { get; set; }

    /// <summary>文件指纹</summary>
    public string Fingerprint { get; set; }

    /// <summary>文件名</summary>
    public string FileName { get; set; }

    /// <summary>对外访问地址（上传成功后才有值）</summary>
    public string Url { get; set; }

    /// <summary>错误信息</summary>
    public string ErrorMessage { get; set; }

    /// <summary>发起人</summary>
    public string OperatorName { get; set; }

    /// <summary>列举入库时间</summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>上传完成时间</summary>
    public DateTime? FinishTime { get; set; }
}

/// <summary>日志统计（概览卡片）</summary>
public class LogStatsOutput
{
    /// <summary>记录总数</summary>
    public int Total { get; set; }

    /// <summary>待上传数</summary>
    public int PendingCount { get; set; }

    /// <summary>上传中数</summary>
    public int UploadingCount { get; set; }

    /// <summary>已上传数</summary>
    public int UploadedCount { get; set; }

    /// <summary>失败数</summary>
    public int FailedCount { get; set; }

    /// <summary>全部日志总大小（字节）</summary>
    public long TotalSize { get; set; }

    /// <summary>已上传大小（字节）</summary>
    public long UploadedSize { get; set; }
}

/// <summary>日志统计查询</summary>
public class LogStatsInput
{
    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>机场 SN</summary>
    public string DockSn { get; set; }
}
