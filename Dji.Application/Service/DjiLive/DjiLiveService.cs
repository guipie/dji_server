// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
//
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Cloud.Core;
using Furion.JsonSerialization;
using Dji.Application.Cloud.Dto.Live;
using Dji.Application.Cloud.Entity;
using Dji.Application.CloudRepository;
using Dji.Application.Option;
using Dji.Application.Service.DjiLive.Dto;
using Dji.Core.Entity.DjiEntity;
using Dji.Core.Enum.DjiEnum.Live;
using Microsoft.Extensions.Options;
using System.Linq;

namespace Dji.Application.Service.DjiLive;

/// <summary>
/// 直播服务（直播中心）。
/// </summary>
/// <remarks>
/// <para>
/// <b>架构</b>：本项目采用<b>自建 SRS</b>。云端只做信令 ——
/// 把流媒体地址通过 <c>live_start_push</c> 下发给机场，机场把码流推到 SRS，
/// 浏览器再从 SRS 拉 HTTP-FLV / HLS。<b>码流不经过本服务</b>。
/// </para>
/// <para>
/// <b>与上行处理的分层</b>：设备上报的直播能力（<c>live_capacity</c>）与在播状态（<c>live_status</c>）
/// 由 <c>MqLiveService</c> 落库到 <c>dji_dock_state</c>；本类只<b>读</b>该快照并负责下发指令、
/// 维护会话（<c>dji_live_stream</c>），不解析上行报文。
/// </para>
/// <para>
/// <b>为什么必须先拉通道再开播</b>：<c>live_start_push</c> 的 <c>video_id</c> 是
/// <c>{sn}/{camera_index}/{video_index}</c> 三段结构，取值完全由设备决定，
/// 云端无法凭规则推导（不同机型/负载的子类型号与相机索引都不同）。
/// 因此前端流程固定为「拉通道 → 选一路 → 开播」，服务端对传入的 video_id 做白名单校验。
/// </para>
/// </remarks>
[ApiDescriptionSettings(ApplicationConst.GroupName, Order = 110)]
public class DjiLiveService : IDynamicApiController, ITransient
{
    /// <summary>直播指令回包等待时长（机场需切换编码器并建立推流，比普通指令慢）</summary>
    private const int CommandReplyTimeoutSeconds = 20;

    private readonly SqlSugarRepository<DjiLiveStream> _streamRep;
    private readonly SqlSugarRepository<Dji.Core.Entity.DjiDevice> _deviceRep;
    private readonly DjiLiveRepository _liveRepository;
    private readonly MqttGatewayPublish _publish;
    private readonly UserManager _userManager;
    private readonly LiveOptions _liveOptions;
    private readonly ILogger<DjiLiveService> _logger;

    public DjiLiveService(
        SqlSugarRepository<DjiLiveStream> streamRep,
        SqlSugarRepository<Dji.Core.Entity.DjiDevice> deviceRep,
        DjiLiveRepository liveRepository,
        MqttGatewayPublish publish,
        UserManager userManager,
        IOptions<DjiOptions> options,
        ILogger<DjiLiveService> logger)
    {
        _streamRep = streamRep;
        _deviceRep = deviceRep;
        _liveRepository = liveRepository;
        _publish = publish;
        _userManager = userManager;
        _liveOptions = options.Value.Live ?? new LiveOptions();
        _logger = logger;
    }

    #region 查询

