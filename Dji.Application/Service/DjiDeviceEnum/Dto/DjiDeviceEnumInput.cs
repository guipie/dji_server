using Dji.Core;
using System.ComponentModel.DataAnnotations;

namespace Dji.Application;

    /// <summary>
    /// 设备枚举基础输入参数
    /// </summary>
    public class DjiDeviceEnumBaseInput
    {
        /// <summary>
        /// 产品名称
        /// </summary>
        public virtual string Name { get; set; }
        
        /// <summary>
        /// 领域
        /// </summary>
        public virtual DomainEnum Domain { get; set; }
        
        /// <summary>
        /// 主类型
        /// </summary>
        public virtual long Type { get; set; }
        
        /// <summary>
        /// 子类型
        /// </summary>
        public virtual long SubType { get; set; }
        
        /// <summary>
        /// desc
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
    /// 设备枚举分页查询输入参数
    /// </summary>
    public class DjiDeviceEnumInput : BasePageInput
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
        
    }

    /// <summary>
    /// 设备枚举增加输入参数
    /// </summary>
    public class AddDjiDeviceEnumInput : DjiDeviceEnumBaseInput
    {
        /// <summary>
        /// 产品名称
        /// </summary>
        [Required(ErrorMessage = "产品名称不能为空")]
        public override string Name { get; set; }
        
        /// <summary>
        /// 领域
        /// </summary>
        [Required(ErrorMessage = "领域不能为空")]
        public override DomainEnum Domain { get; set; }
        
        /// <summary>
        /// 主类型
        /// </summary>
        [Required(ErrorMessage = "主类型不能为空")]
        public override long Type { get; set; }
        
        /// <summary>
        /// 子类型
        /// </summary>
        [Required(ErrorMessage = "子类型不能为空")]
        public override long SubType { get; set; }
        
        /// <summary>
        /// is_delete
        /// </summary>
        [Required(ErrorMessage = "is_delete不能为空")]
        public override bool IsDelete { get; set; }
        
    }

    /// <summary>
    /// 设备枚举删除输入参数
    /// </summary>
    public class DeleteDjiDeviceEnumInput : BaseIdInput
    {
    }

    /// <summary>
    /// 设备枚举更新输入参数
    /// </summary>
    public class UpdateDjiDeviceEnumInput : DjiDeviceEnumBaseInput
    {
        /// <summary>
        /// id
        /// </summary>
        [Required(ErrorMessage = "id不能为空")]
        public long Id { get; set; }
        
    }

    /// <summary>
    /// 设备枚举主键查询输入参数
    /// </summary>
    public class QueryByIdDjiDeviceEnumInput : DeleteDjiDeviceEnumInput
    {

    }
