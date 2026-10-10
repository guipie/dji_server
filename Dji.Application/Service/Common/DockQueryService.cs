// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using System.Linq;
using System.Linq.Expressions;
using Dji.Application.CloudRepository;
using Dji.Application.Util;
using Dji.Core;

namespace Dji.Application.Service.Common;

/// <summary>
/// 机场下拉选项的公共基础数据。
/// </summary>
/// <remarks>
/// 各业务模块（告警 / 控制 / 媒体 / 直播 / 飞行区 / AirSense / 航线任务）的「机场下拉」
/// 都包含同一套基础字段，且在线判断、显示名拼装规则完全一致。
/// 本类型只承载这些公共字段；各业务的下拉输出 DTO 在此基础上追加自己的业务字段。
/// </remarks>
public class DockOptionBase
{
    /// <summary>机场 SN</summary>
    public string Sn { get; set; }

    /// <summary>机场昵称</summary>
    public string Nick { get; set; }

    /// <summary>设备型号</summary>
    public string Model { get; set; }

    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>是否在线（以 OSD 缓存命中为准，见 <see cref="OnlineStateExtension"/>)</summary>
    public bool IsOnline { get; set; }

    /// <summary>下拉显示文本（昵称优先，格式 <c>昵称（SN）</c>）</summary>
    public string Label { get; set; }
}

/// <summary>
/// 机场下拉的公共查询入参。
/// </summary>
/// <remarks>
/// 各模块（控制 / 告警 / 直播 / 媒体 / 运维 / 航线）的下拉入参此前各定义了一份同构的
/// <c>xxxDockOptionInput</c>（都只有一个 <see cref="WorkspaceId"/>），现统一为这一处，
/// 避免「改一个字段要改六处」的维护负担。
/// </remarks>
public class DockOptionInput
{
    /// <summary>所属工作空间；留空返回全部机场</summary>
    public string WorkspaceId { get; set; }
}

/// <summary>
/// 机场下拉公共查询服务。
/// </summary>
/// <remarks>
/// <para>
/// 收敛点：全项目所有「机场下拉」接口的公共查询逻辑（查机场档案 + 按工作空间过滤 +
/// 在线判断 + 显示名拼装）统一在此实现，各业务 Service 只负责追加自己的业务字段，
/// 避免「查 <c>DjiDevice</c> 表 + 拼 Label + 判在线」散落在七、八个 Service 里重复维护。
/// </para>
/// <para>
/// 机场筛选口径统一为 <c>Domain == Dock</c>；若某模块有额外过滤（如告警中心还要求
/// <c>ParentSn</c> 为空），由调用方在拿到基础列表后再自行收窄。
/// </para>
/// </remarks>
public class DockQueryService(
    SqlSugarRepository<Dji.Core.Entity.DjiDevice> deviceRep,
    SysCacheService cache) : ITransient
{
    private readonly SqlSugarRepository<Dji.Core.Entity.DjiDevice> _deviceRep = deviceRep;
    private readonly SysCacheService _cache = cache;

    /// <summary>
    /// 查询机场下拉的基础数据（含在线判断与显示名），按昵称排序。
    /// </summary>
    /// <param name="workspaceId">可选的工作空间过滤；为空则返回全部机场</param>
    /// <param name="extraFilter">可选的额外过滤条件（如告警中心要求 <c>ParentSn</c> 为空）</param>
    public async Task<List<DockOptionBase>> GetDockOptionsAsync(string workspaceId, Expression<Func<Dji.Core.Entity.DjiDevice, bool>> extraFilter = null)
    {
        var docks = await _deviceRep.AsQueryable()
            .Where(m => m.Domain == DomainEnum.Dock)
            .WhereIF(extraFilter != null, extraFilter)
            .WhereIF(!workspaceId.IsNullOrWhiteSpace(), m => m.WorkspaceId == workspaceId)
            .OrderBy(m => m.Nick)
            .ToListAsync();

        return docks.Select(m => new DockOptionBase
        {
            Sn = m.Sn,
            Nick = m.Nick,
            Model = m.Model,
            WorkspaceId = m.WorkspaceId,
            IsOnline = _cache.IsDockOnline(m.Sn),
            Label = m.Nick.IsNullOrWhiteSpace() ? m.Sn : $"{m.Nick}（{m.Sn}）",
        }).ToList();
    }

    /// <summary>机场是否在线（以 OSD 缓存命中为准，口径见 <see cref="OnlineStateExtension"/>）。</summary>
    public bool IsDockOnline(string sn) => _cache.IsDockOnline(sn);

    /// <summary>
    /// 解析并校验一个机场设备（各业务下发 / 控制接口的统一前置入口）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 此前「查机场 → 校验 <c>Domain == Dock</c> → 校验已绑定工作空间」在六、七个 Service 里
    /// 各有一份几乎逐字复制的私有 <c>ResolveDockAsync</c>，一旦规则调整（如新增在线校验）就要改遍所有副本。
    /// 现收敛为这一处；各业务仅通过 <paramref name="domainHint"/> 定制「不是机场设备」这句文案，
    /// 其余口径完全统一。
    /// </para>
    /// </remarks>
    /// <param name="dockSn">机场 SN</param>
    /// <param name="domainHint">「不是机场设备」报错时的补充说明（如「无法直播」「无法执行航线任务」）</param>
    public async Task<Dji.Core.Entity.DjiDevice> ResolveDockAsync(string dockSn, string domainHint = null)
    {
        if (dockSn.IsNullOrWhiteSpace()) throw Oops.Oh("机场不能为空");

        var dock = await _deviceRep.GetFirstAsync(m => m.Sn == dockSn) ?? throw Oops.Oh($"未找到机场：{dockSn}");
        if (dock.Domain != DomainEnum.Dock)
            throw Oops.Oh($"{dockSn} 不是机场设备{(domainHint.IsNullOrWhiteSpace() ? string.Empty : $"，{domainHint}")}");

        if (dock.WorkspaceId.IsNullOrWhiteSpace())
            throw Oops.Oh($"机场【{dock.Nick ?? dock.Sn}】尚未绑定工作空间");

        return dock;
    }

    /// <summary>
    /// 按一批 SN 一次性查回「SN → 昵称」映射，供各列表批量回填 <c>DockNick</c>。
    /// </summary>
    /// <remarks>
    /// 各 Service 的 <c>FillAsync</c> 里都有一份「页内 SN 去重 → 查 <c>DjiDevice</c> → 建 nickMap → 回填」，
    /// 现收敛为这一处，避免逐行查库与重复拼装。
    /// </remarks>
    /// <param name="sns">SN 集合；空或全空白时返回空字典</param>
    public async Task<Dictionary<string, string>> ResolveDockNickMapAsync(IEnumerable<string> sns)
    {
        var list = sns?.Where(m => !m.IsNullOrWhiteSpace()).Distinct().ToList();
        if (list is not { Count: > 0 }) return new Dictionary<string, string>();

        var docks = await _deviceRep.AsQueryable().Where(m => list.Contains(m.Sn)).ToListAsync();
        return docks.ToDictionary(m => m.Sn, m => m.Nick);
    }
}