    /// <summary>分页查询直播会话</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Page")]
    public async Task<SqlSugarPagedList<LiveSessionOutput>> Page(LiveSessionSearchInput input)
    {
        LiveStreamStatusEnum[] active = [LiveStreamStatusEnum.Starting, LiveStreamStatusEnum.Live];

        var query = _streamRep.AsQueryable()
            .WhereIF(!input.WorkspaceId.IsNullOrWhiteSpace(), m => m.WorkspaceId == input.WorkspaceId)
            .WhereIF(!input.DockSn.IsNullOrWhiteSpace(), m => m.DockSn == input.DockSn)
            .WhereIF(input.Status.HasValue, m => m.Status == input.Status)
            .WhereIF(input.OnlyActive == true, m => active.Contains(m.Status))
            .OrderBy(m => m.CreateTime, OrderByType.Desc);

        var paged = await query.Select(m => new LiveSessionOutput
        {
            Id = m.Id,
            WorkspaceId = m.WorkspaceId,
            DockSn = m.DockSn,
            VideoSn = m.VideoSn,
            VideoId = m.VideoId,
            CameraIndex = m.CameraIndex,
            VideoIndex = m.VideoIndex,
            VideoType = m.VideoType,
            UrlType = m.UrlType,
            VideoQuality = m.VideoQuality,
            StreamName = m.StreamName,
            PushUrl = m.PushUrl,
            PlayUrl = m.PlayUrl,
            HlsUrl = m.HlsUrl,
            Status = m.Status,
            LastResult = m.LastResult,
            ErrorMessage = m.ErrorMessage,
            StartTime = m.StartTime,
            StopTime = m.StopTime,
            LastActiveTime = m.LastActiveTime,
            OperatorName = m.OperatorName,
            CreateTime = m.CreateTime,
        }).ToPagedListAsync(input.Page, input.PageSize);

        return await FillNamesAsync(paged);
    }

    /// <summary>会话详情</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "Detail")]
    public async Task<LiveSessionOutput> Detail([FromQuery] LiveControlInput input)
    {
        var session = await GetSessionAsync(input.Id);
        var output = session.Adapt<LiveSessionOutput>();
        var list = new List<LiveSessionOutput> { output };
        var filled = await FillNamesAsync(list);
        return filled[0];
    }

    /// <summary>
    /// 机场直播状态（直播中心主视图）。
    /// </summary>
    /// <remarks>返回通道树、在播状态、并发上限，以及「服务端是否已配置直播」的判定结果。</remarks>
    [HttpPost]
    [ApiDescriptionSettings(Name = "DockState")]
    public async Task<LiveDockStateOutput> DockState(LiveChannelInput input)
    {
        var dock = await ResolveDockAsync(input.DockSn);
        var output = new LiveDockStateOutput
        {
            DockSn = dock.Sn,
            DockNick = dock.Nick,
            IsOnline = dock.IsOnline,
        };

        var (enabled, reason) = CheckLiveConfigured();
        output.LiveEnabled = enabled;
        output.DisabledReason = reason;

        var state = await _liveRepository.GetStateAsync(dock.Sn);
        if (state == null)
        {
            // 设备尚未上报 live_capacity（未联网 / 固件较早），此时无法开播
            output.DisabledReason ??= "机场尚未上报直播能力，请确认机场在线并已连接本平台（能力数据在设备状态变化时推送）";
            return output;
        }

        output.HasCapacity = !state.CapacityJson.IsNullOrWhiteSpace();
        output.AvailableVideoNumber = state.AvailableVideoNumber ?? 0;
        output.CoexistVideoNumberMax = state.CoexistVideoNumberMax ?? 0;
        output.RemainUpload = state.RemainUpload ?? 0;
        output.UpdateTime = state.ReceivedTime;
        output.Channels = await BuildChannelsAsync(dock.Sn, state);
        output.LiveCount = output.Channels.Count(m => m.IsLive);

        return output;
    }

