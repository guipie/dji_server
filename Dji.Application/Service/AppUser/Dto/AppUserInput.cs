using System.ComponentModel.DataAnnotations;

namespace Admin.NET.Application.JuAI;

/// <summary>
/// 聚AI用户基础输入参数
/// </summary>
public class AppUserBaseInput
{
    /// <summary>
    /// 账号
    /// </summary>
    public virtual string Account { get; set; }

    /// <summary>
    /// 密码
    /// </summary>
    public virtual string Password { get; set; }

    /// <summary>
    /// 昵称
    /// </summary>
    public virtual string NickName { get; set; }

    /// <summary>
    /// 头像
    /// </summary>
    public virtual string? Avatar { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    public virtual int Sex { get; set; }

    /// <summary>
    /// 年龄
    /// </summary>
    public virtual int Age { get; set; }

    /// <summary>
    /// 出生日期
    /// </summary>
    public virtual DateTime? Birthday { get; set; }

    /// <summary>
    /// 手机号码
    /// </summary>
    public virtual string Phone { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public virtual string? Email { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public virtual int Status { get; set; }

    /// <summary>
    /// AI值
    /// </summary>
    public virtual long TokenNum { get; set; }

    /// <summary>
    /// AI最后使用时间
    /// </summary>
    public virtual DateTime Tokenlastusedate { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public virtual string? Remark { get; set; }

    /// <summary>
    /// 账号类型
    /// </summary>
    public virtual int AccountType { get; set; }

    /// <summary>
    /// 最新登录Ip
    /// </summary>
    public virtual string? LastLoginIp { get; set; }

    /// <summary>
    /// 最新登录地点
    /// </summary>
    public virtual string? LastLoginAddress { get; set; }

    /// <summary>
    /// 最新登录时间
    /// </summary>
    public virtual DateTime? LastLoginTime { get; set; }

    /// <summary>
    /// 最新登录设备
    /// </summary>
    public virtual string? LastLoginDevice { get; set; }

    /// <summary>
    /// 邀请码
    /// </summary>
    public virtual string? Vcode { get; set; }

}

/// <summary>
/// 聚AI用户分页查询输入参数
/// </summary>
public class AppUserInput : BasePageInput
{
    /// <summary>
    /// 账号
    /// </summary>
    public string Account { get; set; }

    /// <summary>
    /// 昵称
    /// </summary>
    public string NickName { get; set; }

    /// <summary>
    /// 手机号码
    /// </summary>
    public string Phone { get; set; }

    /// <summary>
    /// AI值
    /// </summary>
    public long TokenNum { get; set; }

}


/// <summary>
/// 聚AI用户删除输入参数
/// </summary>
public class DeleteAppUserInput : BaseIdInput
{
}

/// <summary>
/// 聚AI用户主键查询输入参数
/// </summary>
public class QueryByIdAppUserInput : DeleteAppUserInput
{

}
