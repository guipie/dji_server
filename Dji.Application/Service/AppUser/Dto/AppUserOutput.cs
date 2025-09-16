namespace Admin.NET.Application.JuAI;

/// <summary>
/// 聚AI用户输出参数
/// </summary>
public class AppUserOutput
{
    /// <summary>
    /// Id
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 账号
    /// </summary>
    public string Account { get; set; }
    /// <summary>
    /// 昵称
    /// </summary>
    public string NickName { get; set; } 

    /// <summary>
    /// 性别
    /// </summary>
    public int Sex { get; set; }

    /// <summary>
    /// 年龄
    /// </summary>
    public int Age { get; set; }

    /// <summary>
    /// 手机号码
    /// </summary>
    public string Phone { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// AI值
    /// </summary>
    public long TokenNum { get; set; }

    /// <summary>
    /// AI最后使用时间
    /// </summary>
    public DateTime? TokenLastUseDate { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 账号类型
    /// </summary>
    public int AccountType { get; set; }

    /// <summary>
    /// 最新登录Ip
    /// </summary>
    public string? LastLoginIp { get; set; }

    /// <summary>
    /// 最新登录地点
    /// </summary>
    public string? LastLoginAddress { get; set; }

    /// <summary>
    /// 最新登录时间
    /// </summary>
    public DateTime? LastLoginTime { get; set; }

    /// <summary>
    /// 最新登录设备
    /// </summary>
    public string? LastLoginDevice { get; set; }

}