    /// <summary>可直播机场下拉（含是否有在途直播）</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "DockOptions")]
    public async Task<List<LiveDockOptionOutput>> DockOptions(LiveDockOptionInput input)
    {
        var docks = await _deviceRep.AsQueryable()
            .Where(m => m.Domain == DomainEnum.Dock)
            .WhereIF(!input.WorkspaceId.IsNullOrWhiteSpace(), m => m.WorkspaceId == input.WorkspaceId)
            .OrderBy(m => m.Nick)
            .ToListAsync();

        LiveStreamStatusEnum[] active = [LiveStreamStatusEnum.Starting, LiveStreamStatusEnum.Live];
        var activeDocks = await _streamRep.AsQueryable()
            .Where(m => active.Contains(m.Status))
            .Select(m => m.DockSn)
            .ToListAsync();

        return docks.Select(m => new LiveDockOptionOutput
        {
            Sn = m.Sn,
            Nick = m.Nick,
            WorkspaceId = m.WorkspaceId,
            IsOnline = m.IsOnline,
            HasActiveSession = activeDocks.Contains(m.Sn),
            Label = m.Nick.IsNullOrWhiteSpace() ? m.Sn : $"{m.Nick}（{m.Sn}）",
        }).ToList();
    }

    /// <summary>清晰度字典</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "QualityOptions")]
    public List<LiveQualityOption> QualityOptions()
    {
        return typeof(LiveVideoQualityEnum).GetEnumDescDictionary()
            .OrderBy(m => m.Key)
            .Select(m => new LiveQualityOption { Value = m.Key, Label = m.Value })
            .ToList();
    }

    /// <summary>镜头字典</summary>
    /// <remarks>对外暴露协议取值（normal/wide/zoom/ir），因为下发指令时用的就是这个字符串。</remarks>
    [HttpGet]
    [ApiDescriptionSettings(Name = "LensOptions")]
    public List<LiveLensOption> LensOptions()
    {
        return System.Enum.GetValues<LiveLensTypeEnum>()
            .Select(m => new LiveLensOption { Value = ToProtocolLens(m), Label = DescribeEnum(m) })
            .ToList();
    }

    /// <summary>会话状态字典</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "StatusOptions")]
    public List<LiveStatusOption> StatusOptions()
    {
        return typeof(LiveStreamStatusEnum).GetEnumDescDictionary()
            .OrderBy(m => m.Key)
            .Select(m => new LiveStatusOption { Value = m.Key, Label = m.Value })
            .ToList();
    }

    #endregion

    #region 开播 / 停播

    /// <summary>
    /// 开始直播。
    /// </summary>
    /// <remarks>
    /// <para>前置校验（全部通过才下发，避免机场侧报错后前端拿不到可读原因）：</para>
    /// <list type="number">
    /// <item>服务端已配置 SRS 地址（<c>Dji.Live</c>）；</item>
    /// <item>机场在线且已上报 <c>live_capacity</c>；</item>
    /// <item>传入的 <c>video_id</c> 确实存在于设备的通道树中（白名单校验）；</item>
    /// <item>该路流当前未在播；</item>
    /// <item>并发路数未超过 <c>coexist_video_number_max</c>。</item>
    /// </list>
    /// <para>
    /// 会话先落库为「启动中」再下发指令：机场回包与状态上报是两条独立链路，
    /// 若等回包再建会话，回包丢失时会话就丢了，设备却在推流（幽灵流）。
    /// </para>
    /// </remarks>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Start")]
    public async Task<LiveSessionOutput> Start(StartLiveInput input)
    {
        var (enabled, reason) = CheckLiveConfigured();
        if (!enabled) throw Oops.Oh(reason);

        var dock = await ResolveDockAsync(input.DockSn);

        var state = await _liveRepository.GetStateAsync(dock.Sn)
                    ?? throw Oops.Oh($"机场【{dock.Nick ?? dock.Sn}】尚未上报直播能力，暂时无法开播");

        var channels = await BuildChannelsAsync(dock.Sn, state);
        var channel = channels.FirstOrDefault(m => m.VideoId == input.VideoId)
                      ?? throw Oops.Oh("所选直播通道不存在，请刷新通道列表后重试（设备能力可能已变化）");

        if (channel.IsLive)
            throw Oops.Oh($"该通道已在直播中（{channel.ChannelName}），请先停止后再开播");

        var maxCoexist = state.CoexistVideoNumberMax ?? _liveOptions.MaxConcurrentPerDock;
        if (maxCoexist > 0 && channels.Count(m => m.IsLive) >= maxCoexist)
            throw Oops.Oh($"机场最多同时推流 {maxCoexist} 路，当前已达上限，请先停止一路再开播");

        var session = new DjiLiveStream
        {
            WorkspaceId = dock.WorkspaceId,
            DockSn = dock.Sn,
            VideoId = channel.VideoId,
            VideoSn = channel.Sn,
            CameraIndex = channel.CameraIndex,
            VideoIndex = channel.VideoIndex,
            VideoType = channel.VideoType,
            UrlType = LiveUrlTypeEnum.Rtmp,
            VideoQuality = input.VideoQuality,
            Status = LiveStreamStatusEnum.Starting,
            OperatorName = _userManager.RealName ?? _userManager.Account,
        };

        BuildStreamAddresses(session);
        session = await _liveRepository.CreateSessionAsync(session);

        var result = await SendAsync(dock.Sn, TopicMethods.LiveStartPush, new LiveStartPushInput
        {
            UrlType = (int)LiveUrlTypeEnum.Rtmp,
            Url = session.PushUrl,
            VideoId = session.VideoId,
            VideoQuality = (int)session.VideoQuality,
        }, "开始直播");

        // 设备接受后状态仍为「启动中」，真正的「直播中」由 OSD live_status 确认（见仓库注释）
        await _liveRepository.UpdateSessionStatusAsync(session.Id,
            result == 0 ? LiveStreamStatusEnum.Starting : LiveStreamStatusEnum.Failed,
            result,
            result == 0 ? null : $"机场拒绝开始直播，错误码：{result}",
            stopWhenClosed: false);

        if (result != 0) throw Oops.Oh($"机场拒绝开始直播，错误码：{result}");

        _logger.LogInformation("已下发开始直播，机场:{DockSn}，通道:{VideoId}，流名:{Stream}，清晰度:{Quality}",
            dock.Sn, session.VideoId, session.StreamName, session.VideoQuality);

        return await Detail(new LiveControlInput { Id = session.Id });
    }

