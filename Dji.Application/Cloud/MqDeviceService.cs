
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
using Dji.Application.Cloud.Entity;
using Dji.Application.CloudRepository;

namespace Dji.Application.Cloud;
internal class MqDeviceService(ILogger<MqDeviceService> logger, DeviceRepository deviceRepository) : BaseModuleService
{
    //日志
    private readonly ILogger<MqDeviceService> _logger = logger;
    private readonly DeviceRepository _deviceRepository = deviceRepository;


    /// <summary>
    /// 机场osd数据
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    [MqttSubscribe(Topics.ThingProductOsd, DomainEnum.Dock)]
    public async Task DockOsdAsync(CloudMqData<DockOsd> data)
    {
        //Console.WriteLine("机场{0}坐标：{1}，{2}", data.Gateway, data.Data.Longitude, data.Data.Latitude);
        await _deviceRepository.BindDockOsd(data.Gateway, data.Data);
        await Task.Delay(1);
    }
    /// <summary>
    /// 无人机osd数据
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    [MqttSubscribe(Topics.ThingProductOsd, DomainEnum.Drone)]
    public async Task DroneOsdAsync(CloudMqData<DroneOsd> data)
    {

        await _deviceRepository.BindDroneOsd(data.Gateway, data.DroneSn, data.Data);
        Console.WriteLine("机场{0}坐标：{1}，{2}", data.Gateway, data.Data.Longitude, data.Data.Latitude);
        //await _deviceRepository.BindSn(data.Gateway);
        await Task.Delay(1);
    }

    /// <summary>
    /// 设备拓扑更新
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    [MqttSubscribe(Topics.ThingProductStatus, TopicMethods.UpdateTopo)]
    public async Task DeviceManageAsync(CloudMqData<UpdateTopoDevice> data)
    {
        Console.WriteLine("UpdateTop:{0} , data:{1}", data.Gateway, data.Data.ToJson());
        await _deviceRepository.BindTopo(data.Gateway, data.Data);
        await Task.Delay(1);
    }
}
