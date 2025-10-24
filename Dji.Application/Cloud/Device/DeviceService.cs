<<<<<<< HEAD
﻿// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Cloud.Core;
using Dji.Application.Cloud.Device.Dto;
using Dji.Application.Cloud.Entity;

namespace Dji.Application.Cloud.Device;
=======
﻿// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Cloud.Core;
using Dji.Application.Cloud.Device.Dto;
using Dji.Application.Cloud.Entity;

namespace Dji.Application.Cloud.Device;
>>>>>>> d2f523d79261c8c09d05866fe056433422042b3e
internal class DeviceService(ILogger<DeviceService> logger) : BaseModuleService
{
    //日志
    private readonly ILogger<DeviceService> _logger = logger;


    //机场osd数据
    [MqttSubscribe(Topics.ThingProductOsd, 1)]
    public async Task DockOsdAsync(CloudMqData<DockOsd> data)
    {
        _logger.LogInformation("{0}坐标：{1}，{2}", data.Gateway, data.Data.Longitude, data.Data.Latitude);
        Console.WriteLine("机场{0}坐标：{1}，{2}", data.Gateway, data.Data.Longitude, data.Data.Latitude);
        await Task.Delay(1);
    }
    //无人机osd数据
    [MqttSubscribe(Topics.ThingProductOsd, 2)]
    public async Task DroneOsdAsync(CloudMqData<DockOsd> data)
    {
        Console.WriteLine("无人机{0}坐标：{1}，{2}", data.Gateway, data.Data.Longitude, data.Data.Latitude);
        await Task.Delay(1);
    }

    /// <summary>
    /// 设备拓扑更新
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
<<<<<<< HEAD
    [MqttSubscribe(Topics.ThingProductOsd)]
    public async Task DeviceManageAsync(CloudMqData<UpdateTopoDevice> data)
    {
        Console.WriteLine("{0}坐标：{1}，{2}", data.Gateway);
        await Task.Delay(1);
    }
}
=======
    [MqttSubscribe(Topics.ThingProductStatus, TopicMethods.UpdateTopo)]
    public async Task DeviceManageAsync(CloudMqData<UpdateTopoDevice> data)
    {
        Console.WriteLine("{0}", data.Gateway);
        await Task.Delay(1);
    }
}
>>>>>>> d2f523d79261c8c09d05866fe056433422042b3e