    /// <summary>停止直播</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Stop")]
    public async Task<bool> Stop(LiveControlInput input)
    {
        var session = await GetSessionAsync(input.Id);
        if (session.Status is LiveStreamStatusEnum.Stopped or LiveStreamStatusEnum.Failed)
            throw Oops.Oh($"该直播会话{GetStatusText(session.Status)}，无需停止");

        var result = await SendAsync(session.DockSn, TopicMethods.LiveStopPush,
            new LiveStopPushInput { VideoId = session.VideoId }, "停止直播");

        // 无论机场是否回包成功都收口为已停止：用户意图是「停止」，
        // 若机场无应答（离线），继续显示「直播中」反而误导；设备重新推流会在 OSD 里被再次识别。
        await _liveRepository.UpdateSessionStatusAsync(session.Id, LiveStreamStatusEnum.Stopped,
            result, result == 0 ? null : $"机场未确认停止直播，错误码：{result}");

        if (result != 0) _logger.LogWarning("停止直播未获机场确认，机场:{DockSn}，通道:{VideoId}，result:{Result}",
            session.DockSn, session.VideoId, result);

        return result == 0;
    }

    /// <summary>设置直播清晰度</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "SetQuality")]
    public async Task<bool> SetQuality(SetLiveQualityInput input)
    {
        var session = await EnsureSessionOpenAsync(input.Id);

        var result = await SendAsync(session.DockSn, TopicMethods.LiveSetQuality, new LiveSetQualityInput
        {
            VideoId = session.VideoId,
            VideoQuality = (int)input.VideoQuality,
        }, "设置清晰度");

        if (result == 0)
        {
            session.VideoQuality = input.VideoQuality;
            await _streamRep.AsUpdateable(session)
                .UpdateColumns(m => new { m.VideoQuality, m.LastActiveTime })
                .Where(m => m.Id == session.Id)
                .ExecuteCommandAsync();
        }

        return result == 0;
    }

