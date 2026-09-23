// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。




using AngleSharp.Dom;
using Dji.Application.Cloud.Dto.Device;
using Dji.Application.Cloud.Dto.Org;
using System.Linq;

namespace Dji.Application.CloudRepository;


internal class DeviceRepository(SqlSugarRepository<DjiDevice> sugarRepository, SqlSugarRepository<DjiDeviceEnum> domainRes, ILogger<DeviceRepository> logger, SysCacheService sysCacheService,
    ISqlSugarClient db) : BaseRepository
{
    private readonly SqlSugarRepository<DjiDevice> _deviceRes = sugarRepository;
    private readonly SqlSugarRepository<DjiDeviceEnum> _domainRes = domainRes;
    private readonly ISqlSugarClient _db = db.AsTenant().GetConnectionScope(SqlSugarConst.MainConfigId);
    private readonly SysCacheService _sysCache = sysCacheService;
    private readonly ILogger<DeviceRepository> _logger = logger;

    public bool DeviceExists(string sn)
    {
        if (_sysCache.ExistKey(sn.Device())) return true;
        var entity = _deviceRes.GetFirst(m => m.Sn == sn);
        if (entity != null)
        {
            _sysCache.Set(sn.Device(), entity, TimeSpan.FromSeconds(5 * 60));
            return true;
        }
        return false;
    }
    public async Task<bool> SetDeviceOnline(string sn, int expireSeconds = 30)
    {
        var entity = await GetFullDeviceBySn(sn);
        return _sysCache.Set(sn.Device(), entity, TimeSpan.FromSeconds(expireSeconds));
    }
    public async Task<DjiDevice> GetFullDeviceBySn(string sn)
    {
        if (_sysCache.ExistKey(sn.Device())) return _sysCache.Get<DjiDevice>(sn.Device());
        var entity = await _deviceRes.AsQueryable().FirstAsync(m => m.Sn == sn);
        if (entity != null)
        {
            entity.Children = await _deviceRes.GetListAsync(m => m.ParentSn == sn);
            _sysCache.Set(sn.Device(), entity, TimeSpan.FromSeconds(60));
        }
        return entity;
    }
    public async Task<DjiDevice> GetDroneBySn(string droneSn)
    {
        return await _deviceRes.GetFirstAsync(m => m.Sn == droneSn);
    }
    public async Task<DjiDevice> Insert(DjiDevice device)
    {
        if (device.Sn.IsNullOrWhiteSpace() || device.WorkspaceId.IsNullOrWhiteSpace())
        {
            _logger.LogError("device data is empty,data:{}", device.ToJson());
            return null;
        }
        var data = await GetFullDeviceBySn(device.Sn);
        if (data != null) return data;
        return await _deviceRes.InsertReturnEntityAsync(device);
    }
    public async Task BindDockOsd(string dockSn, DockOsd dockOsd)
    {
        //if (DeviceExists(dockSn)) return;
        var dock = await GetFullDeviceBySn(dockSn);
        if (dock == null)
        {
            var entity = new DjiDevice() { Sn = dockSn, Longitude = dockOsd.Longitude.Value, Latitude = dockOsd.Latitude.Value, Altitude = dockOsd.Height.Value };
            dock = await _deviceRes.InsertReturnEntityAsync(entity);
        }
        if (dockOsd.SubDevice != null && !dockOsd.SubDevice.DeviceSn.IsNullOrWhiteSpace() && dock.Children.IsEmptyList())
        {
            var entity = new DjiDevice() { Sn = dockOsd.SubDevice.DeviceSn, ParentSn = dockSn, WorkspaceId = dock.WorkspaceId };
            await _db.Storageable(entity).WhereColumns(m => m.Sn).ToStorage().AsInsertable.ExecuteCommandAsync();
            dock.Children = dock.Children.IsEmptyDefault().ToList();
            dock.Children.Add(entity);
        }
        _sysCache.Set(dockSn.OsdOnline(), dockOsd, TimeSpan.FromSeconds(60));
        _sysCache.Set(dockSn.Device(), dock, TimeSpan.FromSeconds(60));

    }

    public async Task BindDroneOsd(string dockSn, string droneSn, DroneOsd droneOsd)
    {
        var fullDevice = await GetFullDeviceBySn(dockSn);
        var drone = fullDevice.Children.FirstOrDefault(m => m.Sn == droneSn);
        if (drone == null)
        {
            var entity = new DjiDevice() { Sn = dockSn, Longitude = droneOsd.Longitude, Latitude = droneOsd.Latitude, Altitude = droneOsd.Height };
            await _db.Storageable(entity).WhereColumns(m => m.Sn).ToStorage().AsInsertable.ExecuteCommandAsync();
        }
        //else if ((droneOsd.Longitude > 0 && droneOsd.Latitude > 0) || (drone.FirmwareVersion != droneOsd.FirmwareVersion && droneOsd.FirmwareVersion.IsNullOrWhiteSpace()))
        //{
        //    drone.Longitude = droneOsd.Longitude.IsGtGet(drone.Longitude);
        //    drone.Latitude = droneOsd.Latitude.IsGtGet(drone.Latitude);
        //    drone.Altitude = droneOsd.Height;
        //    drone.FirmwareVersion = droneOsd.FirmwareVersion.IsNullGet(drone.FirmwareVersion);
        //    await _deviceRes.UpdateAsync(drone);
        //}
        _sysCache.Set(droneSn.OsdOnline(), droneOsd, TimeSpan.FromSeconds(10));

    }
    public async Task BindTopo(string sn, UpdateTopoDevice data)
    {
        var fullDevice = await GetFullDeviceBySn(sn);
        if (fullDevice == null) return;
        if (fullDevice.Domain == null)
        {
            var model = await _domainRes.GetFirstAsync(m => m.Domain == data.Domain && m.Type == data.Type && m.SubType == data.SubType);
            // 将 fullDevice.Domain = data.Domain; 修改为强制类型转换
            fullDevice.Domain =data.Domain;
                fullDevice.Domain = data.Domain;
            fullDevice.Type = data.Type;
            fullDevice.SubType = data.SubType;
            fullDevice.ThingVersion = data.ThingVersion;
            fullDevice.Model = model.Name;
            await _deviceRes.UpdateAsync(fullDevice);
        }
        var sns = fullDevice.Children==null?[]: fullDevice.Children.Where(m => m.Domain != null).Select(m => m.Sn);
        foreach (var child in data.SubDevices)
        {
            if (sns.Any()&&sns.Contains(child.Sn)) continue;
            var model = await _domainRes.GetFirstAsync(m => m.Domain == child.Domain && m.Type == child.Type && m.SubType == child.SubType);
            var entity = new DjiDevice() { Sn = child.Sn, ParentSn = sn, Domain = child.Domain, Type = child.Type, SubType = child.SubType, ThingVersion = child.ThingVersion, Index = child.Index, Model = model.Name };
            await _db.Storageable(entity).WhereColumns(m => m.Sn).ToStorage().AsInsertable.ExecuteCommandAsync();
        }
        _sysCache.Set(sn.DeviceTopo(), data);
    }
    public async Task UpdateDeviceAirportBind(DeviceOrganization data)
    {
        var entity = await GetFullDeviceBySn(data.SN);
        if (entity == null)
        {
            await Insert(new DjiDevice() { Sn = data.SN, WorkspaceId = data.OrganizationId, Nick = data.DeviceCallsign, Binded = data.IsDeviceBindOrganization, BindTime = DateTime.Now });
        }
        else
        {
            entity.BindNum = entity.BindNum+1;
            entity.WorkspaceId = data.OrganizationId;
            entity.Nick = data.DeviceCallsign;
            entity.Binded = data.IsDeviceBindOrganization;
            await _deviceRes.UpdateAsync(entity);
        }
    }
}
