using Dji.Core;
using System.ComponentModel.DataAnnotations;

namespace Dji.Application;

/// <summary>
/// 空间用户基础输入参数
/// </summary>
public class DjiWorkspaceUserBaseInput
{
    /// <summary>
    /// workspace_id
    /// </summary>
    public virtual string WorkspaceId { get; set; }

    /// <summary>
    /// workspace_name
    /// </summary>
    public virtual string WorkspaceName { get; set; }

    /// <summary>
    /// user_name
    /// </summary>
    public virtual string UserName { get; set; }

    /// <summary>
    /// user_id
    /// </summary>
    public virtual string UserId { get; set; }

    /// <summary>
    /// tenant_id
    /// </summary>
    public virtual long? TenantId { get; set; }

    /// <summary>
    /// create_time
    /// </summary>
    public virtual DateTime? CreateTime { get; set; }

    /// <summary>
    /// update_time
    /// </summary>
    public virtual DateTime? UpdateTime { get; set; }

    /// <summary>
    /// create_user_id
    /// </summary>
    public virtual long? CreateUserId { get; set; }

    /// <summary>
    /// create_user_name
    /// </summary>
    public virtual string? CreateUserName { get; set; }

    /// <summary>
    /// update_user_id
    /// </summary>
    public virtual long? UpdateUserId { get; set; }

    /// <summary>
    /// update_user_name
    /// </summary>
    public virtual string? UpdateUserName { get; set; }

    /// <summary>
    /// is_delete
    /// </summary>
    public virtual bool IsDelete { get; set; }

}

/// <summary>
/// 空间用户分页查询输入参数
/// </summary>
public class DjiWorkspaceUserInput : BasePageInput
{
    /// <summary>
    /// 关键字查询
    /// </summary>
    public string? SearchKey { get; set; }

    /// <summary>
    /// workspace_name
    /// </summary>
    public string? WorkspaceId { get; set; }

}

/// <summary>
/// 空间用户增加输入参数
/// </summary>
public class AddDjiWorkspaceUserInput : DjiWorkspaceUserBaseInput
{
    /// <summary>
    /// workspace_id
    /// </summary>
    [Required(ErrorMessage = "workspace_id不能为空")]
    public override string WorkspaceId { get; set; }

    /// <summary>
    /// workspace_name
    /// </summary>
    [Required(ErrorMessage = "workspace_name不能为空")]
    public override string WorkspaceName { get; set; }

    /// <summary>
    /// user_name
    /// </summary>
    [Required(ErrorMessage = "user_name不能为空")]
    public override string UserName { get; set; }

    /// <summary>
    /// user_id
    /// </summary>
    [Required(ErrorMessage = "user_id不能为空")]
    public override string UserId { get; set; }

    /// <summary>
    /// is_delete
    /// </summary>
    [Required(ErrorMessage = "is_delete不能为空")]
    public override bool IsDelete { get; set; }

}

/// <summary>
/// 空间用户删除输入参数
/// </summary>
public class DeleteDjiWorkspaceUserInput
{
    public long UserId { get; set; }
}

/// <summary>
/// 空间用户更新输入参数
/// </summary>
public class UpdateDjiWorkspaceUserInput : DjiWorkspaceUserBaseInput
{
    /// <summary>
    /// id
    /// </summary>
    [Required(ErrorMessage = "id不能为空")]
    public long Id { get; set; }

}

/// <summary>
/// 空间用户主键查询输入参数
/// </summary>
public class QueryByIdDjiWorkspaceUserInput : BaseIdInput
{

}


public class SetDjiWorkspaceUserInput
{
    public List<long> UserIds { get; set; }

    public List<string> WorkspaceIds { get; set; }
}