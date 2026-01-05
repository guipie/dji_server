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
public class WaypointTurnParam
{
    /// <summary>
    /// 航点类型（航点转弯模式） (必需元素)
    /// </summary>
    public WaypointTurnModeEnum WaypointTurnMode { get; set; }

    /// <summary>
    /// 航点转弯截距 (单位: 米) (0, 航段最大长度] 注：两航点间航段长度必需大于两航点转弯截距之和。此元素定义了飞行器在距离该航点若干米前，提前多少距离转弯。(必需元素，当且仅当“wpml:waypointTurnMode”为“coordinateTurn”或“wpml:waypointTurnMode”为“toPointAndPassWithContinuityCurvature”，且“wpml:useStraightLine”为“1”时必需)
    /// </summary>
    public double? WaypointTurnDampingDist { get; set; }
}
/// <summary>
/// 航点转弯模式枚举
/// </summary>
public enum WaypointTurnModeEnum
{
    /// <summary>
    /// 协调转弯，不过点，提前转弯
    /// </summary>
    coordinateTurn,

    /// <summary>
    /// 直线飞行，飞行器到点停
    /// </summary>
    toPointAndStopWithDiscontinuityCurvature,

    /// <summary>
    /// 曲线飞行，飞行器到点停
    /// </summary>
    toPointAndStopWithContinuityCurvature,

    /// <summary>
    /// 曲线飞行，飞行器过点不停
    /// </summary>
    toPointAndPassWithContinuityCurvature,
}
