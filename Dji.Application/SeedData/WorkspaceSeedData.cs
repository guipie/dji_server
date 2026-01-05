// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。



namespace Dji.Application.SeedData;
public class WorkspaceSeedData : ISqlSugarEntitySeedData<DjiWorkspace>
{
    public IEnumerable<DjiWorkspace> HasData()
    {
        return [
         new DjiWorkspace(){Id=1,TenantId=SqlSugarConst.MainConfigId.ToLong(), WorkspaceId="e3dea0f5-37f2-4d79-ae58-490af3228069", WorkspaceName="W",NickName="武汉",WorkspaceBindCode="qwe"},
         new DjiWorkspace(){Id=2,TenantId=SqlSugarConst.MainConfigId.ToLong(),WorkspaceId="e3dea0f5-37f2-4d79-ae58-490af3228070", WorkspaceName="QT",NickName="苏州",WorkspaceBindCode="sz"},
         new DjiWorkspace(){Id=3,TenantId=SqlSugarConst.MainConfigId.ToLong(),WorkspaceId="e3dea0f5-37f2-4d79-ae58-490af32280qt", WorkspaceName="QT",NickName="荆江",WorkspaceBindCode="qtcode"},
         new DjiWorkspace(){Id=4,TenantId=SqlSugarConst.MainConfigId.ToLong(),WorkspaceId="e3dea0f5-37f2-4d79-ae58-490af32280wh", WorkspaceName="WH",NickName="芜湖",WorkspaceBindCode="whcode"},
         new DjiWorkspace(){Id=5,TenantId=SqlSugarConst.MainConfigId.ToLong(),WorkspaceId="e3dea0f5-37f2-4d79-ae58-490af32280cq", WorkspaceName="CQ",NickName="重庆",WorkspaceBindCode="cqcode"},
        ];
    }
}
