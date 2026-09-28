// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using System.Linq;
using Dji.Application.CloudRepository;
using Dji.Application.Service.DjiOps.Dto;
using Dji.Core.Enum.DjiEnum.Ops;

namespace Dji.Application.Service.DjiOps;

/// <summary>
/// AirSense（周边民航客机感知）服务。
/// </summary>
/// <remarks>
/// <para>
/// <b>纯只读 + 清理</b>：AirSense 是设备侧的被动感知能力，云端既不能开启也不能调参，
/// 因此没有「下发」类接口。数据全部来自 <c>airsense_warning</c> 上行，由 <c>MqAirSenseService</c> 落库。
/// </para>
/// <para>
/// <b>这是流水表，默认查询必须带时间范围</b>：机场一天可能产出数万条记录（每推一次就是一批），
/// 页面上无条件翻页会随数据增长越来越慢。因此列表接口在未显式指定时间时，
/// 由前端参数约束（<c>StartTime</c> / <c>EndTime</c>）；服务端按上报时间倒序返回，保证首屏总是最新的。
/// </para>
/// <para>
/// <b>等级语义要在界面上显式表达</b>：官方口径是 <c>warning_level ≥ 3</c> 建议无人机主动避让。
/// 本服务额外输出 <c>IsAlert</c> 布尔量，避免每个前端页面各自硬编码「3」这个阈值
/// —— 一旦官方调整口径，只需改 <see cref="AlertWarningLevel"/> 一处。
/// </para>
/// </remarks>
[ApiDescriptionSettings(ApplicationConst.GroupName, Order = 180)]
public class DjiAirSenseService(
    SqlSugarRepository<Dji.Core.Entity.DjiDevice> deviceRep,
    SqlSugarRepository<DjiAirSenseAlarm> alarmRep,
    DjiAirSenseRepository airSenseRepository,
    ILogger<DjiAirSenseService> logger) : IDynamicApiController, ITransient
{
    /// <summary>「建议避让」的等级阈值（官方口径：≥ 3）</summary>
    private const int AlertWarningLevel = (int)AirSenseWarningLevelEnum.Level3;

    private readonly SqlSugarRepository<Dji.Core.Entity.DjiDevice> _deviceRep = deviceRep;
    private readonly SqlSugarRepository<DjiAirSenseAlarm> _alarmRep = alarmRep;
    private readonly DjiAirSenseRepository _airSenseRepository = airSenseRepository;
    private readonly ILogger<DjiAirSenseService> _logger = logger;

    #region 查询

    /// <summary>AirSense 告警分页</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Page")]
    public async Task<SqlSugarPagedList<AirSenseAlarmOutput>> Page(AirSenseSearchInput input)
    {
        input ??= new AirSenseSearchInput();

        var query = _alarmRep.AsQueryable()
            .WhereIF(!input.WorkspaceId.IsNullOrWhiteSpace(), m => m.WorkspaceId == input.WorkspaceId)
            .WhereIF(!input.DockSn.IsNullOrWhiteSpace(), m => m.DockSn == input.DockSn)
            .WhereIF(input.WarningLevel.HasValue, m => m.WarningLevel == input.WarningLevel)
            .WhereIF(input.OnlyAlert == true, m => m.WarningLevel >= (AirSenseWarningLevelEnum)AlertWarningLevel)
            .WhereIF(!input.Keyword.IsNullOrWhiteSpace(), m =>
                m.Icao != null && m.Icao.Contains(input.Keyword.Trim()))
            .WhereIF(input.MaxDistance.HasValue, m => m.Distance != null && m.Distance <= input.MaxDistance)
            .WhereIF(input.StartTime.HasValue, m => m.CreateTime >= input.StartTime)
            .WhereIF(input.EndTime.HasValue, m => m.CreateTime <= input.EndTime)
            // 按设备时钟倒序：设备推送顺序就是事件顺序，落库时刻只反映平台处理快慢
            .OrderBy(m => m.ReportTimestamp, OrderByType.Desc);

        var paged = await query.Select(m => new AirSenseAlarmOutput
        {
            Id = m.Id,
            WorkspaceId = m.WorkspaceId,
            DockSn = m.DockSn,
            Icao = m.Icao,
            WarningLevel = m.WarningLevel,
            Latitude = m.Latitude,
            Longitude = m.Longitude,
            Altitude = m.Altitude,
            AltitudeType = m.AltitudeType,
            Heading = m.Heading,
            RelativeAltitude = m.RelativeAltitude,
            VertTrend = m.VertTrend,
            Distance = m.Distance,
            DockLongitude = m.DockLongitude,
            DockLatitude = m.DockLatitude,
            ReportTimestamp = m.ReportTimestamp,
            CreateTime = m.CreateTime,
        }).ToPagedListAsync(input.Page, input.PageSize);

        await FillAsync(paged.Items.ToList());
        return paged;
    }

    /// <summary>某机场最近的若干条记录（控制面板 / 地图告警条）</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "Recent")]
    public async Task<List<AirSenseAlarmOutput>> Recent([FromQuery] string dockSn, [FromQuery] int limit = 50)
    {
        if (dockSn.IsNullOrWhiteSpace()) return [];

        var rows = await _airSenseRepository.GetRecentAsync(dockSn, limit <= 0 ? 50 : limit);
        var output = rows.Select(ToOutput).ToList();
        await FillAsync(output);
        return output;
    }

    /// <summary>AirSense 统计（概览卡片）</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Stats")]
    public async Task<AirSenseStatsOutput> Stats(AirSenseStatsInput input)
    {
        input ??= new AirSenseStatsInput();

        var rows = await _alarmRep.AsQueryable()
            .WhereIF(!input.WorkspaceId.IsNullOrWhiteSpace(), m => m.WorkspaceId == input.WorkspaceId)
            .WhereIF(!input.DockSn.IsNullOrWhiteSpace(), m => m.DockSn == input.DockSn)
            .ToListAsync();

        var byLevel = rows
            .GroupBy(m => (int)m.WarningLevel)
            .OrderBy(g => g.Key)
            .Select(g => new AirSenseLevelStatOutput
            {
                Level = g.Key,
                LevelName = Desc((AirSenseWarningLevelEnum)g.Key),
                Count = g.Count(),
            })
            .ToList();

        return new AirSenseStatsOutput
        {
            Total = rows.Count,
            AlertCount = rows.Count(m => (int)m.WarningLevel >= AlertWarningLevel),
            ByLevel = byLevel,
            TargetCount = rows
                .Select(m => m.Icao)
                .Where(m => !m.IsNullOrWhiteSpace())
                .Distinct()
                .Count(),
            LastTime = rows.Count == 0 ? null : rows.Max(m => m.CreateTime),
        };
    }

    /// <summary>机场下拉</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "DockOptions")]
    public async Task<List<OpsDockOptionOutput>> DockOptions(OpsDockOptionInput input)
    {
        input ??= new OpsDockOptionInput();

        var docks = await _deviceRep.AsQueryable()
            .Where(m => m.Domain == DomainEnum.Dock)
            .WhereIF(!input.WorkspaceId.IsNullOrWhiteSpace(), m => m.WorkspaceId == input.WorkspaceId)
            .OrderBy(m => m.Nick)
            .ToListAsync();

        return docks.Select(m => new OpsDockOptionOutput
        {
            Sn = m.Sn,
            Nick = m.Nick,
            WorkspaceId = m.WorkspaceId,
            IsOnline = m.IsOnline,
            Label = $"{m.Nick ?? m.Sn}（{m.Sn}）",
        }).ToList();
    }

    /// <summary>告警等级字典</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "LevelOptions")]
    public List<OpsDictOptionOutput> LevelOptions()
        => typeof(AirSenseWarningLevelEnum).GetEnumDescDictionary()
            .OrderBy(m => m.Key)
            .Select(m => new OpsDictOptionOutput { Value = m.Key, Label = m.Value })
            .ToList();

    #endregion

    #region 清理

    /// <summary>
    /// 删除 AirSense 记录。
    /// </summary>
    /// <remarks>
    /// <b>物理删除且不可恢复</b>。这些记录是安全事件证据，删除前请确认已导出留档。
    /// 常规的表体积控制应交给定时清理（按保留期自动删除），而不是人工删除。
    /// </remarks>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Delete")]
    public async Task<int> Delete([FromBody] AirSenseIdsInput input)
    {
        var count = await _airSenseRepository.DeleteAsync(input?.Ids);

        // 删除的是安全事件证据，必须留痕：出问题时至少要能查出「谁在什么时候清了哪些记录」
        _logger.LogWarning("已删除 AirSense 告警记录 {Count} 条：ids={Ids}",
            count, input?.Ids == null ? "-" : string.Join(",", input.Ids));

        return count;
    }

    #endregion

    #region 私有实现

    /// <summary>填充派生字段（昵称、枚举名、告警判定）</summary>
    private async Task FillAsync(List<AirSenseAlarmOutput> items)
    {
        if (items.Count == 0) return;

        foreach (var item in items)
        {
            item.WarningLevelName = Desc(item.WarningLevel);
            item.AltitudeTypeName = Desc(item.AltitudeType);
            item.VertTrendName = Desc(item.VertTrend);
            item.IsAlert = (int)item.WarningLevel >= AlertWarningLevel;
        }

        var dockSns = items.Select(m => m.DockSn).Where(m => !m.IsNullOrWhiteSpace()).Distinct().ToList();
        if (dockSns.Count == 0) return;

        var docks = await _deviceRep.AsQueryable().Where(m => dockSns.Contains(m.Sn)).ToListAsync();
        var nickMap = docks.ToDictionary(m => m.Sn, m => m.Nick);

        foreach (var item in items) item.DockNick = nickMap.GetValueOrDefault(item.DockSn);
    }

    private static AirSenseAlarmOutput ToOutput(DjiAirSenseAlarm entity) => new()
    {
        Id = entity.Id,
        WorkspaceId = entity.WorkspaceId,
        DockSn = entity.DockSn,
        Icao = entity.Icao,
        WarningLevel = entity.WarningLevel,
        Latitude = entity.Latitude,
        Longitude = entity.Longitude,
        Altitude = entity.Altitude,
        AltitudeType = entity.AltitudeType,
        Heading = entity.Heading,
        RelativeAltitude = entity.RelativeAltitude,
        VertTrend = entity.VertTrend,
        Distance = entity.Distance,
        DockLongitude = entity.DockLongitude,
        DockLatitude = entity.DockLatitude,
        ReportTimestamp = entity.ReportTimestamp,
        CreateTime = entity.CreateTime,
    };

    /// <summary>取枚举的 <c>[Description]</c>；显式写全限定名以避免与 NewLife 的同名扩展二义</summary>
    private static string Desc<T>(T value) where T : struct, System.Enum
        => Dji.Core.EnumExtension.GetDescription(value);

    #endregion
}
