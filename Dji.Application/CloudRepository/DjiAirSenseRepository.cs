// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using System.Linq;
using Dji.Application.Cloud.Dto.Ops;
using Dji.Core.Enum.DjiEnum.Ops;

namespace Dji.Application.CloudRepository;

/// <summary>
/// AirSense（周边民航客机感知）告警仓储。
/// </summary>
/// <remarks>
/// <para>
/// <b>与飞行区距离快照相反，这里每条都落一行</b>：民航客机是从外部闯入的第三方目标，
/// 「当时几架飞机在附近、最近多少米、等级几级」正是安全审计最需要的证据，
/// 事后无法从任何快照反推出。表增长靠 <see cref="CleanAsync"/> 定期清理。
/// </para>
/// <para>
/// <b>落库失败不影响应答</b>：设备报 <c>need_reply = 1</c>，必须回 <c>events_reply</c>，
/// 回包逻辑放在服务层并与本仓储的解耦（本仓储只返回落库条数，不抛异常）。
/// 机场未建档时告警照旧无法归属空间，此时只打日志。
/// </para>
/// </remarks>
public class DjiAirSenseRepository(
    SqlSugarRepository<DjiAirSenseAlarm> alarmRes,
    SqlSugarRepository<DjiDevice> deviceRes,
    DjiDockStateRepository dockStateRepository,
    ILogger<DjiAirSenseRepository> logger) : BaseRepository
{
    private readonly SqlSugarRepository<DjiAirSenseAlarm> _alarmRes = alarmRes;
    private readonly SqlSugarRepository<DjiDevice> _deviceRes = deviceRes;
    private readonly DjiDockStateRepository _dockStateRepository = dockStateRepository;
    private readonly ILogger<DjiAirSenseRepository> _logger = logger;

    #region 读取

    /// <summary>按主键取单条</summary>
    public async Task<DjiAirSenseAlarm> GetAsync(long id)
        => id <= 0 ? null : await _alarmRes.GetByIdAsync(id);

    /// <summary>取某机场最近的若干条告警（概览用）</summary>
    public async Task<List<DjiAirSenseAlarm>> GetRecentAsync(string dockSn, int limit = 50)
        => await _alarmRes.AsQueryable()
            .Where(m => m.DockSn == dockSn)
            .OrderBy(m => m.ReportTimestamp, OrderByType.Desc)
            .Take(limit)
            .ToListAsync();

    #endregion

    #region 写入

    /// <summary>
    /// 落库一次 <c>airsense_warning</c> 报文（注意报文 <c>data</c> 本身是数组）。
    /// </summary>
    /// <param name="dockSn">机场 SN</param>
    /// <param name="items">报文里的全部目标</param>
    /// <param name="reportTimestamp">报文时间戳（毫秒）</param>
    /// <returns>实际写入条数；机场未建档时返回 0</returns>
    /// <remarks>
    /// <b>顺带补上机场坐标</b>：协议不带本机位置，而前端要在地图上画出「谁在谁的哪一侧」。
    /// 机场坐标是一次极轻量的本地读（快照表按机场 SN 唯一），比让前端再做一次请求划算得多。
    /// 快照缺失时坐标留空，不影响告警本身。
    /// </remarks>
    public async Task<int> InsertAsync(string dockSn, List<AirSenseWarningItem> items, long reportTimestamp)
    {
        if (dockSn.IsNullOrWhiteSpace() || items is not { Count: > 0 }) return 0;

        var device = await _deviceRes.GetFirstAsync(m => m.Sn == dockSn);
        if (device == null)
        {
            _logger.LogWarning("机场 {DockSn} 上报 AirSense 告警但尚未建档，本次 {Count} 条未落库", dockSn, items.Count);
            return 0;
        }

        double? dockLng = null;
        double? dockLat = null;
        try
        {
            var state = await _dockStateRepository.GetAsync(dockSn);
            dockLng = state?.Longitude;
            dockLat = state?.Latitude;
        }
        catch (Exception ex)
        {
            // 坐标只是展示增强，取不到不能连累告警落库
            _logger.LogWarning(ex, "读取机场 {DockSn} 坐标失败，AirSense 告警将以空坐标落库", dockSn);
        }

        var rows = new List<DjiAirSenseAlarm>(items.Count);
        foreach (var item in items)
        {
            if (item == null) continue;

            rows.Add(new DjiAirSenseAlarm
            {
                WorkspaceId = device.WorkspaceId,
                DockSn = dockSn,
                Icao = item.Icao,
                WarningLevel = (AirSenseWarningLevelEnum)(item.WarningLevel ?? 0),
                Latitude = item.Latitude,
                Longitude = item.Longitude,
                Altitude = item.Altitude,
                AltitudeType = (AirSenseAltitudeTypeEnum)(item.AltitudeType ?? 0),
                Heading = item.Heading,
                RelativeAltitude = item.RelativeAltitude,
                VertTrend = (AirSenseVertTrendEnum)(item.VertTrend ?? 0),
                Distance = item.Distance,
                DockLongitude = dockLng,
                DockLatitude = dockLat,
                ReportTimestamp = reportTimestamp,
            });
        }

        if (rows.Count == 0) return 0;

        // 批量插入：一次报文可能带多架飞机，逐条 Insert 会在高频告警下放大数据库往返
        await _alarmRes.InsertRangeAsync(rows);
        return rows.Count;
    }

    /// <summary>
    /// 删除指定时间点之前的历史告警（由定时任务调用）。
    /// </summary>
    /// <remarks>
    /// 以 <c>CreateTime</c>（平台落库时刻）而不是 <c>ReportTimestamp</c>（设备时钟）为界：
    /// 设备时钟可能被校时跳变，用它会误删刚写进来的记录。
    /// </remarks>
    public async Task<int> CleanAsync(DateTime before)
        => await _alarmRes.AsDeleteable()
            .Where(m => m.CreateTime != null && m.CreateTime < before)
            .ExecuteCommandAsync();

    /// <summary>手动删除（物理删除）</summary>
    public async Task<int> DeleteAsync(List<long> ids)
    {
        if (ids == null || ids.Count == 0) return 0;
        return await _alarmRes.AsDeleteable().Where(m => ids.Contains(m.Id)).ExecuteCommandAsync();
    }

    #endregion
}
