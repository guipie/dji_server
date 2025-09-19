namespace Dji.Application;

    /// <summary>
    /// AIModels输出参数
    /// </summary>
    public class ChatModelDto
    {
        /// <summary>
        /// 主键Id
        /// </summary>
        public long Id { get; set; }
        
        /// <summary>
        /// 模型
        /// </summary>
        public string ModelId { get; set; }
        
        /// <summary>
        /// 模型
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 模型短称
        /// </summary>
        public string ShortName { get; set; }
        
        /// <summary>
        /// 模型头像
        /// </summary>
        public string? AvatarUrl { get; set; }
        
        /// <summary>
        /// 模型类型
        /// </summary>
        public int ModelType { get; set; }
        
        /// <summary>
        /// 模型种类供应商
        /// </summary>
        public string Category { get; set; }
        
        /// <summary>
        /// 接口地址
        /// </summary>
        public string? Url { get; set; }
        
        /// <summary>
        /// 最大token数量
        /// </summary>
        public int MaxToken { get; set; }
        
        /// <summary>
        /// 模型描述
        /// </summary>
        public string? Desc { get; set; }
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime? CreateTime { get; set; }
        
        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdateTime { get; set; }
        
        /// <summary>
        /// 创建者Id
        /// </summary>
        public long? CreateUserId { get; set; }
        
        /// <summary>
        /// 修改者Id
        /// </summary>
        public long? UpdateUserId { get; set; }
        
        /// <summary>
        /// 软删除
        /// </summary>
        public bool IsDelete { get; set; }
        
        /// <summary>
        /// 千个token多少钱
        /// </summary>
        public double ThousandTokenCoin { get; set; }
        
        /// <summary>
        /// 模型标签逗号分割
        /// </summary>
        public string Tags { get; set; }
        
        /// <summary>
        /// 模型设置
        /// </summary>
        public string? Settings { get; set; }
        
    }
