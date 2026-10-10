// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Cloud.Dto.Device;
using Dji.Core.Service;

namespace Dji.Application.Util;

/// <summary>
/// 设备在线状态判定扩展。
/// </summary>
/// <remarks>
/// 在线状态的<b>唯一口径</b>是「OSD 缓存是否命中」：
/// OSD 为 0.5Hz 定频上报（每 2 秒一条），机场 OSD 缓存 TTL 为 5 秒、飞行器为 10 秒，
/// 缓存命中即在线上报周期内，命中为在线、过期未命中为离线。
/// <b>不使用</b>落库的 <c>DjiDevice.IsOnline</c> 字段 —— 它是分钟级任务异步同步的，存在滞后与误判。
/// 所有入口（控制面板、下拉选项、各业务列表）统一走这里，避免散落重复的 <c>Get&lt;DockOsd&gt;(...)</c> 判断。
/// </remarks>
[SuppressSniffer]
public static class OnlineStateExtension
{
    /// <summary>机场是否在线（以 OSD 缓存命中为准）</summary>
    public static bool IsDockOnline(this SysCacheService cache, string sn)
        => !sn.IsNullOrWhiteSpace() && cache.Get<DockOsd>(sn.OsdOnline()) != null;

    /// <summary>飞行器是否在线（以 OSD 缓存命中为准）</summary>
    public static bool IsDroneOnline(this SysCacheService cache, string sn)
        => !sn.IsNullOrWhiteSpace() && cache.Get<DroneOsd>(sn.OsdOnline()) != null;
}
