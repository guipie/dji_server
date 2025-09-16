
namespace Admin.NET.Core.JuAI;

[SugarTable("SysFileRelation", "文件关联表"), IncreTable]
[Tenant("app")]
public class SysFileRelation : EntityAppBase
{
    [SugarColumn(IsPrimaryKey = true, IsNullable = false)]//中间表可以不是主键
    public long RelationId { get; set; }

    [SugarColumn(IsPrimaryKey = true, IsNullable = false)]//中间表可以不是主键
    public long FileId { get; set; }

    [SugarColumn(ColumnDescription = "关联主体的类型", IsNullable = false)]
    public FileRelationEnum FileRelationType { get; set; }
}
