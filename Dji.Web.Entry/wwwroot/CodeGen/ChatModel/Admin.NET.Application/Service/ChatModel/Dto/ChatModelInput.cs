using Dji.Core;
using System.ComponentModel.DataAnnotations;

namespace Dji.Application;

    /// <summary>
    /// AIModels基础输入参数
    /// </summary>
    public class ChatModelBaseInput
    {
        /// <summary>
        /// 模型
        /// </summary>
        public virtual string ModelId { get; set; }
        
        /// <summary>
        /// 模型
        /// </summary>
        public virtual string Name { get; set; }
        
        /// <summary>
        /// 模型短称
        /// </summary>
        public virtual string ShortName { get; set; }
        
        /// <summary>
        /// 模型头像
        /// </summary>
        public virtual string? AvatarUrl { get; set; }
        
        /// <summary>
        /// 模型类型
        /// </summary>
        public virtual int ModelType { get; set; }
        
        /// <summary>
        /// 模型种类供应商
        /// </summary>
        public virtual string Category { get; set; }
        
        /// <summary>
        /// 接口地址
        /// </summary>
        public virtual string? Url { get; set; }
        
        /// <summary>
        /// 最大token数量
        /// </summary>
        public virtual int MaxToken { get; set; }
        
        /// <summary>
        /// 模型描述
        /// </summary>
        public virtual string? Desc { get; set; }
        
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
        /// 千个token多少钱
        /// </summary>
        public virtual double ThousandTokenCoin { get; set; }
        
        /// <summary>
        /// 模型标签逗号分割
        /// </summary>
        public virtual string Tags { get; set; }
        
        /// <summary>
        /// 模型设置
        /// </summary>
        public virtual string? Settings { get; set; }
        
    }

    /// <summary>
    /// AIModels分页查询输入参数
    /// </summary>
    public class ChatModelInput : BasePageInput
    {
        /// <summary>
        /// 关键字查询
        /// </summary>
        public string? SearchKey { get; set; }

        /// <summary>
        /// 模型
        /// </summary>
        public string? ModelId { get; set; }
        
        /// <summary>
        /// 模型
        /// </summary>
        public string? Name { get; set; }
        
        /// <summary>
        /// 模型类型
        /// </summary>
        public int? ModelType { get; set; }
        
        /// <summary>
        /// 模型种类供应商
        /// </summary>
        public string? Category { get; set; }
        
        /// <summary>
        /// 模型描述
        /// </summary>
        public string? Desc { get; set; }
        
    }

    /// <summary>
    /// AIModels增加输入参数
    /// </summary>
    public class AddChatModelInput : ChatModelBaseInput
    {
        /// <summary>
        /// 模型
        /// </summary>
        [Required(ErrorMessage = "模型不能为空")]
        public override string ModelId { get; set; }
        
        /// <summary>
        /// 模型
        /// </summary>
        [Required(ErrorMessage = "模型不能为空")]
        public override string Name { get; set; }
        
        /// <summary>
        /// 模型短称
        /// </summary>
        [Required(ErrorMessage = "模型短称不能为空")]
        public override string ShortName { get; set; }
        
        /// <summary>
        /// 模型类型
        /// </summary>
        [Required(ErrorMessage = "模型类型不能为空")]
        public override int ModelType { get; set; }
        
        /// <summary>
        /// 模型种类供应商
        /// </summary>
        [Required(ErrorMessage = "模型种类供应商不能为空")]
        public override string Category { get; set; }
        
        /// <summary>
        /// 最大token数量
        /// </summary>
        [Required(ErrorMessage = "最大token数量不能为空")]
        public override int MaxToken { get; set; }
        
        /// <summary>
        /// 千个token多少钱
        /// </summary>
        [Required(ErrorMessage = "千个token多少钱不能为空")]
        public override double ThousandTokenCoin { get; set; }
        
        /// <summary>
        /// 模型标签逗号分割
        /// </summary>
        [Required(ErrorMessage = "模型标签逗号分割不能为空")]
        public override string Tags { get; set; }
        
    }

    /// <summary>
    /// AIModels删除输入参数
    /// </summary>
    public class DeleteChatModelInput : BaseIdInput
    {
    }

    /// <summary>
    /// AIModels更新输入参数
    /// </summary>
    public class UpdateChatModelInput : ChatModelBaseInput
    {
        /// <summary>
        /// 主键Id
        /// </summary>
        [Required(ErrorMessage = "主键Id不能为空")]
        public long Id { get; set; }
        
    }

    /// <summary>
    /// AIModels主键查询输入参数
    /// </summary>
    public class QueryByIdChatModelInput : DeleteChatModelInput
    {

    }
