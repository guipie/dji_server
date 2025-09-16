namespace Admin.NET.Application.JuAI;

/// <summary>
/// 聚AI用户输出参数
/// </summary>
public class AppUserPayedOutput
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

    public int PayedCount { get; set; }

    public long PayedNum { get; set; }
    public double PayedMoney { get; set; }

}


