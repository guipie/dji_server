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
         // 默认空间：初始化时必须存在。
         // 一是归档颗粒度——新增设备/航线/飞行区若没指定空间，就落到这里；
         // 二是兜底运行时——用户不属于任何空间时会被自动挂进来（见 WorkspaceUserSeedData 与 DjiWorkspaceUserService.My），
         // 否则保存会因 WorkspaceId 的 NOT NULL 约束直接失败。
         new DjiWorkspace(){Id=6,TenantId=SqlSugarConst.MainConfigId.ToLong(), WorkspaceId=ApplicationConst.DefaultWorkspaceId, WorkspaceName=ApplicationConst.DefaultWorkspaceName, WorkspaceNickName="默认空间", WorkspaceBindCode=ApplicationConst.DefaultWorkspaceBindCode, WorkspaceDesc="系统默认空间，初始化时所有用户都会归属到该空间"},

         new DjiWorkspace(){Id=1,TenantId=SqlSugarConst.MainConfigId.ToLong(), WorkspaceId="e3dea0f5-37f2-4d79-ae58-490af3228069", WorkspaceName="W",WorkspaceNickName="武汉",WorkspaceBindCode="qwe"},
         new DjiWorkspace(){Id=2,TenantId=SqlSugarConst.MainConfigId.ToLong(),WorkspaceId="e3dea0f5-37f2-4d79-ae58-490af3228070", WorkspaceName="QT", WorkspaceNickName="苏州",WorkspaceBindCode="sz"},
         new DjiWorkspace(){Id=3,TenantId=SqlSugarConst.MainConfigId.ToLong(),WorkspaceId="e3dea0f5-37f2-4d79-ae58-490af32280qt", WorkspaceName="QT",WorkspaceNickName="荆江",WorkspaceBindCode="qtcode"},
         new DjiWorkspace(){Id=4,TenantId=SqlSugarConst.MainConfigId.ToLong(),WorkspaceId="e3dea0f5-37f2-4d79-ae58-490af32280wh", WorkspaceName="WH",WorkspaceNickName="芜湖",WorkspaceBindCode="whcode"},
         new DjiWorkspace(){Id=5,TenantId=SqlSugarConst.MainConfigId.ToLong(),WorkspaceId="e3dea0f5-37f2-4d79-ae58-490af32280cq", WorkspaceName="CQ",WorkspaceNickName="重庆",WorkspaceBindCode="cqcode"},
        ];
    }
}
