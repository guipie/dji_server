// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Cloud.Core;
using Dji.Application.Cloud.Dto.Media;
using Dji.Application.Cloud.Entity;
using Dji.Application.CloudRepository;
using Dji.Application.Service.Common;

namespace Dji.Application.Cloud;

/// <summary>
/// 机场媒体回传上行处理（MQTT）。
/// </summary>
/// <remarks>
/// <para>
/// <b>机场侧的媒体链路只有 MQTT，没有 HTTP</b>（详见 <c>MediaDto.cs</c> 的类注释）：
/// <c>storage_config_get</c> 取凭证 → 直传对象存储 → <c>file_upload_callback</c> 回报结果。
/// 因此本类只需处理这三个上行方法。
/// </para>
/// <para>
/// <b>订阅唯一性</b>：<c>MqttService</c> 匹配到多个处理器时只执行其中一条并打印错误日志，
/// 因此同一 (Topic, Method) 不能出现两处 <c>MqttSubscribe</c>。
/// </para>
/// </remarks>
internal class MqMediaService(
    ILogger<MqMediaService> logger,
    MqttGatewayPublish gatewayPublish,
    DjiMediaRepository mediaRepository,
    DjiStorageService storageService,
    SqlSugarRepository<DjiDevice> deviceRes) : BaseModuleService
{
    private readonly ILogger<MqMediaService> _logger = logger;
    private readonly MqttGatewayPublish _gatewayPublish = gatewayPublish;
    private readonly DjiMediaRepository _mediaRepository = mediaRepository;
    private readonly DjiStorageService _storageService = storageService;
    private readonly SqlSugarRepository<DjiDevice> _deviceRes = deviceRes;

    /// <summary>
    /// 机场索取对象存储临时凭证。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>这是媒体回传链路的起点，不回复机场就无法回传任何媒体。</b>
    /// 应答里的 <c>object_key_prefix</c> 决定文件在桶里的位置，本平台用<b>工作空间 ID</b> 作为前缀，
    /// 这样媒体天然按空间隔离，媒体库按空间筛选不需要额外字段。
    /// </para>
    /// <para>
    /// <b>日志与媒体共用同一个上报入口</b>（<c>module</c>：0 媒体 / 1 日志）：
    /// 远程日志是「设备直传对象存储」的同一条链路，因此这里对日志也下发<b>同一份凭证</b>，
    /// 只在 Key 前缀上追加 <c>log</c> 段，让日志与媒体在桶里分目录存放，便于分别做生命周期规则。
    /// </para>
    /// <para>
    /// 出现协议未定义的 <c>module</c> 值时回成功但不带配置（<c>output = null</c>）——
    /// 回错误码会让设备反复重试，而我们其实无法处理；
    /// 打一条日志说明「收到过但没处理」比刷屏有价值。
    /// </para>
    /// </remarks>
    [MqttSubscribe(Topics.ThingProductRequests, TopicMethods.StorageConfigGet)]
    public async Task StorageConfigGetAsync(CloudMqData<StorageConfigGetInput> data)
    {
        var module = data.Data?.Module ?? MediaConstants.ModuleMedia;
        var dockSn = data.Gateway;

        if (module is not (MediaConstants.ModuleMedia or MediaConstants.ModuleLog))
        {
            _logger.LogWarning("机场 {Gateway} 请求未知模块（module={Module}）的存储凭证，已忽略", dockSn, module);
            await _gatewayPublish.PublishAsync(Topics.ThingProductRequestsReply,
                ToPublishOutputData<object, StorageConfigGetInput>(null, data));
            return;
        }

        var prefix = await ResolveObjectKeyPrefixAsync(dockSn);
        // 日志与媒体分目录：同一工作站下 media 与 log 各自成树，便于分别配生命周期与权限
        if (module == MediaConstants.ModuleLog) prefix = $"{prefix}/log";

        var config = _storageService.BuildStorageConfig(prefix);

        if (config == null)
        {
            _logger.LogWarning("机场 {Gateway} 请求{Kind}存储凭证，但本平台对象存储未就绪，机场将无法回传。" +
                               "请检查 Upload.json 的 OSSProvider 段（IsEnable / Bucket / Endpoint）",
                dockSn, module == MediaConstants.ModuleLog ? "日志" : "媒体");
            await _gatewayPublish.PublishAsync(Topics.ThingProductRequestsReply,
                ToPublishOutputDataError(data, Enum.DjiReplyErrorEnum.UNKNOWN_ERROR));
            return;
        }

        _logger.LogInformation("已向机场 {Gateway} 下发{Kind}存储凭证：provider={Provider}, bucket={Bucket}, prefix={Prefix}",
            dockSn, module == MediaConstants.ModuleLog ? "日志" : "媒体",
            config.Provider, config.Bucket, config.ObjectKeyPrefix);

        await _gatewayPublish.PublishAsync(Topics.ThingProductRequestsReply, ToPublishOutputData(config, data));
    }

    /// <summary>
    /// 媒体文件上传结果上报。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 该报文带 <c>need_reply = 1</c>，<b>必须回 <c>events_reply</c></b>，否则机场会持续重发。
    /// 因此这里无论落库成功与否都要回包 —— 落库失败（例如设备未建档）是本平台的问题，
    /// 让机场无限重发并不能解决，只会放大日志量。
    /// </para>
    /// <para>
    /// 落库完成后由 <c>MqttService</c> 统一推送给所属工作空间在线用户，前端媒体库据此增量刷新。
    /// </para>
    /// </remarks>
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.FileUploadCallback)]
    public async Task FileUploadCallbackAsync(CloudMqData<FileUploadCallbackInput> data)
    {
        var file = data.Data?.File;
        if (file == null || file.ObjectKey.IsNullOrWhiteSpace())
        {
            _logger.LogWarning("媒体上传回调缺少 file.object_key，已忽略，gateway:{Gateway}", data.Gateway);
            await ReplyEventAsync(data);
            return;
        }

        var saved = await _mediaRepository.SaveUploadResultAsync(file, data.Gateway, data.TimeStamp);
        if (saved == null)
        {
            // 落库失败原因已在仓储内打日志（通常是设备未建档 / 无空间归属）
            _logger.LogWarning("媒体记录未落库，object_key:{ObjectKey}，gateway:{Gateway}", file.ObjectKey, data.Gateway);
        }
        else
        {
            _logger.LogInformation("媒体回传已入库：{FileName}（{Type}），任务:{FlightId}，机场:{DockSn}",
                saved.FileName, saved.FileType, saved.FlightId ?? "-", saved.DockSn);
        }

        await ReplyEventAsync(data);
    }

    /// <summary>
    /// 机场告知当前优先级最高的上传任务。
    /// </summary>
    /// <remarks>
    /// 机场自主选出「最该先传」的任务，云端仅做展示与留痕（媒体库据此提示用户「N 个文件排队中」）。
    /// 真正的排队顺序由机场决定，云端不干预 —— 因此这里不落业务表，只记日志，
    /// 避免为一条展示性信息引入一张高写入频率的表。
    /// </remarks>
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.HighestPriorityUploadFlighttaskMedia)]
    public async Task HighestPriorityUploadAsync(CloudMqData<HighestPriorityUploadInput> data)
    {
        _logger.LogInformation("机场 {Gateway} 上报当前最高优先级上传任务：flight_id={FlightId}",
            data.Gateway, data.Data?.FlightId);

        await ReplyEventAsync(data);
    }

    #region 私有实现

    /// <summary>
    /// 解析对象存储 Key 前缀。
    /// </summary>
    /// <remarks>
    /// 用工作空间 ID 作前缀，让媒体在桶里天然按空间隔离。
    /// 设备尚未建档（拿不到空间）时退化为机场 SN —— 必须返回一个<b>非空</b>值，
    /// 因为前缀为空会让文件散落在桶根目录，后续无法按空间治理。
    /// </remarks>
    private async Task<string> ResolveObjectKeyPrefixAsync(string dockSn)
    {
        if (dockSn.IsNullOrWhiteSpace()) return "default";

        var device = await _deviceRes.GetFirstAsync(m => m.Sn == dockSn);
        if (device?.WorkspaceId.IsNullOrWhiteSpace() == false) return device.WorkspaceId;

        _logger.LogWarning("机场 {Gateway} 尚未建档或无空间归属，媒体 Key 前缀退化为机场 SN", dockSn);
        return dockSn;
    }

    /// <summary>回复事件（<c>events_reply</c>），协议要求对 <c>need_reply=1</c> 的事件必须应答</summary>
    private async Task ReplyEventAsync<T>(CloudMqData<T> from)
    {
        var reply = new CloudMqRequest<MqEventReply>(from.Method, new MqEventReply { Result = 0 }, from.Gateway, from.Tid, from.Bid);
        await _gatewayPublish.PublishAsync(Topics.ThingProductEventsReply, reply);
    }

    #endregion
}
