// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。


using Dji.Application.Cloud.Dto.Device;
using Dji.Application.Cloud.Dto.Org;
using System.Linq;

namespace Dji.Application.CloudRepository;


/// <summary>
/// 设备仓库：负责设备的落库、拓扑维护与缓存。
/// </summary>
/// <remarks>
/// 缓存约定（解决同一 Key 多 TTL 的历史问题）：
/// <list type="bullet">
/// <item><c>Dji_Device:{sn}</c> —— 设备档案（含子设备），TTL 固定为 <see cref="DeviceCacheTtl"/>，由本类独占写入；</item>
/// <item><c>Dji_OsdOnlie:{sn}</c> —— 实时 OSD 快照，机场 <see cref="DockOsdTtl"/>、飞行器 <see cref="DroneOsdTtl"/>；</item>
/// <item><c>Dji_UpdateTopo:{sn}</c> —— 最近一次拓扑报文，TTL 同设备档案。</item>
/// </list>
/// 在线状态同时落库（<see cref="DjiDevice.IsOnline"/> 等），避免服务重启后前端“在线机场”页空白。
/// </remarks>
internal class DeviceRepository(SqlSugarRepository<DjiDevice> sugarRepository, SqlSugarRepository<DjiDeviceEnum> domainRes, ILogger<DeviceRepository> logger, SysCacheService sysCacheService,
    ISqlSugarClient db, WorkspaceRepository workspaceRepository) : BaseRepository
{
    /// <summary>设备档案缓存时长</summary>
    private static readonly TimeSpan DeviceCacheTtl = TimeSpan.FromSeconds(60);

    /// <summary>机场 OSD 缓存时长；超过该时长未上报即视为离线</summary>
    private static readonly TimeSpan DockOsdTtl = TimeSpan.FromSeconds(60);

    /// <summary>飞行器 OSD 缓存时长（上报频率远高于机场）</summary>
    private static readonly TimeSpan DroneOsdTtl = TimeSpan.FromSeconds(10);

    private readonly SqlSugarRepository<DjiDevice> _deviceRes = sugarRepository;
    private readonly SqlSugarRepository<DjiDeviceEnum> _domainRes = domainRes;
    private readonly WorkspaceRepository _workspaceRepository = workspaceRepository;
    private readonly ISqlSugarClient _db = db.AsTenant().GetConnectionScope(SqlSugarConst.MainConfigId);
    private readonly SysCacheService _sysCache = sysCacheService;
    private readonly ILogger<DeviceRepository> _logger = logger;

    #region 查询

    /// <summary>
    /// 设备是否已入库。
    /// </summary>
    /// <remarks>
    /// 只读判断，不写设备档案缓存 —— 设备档案缓存必须由 <see cref="LoadFullDeviceBySn"/> 独占写入，
    /// 否则会写入一个未加载 <see cref="DjiDevice.Children"/> 的半成品，导致子设备判断出错。
    /// </remarks>
    public bool DeviceExists(string sn)
    {
        if (sn.IsNullOrWhiteSpace()) return false;
        return _deviceRes.IsAny(m => m.Sn == sn);
    }

    /// <summary>按 SN 取设备档案（含子设备），优先读缓存</summary>
    public async Task<DjiDevice> GetFullDeviceBySn(string sn)
    {
        if (sn.IsNullOrWhiteSpace()) return null;
        var cached = _sysCache.Get<DjiDevice>(sn.Device());
        return cached ?? await LoadFullDeviceBySn(sn);
    }

    /// <summary>按 SN 取飞行器档案</summary>
    public async Task<DjiDevice> GetDroneBySn(string droneSn)
    {
        if (droneSn.IsNullOrWhiteSpace()) return null;
        return await _deviceRes.GetFirstAsync(m => m.Sn == droneSn);
    }

    /// <summary>跳过缓存加载设备档案并回写缓存</summary>
    private async Task<DjiDevice> LoadFullDeviceBySn(string sn)
    {
        var entity = await _deviceRes.AsQueryable().FirstAsync(m => m.Sn == sn);
        if (entity == null) return null;

        entity.Children = await _deviceRes.GetListAsync(m => m.ParentSn == sn) ?? [];
        CacheDevice(entity);
        return entity;
    }

    /// <summary>把设备档案写回缓存（统一 TTL 的唯一入口）</summary>
    private void CacheDevice(DjiDevice device)
    {
        if (device == null || device.Sn.IsNullOrWhiteSpace()) return;
        _sysCache.Set(device.Sn.Device(), device, DeviceCacheTtl);
    }

    /// <summary>获取机型枚举（Domain + Type + SubType 唯一确定）</summary>
    private async Task<DjiDeviceEnum> FindDeviceModelAsync(DomainEnum domain, int type, int subType)
    {
        return await _domainRes.GetFirstAsync(m => m.Domain == domain && m.Type == type && m.SubType == subType);
    }

    #endregion

    #region 写入

    /// <summary>
    /// 按 SN 幂等写入设备。
    /// </summary>
    /// <remarks>用 Storageable + WhereColumns(Sn) 代替裸 Insert，避免设备并发上线时撞唯一索引。</remarks>
    private async Task UpsertDeviceAsync(DjiDevice device)
    {
        if (device == null || device.Sn.IsNullOrWhiteSpace()) return;
        try
        {
            await _db.Storageable(device).WhereColumns(m => m.Sn).ToStorage().AsInsertable.ExecuteCommandAsync();
        }
        catch (Exception ex)
        {
            // 并发场景下可能仍撞唯一索引，此处降级为更新，保证不因重复上报而丢数据
            _logger.LogWarning(ex, "设备写入冲突，降级为更新，sn:{Sn}", device.Sn);
            await _deviceRes.AsUpdateable(device).Where(m => m.Sn == device.Sn).ExecuteCommandAsync();
        }
    }

    /// <summary>
    /// 新增设备（按 SN 幂等：已存在直接返回既有记录）。
    /// </summary>
    /// <returns>设备实体；SN 或空间为空返回 null</returns>
    public async Task<DjiDevice> Insert(DjiDevice device)
    {
        if (device == null || device.Sn.IsNullOrWhiteSpace())
        {
            _logger.LogError("设备数据为空，data:{Data}", device?.ToJson());
            return null;
        }

        var exists = await GetFullDeviceBySn(device.Sn);
        if (exists != null) return exists;

        if (device.WorkspaceId.IsNullOrWhiteSpace())
        {
            _logger.LogError("设备空间为空，拒绝建档，sn:{Sn}", device.Sn);
            return null;
        }

        await UpsertDeviceAsync(device);
        return await LoadFullDeviceBySn(device.Sn) ?? device;
    }

    /// <summary>标记设备在线（仅改内存对象与缓存，由 <c>MqttDataSyncJob</c> 定期落库）</summary>
    private static void MarkOnline(DjiDevice device, DateTime now)
    {
        if (device == null) return;
        device.IsOnline = true;
        device.LastOnlineTime = now;
        device.LastOsdTime = now;
    }

    /// <summary>
    /// 标记设备离线（立即落库并清理 OSD 快照）。
    /// </summary>
    /// <remarks>由 <c>sys/product/{sn}/status</c> 的 <c>offline</c> 事件触发。</remarks>
    public async Task<bool> SetOffline(string sn)
    {
        if (sn.IsNullOrWhiteSpace()) return false;

        var device = await GetFullDeviceBySn(sn);
        if (device == null) return false;

        device.IsOnline = false;
        var ok = await _deviceRes.AsUpdateable(device)
            .UpdateColumns(m => new { m.IsOnline })
            .Where(m => m.Sn == sn)
            .ExecuteCommandAsync() > 0;

        _sysCache.Remove(sn.OsdOnline());
        CacheDevice(device);
        _logger.LogInformation("设备离线，sn:{Sn}", sn);
        return ok;
    }

    #endregion

    #region MQTT 上行

    /// <summary>
    /// 处理机场 OSD：首次上报时建档，并维护其飞行器子设备。
    /// </summary>
    public async Task BindDockOsd(string dockSn, DockOsd dockOsd)
    {
        if (dockSn.IsNullOrWhiteSpace() || dockOsd == null) return;

        var dock = await GetFullDeviceBySn(dockSn);
        if (dock == null)
        {
            // 设备首次上云时可能还没收到 update_topo，先用 OSD 里的基础信息建档
            if (dockOsd.Longitude != null && dockOsd.Latitude != null)
            {
                await UpsertDeviceAsync(new DjiDevice
                {
                    Sn = dockSn,
                    Longitude = dockOsd.Longitude,
                    Latitude = dockOsd.Latitude,
                    Altitude = dockOsd.Height,
                    FirmwareVersion = dockOsd.FirmwareVersion,
                    Domain = DomainEnum.Dock,
                });
            }
            dock = await LoadFullDeviceBySn(dockSn);
            if (dock == null) return;
        }

        // update_topo 尚未到达或未携带子设备时，用 OSD 里的飞行器信息兜底
        var subSn = dockOsd.SubDevice?.DeviceSn;
        if (!subSn.IsNullOrWhiteSpace() && dock.Children.All(m => m.Sn != subSn))
        {
            var drone = new DjiDevice
            {
                Sn = subSn,
                ParentSn = dockSn,
                WorkspaceId = dock.WorkspaceId,
                Domain = DomainEnum.Drone,
            };
            await UpsertDeviceAsync(drone);
            dock.Children.Add(drone);
            _logger.LogInformation("由机场 OSD 补建飞行器，dock:{DockSn}, drone:{DroneSn}", dockSn, subSn);
        }

        MarkOnline(dock, DateTime.Now);
        _sysCache.Set(dockSn.OsdOnline(), dockOsd, DockOsdTtl);
        CacheDevice(dock);
    }

    /// <summary>
    /// 处理飞行器 OSD：维护其在机场下的子设备记录。
    /// </summary>
    /// <remarks>
    /// 历史实现把 <c>Sn</c> 误写成 <c>dockSn</c>，会用飞行器坐标覆盖机场记录、且飞行器永远无法入库；
    /// 这里修正为 <c>Sn = droneSn, ParentSn = dockSn</c>。
    /// </remarks>
    public async Task BindDroneOsd(string dockSn, string droneSn, DroneOsd droneOsd)
    {
        if (dockSn.IsNullOrWhiteSpace() || droneSn.IsNullOrWhiteSpace() || droneOsd == null) return;

        var dock = await GetFullDeviceBySn(dockSn);
        if (dock == null)
        {
            _logger.LogWarning("飞行器 OSD 先于机场建档到达，暂不处理，dock:{DockSn}, drone:{DroneSn}", dockSn, droneSn);
            _sysCache.Set(droneSn.OsdOnline(), droneOsd, DroneOsdTtl);
            return;
        }

        if (dock.Children.All(m => m.Sn != droneSn))
        {
            var drone = new DjiDevice
            {
                Sn = droneSn,
                ParentSn = dockSn,
                WorkspaceId = dock.WorkspaceId,
                Domain = DomainEnum.Drone,
                Longitude = droneOsd.Longitude,
                Latitude = droneOsd.Latitude,
                Altitude = droneOsd.Height,
                FirmwareVersion = droneOsd.FirmwareVersion,
            };
            await UpsertDeviceAsync(drone);
            dock.Children.Add(drone);
            CacheDevice(dock);
            _logger.LogInformation("由飞行器 OSD 建档，dock:{DockSn}, drone:{DroneSn}", dockSn, droneSn);
        }
        else
        {
            MarkOnline(dock, DateTime.Now);
            CacheDevice(dock);
        }

        _sysCache.Set(droneSn.OsdOnline(), droneOsd, DroneOsdTtl);
    }

    /// <summary>
    /// 处理设备拓扑更新（<c>sys/product/{sn}/status</c> 的 <c>update_topo</c>）。
    /// </summary>
    /// <remarks>
    /// 拓扑报文先于首条 OSD 到达时历史实现会直接 return，导致设备型号永久缺失；此处改为先建档再补子设备，
    /// 并在每次拓扑上报时同步型号（机队升级/换型后会变化）。
    /// </remarks>
    public async Task BindTopo(string sn, UpdateTopoDevice data)
    {
        if (sn.IsNullOrWhiteSpace() || data == null) return;

        var now = DateTime.Now;
        var fullDevice = await GetFullDeviceBySn(sn);
        if (fullDevice == null)
        {
            var model = await FindDeviceModelAsync(data.Domain, data.Type, data.SubType);
            await UpsertDeviceAsync(new DjiDevice
            {
                Sn = sn,
                Domain = data.Domain,
                Type = data.Type,
                SubType = data.SubType,
                ThingVersion = data.ThingVersion,
                Model = model?.Name,
            });
            fullDevice = await LoadFullDeviceBySn(sn);
            if (fullDevice == null)
            {
                _logger.LogError("拓扑建档失败，sn:{Sn}", sn);
                return;
            }
        }
        else
        {
            var model = await FindDeviceModelAsync(data.Domain, data.Type, data.SubType);
            var changed = fullDevice.Domain != data.Domain
                          || fullDevice.Type != data.Type
                          || fullDevice.SubType != data.SubType
                          || fullDevice.ThingVersion != data.ThingVersion
                          || (model != null && fullDevice.Model != model.Name);

            if (changed)
            {
                fullDevice.Domain = data.Domain;
                fullDevice.Type = data.Type;
                fullDevice.SubType = data.SubType;
                fullDevice.ThingVersion = data.ThingVersion;
                fullDevice.Model = model?.Name ?? fullDevice.Model;

                await _deviceRes.AsUpdateable(fullDevice)
                    .UpdateColumns(m => new { m.Domain, m.Type, m.SubType, m.ThingVersion, m.Model })
                    .Where(m => m.Sn == sn)
                    .ExecuteCommandAsync();
                _logger.LogInformation("设备型号/版本变更，sn:{Sn}，model:{Model}", sn, fullDevice.Model);
            }
        }

        // 同步子设备（飞行器）
        foreach (var child in data.SubDevices ?? [])
        {
            if (child.Sn.IsNullOrWhiteSpace()) continue;

            var model = await FindDeviceModelAsync(child.Domain, child.Type, child.SubType);
            var existing = fullDevice.Children.FirstOrDefault(m => m.Sn == child.Sn);
            if (existing == null)
            {
                var entity = new DjiDevice
                {
                    Sn = child.Sn,
                    ParentSn = sn,
                    WorkspaceId = fullDevice.WorkspaceId,
                    Domain = child.Domain,
                    Type = child.Type,
                    SubType = child.SubType,
                    ThingVersion = child.ThingVersion,
                    Index = child.Index,
                    Model = model?.Name,
                };
                await UpsertDeviceAsync(entity);
                fullDevice.Children.Add(entity);
            }
            else if (existing.Domain != child.Domain
                     || existing.Type != child.Type
                     || existing.SubType != child.SubType
                     || existing.ThingVersion != child.ThingVersion
                     || existing.Index != child.Index
                     || (model != null && existing.Model != model.Name))
            {
                // 历史数据中飞行器行的 domain/type/sub_type/model 为空（拓扑早于建档到达所致），
                // 这里在每次拓扑上报时补齐，无需人工修数据。
                existing.Domain = child.Domain;
                existing.Type = child.Type;
                existing.SubType = child.SubType;
                existing.ThingVersion = child.ThingVersion;
                existing.Index = child.Index;
                existing.Model = model?.Name ?? existing.Model;

                await _deviceRes.AsUpdateable(existing)
                    .UpdateColumns(m => new { m.Domain, m.Type, m.SubType, m.ThingVersion, m.Index, m.Model })
                    .Where(m => m.Sn == child.Sn)
                    .ExecuteCommandAsync();
            }
        }

        MarkOnline(fullDevice, now);
        CacheDevice(fullDevice);
        _sysCache.Set(sn.DeviceTopo(), data, DeviceCacheTtl);
    }

    /// <summary>
    /// 处理设备组织绑定结果（<c>airport_bind_status</c> 回包）。
    /// </summary>
    public async Task<bool> UpdateDeviceAirportBind(DeviceOrganization data)
    {
        if (data == null || data.SN.IsNullOrWhiteSpace()) return false;

        if (data.IsDeviceBindOrganization && !data.OrganizationId.IsNullOrWhiteSpace())
            await _workspaceRepository.SaveWorkspace(data);

        var entity = await GetFullDeviceBySn(data.SN);
        if (entity == null)
        {
            // 设备尚未建档：按绑定结果建档。未绑定组织时 WorkspaceId 为空，Insert 会返回 null，
            // 历史实现直接取 device.Id 触发 NullReferenceException，此处显式判空。
            var device = await Insert(new DjiDevice
            {
                Sn = data.SN,
                WorkspaceId = data.OrganizationId,
                Nick = data.DeviceCallsign,
                Binded = data.IsDeviceBindOrganization,
                BindTime = DateTime.Now,
            });
            return device is not null && device.Id > 0;
        }

        entity.BindNum += 1;
        entity.WorkspaceId = data.OrganizationId;
        entity.Nick = data.DeviceCallsign;
        entity.Binded = data.IsDeviceBindOrganization;
        if (entity.BindTime == default) entity.BindTime = DateTime.Now;

        var ok = await _deviceRes.AsUpdateable(entity)
            .UpdateColumns(m => new { m.BindNum, m.WorkspaceId, m.Nick, m.Binded, m.BindTime })
            .Where(m => m.Sn == data.SN)
            .ExecuteCommandAsync() > 0;

        // 机场组织变更后，其飞行器需同步空间归属，否则会推不到所属空间的运营后台
        foreach (var child in entity.Children.Where(m => m.WorkspaceId != data.OrganizationId))
        {
            child.WorkspaceId = data.OrganizationId;
            await _deviceRes.AsUpdateable(child)
                .UpdateColumns(m => m.WorkspaceId)
                .Where(m => m.Sn == child.Sn)
                .ExecuteCommandAsync();
        }

        CacheDevice(entity);
        return ok;
    }

    #endregion
}
