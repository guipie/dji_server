// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Cloud.Dto.Device;
using System.Linq;

namespace Dji.Application.Service.DjiDevice;

/// <summary>
/// 设备在线快照：供运营后台首屏渲染，避免只依赖 SignalR 实时推送。
/// </summary>
public partial class DjiDeviceCloudService
{
    /// <summary>
    /// 获取机场（及可选飞行器）在线快照。
    /// </summary>
    /// <remarks>
    /// 前端「在线机场」页在 <c>onMounted</c> 时先调用本接口完成首屏渲染，之后再由 SignalR 增量更新。
    /// 历史实现只有实时推送，页面刷新/断线重连期间会一片空白。
    /// </remarks>
    /// <param name="input">可按工作空间过滤</param>
    [HttpPost]
    [ApiDescriptionSettings(Name = "OnlineSnapshots")]
    public async Task<List<DockOnlineSnapshot>> OnlineSnapshots(DjiDeviceInput input)
    {
        var docks = await _deviceRes.AsQueryable()
            .Where(m => m.ParentSn == null || m.ParentSn == "")
            .WhereIF(!input.WorkspaceId.IsNullOrWhiteSpace(), m => m.WorkspaceId == input.WorkspaceId)
            .ToListAsync();

        if (docks.Count == 0) return [];

        var snapshots = new List<DockOnlineSnapshot>(docks.Count);
        foreach (var dock in docks)
        {
            snapshots.Add(new DockOnlineSnapshot
            {
                Sn = dock.Sn,
                Nick = dock.Nick,
                Model = dock.Model,
                WorkspaceId = dock.WorkspaceId,
                Longitude = dock.Longitude,
                Latitude = dock.Latitude,
                IsOnline = dock.IsOnline,
                LastOnlineTime = dock.LastOnlineTime,
                // OSD 缓存 TTL 为 60 秒，命中即视为在线并返回实时快照
                Osd = _cache.Get<DockOsd>(dock.Sn.OsdOnline()),
            });
        }

        return snapshots;
    }
}

/// <summary>机场在线快照</summary>
public class DockOnlineSnapshot
{
    /// <summary>机场 SN</summary>
    public string Sn { get; set; }

    /// <summary>昵称</summary>
    public string Nick { get; set; }

    /// <summary>型号</summary>
    public string Model { get; set; }

    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>经度</summary>
    public double? Longitude { get; set; }

    /// <summary>纬度</summary>
    public double? Latitude { get; set; }

    /// <summary>是否在线</summary>
    public bool IsOnline { get; set; }

    /// <summary>最近在线时间</summary>
    public DateTime? LastOnlineTime { get; set; }

    /// <summary>实时 OSD 快照；离线超过缓存时长时为 null</summary>
    public DockOsd Osd { get; set; }
}
