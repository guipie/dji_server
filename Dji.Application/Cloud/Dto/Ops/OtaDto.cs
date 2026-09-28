// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Core.Enum.DjiEnum.Ops;

namespace Dji.Application.Cloud.Dto.Ops;

/// <summary>
/// 固件升级下发载荷（<c>ota_create</c>）。
/// </summary>
/// <remarks>
/// <b>固件包信息是否必填取决于升级类型</b>：一致性升级（<c>2</c>）由设备自行从 DJI 服务器取包，
/// 只需给目标版本；普通升级（<c>3</c>）与 PSDK 升级（<c>4</c>）必须由云端提供完整包信息
/// （地址 / MD5 / 大小 / 文件名）。
/// </remarks>
public class OtaCreatePayload
{
    /// <summary>固件升级设备集合</summary>
    public List<OtaCreateDevice> Devices { get; set; } = [];
}

/// <summary>单个待升级设备</summary>
public class OtaCreateDevice
{
    /// <summary>设备序列号</summary>
    public string Sn { get; set; }

    /// <summary>目标固件版本（如 <c>1.00.223</c>）</summary>
    public string ProductVersion { get; set; }

    /// <summary>固件包下载地址（一致性升级可省略）</summary>
    public string FileUrl { get; set; }

    /// <summary>固件包 MD5</summary>
    public string Md5 { get; set; }

    /// <summary>固件包大小（字节）</summary>
    public long? FileSize { get; set; }

    /// <summary>固件包文件名</summary>
    public string FileName { get; set; }

    /// <summary>固件升级类型（2 一致性 / 3 普通 / 4 PSDK）</summary>
    public OtaUpgradeTypeEnum FirmwareUpgradeType { get; set; }
}

/// <summary>
/// 固件升级进度（<c>ota_progress</c>，<c>events</c> 上行）。
/// </summary>
/// <remarks>
/// <para>
/// <b>报文里没有设备 SN</b>：只有 <c>bid</c>、<c>result</c>、<c>output.status</c>
/// 与 <c>output.progress.{percent, current_step}</c>。也就是说设备上报的是<b>整批</b>的进度，
/// 云端无法区分「这一帧是机场还是飞行器」—— 这也是任务表按批次建模的直接原因。
/// </para>
/// <para>
/// 结构上与机场控制指令的进度报文完全一致（同样是 <c>result</c> + <c>output.status</c> + <c>output.progress</c>），
/// 因此直接复用 <see cref="Dji.Application.Cloud.Dto.Dock.DockCommandOutput"/> 作为 <see cref="Output"/> 的类型，
/// 不再复制一份同构结构。
/// </para>
/// </remarks>
public class OtaProgressData
{
    /// <summary>返回码（0 表示无错误）</summary>
    public int? Result { get; set; }

    /// <summary>输出（状态 + 进度）</summary>
    public Dji.Application.Cloud.Dto.Dock.DockCommandOutput Output { get; set; }
}
