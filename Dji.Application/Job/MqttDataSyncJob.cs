//// 麻省理工学院许可证
////
//// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
////
//// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
////
//// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
//// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Cloud.Core;
using Dji.Application.Cloud.Dto.Device;
using Dji.Application.Cloud.Dto.Org;
using Dji.Application.Cloud.Entity;
using Furion.Schedule;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dji.Application.Job;


[JobDetail("job_mqData_sync", Description = "数据同步", GroupName = "dji", Concurrent = false)]
[Minutely(TriggerId = "trigger_syncMqData", Description = "同步mq数据", MaxNumberOfRuns = 0, RunOnStart = true)]
public class MqttDataSyncJob : IJob
{
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
        Console.WriteLine("start sync mqtt data..");  
        var _publish = _serviceScope.ServiceProvider.GetRequiredService<MqttGatewayPublish>();  
        var allData = await _deviceRes.AsQueryable()
            .OrderBy(u => new { u.Sn }).ToTreeAsync(u => u.Children, u => u.ParentSn, null, u => u.Sn);
        _cache.RemoveByPrefixKey("".Device());
        foreach (var item in allData)
        {
            var curDockOsd = _cache.Get<DockOsd>(item.Sn.OsdOnline());
            if (curDockOsd != null && curDockOsd.Longitude > 0 && curDockOsd.Latitude > 0)
            {
                item.Longitude = curDockOsd.Longitude.Value;
                item.Latitude = curDockOsd.Latitude.Value;
                item.Altitude = curDockOsd.Height.Value;
                item.FirmwareVersion = curDockOsd.FirmwareVersion;
                await _deviceRes.UpdateAsync(item);
            }
            if (item.WorkspaceId.IsNullOrWhiteSpace()&&item.BindNum<10)
            {
                var data = new CommonTopicRequest<AirportBindStatusRequest>(TopicMethods.AirportBindStatus, new AirportBindStatusRequest() { Devices = [new DeviceSn() { Sn = item.Sn }] }, item.Sn);
                await _publish.PublishAsync<AirportBindStatusRequest>(Topics.ThingProductRequests, data);
            }
            foreach (var child in item.Children.IsEmptyDefault())
            {
                var curDevice = _cache.Get<DroneOsd>(child.Sn.OsdOnline());
                if (curDevice != null && curDevice.Longitude > 0 && curDevice.Latitude > 0)
                {
                    child.Longitude = curDevice.Longitude;
                    child.Latitude = curDevice.Latitude;
                    child.Altitude = curDevice.Height;
                    child.FirmwareVersion = curDevice.FirmwareVersion;
                    await _deviceRes.UpdateAsync(child);
                }
                if (child.WorkspaceId.IsNullOrWhiteSpace() && child.BindNum < 10)
                {
                    var data = new CommonTopicRequest<AirportBindStatusRequest>(TopicMethods.AirportBindStatus, new AirportBindStatusRequest() { Devices = [new DeviceSn() { Sn = child.Sn }] }, item.Sn);
                    await _publish.PublishAsync<AirportBindStatusRequest>(Topics.ThingProductRequests, data);
                }
            }
            _cache.Set(item.Sn.Device(), item);
        }
    }
}