    /// <summary>
    /// 切换直播镜头。
    /// </summary>
    /// <remarks>
    /// 先按设备上报的 <c>switchable_video_types</c> 校验，越界直接拒绝 ——
    /// 机场对不支持的镜头会回错误码，但错误码含义不直观，本地拦一道能给出更清楚的中文提示。
    /// </remarks>
    [HttpPost]
    [ApiDescriptionSettings(Name = "LensChange")]
    public async Task<bool> LensChange(SetLiveLensInput input)
    {
        var session = await EnsureSessionOpenAsync(input.Id);

        var state = await _liveRepository.GetStateAsync(session.DockSn);
        var channel = state == null
            ? null
            : (await BuildChannelsAsync(session.DockSn, state)).FirstOrDefault(m => m.VideoId == session.VideoId);

        if (channel?.SwitchableVideoTypes is { Count: > 0 } allowed && !allowed.Contains(input.VideoType))
        {
            throw Oops.Oh($"该通道不支持切换到【{DescribeLens(input.VideoType)}】，"
                          + $"当前支持：{string.Join("、", allowed.Select(DescribeLens))}");
        }

        var result = await SendAsync(session.DockSn, TopicMethods.LiveLensChange,
            new LiveLensChangeInput { VideoType = input.VideoType }, "切换镜头");

        if (result == 0)
        {
            session.VideoType = input.VideoType;
            await _streamRep.AsUpdateable(session)
                .UpdateColumns(m => new { m.VideoType, m.LastActiveTime })
                .Where(m => m.Id == session.Id)
                .ExecuteCommandAsync();
        }

        return result == 0;
    }

    /// <summary>切换 FPV 相机位置（舱内 / 舱外）</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "CameraChange")]
    public async Task<bool> CameraChange(SetLiveCameraInput input)
    {
        var session = await EnsureSessionOpenAsync(input.Id);

        var result = await SendAsync(session.DockSn, TopicMethods.LiveCameraChange, new LiveCameraChangeInput
        {
            VideoId = session.VideoId,
            CameraPosition = input.CameraPosition,
        }, "切换FPV相机");

        return result == 0;
    }

    #endregion

    #region 私有实现

    /// <summary>
    /// 把 <c>live_capacity</c> 的三层树拍平成通道列表，并叠加在播状态。
    /// </summary>
    /// <remarks>
    /// 在播判定以设备上报的 <c>live_status</c> 为准（<c>status = 1</c>）；
    /// 本地会话只用来回填 <c>sessionId</c>，不参与「是否在播」的判定 —— 否则会出现
    /// 「本地以为在播、设备其实没推」的假象。
    /// </remarks>
    private async Task<List<LiveChannelOutput>> BuildChannelsAsync(string dockSn, DjiDockState state)
    {
        var channels = new List<LiveChannelOutput>();
        if (state.CapacityJson.IsNullOrWhiteSpace()) return channels;

        var capacity = JSON.Deserialize<LiveCapacityPayload>(state.CapacityJson);
        if (capacity?.DeviceList == null) return channels;

        var liveStatus = state.LiveStatusJson.IsNullOrWhiteSpace()
            ? []
            : JSON.Deserialize<List<LiveStatusItem>>(state.LiveStatusJson) ?? [];

        var sessions = await _liveRepository.GetOpenSessionsAsync(dockSn);

        foreach (var device in capacity.DeviceList)
        {
            foreach (var camera in device.CameraList ?? [])
            {
                foreach (var video in camera.VideoList ?? [])
                {
                    var videoId = $"{device.Sn}/{camera.CameraIndex}/{video.VideoIndex}";
                    var status = liveStatus.FirstOrDefault(m => m.VideoId == videoId);

                    channels.Add(new LiveChannelOutput
                    {
                        Sn = device.Sn,
                        CameraIndex = camera.CameraIndex,
                        VideoIndex = video.VideoIndex,
                        VideoId = videoId,
                        VideoType = status?.VideoType ?? video.VideoType,
                        SwitchableVideoTypes = video.SwitchableVideoTypes ?? [],
                        IsLive = status?.Status == 1,
                        VideoQuality = status?.VideoQuality ?? 0,
                        ErrorStatus = status?.ErrorStatus ?? 0,
                        SessionId = sessions.FirstOrDefault(m => m.VideoId == videoId)?.Id,
                        ChannelName = $"{DescribeLens(status?.VideoType ?? video.VideoType)}｜相机 {camera.CameraIndex}",
                    });
                }
            }
        }

        return channels;
    }

