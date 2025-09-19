using Dji.Core;
using System.ComponentModel.DataAnnotations;

namespace Dji.Application;

    /// <summary>
    /// 模型配置基础输入参数
    /// </summary>
    public class ChatModelOptionsBaseInput
    {
        /// <summary>
        /// 对应的模型
        /// </summary>
        public virtual string ModelId { get; set; }
        
        /// <summary>
        /// 配置名称
        /// </summary>
        public virtual string Name { get; set; }
        
        /// <summary>
        /// 配置类型
        /// </summary>
        public virtual AIModelOptionEnum OptionType { get; set; }
        
        /// <summary>
        /// 最大值
        /// </summary>
        public virtual double MaxNum { get; set; }
        
        /// <summary>
        /// 最小值
        /// </summary>
        public virtual double MinNum { get; set; }
        
        /// <summary>
        /// 选择的值，逗号分割
        /// </summary>
        public virtual string SelectedValues { get; set; }
        
        /// <summary>
        /// 模型描述
        /// </summary>
        public virtual string? Desc { get; set; }
        
        /// <summary>
        /// 默认值
        /// </summary>
        public virtual string? DefaultVal { get; set; }
        
        /// <summary>
        /// 扩展
        /// </summary>
        public virtual string? Extends { get; set; }
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public virtual DateTime? CreateTime { get; set; }
        
        /// <summary>
        /// 更新时间
        /// </summary>
        public virtual DateTime? UpdateTime { get; set; }
        
        /// <summary>
        /// 创建者Id
        /// </summary>
        public virtual long? CreateUserId { get; set; }
        
        /// <summary>
        /// 修改者Id
        /// </summary>
        public virtual long? UpdateUserId { get; set; }
        
        /// <summary>
        /// 软删除
        /// </summary>
        public virtual bool IsDelete { get; set; }
        
        /// <summary>
        /// 配置字段
        /// </summary>
        public virtual string Field { get; set; }
        
    }

    /// <summary>
    /// 模型配置分页查询输入参数
    /// </summary>
    public class ChatModelOptionsInput : BasePageInput
    {
        /// <summary>
        /// 关键字查询
        /// </summary>
        public string? SearchKey { get; set; }

        /// <summary>
        /// 对应的模型
        /// </summary>
        public string? ModelId { get; set; }
        
        /// <summary>
        /// 配置名称
        /// </summary>
        public string? Name { get; set; }
        
        /// <summary>
        /// 配置类型
        /// </summary>
        public AIModelOptionEnum? OptionType { get; set; }
        
        /// <summary>
        /// 配置字段
        /// </summary>
        public string? Field { get; set; }
        
    }

    /// <summary>
    /// 模型配置增加输入参数
    /// </summary>
    public class AddChatModelOptionsInput : ChatModelOptionsBaseInput
    {
        /// <summary>
        /// 对应的模型
        /// </summary>
        [Required(ErrorMessage = "对应的模型不能为空")]
        public override string ModelId { get; set; }
        
        /// <summary>
        /// 配置名称
        /// </summary>
        [Required(ErrorMessage = "配置名称不能为空")]
        public override string Name { get; set; }
        
        /// <summary>
        /// 配置类型
        /// </summary>
        [Required(ErrorMessage = "配置类型不能为空")]
        public override AIModelOptionEnum OptionType { get; set; }
        
        /// <summary>
        /// 最大值
        /// </summary>
        [Required(ErrorMessage = "最大值不能为空")]
        public override double MaxNum { get; set; }
        
        /// <summary>
        /// 最小值
        /// </summary>
        [Required(ErrorMessage = "最小值不能为空")]
        public override double MinNum { get; set; }
        
        /// <summary>
        /// 选择的值，逗号分割
        /// </summary>
        [Required(ErrorMessage = "选择的值，逗号分割不能为空")]
        public override string SelectedValues { get; set; }
        
        /// <summary>
        /// 配置字段
        /// </summary>
        [Required(ErrorMessage = "配置字段不能为空")]
        public override string Field { get; set; }
        
    }

    /// <summary>
    /// 模型配置删除输入参数
    /// </summary>
    public class DeleteChatModelOptionsInput : BaseIdInput
    {
    }

    /// <summary>
    /// 模型配置更新输入参数
    /// </summary>
    public class UpdateChatModelOptionsInput : ChatModelOptionsBaseInput
    {
        /// <summary>
        /// 主键Id
        /// </summary>
        [Required(ErrorMessage = "主键Id不能为空")]
        public long Id { get; set; }
        
    }

    /// <summary>
    /// 模型配置主键查询输入参数
    /// </summary>
    public class QueryByIdChatModelOptionsInput : DeleteChatModelOptionsInput
    {

    }
