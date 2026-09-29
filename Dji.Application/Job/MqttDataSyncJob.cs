// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Cloud.Core;
using Dji.Application.Cloud.Dto.Device;
using Dji.Application.Cloud.Dto.Org;
using Dji.Application.Cloud.Entity;
using Dji.Schedule;
using System.Linq;

namespace Dji.Application.Job;

/// <summary>
/// 设备运行态同步。
/// </summary>
/// <remarks>
/// MQTT 上行只写缓存，本任务周期性地把缓存中的实时状态（位置、固件版本、在线状态）落库，
/// 既避免高频 OSD 直接写库，也让前端刷新页面后仍有数据可读。
/// 与历史实现的差异：
/// <list type="bullet">
/// <item>不再每分钟 <c>RemoveByPrefixKey</c> 清空全部设备缓存（原本会造成周期性缓存击穿）；</item>
/// <item>绑定信息重发改为「仅在设备在线、未绑定、且重试次数未超上限」时发送，并真正累加重试次数，
/// 修复原先每分钟无限重发 <c>airport_bind_status</c> 的问题。</item>
/// </list>
/// </remarks>
[JobDetail("job_mqData_sync", Description = "设备运行态同步", GroupName = "dji", Concurrent = false)]
[Minutely(TriggerId = "trigger_syncMqData", Description = "同步设备运行态", MaxNumberOfRuns = 0, RunOnStart = false)]
public class MqttDataSyncJob : IJob
{
    /// <summary>绑定组织信息的最大重试次数</summary>
    private const int MaxBindRetry = 10;

    /// <summary>设备档案缓存时长，与 DeviceRepository 保持一致</summary>
    private static readonly TimeSpan DeviceCacheTtl = TimeSpan.FromSeconds(60);

    private readonly IServiceScope _serviceScope;
    private readonly SysCacheService _cache;
    private readonly ILogger<MqttDataSyncJob> _logger;
    private readonly SqlSugarRepository<DjiDevice> _deviceRes;

    public MqttDataSyncJob(IServiceScopeFactory scopeFactory)
    {
        _serviceScope = scopeFactory.CreateScope();
        _cache = _serviceScope.ServiceProvider.GetRequiredService<SysCacheService>();
        _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<MqttDataSyncJob>>();
        _deviceRes = _serviceScope.ServiceProvider.GetRequiredService<SqlSugarRepository<DjiDevice>>();
    }

    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        // 机场即顶层设备（ParentSn 为空），飞行器挂在机场下
        var docks = await _deviceRes.AsQueryable()
            .Where(m => m.ParentSn == null || m.ParentSn == "")
            .ToListAsync();
        if (docks.Count == 0) return;

        var dockSns = docks.Select(m => m.Sn).ToList();
        var drones = await _deviceRes.AsQueryable()
            .Where(m => dockSns.Contains(m.ParentSn))
            .ToListAsync();
        var dronesByDock = drones.GroupBy(m => m.ParentSn).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var dock in docks)
        {
            await SyncDockAsync(dock, stoppingToken);

            foreach (var drone in dronesByDock.GetValueOrDefault(dock.Sn, []))
            {
                await SyncDroneAsync(drone, stoppingToken);
            }
        }
    }

    /// <summary>同步单台机场的运行态</summary>
    private async Task SyncDockAsync(DjiDevice dock, CancellationToken ct)
    {
        var osd = _cache.Get<DockOsd>(dock.Sn.OsdOnline());
        var online = osd != null;
        var changed = SyncOnlineState(dock, online);

        if (osd != null)
        {
            if (osd.Longitude != null && osd.Latitude != null)
            {
                dock.Longitude = osd.Longitude;
                dock.Latitude = osd.Latitude;
                dock.Altitude = osd.Height;
                changed = true;
            }

            if (!osd.FirmwareVersion.IsNullOrWhiteSpace() && dock.FirmwareVersion != osd.FirmwareVersion)
            {
                dock.FirmwareVersion = osd.FirmwareVersion;
                changed = true;
            }
        }

        if (changed)
        {
            await _deviceRes.AsUpdateable(dock)
                .UpdateColumns(m => new { m.Longitude, m.Latitude, m.Altitude, m.FirmwareVersion, m.IsOnline, m.LastOnlineTime, m.LastOsdTime })
                .Where(m => m.Id == dock.Id)
                .ExecuteCommandAsync(ct);
            _logger.LogDebug("机场运行态已同步，sn:{Sn}，online:{Online}", dock.Sn, online);
        }

        await TryRequestBindStatusAsync(dock, online, ct);

        // 只刷新当前设备缓存，不再整体清除
        _cache.Set(dock.Sn.Device(), dock, DeviceCacheTtl);
    }

    /// <summary>同步单台飞行器的运行态</summary>
    private async Task SyncDroneAsync(DjiDevice drone, CancellationToken ct)
    {
        var osd = _cache.Get<DroneOsd>(drone.Sn.OsdOnline());
        var online = osd != null;
        var changed = SyncOnlineState(drone, online);

        if (osd != null)
        {
            // 机场/飞行器未定位成功时 OSD 上报的经纬度为 0，不能当作有效坐标写入，
            // 否则设备档案会被 0,0 覆盖掉最后一次有效位置。
            if (osd.Longitude != 0 && osd.Latitude != 0)
            {
                drone.Longitude = osd.Longitude;
                drone.Latitude = osd.Latitude;
                drone.Altitude = osd.Height;
                changed = true;
            }

            if (!osd.FirmwareVersion.IsNullOrWhiteSpace() && drone.FirmwareVersion != osd.FirmwareVersion)
            {
                drone.FirmwareVersion = osd.FirmwareVersion;
                changed = true;
            }
        }

        if (changed)
        {
            await _deviceRes.AsUpdateable(drone)
                .UpdateColumns(m => new { m.Longitude, m.Latitude, m.Altitude, m.FirmwareVersion, m.IsOnline, m.LastOnlineTime, m.LastOsdTime })
                .Where(m => m.Id == drone.Id)
                .ExecuteCommandAsync(ct);
        }

        await TryRequestBindStatusAsync(drone, online, ct);
    }

    /// <summary>按 OSD 缓存是否命中刷新在线状态</summary>
    private static bool SyncOnlineState(DjiDevice device, bool online)
    {
        if (device.IsOnline == online) return false;

        device.IsOnline = online;
        return true;
    }

    /// <summary>
    /// 设备在线但尚未绑定组织时，主动向机场查询绑定状态。
    /// </summary>
    /// <remarks>必须累加重试次数，否则会每分钟无限重发。</remarks>
    private async Task TryRequestBindStatusAsync(DjiDevice device, bool online, CancellationToken ct)
    {
        if (!online) return;
        if (!device.WorkspaceId.IsNullOrWhiteSpace()) return;
        if (device.BindNum >= MaxBindRetry) return;

        var publish = _serviceScope.ServiceProvider.GetRequiredService<MqttGatewayPublish>();
        var request = new CloudMqRequest<AirportBindStatusRequest>(
            TopicMethods.AirportBindStatus,
            new AirportBindStatusRequest { Devices = [new DeviceSn { Sn = device.Sn }] },
            device.Sn);

        await publish.PublishAsync(Topics.ThingProductRequests, request);

        // 累加重试次数，避免无限重发；绑定成功后 UpdateDeviceAirportBind 会再次 +1
        device.BindNum += 1;
        await _deviceRes.AsUpdateable(device)
            .UpdateColumns(m => m.BindNum)
            .Where(m => m.Id == device.Id)
            .ExecuteCommandAsync(ct);
    }
}
