
namespace Admin.NET.Core.JuAI;

/// <summary>
/// 用户反馈表
/// </summary>
[SugarTable("Feedback", "Feedback表"), IncreTable]
[Tenant("app")]
public class Feedback : EntityAppTenant
{
    [SugarColumn(ColumnDescription = "内容", Length = 2000)]
    [Required]
    public string Content { get; set; } = string.Empty;

    [SugarColumn(ColumnDescription = "是否匿名")]
    public int IsAnonymous { get; set; }



    [Navigate(typeof(SysFileRelation), nameof(SysFileRelation.RelationId), nameof(SysFileRelation.FileId))]//注意顺序
    public List<SysFile>? Files { get; set; }//只能是null不能赋默认值
}
