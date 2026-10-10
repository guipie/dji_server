// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using System.Linq;
using Dji.Core.Entity.DjiEntity;

namespace Dji.Application.Service.Common;

/// <summary>
/// 工作空间解析（各业务「保存 / 创建」时的统一前置入口）。
/// </summary>
/// <remarks>
/// <para>
/// 运营端空间选择器是「可空 = 全部」，而实体 <see cref="DjiWorkspaceUser.WorkspaceId"/> 与
/// <c>EntityWorkspaceBase.WorkspaceId</c> 是 <b>NOT NULL</b>。直接拿入参的 <c>WorkspaceId</c> 落库，
/// 在用户没选空间时就会触发 <c>SQLite Error 19: NOT NULL constraint failed</c> —— 一个用户看不懂、
/// 也无从自行修复的错误。
/// </para>
/// <para>
/// 因此这里统一兜底：入参为空时取「当前用户的默认空间」（无默认则退到第一个空间）。
/// 历史上有多个服务各抄了一份同口径实现（飞行区、航线），现收敛到此单一入口。
/// </para>
/// </remarks>
public class WorkspaceResolver(
    SqlSugarRepository<DjiWorkspaceUser> workspaceUserRep,
    UserManager userManager) : ITransient
{
    private readonly SqlSugarRepository<DjiWorkspaceUser> _workspaceUserRep = workspaceUserRep;
    private readonly UserManager _userManager = userManager;

    /// <summary>
    /// 解析空间 ID：入参非空则原样返回，否则取当前用户的默认空间。
    /// </summary>
    /// <param name="workspaceId">运营端传入的空间 ID（可空）</param>
    /// <param name="scene">场景文案，用于拼装「未分配空间」时的业务异常提示</param>
    public async Task<string> ResolveWorkspaceIdAsync(string workspaceId, string scene)
    {
        if (!string.IsNullOrWhiteSpace(workspaceId)) return workspaceId.Trim();

        var spaces = await _workspaceUserRep.GetListAsync(m => m.UserId == _userManager.UserId);
        var current = spaces.FirstOrDefault(m => m.IsDefault) ?? spaces.FirstOrDefault();
        if (current == null)
            throw Oops.Oh($"当前用户未分配工作空间，请先在“工作空间用户”中配置，或在页面上选择一个空间后再{scene}");
        return current.WorkspaceId;
    }
}
