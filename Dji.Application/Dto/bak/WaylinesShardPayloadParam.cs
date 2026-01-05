// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dji.Application.Dto.bak;




/// <summary>
/// 负载配置信息
/// </summary>
public class PayloadConfiguration
{
    /// <summary>
    /// 负载挂载位置（必需元素）请参考产品支持页面中的相机枚举值中type-subtype-gimbalindex的gimbalindex字段
    /// </summary>
    public int PayloadPositionIndex { get; set; }

    /// <summary>
    /// 负载对焦模式 (firstPoint: 首个航点自动对焦, custom: 标定对焦值对焦)
    /// </summary>
    public FocusModeEnum FocusMode { get; set; }

    /// <summary>
    /// 负载测光模式 (average: 全局测光, spot: 点测光)
    /// </summary>
    public MeteringModeEnum MeteringMode { get; set; }

    /// <summary>
    /// 是否开启畸变矫正 (0: 不开启, 1: 开启)
    /// </summary>
    public bool DewarpingEnable { get; set; }

    /// <summary>
    /// 激光雷达回波模式 (singleReturnStrongest: 单回波, dualReturn: 双回波, tripleReturn: 三回波)
    /// </summary>
    public ReturnModeEnum ReturnMode { get; set; }

    /// <summary>
    /// 负载采样率 (60000, 80000, 120000, 160000, 180000, 240000 Hz)
    /// </summary>
    public int SamplingRate { get; set; }

    /// <summary>
    /// 负载扫描模式 (repetitive: 重复扫描, nonRepetitive: 非重复扫描)
    /// </summary>
    public ScanningModeEnum ScanningMode { get; set; }

    /// <summary>
    /// 真彩上色 (0: 不上色, 1: 真彩上色)
    /// </summary>
    public bool ModelColoringEnable { get; set; }

    /// <summary>
    /// 图片格式列表 (wide: 存储广角镜头照片, zoom: 存储变焦镜头照片, ir: 存储红外镜头照片, narrow_band: 存储窄带镜头拍摄照片, visable: 可见光照片)
    /// </summary>
    public List<ImageFormatEnum> ImageFormat { get; set; }
}

/// <summary>
/// 负载对焦模式枚举
/// </summary>
public enum FocusModeEnum
{
    /// <summary>
    /// 首个航点自动对焦
    /// </summary>
    firstPoint,

    /// <summary>
    /// 标定对焦值对焦
    /// </summary>
    custom,
}

/// <summary>
/// 负载测光模式枚举
/// </summary>
public enum MeteringModeEnum
{
    /// <summary>
    /// 全局测光
    /// </summary>
    average,

    /// <summary>
    /// 点测光
    /// </summary>
    spot,
}

/// <summary>
/// 激光雷达回波模式枚举
/// </summary>
public enum ReturnModeEnum
{
    /// <summary>
    /// 单回波
    /// </summary>
    singleReturnStrongest,

    /// <summary>
    /// 双回波
    /// </summary>
    dualReturn,

    /// <summary>
    /// 三回波
    /// </summary>
    tripleReturn,
}

/// <summary>
/// 负载扫描模式枚举
/// </summary>
public enum ScanningModeEnum
{
    /// <summary>
    /// 重复扫描
    /// </summary>
    repetitive,

    /// <summary>
    /// 非重复扫描
    /// </summary>
    nonRepetitive,
}

/// <summary>
/// 图片格式枚举
/// </summary>
public enum ImageFormatEnum
{
    /// <summary>
    /// 存储广角镜头照片
    /// </summary>
    wide,

    /// <summary>
    /// 存储变焦镜头照片
    /// </summary>
    zoom,

    /// <summary>
    /// 存储红外镜头照片
    /// </summary>
    ir,

    /// <summary>
    /// 存储窄带镜头拍摄照片
    /// </summary>
    narrow_band,

    /// <summary>
    /// 可见光照片
    /// </summary>
    visable,
}
