// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023 yanyi  联系电话/微信：18600766045  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

namespace Dji.Application.Const;

/// <summary>
/// 业务应用相关常量
/// </summary>
public class ApplicationConst
{
    /// <summary>
    /// API分组名称
    /// </summary>
    public const string DjiCloud = "大疆上云接口";
    public const string GroupName = "大疆业务接口";

    /// <summary>
    /// 系统默认空间的 WorkspaceId
    /// </summary>
    /// <remarks>
    /// 设备/航线/飞行区等实体上的 <c>WorkspaceId</c> 是 NOT NULL，业务侧保存时会把没传空间的请求
    /// 兜底到「当前用户的默认空间」。为保证任何一个用户（含初始化账号与后来新建的账号）都能拿到
    /// 这个兜底值，初始化时会建一个默认空间，并把所有用户挂进去 —— 这里就是它的唯一标识。
    /// </remarks>
    public const string DefaultWorkspaceId = "default";

    /// <summary>
    /// 系统默认空间的名称/昵称
    /// </summary>
    public const string DefaultWorkspaceName = "d";

    /// <summary>
    /// 系统默认空间的绑定码
    /// </summary>
    public const string DefaultWorkspaceBindCode = "dcode";
}