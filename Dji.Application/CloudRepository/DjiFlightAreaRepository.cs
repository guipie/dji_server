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
/// 自定义飞行区仓储（文件版本 + 同步状态 + 距离快照）。
/// </summary>
/// <remarks>
/// <para>
/// <b>设备侧只报摘要，所以「对上是哪一版」全靠 checksum</b>。设备在
/// <c>flight_areas_sync_progress</c> 里给的是 <c>(name, checksum)</c>，文件名会被重复使用，
/// 因此匹配一律<b>先按 checksum、再退化到 name</b>，与表上的唯一索引口径一致。
/// </para>
/// <para>
/// <b>同步进度里的设备没有建档时不能补建</b>：这条链路的文件一律由云端发布
/// （<c>DjiFlightAreaService</c> 注册），设备不可能自己造出一份。若匹配不到，
/// 说明设备在同步一份云端已经删掉的旧文件 —— 这恰恰是运维要知道的信息，但不该污染文件清单。
/// </para>
/// </remarks>
public class DjiFlightAreaRepository(
    SqlSugarRepository<DjiFlightArea> areaRes,
    SqlSugarRepository<DjiFlightAreaLocation> locationRes,
    SqlSugarRepository<DjiDevice> deviceRes,
    ILogger<DjiFlightAreaRepository> logger) : BaseRepository
{
    private readonly SqlSugarRepository<DjiFlightArea> _areaRes = areaRes;
    private readonly SqlSugarRepository<DjiFlightAreaLocation> _locationRes = locationRes;
    private readonly SqlSugarRepository<DjiDevice> _deviceRes = deviceRes;
    private readonly ILogger<DjiFlightAreaRepository> _logger = logger;

    #region 文件：读取

    /// <summary>按主键取单条</summary>
    public async Task<DjiFlightArea> GetAsync(long id)
        => id <= 0 ? null : await _areaRes.GetByIdAsync(id);

    /// <summary>按主键批量取</summary>
    public async Task<List<DjiFlightArea>> GetByIdsAsync(List<long> ids)
        => ids == null || ids.Count == 0
            ? []
            : await _areaRes.AsQueryable().Where(m => ids.Contains(m.Id)).ToListAsync();

    /// <summary>取某机场的全部飞行区文件（新的在前）</summary>
    public async Task<List<DjiFlightArea>> GetByDockAsync(string dockSn)
        => await _areaRes.AsQueryable()
            .Where(m => m.DockSn == dockSn)
            .OrderBy(m => m.IsActive, OrderByType.Desc)
            .OrderBy(m => m.CreateTime, OrderByType.Desc)
            .ToListAsync();

    /// <summary>取某机场当前启用中的文件（设备来要文件时用；正常至多一份）</summary>
    public async Task<List<DjiFlightArea>> GetActiveByDockAsync(string dockSn)
        => await _areaRes.AsQueryable()
            .Where(m => m.DockSn == dockSn && m.IsActive)
            .ToListAsync();

    #endregion

    #region 文件：写入

    /// <summary>
    /// 登记（或更新）一份飞行区文件。
    /// </summary>
    /// <param name="dockSn">机场 SN</param>
    /// <param name="fileName">文件名</param>
    /// <param name="objectKey">桶内 Key</param>
    /// <param name="checksum">SHA256 摘要（服务端从桶内真实内容算出）</param>
    /// <param name="fileSize">字节数</param>
    /// <returns>落库后的实体；机场未建档时返回 null</returns>
    /// <remarks>
    /// <para>
    /// <b>按 (机场, checksum) 幂等</b>：同一份文件重复登记（例如运维点了两次「保存」）
    /// 不产生第二行，而是把文件名刷新成最新值 —— 因为设备只认摘要，摘要相同就是同一版。
    /// </para>
    /// <para>
    /// <b>新登记的文件不自动置为启用</b>（<c>IsActive = false</c>），要等设备真的同步成功
    /// 才由 <see cref="UpdateSyncAsync"/> 置位。否则会出现「云端认为已生效、设备还在跑旧版」的假象。
    /// </para>
    /// </remarks>
    public async Task<DjiFlightArea> RegisterAsync(string dockSn, string fileName, string objectKey,
        string checksum, long? fileSize)
    {
        if (dockSn.IsNullOrWhiteSpace() || fileName.IsNullOrWhiteSpace()) return null;

        var device = await _deviceRes.GetFirstAsync(m => m.Sn == dockSn);
        if (device == null)
        {
            _logger.LogWarning("机场 {DockSn} 尚未建档，飞行区文件未登记：{FileName}", dockSn, fileName);
            return null;
        }

        var exist = await FindAsync(dockSn, checksum, fileName);
        if (exist != null)
        {
            exist.FileName = fileName;
            exist.ObjectKey = objectKey ?? exist.ObjectKey;
            exist.Checksum = checksum ?? exist.Checksum;
            if (fileSize.HasValue) exist.FileSize = fileSize;

            await _areaRes.AsUpdateable(exist)
                .UpdateColumns(m => new { m.FileName, m.ObjectKey, m.Checksum, m.FileSize })
                .ExecuteCommandAsync();

            return exist;
        }

        var entity = new DjiFlightArea
        {
            WorkspaceId = device.WorkspaceId,
            DockSn = dockSn,
            FileName = fileName,
            ObjectKey = objectKey,
            Checksum = checksum,
            FileSize = fileSize,
            SyncStatus = FlightAreaSyncStatusEnum.WaitSync,
            SyncReason = FlightAreaSyncReasonEnum.None,
            IsActive = false,
        };

        await _areaRes.InsertAsync(entity);
        _logger.LogInformation("已登记飞行区文件：机场={DockSn} 文件={FileName} 摘要={Checksum}",
            dockSn, fileName, checksum);

        return entity;
    }

    /// <summary>记录一次下发给设备的地址（仅用于排查「设备报下载失败」）</summary>
    public async Task SaveUrlAsync(long id, string url)
    {
        if (id <= 0) return;

        await _areaRes.AsUpdateable()
            .SetColumns(m => new DjiFlightArea { Url = url })
            .Where(m => m.Id == id)
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 用一次 <c>flight_areas_sync_progress</c> 刷新同步状态。
    /// </summary>
    /// <param name="dockSn">机场 SN</param>
    /// <param name="status">同步状态</param>
    /// <param name="reason">失败原因码</param>
    /// <param name="file">文件标识（name + checksum）</param>
    /// <param name="reportTimestamp">报文时间戳（毫秒）</param>
    /// <returns>命中的文件行；匹配不到时返回 null</returns>
    /// <remarks>
    /// <b>同步成功时会把该机场其它文件一并置为「未启用」</b>：设备上的作业区域有且只有一套，
    /// 允许两行同时为 true 会让前端展示出「两份都生效」的错觉，也会让
    /// <see cref="GetActiveByDockAsync"/> 的结果变成不确定的多条。
    /// </remarks>
    public async Task<DjiFlightArea> UpdateSyncAsync(string dockSn, FlightAreaSyncStatusEnum status,
        FlightAreaSyncReasonEnum reason, FlightAreaSyncFile file, long reportTimestamp)
    {
        if (dockSn.IsNullOrWhiteSpace() || file == null) return null;

        var row = await FindAsync(dockSn, file.Checksum, file.Name);
        if (row == null)
        {
            _logger.LogWarning("收到无法归属的飞行区同步进度（云端未登记该文件），已忽略：机场={DockSn} 文件={FileName} 摘要={Checksum} 状态={Status}",
                dockSn, file.Name, file.Checksum, status);
            return null;
        }

        var now = DateTime.Now;
        row.SyncStatus = status;
        row.SyncReason = reason;
        row.LastTime = now;
        row.ReportTimestamp = reportTimestamp;

        // 只有「已同步」才算真正生效；失败与同步中都不改变当前的启用版本
        if (status == FlightAreaSyncStatusEnum.Synchronized) row.IsActive = true;

        await _areaRes.AsUpdateable(row)
            .UpdateColumns(m => new { m.SyncStatus, m.SyncReason, m.LastTime, m.ReportTimestamp })
            .ExecuteCommandAsync();

        if (status == FlightAreaSyncStatusEnum.Synchronized)
            await ActivateOnlyAsync(dockSn, row.Id);

        if (status is FlightAreaSyncStatusEnum.Fail or FlightAreaSyncStatusEnum.SwitchFail)
        {
            _logger.LogWarning("机场 {DockSn} 同步飞行区失败：文件={FileName} 状态={Status} 原因={Reason}",
                dockSn, row.FileName, status, Dji.Core.EnumExtension.GetDescription(reason));
        }

        return row;
    }

    /// <summary>把指定文件置为唯一启用版本，其余同机场文件全部取消启用</summary>
    /// <remarks>
    /// 刻意拆成两条语句而不是用 <c>SetColumns(m =&gt; new DjiFlightArea { IsActive = m.Id == id })</c>：
    /// 把「列与列的比较」写进表达式树里，翻译结果依具体数据库而异，而这里只是两条固定取值的更新，
    /// 拆开后语义在 SQLite / MySQL 上完全一致。
    /// </remarks>
    public async Task<int> ActivateOnlyAsync(string dockSn, long id)
    {
        if (dockSn.IsNullOrWhiteSpace() || id <= 0) return 0;

        var cleared = await _areaRes.AsUpdateable()
            .SetColumns(m => new DjiFlightArea { IsActive = false })
            .Where(m => m.DockSn == dockSn && m.Id != id && m.IsActive)
            .ExecuteCommandAsync();

        await _areaRes.AsUpdateable()
            .SetColumns(m => new DjiFlightArea { IsActive = true })
            .Where(m => m.Id == id)
            .ExecuteCommandAsync();

        return cleared;
    }

    /// <summary>
    /// 删除文件记录。
    /// </summary>
    /// <remarks>
    /// <b>只删库里的记录，不删对象存储里的文件</b>：一旦云端记录被删而设备手上还有这份文件，
    /// 设备会持续上报「无法归属的同步进度」。保留桶里的文件，可以让运维在误删后迅速重新登记。
    /// 桶内文件的清理属于存储生命周期策略的职责。
    /// </remarks>
    public async Task<int> DeleteAsync(List<long> ids)
    {
        if (ids == null || ids.Count == 0) return 0;
        return await _areaRes.AsDeleteable().Where(m => ids.Contains(m.Id)).ExecuteCommandAsync();
    }

    #endregion

    #region 距离快照

    /// <summary>取某机场的全部区域距离快照</summary>
    public async Task<List<DjiFlightAreaLocation>> GetLocationsAsync(string dockSn)
        => await _locationRes.AsQueryable()
            .Where(m => m.DockSn == dockSn)
            .OrderBy(m => m.IsInArea, OrderByType.Desc)
            .OrderBy(m => m.MinDistance, OrderByType.Asc)
            .ToListAsync();

    /// <summary>
    /// 刷新飞行器与各区域的距离快照（<c>flight_areas_drone_location</c>，高频）。
    /// </summary>
    /// <param name="dockSn">机场 SN</param>
    /// <param name="locations">本次上报的距离列表</param>
    /// <param name="reportTimestamp">报文时间戳（毫秒）</param>
    /// <returns>发生「进入 / 离开」翻转的记录，供调用方决定是否告警</returns>
    /// <remarks>
    /// <para>
    /// <b>只做快照不做流水</b>：这个报文飞行中每秒可能来数次，落流水几天就能到千万行，
    /// 而单条历史距离没有任何回溯价值。真正需要留痕的是「进入过几次、什么时候」，
    /// 因此这里只维护 <c>EnterCount</c> / <c>LastEnterTime</c> / <c>LastExitTime</c> 三个累计字段。
    /// </para>
    /// <para>
    /// <b>翻转判定需要一次读库</b>：<c>is_in_area</c> 是布尔量，只有和上一帧比对才知道「变化」，
    /// 因此每次都要先把该机场的快照读出来。设备数量级是「一台机场几个区域」，
    /// 这一次查询的量极小，远小于把判定权交给前端的代价。
    /// </para>
    /// <para>
    /// <b>首次出现的区域不算「进入」</b>：平台刚启动或设备刚上线时，快照里本来就没有这条记录，
    /// 若按「从 false 变 true」处理，会在每次重启后刷出一批假的进入告警。
    /// </para>
    /// </remarks>
    public async Task<List<(DjiFlightAreaLocation Row, bool Entered)>> RefreshLocationsAsync(
        string dockSn, List<FlightAreaDroneLocation> locations, long reportTimestamp)
    {
        var result = new List<(DjiFlightAreaLocation, bool)>();
        if (dockSn.IsNullOrWhiteSpace() || locations is not { Count: > 0 }) return result;

        var device = await _deviceRes.GetFirstAsync(m => m.Sn == dockSn);
        if (device == null)
        {
            _logger.LogWarning("机场 {DockSn} 上报飞行区距离但尚未建档，本次未落库", dockSn);
            return result;
        }

        var existings = await _locationRes.AsQueryable().Where(m => m.DockSn == dockSn).ToListAsync();
        var map = existings.Where(m => !m.AreaId.IsNullOrWhiteSpace())
            .GroupBy(m => m.AreaId)
            .ToDictionary(g => g.Key, g => g.First());

        var now = DateTime.Now;

        foreach (var item in locations)
        {
            if (item?.AreaId.IsNullOrWhiteSpace() != false) continue;

            map.TryGetValue(item.AreaId, out var row);
            var abs = item.AreaDistance == null ? (double?)null : Math.Abs(item.AreaDistance.Value);

            if (row == null)
            {
                // 首次见到该区域：只落当前值，不记「进入」（否则每次重启都会刷一批假告警）
                row = new DjiFlightAreaLocation
                {
                    WorkspaceId = device.WorkspaceId,
                    DockSn = dockSn,
                    AreaId = item.AreaId,
                    AreaDistance = item.AreaDistance,
                    IsInArea = item.IsInArea,
                    MinDistance = abs,
                    EnterCount = item.IsInArea ? 1 : 0,
                    LastEnterTime = item.IsInArea ? now : null,
                    LastTime = now,
                    ReportTimestamp = reportTimestamp,
                };

                await _locationRes.InsertAsync(row);
                continue;
            }

            var entered = row.IsInArea != item.IsInArea && item.IsInArea;
            var exited = row.IsInArea != item.IsInArea && !item.IsInArea;

            if (entered)
            {
                row.EnterCount += 1;
                row.LastEnterTime = now;
            }
            else if (exited)
            {
                row.LastExitTime = now;
            }

            row.AreaDistance = item.AreaDistance;
            row.IsInArea = item.IsInArea;
            // 历史最近距离只统计「区外」的正距离，避免区内的负值把最小值拉到毫无意义的量级
            if (abs.HasValue && (row.MinDistance == null || abs.Value < row.MinDistance.Value))
                row.MinDistance = abs;
            row.LastTime = now;
            row.ReportTimestamp = reportTimestamp;

            await _locationRes.AsUpdateable(row)
                .UpdateColumns(m => new
                {
                    m.AreaDistance, m.IsInArea, m.MinDistance,
                    m.EnterCount, m.LastEnterTime, m.LastExitTime,
                    m.LastTime, m.ReportTimestamp
                })
                .ExecuteCommandAsync();

            if (entered) result.Add((row, true));
        }

        return result;
    }

    #endregion

    #region 私有实现

    /// <summary>
    /// 先按摘要、再按文件名定位文件行。
    /// </summary>
    /// <remarks>
    /// 摘要优先的原因见类注释；退化到文件名是为了兼容设备固件只回 name 的情况 ——
    /// 此时若同名有多行（历史版本），取最新的一行，因为设备几乎不可能在同步一个很旧的版本。
    /// </remarks>
    private async Task<DjiFlightArea> FindAsync(string dockSn, string checksum, string fileName)
    {
        if (!checksum.IsNullOrWhiteSpace())
        {
            var byChecksum = await _areaRes.GetFirstAsync(m => m.DockSn == dockSn && m.Checksum == checksum);
            if (byChecksum != null) return byChecksum;
        }

        if (fileName.IsNullOrWhiteSpace()) return null;

        return await _areaRes.AsQueryable()
            .Where(m => m.DockSn == dockSn && m.FileName == fileName)
            .OrderBy(m => m.CreateTime, OrderByType.Desc)
            .FirstAsync();
    }

    #endregion
}