    /// <summary>
    /// 生成流名与三个地址。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 流名格式：<c>{前缀}{sn}_{camera_index}_{video_index}_{秒级时间戳}</c>。
    /// 三段标识里的 <c>/</c> 必须替换为 <c>_</c>（SRS 流名不允许斜杠）。
    /// </para>
    /// <para>
    /// <b>为什么带时间戳</b>：SRS 会短暂保留已断流的分片，若复用一个固定流名，
    /// 「停播后立刻重播」时播放器可能先拉到上一段的残留画面。带上会话时间戳后每次开播都是新流，
    /// 从根上避免这个现象。
    /// </para>
    /// </remarks>
    private void BuildStreamAddresses(DjiLiveStream session)
    {
        var sanitized = session.VideoId.Replace('/', '_');
        var streamName = $"{_liveOptions.StreamPrefix}{sanitized}_{DateTimeOffset.Now.ToUnixTimeSeconds()}";

        session.StreamName = streamName;
        session.PushUrl = $"{_liveOptions.RtmpPushBaseUrl.TrimEnd('/')}/{streamName}";
        session.PlayUrl = _liveOptions.FlvPlayBaseUrl.IsNullOrWhiteSpace()
            ? null
            : $"{_liveOptions.FlvPlayBaseUrl.TrimEnd('/')}/{streamName}.flv";
        session.HlsUrl = _liveOptions.HlsPlayBaseUrl.IsNullOrWhiteSpace()
            ? null
            : $"{_liveOptions.HlsPlayBaseUrl.TrimEnd('/')}/{streamName}.m3u8";
    }

    /// <summary>检查直播配置是否就绪</summary>
    private (bool Enabled, string Reason) CheckLiveConfigured()
    {
        if (!_liveOptions.Enabled)
            return (false, "本平台未启用直播功能，请在 Dji.json 的 Dji:Live 段将 Enabled 置为 true 并配置 SRS 推流地址");

        if (_liveOptions.RtmpPushBaseUrl.IsNullOrWhiteSpace())
            return (false, "未配置 SRS 推流地址（Dji:Live:RtmpPushBaseUrl），无法下发开播指令");

        return (true, null);
    }

    private async Task<Dji.Core.Entity.DjiDevice> ResolveDockAsync(string dockSn)
    {
        if (dockSn.IsNullOrWhiteSpace()) throw Oops.Oh("机场不能为空");

        var dock = await _deviceRep.GetFirstAsync(m => m.Sn == dockSn) ?? throw Oops.Oh($"未找到机场：{dockSn}");
        if (dock.Domain != DomainEnum.Dock) throw Oops.Oh($"{dockSn} 不是机场设备，无法直播");
        if (dock.WorkspaceId.IsNullOrWhiteSpace()) throw Oops.Oh($"机场【{dock.Nick ?? dock.Sn}】尚未绑定工作空间");

        return dock;
    }

    private async Task<DjiLiveStream> GetSessionAsync(long id)
    {
        if (id <= 0) throw Oops.Oh("直播会话Id不能为空");
        return await _streamRep.GetFirstAsync(m => m.Id == id) ?? throw Oops.Oh(ErrorCodeEnum.D1002);
    }

    /// <summary>取出会话并确认仍在进行中</summary>
    private async Task<DjiLiveStream> EnsureSessionOpenAsync(long id)
    {
        var session = await GetSessionAsync(id);
        if (session.Status is not (LiveStreamStatusEnum.Starting or LiveStreamStatusEnum.Live))
            throw Oops.Oh($"该直播会话{GetStatusText(session.Status)}，不支持此操作");

        return session;
    }

