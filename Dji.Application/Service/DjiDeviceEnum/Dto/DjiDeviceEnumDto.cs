namespace Dji.Application;

    /// <summary>
    /// 设备枚举输出参数
    /// </summary>
    public class DjiDeviceEnumDto
    {
        /// <summary>
        /// id
        /// </summary>
        public long Id { get; set; }
        
        /// <summary>
        /// 产品名称
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 领域
        /// </summary>
        public DomainEnum Domain { get; set; }
        
        /// <summary>
        /// 主类型
        /// </summary>
        public long Type { get; set; }
        
        /// <summary>
        /// 子类型
        /// </summary>
        public long SubType { get; set; }
        
        /// <summary>
        /// desc
        /// </summary>
        public string? Desc { get; set; }
        
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
