namespace Dji.Application;

/// <summary>
/// workspace输出参数
/// </summary>
public class DjiWorkspaceOutput
{
    /// <summary>
    /// id
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// workspace_id
    /// </summary>
    public string WorkspaceId { get; set; }
    
    /// <summary>
    /// workspace_name
    /// </summary>
    public string WorkspaceName { get; set; }
    
    /// <summary>
    /// platform_name
    /// </summary>
    public string WorkspaceNickName { get; set; }
    
    /// <summary>
    /// workspace_bind_code
    /// </summary>
    public string WorkspaceBindCode { get; set; }
    
    /// <summary>
    /// workspace_desc
    /// </summary>
    public string? WorkspaceDesc { get; set; }
    
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
    /// update_user_id
    /// </summary>
    public long? UpdateUserId { get; set; }
    
    /// <summary>
    /// is_delete
    /// </summary>
    public bool IsDelete { get; set; }
    
    }
 

