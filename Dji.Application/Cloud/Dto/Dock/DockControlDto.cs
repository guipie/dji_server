// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Core.Enum.DjiEnum.Ops;

namespace Dji.Application.Cloud.Dto.Dock;

/// <summary>
/// 机场控制指令的执行进度（<c>events</c> 上行，方法与下发同名）。
/// </summary>
/// <remarks>
/// <para>
/// 机场对每一条控制指令都会「先回 <c>services_reply</c>（表示收到），再持续推 <c>events</c>（表示做到哪了）」。
/// 两种报文的 data 结构不同：<c>services_reply</c> 只有 <c>result</c>，
/// <c>events</c> 则带 <c>output.status</c> 与 <c>output.progress</c>，本类对应后者。
/// </para>
/// <para>
/// <b>字段一律声明为可空</b>：不同指令的进度粒度差异很大 ——
/// <c>drone_close</c> 会给 <c>step_key</c>（检查市电、检查工作模式…），
/// 而 <c>charge_open</c> 可能只给一个百分比。用同一个宽松结构承载、缺失即忽略，
/// 比给每条指令写一个 DTO 更耐受固件差异。
/// </para>
/// </remarks>
public class DockCommandProgressData
{
    /// <summary>返回码（0 表示无错误）</summary>
    public int? Result { get; set; }

    /// <summary>输出</summary>
    public DockCommandOutput Output { get; set; }
}

/// <summary>控制指令输出结构</summary>
public class DockCommandOutput
{
    /// <summary>任务状态字符串（<c>sent</c> / <c>in_progress</c> / <c>ok</c> / <c>failed</c> …）</summary>
    public string Status { get; set; }

    /// <summary>进度信息</summary>
    public DockCommandProgress Progress { get; set; }
}

/// <summary>控制指令进度信息</summary>
public class DockCommandProgress
{
    /// <summary>百分比（0~100）</summary>
    public int? Percent { get; set; }

    /// <summary>当前步骤键（如 <c>check_work_mode</c>、<c>close_drone</c>），部分指令不提供</summary>
    public string StepKey { get; set; }

    /// <summary>当前步骤（部分指令用数值而非字符串，如 RTK 标定固定为 1）</summary>
    public string CurrentStep { get; set; }
}

/// <summary>
/// RTK 一键标定的结果上报（<c>rtk_calibration</c> 的 <c>events</c> 上行）。
/// </summary>
/// <remarks>
/// 与其它控制指令不同：标定是<b>按设备逐个出结果</b>的（机场本体 + 中继），
/// 因此 <c>output.ext.devices</c> 是数组，每个设备有独立的 <c>result</c> 与 <c>status</c>；
/// 只有全部设备成功，外层 <c>output.status</c> 才为 <c>ok</c>。
/// </remarks>
public class RtkCalibrationResultData
{
    /// <summary>固定为 0；真正的错误码在 <c>output.ext.devices[].result</c> 里</summary>
    public int? Result { get; set; }

    /// <summary>输出</summary>
    public RtkCalibrationOutput Output { get; set; }
}

/// <summary>
/// 机场控制指令的统一下发载荷。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么把这些互不相关的参数塞进一个类</b>：协议里每条带参指令的 <c>data</c> 只有一两个字段
/// （<c>action</c> / <c>link_workmode</c> / <c>imei</c> + <c>device_type</c> …），
/// 而序列化配置是 <c>NullValueHandling.Ignore</c>，未赋值的属性根本不会出现在报文里。
/// 因此用「一个松散载荷 + 只填必要字段」即可覆盖全部带参指令，
/// 不必为每条指令定义一个 DTO，也不必在服务里做 <c>object</c> 装箱。
/// </para>
/// <para>
/// <b>字段名到协议名的映射由命名策略自动完成</b>：<c>SnakeCaseNamingStrategy</c>
/// 会把 <c>LinkWorkmode</c> 写成 <c>link_workmode</c>、<c>DeviceType</c> 写成 <c>device_type</c>，
/// 与协议逐字一致，无需手工标注。
/// </para>
/// </remarks>
public class DockCommandPayload
{
    /// <summary>操作值（电池保养 / 声光报警 0-1；空调模式 0-3；电池运行模式 1-2）</summary>
    public int? Action { get; set; }

