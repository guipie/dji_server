namespace Dji.Application;

/// <summary>
/// 相机枚举输出参数
/// </summary>
public class DjiDeviceCameraEnumOutput
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
    /// 产品类型
    /// </summary>
    public string ProductType { get; set; }
    
    /// <summary>
    /// 领域
    /// </summary>
    public DomainEnum Domain { get; set; }
    
    /// <summary>
    /// type-subtype-gimbalindex
    /// </summary>
    public string TsgIndex { get; set; }
    
    /// <summary>
    /// 相机位置
    /// </summary>
    public CameraPositionEnum CameraPosition { get; set; }
    
    /// <summary>
    /// 主云台
    /// </summary>
    public bool IsMainGimbal { get; set; }
    
    /// <summary>
    /// 备注
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
 

