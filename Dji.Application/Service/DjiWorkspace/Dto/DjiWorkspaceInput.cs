using Dji.Core;
using System.ComponentModel.DataAnnotations;

namespace Dji.Application;

    /// <summary>
    /// workspace基础输入参数
    /// </summary>
    public class DjiWorkspaceBaseInput
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
        /// platform_name
        /// </summary>
        public virtual string WorkspaceNickName { get; set; }
        
        /// <summary>
        /// workspace_bind_code
        /// </summary>
        public virtual string WorkspaceBindCode { get; set; }
        
        /// <summary>
        /// workspace_desc
        /// </summary>
        public virtual string? WorkspaceDesc { get; set; }
        
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
        /// update_user_id
        /// </summary>
        public virtual long? UpdateUserId { get; set; }
        
        /// <summary>
        /// is_delete
        /// </summary>
        public virtual bool IsDelete { get; set; }
        
    }

    /// <summary>
    /// workspace分页查询输入参数
    /// </summary>
    public class DjiWorkspaceInput : BasePageInput
    {
        /// <summary>
        /// 关键字查询
        /// </summary>
        public string? SearchKey { get; set; }

        /// <summary>
        /// workspace_id
        /// </summary>
        public string? WorkspaceId { get; set; }
        
        /// <summary>
        /// workspace_name
        /// </summary>
        public string? WorkspaceName { get; set; }
        
        /// <summary>
        /// platform_name
        /// </summary>
        public string? WorkspaceNickName { get; set; }
        
        /// <summary>
        /// workspace_bind_code
        /// </summary>
        public string? WorkspaceBindCode { get; set; }
        
        /// <summary>
        /// workspace_desc
        /// </summary>
        public string? WorkspaceDesc { get; set; }
        
    }

    /// <summary>
    /// workspace增加输入参数
    /// </summary>
    public class AddDjiWorkspaceInput : DjiWorkspaceBaseInput
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
        /// platform_name
        /// </summary>
        [Required(ErrorMessage = "platform_name不能为空")]
        public override string WorkspaceNickName { get; set; }
        
        /// <summary>
        /// workspace_bind_code
        /// </summary>
        [Required(ErrorMessage = "workspace_bind_code不能为空")]
        public override string WorkspaceBindCode { get; set; }
        
        /// <summary>
        /// is_delete
        /// </summary>
        [Required(ErrorMessage = "is_delete不能为空")]
        public override bool IsDelete { get; set; }
        
    }

    /// <summary>
    /// workspace删除输入参数
    /// </summary>
    public class DeleteDjiWorkspaceInput : BaseIdInput
    {
    }

    /// <summary>
    /// workspace更新输入参数
    /// </summary>
    public class UpdateDjiWorkspaceInput : DjiWorkspaceBaseInput
    {
        /// <summary>
        /// id
        /// </summary>
        [Required(ErrorMessage = "id不能为空")]
        public long Id { get; set; }
        
    }

    /// <summary>
    /// workspace主键查询输入参数
    /// </summary>
    public class QueryByIdDjiWorkspaceInput : DeleteDjiWorkspaceInput
    {

    }
