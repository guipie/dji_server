// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using System.Linq;

namespace Dji.Application.SeedData;

/// <summary>
/// 工作空间用户种子数据
/// </summary>
/// <remarks>
/// 存在的理由只有一条：设备 / 航线 / 飞行区这些表上的 <c>WorkspaceId</c> 是 <b>NOT NULL</b>，
/// 而业务侧的做法是「用户没指定空间就用他的默认空间」（见 <c>DjiFlyZoneService.ResolveWorkspaceIdAsync</c>
/// 与 <c>DjiWaylineService.ResolveWorkspaceIdAsync</c>）。用户不在任何空间里时这两处只能抛错，
/// 而且报错信息用户自己也无从修复。所以初始化阶段就把全部初始账号挂到
/// <see cref="ApplicationConst.DefaultWorkspaceId"/> 上，并把该空间置为他们的默认空间。
/// </remarks>
public class WorkspaceUserSeedData : ISqlSugarEntitySeedData<DjiWorkspaceUser>
{
    /// <summary>
    /// 初始用户清单：Id / Account / NickName 必须与 <c>SysUserSeedData</c> 保持一致，
    /// 否则空间里会挂着「用户表查不到」的孤儿记录。
    /// </summary>
    /// <remarks>
    /// 这里刻意<b>不</b>写 <c>new SysUserSeedData().HasData()</c> 去复用那份清单：它一进去就会执行
    /// <c>CryptogramUtil.Encrypt("123456")</c>，依赖 Cryptogram（SM2）密钥；密钥缺失或还是占位串时会抛
    /// ArgumentOutOfRangeException，连带把整个空间初始化一起带崩，服务直接起不来。
    /// 本种子只负责「挂空间」，本来也不需要密码，因此只复刻身份字段。
    /// </remarks>
    private static readonly (long Id, string Account, string NickName)[] SeedUsers =
    [
        (1300000000101, "superadmin", "超级管理员"),
        (1300000000111, "admin", "系统管理员"),
        (1300000000112, "user1", "部门主管"),
        (1300000000113, "user2", "部门职员"),
        (1300000000114, "user3", "普通用户"),
        (1300000000115, "user4", "其他"),
    ];

    public IEnumerable<DjiWorkspaceUser> HasData()
    {
        return SeedUsers.Select((user, index) => new DjiWorkspaceUser
        {
            Id = 1300000000201 + index,
            UserId = user.Id,
            Account = user.Account,
            NickName = user.NickName,
            WorkspaceId = ApplicationConst.DefaultWorkspaceId,
            WorkspaceNickName = ApplicationConst.DefaultNickWorkspaceName,
            IsDefault = true,
        }).ToList();
    }
}
