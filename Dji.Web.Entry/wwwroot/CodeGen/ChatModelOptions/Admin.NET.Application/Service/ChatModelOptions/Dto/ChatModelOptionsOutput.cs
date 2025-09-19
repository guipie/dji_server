namespace Dji.Application;

/// <summary>
/// 模型配置输出参数
/// </summary>
public class ChatModelOptionsOutput
{
    /// <summary>
    /// 主键Id
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// 对应的模型
    /// </summary>
    public string ModelId { get; set; } 
    
    /// <summary>
    /// 对应的模型 描述
    /// </summary>
    public string ModelIdName { get; set; } 
    
    /// <summary>
    /// 配置名称
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// 配置类型
    /// </summary>
    public AIModelOptionEnum OptionType { get; set; }
    
    /// <summary>
    /// 最大值
    /// </summary>
    public double MaxNum { get; set; }
    
    /// <summary>
    /// 最小值
    /// </summary>
    public double MinNum { get; set; }
    
    /// <summary>
    /// 选择的值，逗号分割
    /// </summary>
    public string SelectedValues { get; set; }
    
    /// <summary>
    /// 模型描述
    /// </summary>
    public string? Desc { get; set; }
    
    /// <summary>
    /// 默认值
    /// </summary>
    public string? DefaultVal { get; set; }
    
    /// <summary>
    /// 扩展
    /// </summary>
    public string? Extends { get; set; }
    
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
    /// 配置字段
    /// </summary>
    public string Field { get; set; }
    
    }
 