    /// <summary>图传链路模式（0 仅 SDR / 1 4G 增强）</summary>
    public int? LinkWorkmode { get; set; }

    /// <summary>Dongle IMEI</summary>
    public string Imei { get; set; }

    /// <summary>eSIM 目标设备（<c>dock</c> / <c>drone</c>）</summary>
    public string DeviceType { get; set; }

    /// <summary>SIM 卡槽（1 实体卡 / 2 eSIM）</summary>
    public int? SimSlot { get; set; }

    /// <summary>eSIM 运营商（1 移动 / 2 联通 / 3 电信）</summary>
    public int? EsimOperator { get; set; }

    /// <summary>是否至少填了一个字段（无参指令应下发 <c>data = null</c> 而不是空对象）</summary>
    public bool HasValue
        => Action.HasValue || LinkWorkmode.HasValue || SimSlot.HasValue || EsimOperator.HasValue
            || !Imei.IsNullOrWhiteSpace() || !DeviceType.IsNullOrWhiteSpace();
}

/// <summary>
/// RTK 一键标定的下发载荷。
/// </summary>
/// <remarks>
/// 标定是<b>逐设备</b>指定坐标的：机场本体（<c>module = "3"</c>）与中继（<c>module = "6"</c>）
/// 各占一个数组元素，坐标可取同一个标定点（实际部署中通常就是同一个已知点）。
/// </remarks>
public class RtkCalibrationPayload
{
    /// <summary>标定设备集合</summary>
    public List<RtkCalibrationDeviceInput> Devices { get; set; } = [];
}

/// <summary>单个待标定设备</summary>
public class RtkCalibrationDeviceInput
{
    /// <summary>设备 SN</summary>
    public string Sn { get; set; }

    /// <summary>标定类型（固定 1，表示手动标定）</summary>
    public int Type { get; set; } = (int)RtkCalibrationTypeEnum.Manual;

    /// <summary>模块（<c>"3"</c> 机场 / <c>"6"</c> 中继）</summary>
    public string Module { get; set; }

    /// <summary>标定点坐标</summary>
    public RtkCalibrationPoint Data { get; set; }
}

/// <summary>RTK 标定点（WGS84 经纬度 + 椭球高）</summary>
public class RtkCalibrationPoint
{
    /// <summary>经度</summary>
    public double Longitude { get; set; }

    /// <summary>纬度</summary>
    public double Latitude { get; set; }

    /// <summary>椭球高度（米）</summary>
    public double Height { get; set; }
}

/// <summary>RTK 标定输出</summary>
public class RtkCalibrationOutput
{
    /// <summary>附加信息</summary>
    public RtkCalibrationExt Ext { get; set; }

    /// <summary>整体进度（设备仅在有结果时推送，ok 时 100、failed 时 0）</summary>
    public DockCommandProgress Progress { get; set; }

    /// <summary>整体状态：全部设备成功为 <c>ok</c>，任一失败为 <c>failed</c></summary>
    public string Status { get; set; }
}

/// <summary>RTK 标定附加信息</summary>
public class RtkCalibrationExt
{
    /// <summary>各设备的标定结果</summary>
    public List<RtkCalibrationDeviceResult> Devices { get; set; } = [];

    /// <summary>标定版本号</summary>
    public int? Version { get; set; }
}

/// <summary>单设备 RTK 标定结果</summary>
public class RtkCalibrationDeviceResult
{
    /// <summary>设备 SN</summary>
    public string Sn { get; set; }

    /// <summary>标定类型（1 手动标定）</summary>
    public int? Type { get; set; }

    /// <summary>设备模块（<c>"3"</c> 机场 / <c>"6"</c> 中继）</summary>
    public string Module { get; set; }

    /// <summary>该设备的标定结果码（0 成功）</summary>
    public int? Result { get; set; }

    /// <summary>该设备的标定状态字符串</summary>
    public string Status { get; set; }
}
