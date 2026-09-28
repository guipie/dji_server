// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Core.Enum.DjiEnum.Media;
using System.ComponentModel.DataAnnotations;

namespace Dji.Application.Service.DjiMedia.Dto;

/// <summary>媒体库分页查询输入</summary>
public class DjiMediaSearchInput : BasePageInput
{
    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>回传机场 SN</summary>
    public string DockSn { get; set; }

    /// <summary>所属航线任务 ID（协议 flight_id）</summary>
    public string FlightId { get; set; }

    /// <summary>媒体类型</summary>
    public MediaFileTypeEnum? FileType { get; set; }

    /// <summary>仅看原图</summary>
    public bool? OnlyOriginal { get; set; }

    /// <summary>拍摄时间起</summary>
    public DateTime? StartTime { get; set; }

    /// <summary>拍摄时间止</summary>
    public DateTime? EndTime { get; set; }

    /// <summary>文件名关键字</summary>
    public string FileName { get; set; }
}

/// <summary>删除媒体输入（支持批量）</summary>
public class DeleteMediaInput
{
    /// <summary>媒体主键集合</summary>
    [Required(ErrorMessage = "请选择要删除的媒体")]
    [MinLength(1, ErrorMessage = "请至少选择一条媒体")]
    public List<long> Ids { get; set; } = [];
}

/// <summary>按任务查询媒体输入</summary>
public class MediaByTaskInput
{
    /// <summary>协议 flight_id（与 <see cref="TaskId"/> 二者其一）</summary>
    public string FlightId { get; set; }

    /// <summary>本地任务主键</summary>
    public long? TaskId { get; set; }
}

/// <summary>媒体统计输入</summary>
public class MediaStatsInput
{
    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>回传机场 SN</summary>
    public string DockSn { get; set; }
}

/// <summary>机场下拉输入（媒体筛选用）</summary>
public class MediaDockOptionInput
{
    /// <summary>所属工作空间（留空返回全部）</summary>
    public string WorkspaceId { get; set; }
}

/// <summary>调整上传优先级输入</summary>
public class MediaPrioritizeInput
{
    /// <summary>协议 flight_id</summary>
    [Required(ErrorMessage = "任务ID不能为空")]
    public string FlightId { get; set; }
}
