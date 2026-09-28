// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
//
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using System.Linq;
using Dji.Application.CloudRepository;
using Dji.Application.Service.DjiHms.Dto;
using Dji.Core.Enum.DjiEnum.Hms;

namespace Dji.Application.Service.DjiHms;

/// <summary>
/// HMS 告警中心服务。
/// </summary>
/// <remarks>
/// <para>
/// <b>数据来源</b>：设备上报的 <c>hms</c> 事件，由 <c>MqHmsService</c> 落库。
/// 本服务只读 + 清理，不存在「人工新增告警」的接口 —— 告警是设备自诊断的产物。
/// </para>
/// <para>
/// <b>「活跃」与「已恢复」的呈现策略</b>：默认只查活跃告警（运维真正要处理的），
/// 但保留查询已恢复记录的能力 —— 排查间歇性故障时，「这条告警上周出现过三次、每次持续 40 秒」
/// 往往比「此刻有没有」更有价值。
/// </para>
/// <para>
/// <b>「及时性告警」默认不过滤但会在前端弱化</b>：风力过大这类告警会随条件改善自动消失，
/// 既不需要人工介入、数量又可能很多。这里提供 <c>ExcludeImminent</c> 让调用方按需排除。
/// </para>
/// </remarks>
[ApiDescriptionSettings(ApplicationConst.GroupName, Order = 130)]
public class DjiHmsService(
    DjiHmsRepository hmsRepository,
    SqlSugarRepository<DjiHmsAlarm> alarmRep,
    SqlSugarRepository<Dji.Core.Entity.DjiDevice> deviceRep) : IDynamicApiController, ITransient
{
    private readonly DjiHmsRepository _hmsRepository = hmsRepository;
    private readonly SqlSugarRepository<DjiHmsAlarm> _alarmRep = alarmRep;
    private readonly SqlSugarRepository<Dji.Core.Entity.DjiDevice> _deviceRep = deviceRep;

    #region 查询

    /// <summary>告警分页列表</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Page")]
    public async Task<SqlSugarPagedList<HmsAlarmOutput>> Page(HmsAlarmSearchInput input)
    {
        input ??= new HmsAlarmSearchInput();

        // 状态筛选：OnlyActive 是 Status 的简写形式，两者同时给出时以显式的 Status 为准
        var status = input.Status;
        if (status == null && input.OnlyActive == true) status = HmsAlarmStatusEnum.Active;

        var query = _alarmRep.AsQueryable()
            .WhereIF(!input.WorkspaceId.IsNullOrWhiteSpace(), m => m.WorkspaceId == input.WorkspaceId)
            .WhereIF(!input.DockSn.IsNullOrWhiteSpace(), m => m.DockSn == input.DockSn)
            .WhereIF(input.Level.HasValue, m => m.Level == input.Level)
            .WhereIF(input.Module.HasValue, m => m.Module == input.Module)
            .WhereIF(status.HasValue, m => m.Status == status)
            .WhereIF(!input.Code.IsNullOrWhiteSpace(), m => m.Code.StartsWith(input.Code.Trim()))
            .WhereIF(input.ExcludeImminent == true, m => m.Imminent == 0)
            .WhereIF(!input.Keyword.IsNullOrWhiteSpace(), m =>
                (m.TextZh != null && m.TextZh.Contains(input.Keyword.Trim())) ||
                (m.TextEn != null && m.TextEn.Contains(input.Keyword.Trim())))
            .WhereIF(input.StartTime.HasValue, m => m.LastTime >= input.StartTime)
            .WhereIF(input.EndTime.HasValue, m => m.LastTime <= input.EndTime)
            // 活跃优先（枚举值 Active=1 > Recovered=0），其次按等级、最近上报
            .OrderBy(m => m.Status, OrderByType.Desc)
            .OrderBy(m => m.Level, OrderByType.Desc)
            .OrderBy(m => m.LastTime, OrderByType.Desc);

        var paged = await query.Select(m => new HmsAlarmOutput
        {
            Id = m.Id,
            WorkspaceId = m.WorkspaceId,
            DockSn = m.DockSn,
            DeviceSn = m.DeviceSn,
            DeviceType = m.DeviceType,
            Code = m.Code,
            Level = m.Level,
            Module = m.Module,
            InTheSky = m.InTheSky,
            Imminent = m.Imminent,
            ComponentIndex = m.ComponentIndex,
            SensorIndex = m.SensorIndex,
            TextZh = m.TextZh,
            TextEn = m.TextEn,
            Status = m.Status,
            FirstTime = m.FirstTime,
            LastTime = m.LastTime,
            RecoverTime = m.RecoverTime,
            ReportTimestamp = m.ReportTimestamp,
            CreateTime = m.CreateTime,
        }).ToPagedListAsync(input.Page, input.PageSize);

        // Items 声明为 IEnumerable，这里转 List 只是为了匹配 FillAsync 的签名；
        // 元素是同一批对象引用，回填字段后原分页结果同样生效
        await FillAsync(paged.Items.ToList());
        return paged;
    }

    /// <summary>告警详情</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "Detail")]
    public async Task<HmsAlarmOutput> Detail([FromQuery] long id)
    {
        var entity = await _hmsRepository.GetAsync(id);
        if (entity == null) return null;

        var output = ToOutput(entity);
        await FillAsync(new List<HmsAlarmOutput> { output });
        return output;
    }

    /// <summary>取某机场当前活跃告警（概览卡片 / 控制面板用）</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "Active")]
    public async Task<List<HmsAlarmOutput>> Active([FromQuery] string dockSn)
    {
        var list = await _hmsRepository.GetActiveAsync(dockSn);
        var output = list.Select(ToOutput).ToList();
        await FillAsync(output);
        return output;
    }

    /// <summary>告警统计</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Stats")]
    public async Task<HmsStatsOutput> Stats(HmsStatsInput input)
    {
        input ??= new HmsStatsInput();

        var query = _alarmRep.AsQueryable()
            .WhereIF(!input.WorkspaceId.IsNullOrWhiteSpace(), m => m.WorkspaceId == input.WorkspaceId)
            .WhereIF(!input.DockSn.IsNullOrWhiteSpace(), m => m.DockSn == input.DockSn);

        var all = await query.ToListAsync();
        var actives = all.Where(m => m.Status == HmsAlarmStatusEnum.Active).ToList();

        var byDock = actives
            .GroupBy(m => m.DockSn)
            .Select(g => new HmsDockStatOutput
            {
                DockSn = g.Key,
                ActiveCount = g.Count(),
                WarningCount = g.Count(m => m.Level == HmsLevelEnum.Warning),
            })
            .OrderByDescending(m => m.WarningCount)
            .ThenByDescending(m => m.ActiveCount)
            .ToList();

        var result = new HmsStatsOutput
        {
            Total = all.Count,
            ActiveCount = actives.Count,
            RecoveredCount = all.Count(m => m.Status == HmsAlarmStatusEnum.Recovered),
            WarningCount = actives.Count(m => m.Level == HmsLevelEnum.Warning),
            RemindCount = actives.Count(m => m.Level == HmsLevelEnum.Remind),
            NoticeCount = actives.Count(m => m.Level == HmsLevelEnum.Notice),
            DockCount = byDock.Count,
            ByDock = byDock,
        };

        var dockSns = byDock.Select(m => m.DockSn).Where(m => !m.IsNullOrWhiteSpace()).ToList();
        if (dockSns.Count > 0)
        {
            var docks = await _deviceRep.AsQueryable().Where(m => dockSns.Contains(m.Sn)).ToListAsync();
            var nickMap = docks.ToDictionary(m => m.Sn, m => m.Nick);
            foreach (var item in result.ByDock)
                item.DockNick = nickMap.GetValueOrDefault(item.DockSn);
        }

        return result;
    }

    /// <summary>机场下拉（带各机场的活跃告警数）</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "DockOptions")]
    public async Task<List<HmsDockOptionOutput>> DockOptions(HmsDockOptionInput input)
    {
        input ??= new HmsDockOptionInput();
        var workspaceId = input.WorkspaceId;

        var docks = await _deviceRep.AsQueryable()
            .Where(m => m.ParentSn == null || m.ParentSn == "")
            .WhereIF(!workspaceId.IsNullOrWhiteSpace(), m => m.WorkspaceId == workspaceId)
            .ToListAsync();

        var dockSns = docks.Select(m => m.Sn).ToList();
        var actives = await _alarmRep.AsQueryable()
            .Where(m => m.Status == HmsAlarmStatusEnum.Active && dockSns.Contains(m.DockSn))
            .Select(m => new { m.DockSn, m.Id })
            .ToListAsync();

        var countMap = actives.GroupBy(m => m.DockSn).ToDictionary(g => g.Key, g => g.Count());

        return docks.Select(m => new HmsDockOptionOutput
        {
            Sn = m.Sn,
            Nick = m.Nick,
            WorkspaceId = m.WorkspaceId,
            IsOnline = m.IsOnline,
            ActiveCount = countMap.GetValueOrDefault(m.Sn),
            Label = $"{m.Nick ?? m.Sn}（{m.Sn}）",
        }).ToList();
    }

    /// <summary>等级字典</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "LevelOptions")]
    public List<HmsDictOptionOutput> LevelOptions()
        => typeof(HmsLevelEnum).GetEnumDescDictionary()
            .OrderBy(m => m.Key)
            .Select(m => new HmsDictOptionOutput { Value = m.Key, Label = m.Value })
            .ToList();

    /// <summary>模块字典</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "ModuleOptions")]
    public List<HmsDictOptionOutput> ModuleOptions()
        => typeof(HmsModuleEnum).GetEnumDescDictionary()
            .OrderBy(m => m.Key)
            .Select(m => new HmsDictOptionOutput { Value = m.Key, Label = m.Value })
            .ToList();

    /// <summary>状态字典</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "StatusOptions")]
    public List<HmsDictOptionOutput> StatusOptions()
        => typeof(HmsAlarmStatusEnum).GetEnumDescDictionary()
            .OrderByDescending(m => m.Key)
            .Select(m => new HmsDictOptionOutput { Value = m.Key, Label = m.Value })
            .ToList();

    #endregion

    #region 清理

    /// <summary>
    /// 删除告警记录。
    /// </summary>
    /// <remarks>
    /// 删除是<b>物理删除</b>且不可恢复，且会一并删除已恢复的历史记录，
    /// 间歇性故障将失去追溯依据。因此前端必须二次确认。
    /// </remarks>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Delete")]
    public async Task<int> Delete([FromBody] HmsAlarmIdsInput input)
        => await _hmsRepository.DeleteAsync(input?.Ids);

    #endregion

    #region 私有实现

    /// <summary>填充派生字段（昵称、文案、枚举名、持续时长）</summary>
    private async Task FillAsync(List<HmsAlarmOutput> items)
    {
        if (items.Count == 0) return;

        foreach (var item in items)
        {
            item.LevelName = Desc(item.Level);
            item.ModuleName = Desc(item.Module);
            item.StatusName = Desc(item.Status);
            item.Text = item.TextZh.IsNullOrWhiteSpace() ? item.TextEn : item.TextZh;
            item.DurationText = BuildDuration(item);
        }

        var dockSns = items.Select(m => m.DockSn).Where(m => !m.IsNullOrWhiteSpace()).Distinct().ToList();
        if (dockSns.Count == 0) return;

        var docks = await _deviceRep.AsQueryable().Where(m => dockSns.Contains(m.Sn)).ToListAsync();
        var nickMap = docks.ToDictionary(m => m.Sn, m => m.Nick);
        foreach (var item in items) item.DockNick = nickMap.GetValueOrDefault(item.DockSn);
    }

    private static HmsAlarmOutput ToOutput(DjiHmsAlarm entity) => new()
    {
        Id = entity.Id,
        WorkspaceId = entity.WorkspaceId,
        DockSn = entity.DockSn,
        DeviceSn = entity.DeviceSn,
        DeviceType = entity.DeviceType,
        Code = entity.Code,
        Level = entity.Level,
        Module = entity.Module,
        InTheSky = entity.InTheSky,
        Imminent = entity.Imminent,
        ComponentIndex = entity.ComponentIndex,
        SensorIndex = entity.SensorIndex,
        TextZh = entity.TextZh,
        TextEn = entity.TextEn,
        Status = entity.Status,
        FirstTime = entity.FirstTime,
        LastTime = entity.LastTime,
        RecoverTime = entity.RecoverTime,
        ReportTimestamp = entity.ReportTimestamp,
        CreateTime = entity.CreateTime,
    };

    /// <summary>
    /// 生成持续时长描述。
    /// </summary>
    /// <remarks>
    /// 活跃告警用「已持续」、已恢复用「共持续」，措辞差异是为了让运维一眼分清
    /// 「这个问题还在」与「这个问题已经过去了」。
    /// </remarks>
    private static string BuildDuration(HmsAlarmOutput item)
    {
        if (item.FirstTime == null) return null;

        var end = item.Status == HmsAlarmStatusEnum.Active ? DateTime.Now : item.RecoverTime;
        if (end == null) return null;

        var span = end.Value - item.FirstTime.Value;
        if (span.TotalSeconds < 0) return null;

        var text = span.TotalDays >= 1
            ? $"{span.Days} 天 {span.Hours} 小时"
            : span.TotalHours >= 1
                ? $"{(int)span.TotalHours} 小时 {span.Minutes} 分"
                : span.TotalMinutes >= 1
                    ? $"{(int)span.TotalMinutes} 分 {span.Seconds} 秒"
                    : $"{(int)span.TotalSeconds} 秒";

        return item.Status == HmsAlarmStatusEnum.Active ? $"已持续 {text}" : $"共持续 {text}";
    }

    /// <summary>取枚举的 <c>[Description]</c>；显式写全限定名以避免与 NewLife 的同名扩展二义</summary>
    private static string Desc<T>(T value) where T : struct, System.Enum
        => Dji.Core.EnumExtension.GetDescription(value);

    #endregion
}
