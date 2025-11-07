using Dji.Core;
using System.ComponentModel.DataAnnotations;

namespace Dji.Application;

    /// <summary>
    /// 相机枚举基础输入参数
    /// </summary>
    public class DjiDeviceCameraEnumBaseInput
    {
        /// <summary>
        /// 产品名称
        /// </summary>
        public virtual string Name { get; set; }
        
        /// <summary>
        /// 产品类型
        /// </summary>
        public virtual string ProductType { get; set; }
        
        /// <summary>
        /// 领域
        /// </summary>
        public virtual DomainEnum Domain { get; set; }
        
        /// <summary>
        /// type-subtype-gimbalindex
        /// </summary>
        public virtual string TsgIndex { get; set; }
        
        /// <summary>
        /// 相机位置
        /// </summary>
        public virtual CameraPositionEnum CameraPosition { get; set; }
        
        /// <summary>
        /// 主云台
        /// </summary>
        public virtual bool IsMainGimbal { get; set; }
        
        /// <summary>
        /// 备注
        /// </summary>
        public virtual string? Desc { get; set; }
        
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
    /// 相机枚举分页查询输入参数
    /// </summary>
    public class DjiDeviceCameraEnumInput : BasePageInput
    {
        /// <summary>
        /// 关键字查询
        /// </summary>
        public string? SearchKey { get; set; }

        /// <summary>
        /// 产品名称
        /// </summary>
        public string? Name { get; set; }
        
        /// <summary>
        /// 领域
        /// </summary>
        public DomainEnum? Domain { get; set; }
        
        /// <summary>
        /// type-subtype-gimbalindex
        /// </summary>
        public string? TsgIndex { get; set; }
        
        /// <summary>
        /// 主云台
        /// </summary>
        public bool? IsMainGimbal { get; set; }
        
    }

    /// <summary>
    /// 相机枚举增加输入参数
    /// </summary>
    public class AddDjiDeviceCameraEnumInput : DjiDeviceCameraEnumBaseInput
    {
        /// <summary>
        /// 产品名称
        /// </summary>
        [Required(ErrorMessage = "产品名称不能为空")]
        public override string Name { get; set; }
        
        /// <summary>
        /// 产品类型
        /// </summary>
        [Required(ErrorMessage = "产品类型不能为空")]
        public override string ProductType { get; set; }
        
        /// <summary>
        /// 领域
        /// </summary>
        [Required(ErrorMessage = "领域不能为空")]
        public override DomainEnum Domain { get; set; }
        
        /// <summary>
        /// type-subtype-gimbalindex
        /// </summary>
        [Required(ErrorMessage = "type-subtype-gimbalindex不能为空")]
        public override string TsgIndex { get; set; }
        
        /// <summary>
        /// 主云台
        /// </summary>
        [Required(ErrorMessage = "主云台不能为空")]
        public override bool IsMainGimbal { get; set; }
        
        /// <summary>
        /// is_delete
        /// </summary>
        [Required(ErrorMessage = "is_delete不能为空")]
        public override bool IsDelete { get; set; }
        
    }

    /// <summary>
    /// 相机枚举删除输入参数
    /// </summary>
    public class DeleteDjiDeviceCameraEnumInput : BaseIdInput
    {
    }

    /// <summary>
    /// 相机枚举更新输入参数
    /// </summary>
    public class UpdateDjiDeviceCameraEnumInput : DjiDeviceCameraEnumBaseInput
    {
        /// <summary>
        /// id
        /// </summary>
        [Required(ErrorMessage = "id不能为空")]
        public long Id { get; set; }
        
    }

    /// <summary>
    /// 相机枚举主键查询输入参数
    /// </summary>
    public class QueryByIdDjiDeviceCameraEnumInput : DeleteDjiDeviceCameraEnumInput
    {

    }
