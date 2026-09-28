// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Core.Enum.DjiEnum.Wayline;

namespace Dji.Application.Service.DjiWayline.Dto;

/// <summary>
/// 航线分页查询输入参数
/// </summary>
public class DjiWaylineSearchInput : BasePageInput
{
    /// <summary>关键字查询（航线名称）</summary>
    public string SearchKey { get; set; }

    /// <summary>航线名称</summary>
    public string Name { get; set; }

    /// <summary>工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>飞行器</summary>
    public string Drone { get; set; }

    /// <summary>航线类别</summary>
    public WaylineType? WaylineType { get; set; }
}

/// <summary>
/// 航线删除输入参数
/// </summary>
public class DeleteDjiWaylineInput : BaseIdInput
{
}

/// <summary>
/// 航线主键查询输入参数
/// </summary>
public class QueryByIdDjiWaylineInput : BaseIdInput
{
}

/// <summary>
/// 航线重命名输入参数
/// </summary>
public class RenameDjiWaylineInput : BaseIdInput
{
    /// <summary>新的航线名称</summary>
    [Required(ErrorMessage = "航线名称不能为空")]
    public string NewName { get; set; }
}

/// <summary>
/// 航线复制输入参数
/// </summary>
public class CopyDjiWaylineInput : BaseIdInput
{
    /// <summary>新航线名称；为空时自动生成“原名-副本[n]”</summary>
    public string NewName { get; set; }

    /// <summary>复制到指定空间；为空时沿用原航线空间</summary>
    public string WorkspaceId { get; set; }
}

/// <summary>
/// 航线更新输入参数
/// </summary>
public class UpdateDjiWaylineInput : CreateWaypointWaylineRequest
{
    /// <summary>航线Id</summary>
    [Required(ErrorMessage = "Id不能为空")]
    public long Id { get; set; }
}
