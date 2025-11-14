namespace Dji.Application;

/// <summary>
/// 空间用户输出参数
/// </summary>
public class DjiWorkspaceUserOutput
{

    /// <summary>
    /// workspace_id
    /// </summary>
    public string WorkspaceId { get; set; }

    /// <summary>
    /// workspace_name
    /// </summary>
    public string WorkspaceNickName { get; set; }
    public List<UserWorkspace> Workspaces { get; set; }

    /// <summary>
    /// user_name
    /// </summary>
    public string Account { get; set; }
    public string NickName { get; set; }

    /// <summary>
    /// user_id
    /// </summary>
    public long UserId { get; set; }


    /// <summary>
    /// tenant_id
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// create_time
    /// </summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>
    /// update_time
    /// </summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary>
    /// create_user_id
    /// </summary>
    public long? CreateUserId { get; set; }

    /// <summary>
    /// create_user_name
    /// </summary>
    public string? CreateUserName { get; set; }

    /// <summary>
    /// update_user_id
    /// </summary>
    public long? UpdateUserId { get; set; }

    /// <summary>
    /// update_user_name
    /// </summary>
    public string? UpdateUserName { get; set; }

    /// <summary>
    /// is_delete
    /// </summary>
    public bool IsDelete { get; set; }

}


public class UserWorkspace
{

    public string WorkspaceNickName { get; set; }
    public string WorkspaceId { get; set; }
    public bool IsDefault { get; set; }
}


