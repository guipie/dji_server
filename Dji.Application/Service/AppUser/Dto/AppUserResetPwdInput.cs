using System.ComponentModel.DataAnnotations;

namespace Admin.NET.Application.JuAI;

/// <summary>
/// 聚AI重置密码输入参数
/// </summary>
public class AppUserResetPwdInput
{
    /// <summary>
    /// 账号
    /// </summary>
    public virtual string Account { get; set; }

    /// <summary>
    /// 密码
    /// </summary>
    public virtual string Password { get; set; }



}
