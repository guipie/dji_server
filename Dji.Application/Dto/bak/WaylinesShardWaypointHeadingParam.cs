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
/// 航点配置信息
/// </summary>
public class WaypointHeadingParam
{
    /// <summary>
    /// 飞行器偏航角模式 (必需元素)
    /// </summary>
    public WaypointHeadingModeEnum WaypointHeadingMode { get; set; }

    /// <summary>
    /// 飞行器偏航角度 [-180, 180] 给定某航点的目标偏航角，并在航段飞行过程中均匀过渡至下一航点的目标偏航角。(必需元素, 当且仅当“wpml:waypointHeadingMode”为“smoothTransition”时必需)
    /// </summary>
    public double? WaypointHeadingAngle { get; set; }

    /// <summary>
    /// 兴趣点 数据格式为：纬度,经度,高度 注：仅当wpml:waypointHeadingMode为towardPOI该字段生效。目前不支持Z方向朝向兴趣点，高度可设置为0。
    /// </summary>
    public string WaypointPoiPoint { get; set; }

    /// <summary>
    /// 飞行器偏航角转动方向 (必需元素)
    /// </summary>
    public WaypointHeadingPathModeEnum WaypointHeadingPathMode { get; set; }
}
/// <summary>
/// 飞行器偏航角模式枚举
/// </summary>
public enum WaypointHeadingModeEnum
{
    /// <summary>
    /// 沿航线方向。飞行器机头沿着航线方向飞至下一航点
    /// </summary>
    followWayline,

    /// <summary>
    /// 手动控制。飞行器在飞至下一航点的过程中，用户可以手动控制飞行器机头朝向
    /// </summary>
    manually,

    /// <summary>
    /// 锁定当前偏航角。飞行器机头保持执行完航点动作后的飞行器偏航角飞至下一航点
    /// </summary>
    @fixed,

    /// <summary>
    /// 自定义。通过“wpml:waypointHeadingAngle”给定某航点的目标偏航角，并在航段飞行过程中均匀过渡至下一航点的目标偏航角
    /// </summary>
    smoothTransition,

    /// <summary>
    /// 朝向兴趣点
    /// </summary>
    towardPOI,
}

/// <summary>
/// 飞行器偏航角转动方向枚举
/// </summary>
public enum WaypointHeadingPathModeEnum
{
    /// <summary>
    /// 顺时针旋转飞行器偏航角
    /// </summary>
    clockwise,

    /// <summary>
    /// 逆时针旋转飞行器偏航角
    /// </summary>
    counterClockwise,

    /// <summary>
    /// 沿最短路径旋转飞行器偏航角
    /// </summary>
    followBadArc,
}
