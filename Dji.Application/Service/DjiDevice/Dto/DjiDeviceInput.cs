using Dji.Core;
using System.ComponentModel.DataAnnotations;

namespace Dji.Application;

    /// <summary>
    /// 设备基础输入参数
    /// </summary>
    public class DjiDeviceBaseInput
    {
        /// <summary>
        /// 编号
        /// </summary>
        public virtual string Sn { get; set; }
        
        /// <summary>
        /// 名称
        /// </summary>
        public virtual string Name { get; set; }
        
        /// <summary>
        /// 工作空间
        /// </summary>
        public virtual string WorkspaceId { get; set; }
        
        /// <summary>
        /// 机场编号
        /// </summary>
        public virtual string ParentSn { get; set; }
        
        /// <summary>
        /// 昵称
        /// </summary>
        public virtual string? Nick { get; set; }
        
        /// <summary>
        /// 领域
        /// </summary>
        public virtual DomainEnum Domain { get; set; }
        
        /// <summary>
        /// 主类型
        /// </summary>
        public virtual long? Type { get; set; }
        
        /// <summary>
        /// 子类型
        /// </summary>
        public virtual long? SubType { get; set; }
        
        /// <summary>
        /// index
        /// </summary>
        public virtual string? Index { get; set; }
        
        /// <summary>
        /// 网关版本
        /// </summary>
        public virtual string? ThingVersion { get; set; }
        
        /// <summary>
        /// 固件版本
        /// </summary>
        public virtual string? FirmwareVersion { get; set; }
        
        /// <summary>
        /// desc
        /// </summary>
        public virtual string? Desc { get; set; }
        
        /// <summary>
        /// 经度
        /// </summary>
        public virtual double? Longitude { get; set; }
        
        /// <summary>
        /// 维度
        /// </summary>
        public virtual double? Latitude { get; set; }
        
        /// <summary>
        /// 高度
        /// </summary>
        public virtual double? Altitude { get; set; }
        
        /// <summary>
        /// bind_time
        /// </summary>
        public virtual DateTime? BindTime { get; set; }
        
        /// <summary>
        /// binded
        /// </summary>
        public virtual bool? Binded { get; set; }
        
        /// <summary>
        /// avatar_url
        /// </summary>
        public virtual string? AvatarUrl { get; set; }
        
        /// <summary>
        /// create_time
        /// </summary>
        public virtual DateTime? CreateTime { get; set; }
        
        /// <summary>
        /// 修改时间
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
    /// 设备分页查询输入参数
    /// </summary>
    public class DjiDeviceInput : BasePageInput
    {
        /// <summary>
        /// 关键字查询
        /// </summary>
        public string? SearchKey { get; set; }

        /// <summary>
        /// 编号
        /// </summary>
        public string? Sn { get; set; }
        
        /// <summary>
        /// 名称
        /// </summary>
        public string? Name { get; set; }
        
        /// <summary>
        /// 工作空间
        /// </summary>
        public string? WorkspaceId { get; set; }
        
        
        /// <summary>
        /// 昵称
        /// </summary>
        public string? Nick { get; set; }
        
    }

    /// <summary>
    /// 设备增加输入参数
    /// </summary>
    public class AddDjiDeviceInput : DjiDeviceBaseInput
    {
        /// <summary>
        /// 编号
        /// </summary>
        [Required(ErrorMessage = "编号不能为空")]
        public override string Sn { get; set; }
        
        /// <summary>
        /// 名称
        /// </summary>
        [Required(ErrorMessage = "名称不能为空")]
        public override string Name { get; set; }
        
        /// <summary>
        /// 工作空间
        /// </summary>
        [Required(ErrorMessage = "工作空间不能为空")]
        public override string WorkspaceId { get; set; }
        
        /// <summary>
        /// 机场编号
        /// </summary>
        [Required(ErrorMessage = "机场编号不能为空")]
        public override string ParentSn { get; set; }
        
        /// <summary>
        /// is_delete
        /// </summary>
        [Required(ErrorMessage = "is_delete不能为空")]
        public override bool IsDelete { get; set; }
        
    }

    /// <summary>
    /// 设备删除输入参数
    /// </summary>
    public class DeleteDjiDeviceInput : BaseIdInput
    {
    }

    /// <summary>
    /// 设备更新输入参数
    /// </summary>
    public class UpdateDjiDeviceInput : DjiDeviceBaseInput
    {
        /// <summary>
        /// id
        /// </summary>
        [Required(ErrorMessage = "id不能为空")]
        public long Id { get; set; }
        
    }

    /// <summary>
    /// 设备主键查询输入参数
    /// </summary>
    public class QueryByIdDjiDeviceInput : DeleteDjiDeviceInput
    {

    }