    /// <summary>
    /// 下发直播指令并等待回包。
    /// </summary>
    /// <returns>0 表示机场接受；非 0 为错误码；-1 表示超时 / 异常</returns>
    private async Task<int> SendAsync<T>(string dockSn, string method, T payload, string actionName)
    {
        try
        {
            var request = new CloudMqRequest<T>(method, payload, dockSn);
            var reply = await _publish.PublishWithReplyAsync<T, object>(Topics.ThingProductServices, request, CommandReplyTimeoutSeconds);
            var result = reply?.Data?.Result ?? -1;

            if (result != 0)
                _logger.LogWarning("机场拒绝{Action}：method:{Method}，gateway:{Gateway}，result:{Result}",
                    actionName, method, dockSn, result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Action}指令无应答：method:{Method}，gateway:{Gateway}", actionName, method, dockSn);
            return -1;
        }
    }

    /// <summary>批量补全可读名与机场别名（DockNick 不落库，按 SN 现查）</summary>
    private async Task<SqlSugarPagedList<LiveSessionOutput>> FillNamesAsync(SqlSugarPagedList<LiveSessionOutput> paged)
    {
        var filled = await FillNamesAsync(paged.Items.ToList());
        paged.Items = filled;
        return paged;
    }

    private async Task<List<LiveSessionOutput>> FillNamesAsync(List<LiveSessionOutput> items)
    {
        if (items is not { Count: > 0 }) return items;

        foreach (var item in items)
        {
            item.StatusName = GetStatusText(item.Status);
            item.VideoQualityName = DescribeEnum(item.VideoQuality);
            item.UrlTypeName = DescribeEnum(item.UrlType);
            item.VideoTypeName = DescribeLens(item.VideoType);
        }

        var dockSns = items.Select(m => m.DockSn).Where(m => !m.IsNullOrWhiteSpace()).Distinct().ToList();
        var docks = await _deviceRep.AsQueryable().Where(m => dockSns.Contains(m.Sn)).ToListAsync();
        foreach (var item in items) item.DockNick = docks.FirstOrDefault(m => m.Sn == item.DockSn)?.Nick;

        return items;
    }

    private static string GetStatusText(LiveStreamStatusEnum status) => DescribeEnum(status);

    /// <summary>
    /// 取枚举的 <c>[Description]</c>。
    /// </summary>
    /// <remarks>
    /// 显式调用 <c>EnumExtension</c> 的静态方法而不是扩展方法语法：
    /// <c>NewLife.EnumHelper</c> 也提供了同名扩展方法（<c>NewLife</c> 是全局 using），
    /// 直接用扩展语法会触发 CS0121 二义性错误。
    /// </remarks>
    private static string DescribeEnum<T>(T value) where T : struct, System.Enum
        => EnumExtension.GetDescription(value) ?? value.ToString();

    /// <summary>把协议镜头取值转成可读中文（协议里红外同时有 ir / infrared 两种写法）</summary>
    private static string DescribeLens(string videoType)
    {
        if (videoType.IsNullOrWhiteSpace()) return "未知";

        return videoType.Trim().ToLowerInvariant() switch
        {
            "normal" => "默认",
            "wide" => "广角",
            "zoom" => "变焦",
            "ir" or "infrared" => "红外",
            _ => videoType,
        };
    }

    /// <summary>把镜头枚举转成协议取值（下发给机场用）</summary>
    private static string ToProtocolLens(LiveLensTypeEnum lens) => lens switch
    {
        LiveLensTypeEnum.Normal => "normal",
        LiveLensTypeEnum.Wide => "wide",
        LiveLensTypeEnum.Zoom => "zoom",
        LiveLensTypeEnum.Ir => "ir",
        _ => "normal",
    };

    #endregion
}
