// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

namespace Dji.Application.Cloud.Device.Dto;
/// <summary>
/// 网关设备实体类
/// </summary>
public class UpdateTopoDevice
{
    /// <summary>
    /// 网关设备的命名空间
    /// 参考：https://developer.dji.com/doc/cloud-api-tutorial/cn/overview/product-support.html
    /// </summary>
    public string Domain { get; set; }

    /// <summary>
    /// 网关设备的产品类型
    /// 参考：https://developer.dji.com/doc/cloud-api-tutorial/cn/overview/product-support.html
    /// </summary>
    public int Type { get; set; }

    /// <summary>
    /// 网关子设备的产品子类型
    /// 参考：https://developer.dji.com/doc/cloud-api-tutorial/cn/overview/product-support.html
    /// </summary>
    public int SubType { get; set; }

    /// <summary>
    /// 网关设备的密钥
    /// </summary>
    public string DeviceSecret { get; set; }

    /// <summary>
    /// nonce
    /// </summary>
    public string Nonce { get; set; }

    /// <summary>
    /// 网关设备的物模型版本
    /// </summary>
    public string ThingVersion { get; set; }

    /// <summary>
    /// 子设备列表
    /// </summary>
    public List<UpdateTopoSubDevice> SubDevices { get; set; }
}

/// <summary>
/// 子设备实体类
/// </summary>
public class UpdateTopoSubDevice
{
    /// <summary>
    /// 子设备序列号（SN）
    /// </summary>
    public string Sn { get; set; }

    /// <summary>
    /// 子设备的命名空间
    /// 参考：https://developer.dji.com/doc/cloud-api-tutorial/cn/overview/product-support.html
    /// </summary>
    public string Domain { get; set; }

    /// <summary>
    /// 子设备的产品类型
    /// 参考：https://developer.dji.com/doc/cloud-api-tutorial/cn/overview/product-support.html
    /// </summary>
    public int Type { get; set; }

    /// <summary>
    /// 子设备的产品子类型
    /// 参考：https://developer.dji.com/doc/cloud-api-tutorial/cn/overview/product-support.html
    /// </summary>
    public int SubType { get; set; }

    /// <summary>
    /// 连接网关设备的通道索引
    /// </summary>
    public string Index { get; set; }

    /// <summary>
    /// 子设备的密钥
    /// </summary>
    public string DeviceSecret { get; set; }

    /// <summary>
    /// nonce
    /// </summary>
    public string Nonce { get; set; }

    /// <summary>
    /// 子设备的物模型版本
    /// </summary>
    public string ThingVersion { get; set; }
}
