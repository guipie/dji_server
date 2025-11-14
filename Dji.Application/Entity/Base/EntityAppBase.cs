// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023 yanyi  联系电话/微信：18600766045  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

namespace Dji.Application;


/// <summary>
/// 框架实体基类
/// </summary>

public abstract class EntityAppBase : EntityBase, IDeletedFilter
{
}

[Tenant(SqlSugarConst.MainConfigId)]
public abstract class EntityAppBaseIgnoreUpdate : EntityBaseId, IDeletedFilter
{
    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnDescription = "创建时间", IsOnlyIgnoreUpdate = true)]
    public virtual DateTime? CreateTime { get; set; }

    /// <summary>
    /// 创建者Id
    /// </summary>
    [SugarColumn(ColumnDescription = "创建者Id", IsOnlyIgnoreUpdate = true)]
    public virtual long? CreateUserId { get; set; }

    /// <summary>
    /// 创建者
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(CreateUserId), nameof(SysUser.Id))]
    public virtual SysUser? CreateUser { get; set; }

    /// <summary>
    /// 软删除
    /// </summary>
    [SugarColumn(ColumnDescription = "软删除")]
    public virtual bool IsDelete { get; set; } = false;
}

[Tenant(SqlSugarConst.MainConfigId)]
public class EntityAppTenant : EntityAppBase, ITenantIdFilter, IDeletedFilter
{
    /// <summary>
    /// 租户Id
    /// </summary>
    [SugarColumn(ColumnDescription = "租户Id", IsOnlyIgnoreUpdate = true)]
    public long? TenantId { get; set; }
}


