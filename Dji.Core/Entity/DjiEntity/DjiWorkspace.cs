// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

namespace Dji.Core.Entity;
[SugarIndex("index_DjiWorkspace_WorkspaceId", nameof(WorkspaceId), OrderByType.Asc, true)]  //唯一索引(true 表示唯一索引)
[SugarTable("DjiWorkspace", "工作空间"), SysTable]
public class DjiWorkspace : EntityAppTenant
{
    [SugarColumn(ColumnDescription = "空间id", Length = 20, IsNullable = false)]
    public string WorkspaceId { get; set; }

    [SugarColumn(ColumnDescription = "空间名称", Length = 20, IsNullable = false)]
    public string WorkspaceName { get; set; }

    [SugarColumn(ColumnDescription = "平台名称", Length = 20, IsNullable = false)]
    public string WorkspaceNickName { get; set; }

    [SugarColumn(ColumnDescription = "绑定码", Length = 20, IsNullable = true)]
    public string WorkspaceBindCode { get; set; }

    [SugarColumn(IsNullable = true)]
    public string WorkspaceDesc { get; set; }
}
