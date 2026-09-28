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

namespace Dji.Application.Service.DjiDevice;

/// <summary>
/// 机场云 API 服务（对设备侧 / 运营后台的 HTTP 入口）。
/// </summary>
// 注意：命名空间 Dji.Application.Service.DjiDevice 与实体类型 Dji.Core.Entity.DjiDevice 同名，
// 此处必须使用完全限定名，否则会被解析为命名空间。
public partial class DjiDeviceCloudService(
    MqttGatewayPublish gatewayPublish,
    SqlSugarRepository<Dji.Core.Entity.DjiDevice> deviceRepository,
    SysCacheService cache) : BaseCloudService
{
    private readonly MqttGatewayPublish _publish = gatewayPublish;
    private readonly SqlSugarRepository<Dji.Core.Entity.DjiDevice> _deviceRes = deviceRepository;
    private readonly SysCacheService _cache = cache;
}
