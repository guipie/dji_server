// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Cloud.Core;
using Dji.Application.Cloud.Dto.Org;
using Dji.Application.Cloud.Entity;
using Dji.Application.CloudRepository;

namespace Dji.Application.Cloud;
internal class MqOrgService(ILogger<MqOrgService> logger, DeviceRepository deviceRepository) : BaseModuleService
{
    //日志
    private readonly ILogger<MqOrgService> _logger = logger;
    private readonly DeviceRepository _deviceRepository = deviceRepository;
    /// <summary>
    /// 获取设备绑定信息
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    [MqttSubscribe(Topics.ThingProductRequestsReply, TopicMethods.AirportBindStatus)]
    public async Task AirportBindStatusReplyAsync(CloudMqData<MqOutput<AirportBindStatusRequestReply>> data)
    {
        foreach (var bind in data.Data.Output.BindStatus)
        {
            Console.WriteLine("设备SN:{0},是否绑定组织:{1},组织ID:{2},组织 名称:{3}", bind.SN, bind.IsDeviceBindOrganization, bind.OrganizationId, bind.OrganizationName);
            await _deviceRepository.UpdateDeviceAirportBind(bind);
        }
        await Task.Delay(1);
    }
    /// <summary>
    /// 设备绑定到组织
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    [MqttSubscribe(Topics.ThingProductRequests, TopicMethods.AirportOrganizationBind)]
    public async Task AirportOrganizationBindAsync(CloudMqData<AirportOrganizationBindRequest> data)
    {
        _logger.LogInformation("收到机场组织绑定请求,请求时间:{0},请求数据:{1}", data.TimeStamp, data.ToJson());
        foreach (var item in data.Data.BindDevices)
        {
            await _deviceRepository.UpdateDeviceAirportBind(new DeviceOrganization()
            {
                DeviceCallsign = item.DeviceCallsign,
                IsDeviceBindOrganization = true,
                OrganizationId = item.OrganizationId,
                SN = item.SN,
            });
        }
        await Task.Delay(1);
    }
}
