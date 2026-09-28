// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Cloud.Dto.Live;
using Dji.Core.Enum.DjiEnum.Live;
using Furion.JsonSerialization;
using System.Linq;

namespace Dji.Application.CloudRepository;

/// <summary>
/// 直播仓库：机场状态快照的局部更新 + 直播会话生命周期。
/// </summary>
/// <remarks>
/// <para>
/// <b>本类最容易被写错的地方是状态快照的更新方式</b>。机场的 <c>state</c> 报文是「分多条推送」的，
/// 一条可能只带 <c>live_capacity</c>、另一条只带 <c>live_status</c>。
/// 若用 <c>Storageable</c> 或「整行覆盖」写入，后到的那条会把先到的字段冲成 null，
/// 表现为「直播能力偶尔消失」。因此 <see cref="UpsertStateAsync"/> 严格<b>逐字段判空后只更新非空列</b>。
/// </para>
/// <para>
/// <b>会话状态的真值来源是设备</b>：<see cref="SyncSessionFromLiveStatusAsync"/> 按设备上报的
/// <c>live_status</c> 校正本地会话（含清理设备侧已断流但本地仍显示「直播中」的幽灵会话）。
/// </para>
/// <para>
/// <b>为什么是 public</b>：需被公开的动态 API 服务 <c>DjiLiveService</c> 注入，否则触发 CS0051。
/// </para>
/// </remarks>
public class DjiLiveRepository(
    SqlSugarRepository<DjiDockState> stateRes,
    SqlSugarRepository<DjiLiveStream> streamRes,
    ILogger<DjiLiveRepository> logger) : BaseRepository
{
    private readonly SqlSugarRepository<DjiDockState> _stateRes = stateRes;
    private readonly SqlSugarRepository<DjiLiveStream> _streamRes = streamRes;
    private readonly ILogger<DjiLiveRepository> _logger = logger;

    /// <summary>协议 <c>live_status.status</c>：在直播</summary>
    private const int LiveStatusOn = 1;

    /// <summary>
    /// 已结束 / 未启动的会话状态集合（用于「唯一进行中会话」判定）
    /// </summary>
    private static readonly LiveStreamStatusEnum[] OpenStatuses =
        [LiveStreamStatusEnum.Starting, LiveStreamStatusEnum.Live];

    #region 状态快照

    /// <summary>取机场状态快照</summary>
    public async Task<DjiDockState> GetStateAsync(string dockSn)
        => dockSn.IsNullOrWhiteSpace() ? null : await _stateRes.GetFirstAsync(m => m.DockSn == dockSn);

    /// <summary>
    /// 局部更新机场状态快照。
    /// </summary>
    /// <param name="dockSn">机场 SN</param>
    /// <param name="state">协议 <c>state</c> 载荷（仅声明本平台关心的字段）</param>
    /// <param name="reportTimestamp">协议报文时间戳（毫秒）</param>
    /// <returns>是否有内容被更新（三个兴趣字段都没带时为 false，此时不写库）</returns>
    /// <remarks>
    /// <b>只更新报文中实际出现的字段</b>：三个兴趣字段（直播能力 / 直播状态 / 待上传数）
    /// 各自独立判空，未出现的保持原值。这样无论机场把状态拆成几条推送，最终都能拼出完整视图。
    /// </remarks>
    public async Task<bool> UpsertStateAsync(string dockSn, DockStateData state, long reportTimestamp)
    {
        if (dockSn.IsNullOrWhiteSpace() || state == null) return false;

        var hasCapacity = state.LiveCapacity != null;
        var hasLiveStatus = state.LiveStatus != null;
        var hasRemainUpload = state.MediaFileDetail != null;

        // 该条 state 与直播/媒体都无关（机场的 state 里还有几十个其它字段），直接跳过避免无谓写库
        if (!hasCapacity && !hasLiveStatus && !hasRemainUpload) return false;

        var existing = await GetStateAsync(dockSn);

        if (existing == null)
        {
            await _stateRes.AsInsertable(new DjiDockState
            {
                DockSn = dockSn,
                AvailableVideoNumber = state.LiveCapacity?.AvailableVideoNumber,
                CoexistVideoNumberMax = state.LiveCapacity?.CoexistVideoNumberMax,
                CapacityJson = hasCapacity ? JSON.Serialize(state.LiveCapacity) : null,
                LiveStatusJson = hasLiveStatus ? JSON.Serialize(state.LiveStatus) : null,
                RemainUpload = state.MediaFileDetail?.RemainUpload,
                ReportTimestamp = reportTimestamp,
                ReceivedTime = DateTime.Now,
            }).ExecuteCommandAsync();

            _logger.LogInformation("机场 {DockSn} 首次上报状态快照（直播能力:{HasCapacity}，直播状态:{HasLive}）",
                dockSn, hasCapacity, hasLiveStatus);

            // 首建即视为消费，会话校正交给后续上报
            if (hasLiveStatus) await SyncSessionFromLiveStatusAsync(dockSn, state.LiveStatus);
            return true;
        }

        // 逐字段合并：未出现的字段保持原值，避免分条推送互相覆盖
        if (hasCapacity)
        {
            existing.AvailableVideoNumber = state.LiveCapacity.AvailableVideoNumber;
            existing.CoexistVideoNumberMax = state.LiveCapacity.CoexistVideoNumberMax;
            existing.CapacityJson = JSON.Serialize(state.LiveCapacity);
        }

        if (hasLiveStatus) existing.LiveStatusJson = JSON.Serialize(state.LiveStatus);
        if (hasRemainUpload) existing.RemainUpload = state.MediaFileDetail.RemainUpload;

        existing.ReportTimestamp = reportTimestamp;
        existing.ReceivedTime = DateTime.Now;

        await _stateRes.AsUpdateable(existing)
            .UpdateColumns(m => new
            {
                m.AvailableVideoNumber,
                m.CoexistVideoNumberMax,
                m.CapacityJson,
                m.LiveStatusJson,
                m.RemainUpload,
                m.ReportTimestamp,
                m.ReceivedTime,
            })
            .Where(m => m.DockSn == dockSn)
            .ExecuteCommandAsync();

        if (hasLiveStatus) await SyncSessionFromLiveStatusAsync(dockSn, state.LiveStatus);

        return true;
    }

    /// <summary>设备离线时清理状态快照（避免前端展示过期能力）</summary>
    public async Task ClearStateAsync(string dockSn)
    {
        if (dockSn.IsNullOrWhiteSpace()) return;
        await _stateRes.DeleteAsync(m => m.DockSn == dockSn);
    }

    #endregion

    #region 会话查询

    /// <summary>取进行中的会话（Starting / Live）</summary>
    public async Task<List<DjiLiveStream>> GetOpenSessionsAsync(string dockSn)
    {
        if (dockSn.IsNullOrWhiteSpace()) return [];
        return await _streamRes.AsQueryable()
            .Where(m => m.DockSn == dockSn && OpenStatuses.Contains(m.Status))
            .OrderBy(m => m.CreateTime, OrderByType.Desc)
            .ToListAsync();
    }

    /// <summary>按码流标识取进行中的会话（同码流只允许一个）</summary>
    public async Task<DjiLiveStream> GetOpenSessionByVideoIdAsync(string videoId)
    {
        if (videoId.IsNullOrWhiteSpace()) return null;
        return await _streamRes.AsQueryable()
            .Where(m => m.VideoId == videoId && OpenStatuses.Contains(m.Status))
            .OrderBy(m => m.CreateTime, OrderByType.Desc)
            .FirstAsync();
    }

    /// <summary>按主键取会话</summary>
    public async Task<DjiLiveStream> GetSessionAsync(long id)
        => id > 0 ? await _streamRes.GetFirstAsync(m => m.Id == id) : null;

    /// <summary>取全部超时的进行中会话（供后台任务收口）</summary>
    public async Task<List<DjiLiveStream>> GetTimeoutSessionsAsync(DateTime deadline)
    {
        return await _streamRes.AsQueryable()
            .Where(m => OpenStatuses.Contains(m.Status) && m.StartTime != null && m.StartTime < deadline)
            .ToListAsync();
    }

    /// <summary>
    /// 取长时间停留在「启动中」的会话。
    /// </summary>
    /// <remarks>
    /// 场景：指令下发后机场既不回包也不推流（离线、图传被占用、编码器异常）。
    /// 这类会话既不会超时（<c>StartTime</c> 为空，不算「已开播」），也不会被设备状态校正
    /// （设备根本没上报 live_status），若不收口会永久占用并发名额。因此以 <c>CreateTime</c> 为基准判定。
    /// </remarks>
    public async Task<List<DjiLiveStream>> GetStaleStartingSessionsAsync(DateTime deadline)
    {
        return await _streamRes.AsQueryable()
            .Where(m => m.Status == LiveStreamStatusEnum.Starting && m.CreateTime < deadline)
            .ToListAsync();
    }

    /// <summary>取所有机场的进行中会话（后台任务用）</summary>
    public async Task<List<DjiLiveStream>> GetAllOpenSessionsGlobalAsync()
    {
        return await _streamRes.AsQueryable()
            .Where(m => OpenStatuses.Contains(m.Status))
            .ToListAsync();
    }

    #endregion

    #region 会话写入

    /// <summary>新建会话</summary>
    public async Task<DjiLiveStream> CreateSessionAsync(DjiLiveStream session)
    {
        if (session == null || session.DockSn.IsNullOrWhiteSpace()) return null;
        await _streamRes.AsInsertable(session).ExecuteCommandAsync();
        return session;
    }

    /// <summary>
    /// 更新会话状态。
    /// </summary>
    /// <param name="sessionId">会话主键</param>
    /// <param name="status">目标状态</param>
    /// <param name="result">指令返回码</param>
    /// <param name="errorMessage">失败原因</param>
    /// <param name="stopWhenClosed">是否在终态时写入停播时间</param>
    public async Task UpdateSessionStatusAsync(long sessionId, LiveStreamStatusEnum status,
        int result = 0, string errorMessage = null, bool stopWhenClosed = true)
    {
        if (sessionId <= 0) return;

        var session = await GetSessionAsync(sessionId);
        if (session == null) return;

        if (session.Status == status && result == session.LastResult) return;

        session.Status = status;
        session.LastResult = result;
        session.ErrorMessage = errorMessage;
        session.LastActiveTime = DateTime.Now;

        if (status == LiveStreamStatusEnum.Live && session.StartTime == null) session.StartTime = DateTime.Now;

        var isClosed = status is LiveStreamStatusEnum.Stopped or LiveStreamStatusEnum.Failed;
        if (isClosed && stopWhenClosed) session.StopTime = DateTime.Now;

        await _streamRes.AsUpdateable(session)
            .UpdateColumns(m => new { m.Status, m.LastResult, m.ErrorMessage, m.LastActiveTime, m.StartTime, m.StopTime })
            .Where(m => m.Id == sessionId)
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 按设备上报的 <c>live_status</c> 校正在途会话。
    /// </summary>
    /// <remarks>
    /// <para>三类校正：</para>
    /// <list type="number">
    /// <item>设备侧 <c>status = 1</c> 且本地会话还在「启动中」→ 置为「直播中」（指令已生效，只是回包可能丢）；</item>
    /// <item>设备侧 <c>status = 0</c> 且本地会话是「直播中」→ 置为「已停止」（设备侧已断流，常见于掉线或主动停播）；</item>
    /// <item>设备侧有、本地没有的码流 → 不建会话，只记日志（可能是另一端云平台发起的推流，本平台不应认领）。</item>
    /// </list>
    /// <para>
    /// <b>为什么以设备为准</b>：会话的最终事实只有设备知道；云端若坚持自己的状态，
    /// 会出现「页面显示在播、实际没画面」这类无法自证的假象。
    /// </para>
    /// </remarks>
    public async Task SyncSessionFromLiveStatusAsync(string dockSn, List<LiveStatusItem> liveStatus)
    {
        if (dockSn.IsNullOrWhiteSpace() || liveStatus == null) return;

        foreach (var item in liveStatus)
        {
            if (item.VideoId.IsNullOrWhiteSpace()) continue;

            var session = await GetOpenSessionByVideoIdAsync(item.VideoId);
            if (session == null) continue;

            // 设备侧在播，本地同步为直播中，并回填设备侧真实清晰度/镜头
            if (item.Status == LiveStatusOn)
            {
                session.VideoQuality = (LiveVideoQualityEnum)item.VideoQuality;
                if (!item.VideoType.IsNullOrWhiteSpace()) session.VideoType = item.VideoType;

                if (session.Status == LiveStreamStatusEnum.Starting)
                {
                    await _streamRes.AsUpdateable(session)
                        .UpdateColumns(m => new { m.Status, m.VideoQuality, m.VideoType, m.StartTime, m.LastActiveTime })
                        .Where(m => m.Id == session.Id)
                        .ExecuteCommandAsync();
                    _logger.LogInformation("直播已由设备侧确认在播，video_id:{VideoId}", item.VideoId);
                }
                else
                {
                    session.LastActiveTime = DateTime.Now;
                    await _streamRes.AsUpdateable(session)
                        .UpdateColumns(m => new { m.VideoQuality, m.VideoType, m.LastActiveTime })
                        .Where(m => m.Id == session.Id)
                        .ExecuteCommandAsync();
                }

                continue;
            }

            // 设备侧已不在播，但本地仍标记在途 → 收口为已停止，避免幽灵直播
            if (session.Status is LiveStreamStatusEnum.Starting or LiveStreamStatusEnum.Live)
            {
                var reason = item.ErrorStatus != 0 ? $"设备侧已断流（error_status={item.ErrorStatus}）" : "设备侧已停止推流";
                await UpdateSessionStatusAsync(session.Id, LiveStreamStatusEnum.Stopped, 0, reason);
                _logger.LogWarning("会话与设备状态不一致，已收口为已停止，video_id:{VideoId}，原因:{Reason}",
                    item.VideoId, reason);
            }
        }
    }

    #endregion
}
