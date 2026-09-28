// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using System.Linq;
using Dji.Application.Cloud.Core;
using Dji.Application.Cloud.Dto.Ops;
using Dji.Application.Cloud.Entity;
using Dji.Application.CloudRepository;
using Dji.Application.Service.Common;
using Dji.Core.Enum.DjiEnum.Ops;

namespace Dji.Application.Cloud;

/// <summary>
/// 自定义飞行区上行处理（MQTT）。
/// </summary>
/// <remarks>
/// <para>
/// <b>整条链路是「设备驱动」的，这是最容易理解错的地方</b>：
/// 云端把文件放进对象存储 + 下发 <c>flight_areas_update</c> 之后，<b>并不能</b>主动把文件推给设备。
/// 设备会在自己方便的时候（可能几小时后、可能下次开机）来 <c>flight_areas_get</c> 要地址，
/// 云端此时才现签一份 URL 返回。因此：
/// </para>
/// <list type="number">
/// <item>URL <b>必须随取随签</b>，预生成缓存的一定会过期；</item>
/// <item>「下发成功」不等于「已生效」，真值只在 <c>flight_areas_sync_progress</c> 里；</item>
/// <item>反过来，设备也可能在云端毫无操作时来要文件（重装、恢复出厂设置），
/// 此时也必须能给出正确应答，否则设备会一直停在「同步失败」。</item>
/// </list>
/// </remarks>
internal class MqFlightAreaService(
    ILogger<MqFlightAreaService> logger,
    MqttGatewayPublish gatewayPublish,
    DjiFlightAreaRepository areaRepository,
    DjiStorageService storageService) : BaseModuleService
{
    private readonly ILogger<MqFlightAreaService> _logger = logger;
    private readonly MqttGatewayPublish _gatewayPublish = gatewayPublish;
    private readonly DjiFlightAreaRepository _areaRepository = areaRepository;
    private readonly DjiStorageService _storageService = storageService;

    /// <summary>
    /// 设备索取自定义飞行区文件（<b>设备主动 requests 上行</b>）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>只回「最新的一版」而不是全部历史版本</b>：设备会把拿到的文件全部加载成作业区域，
    /// 若把该机场历史上传过的每一版都返回，多份相互重叠的围栏会同时生效，
    /// 表现为「有些地方莫名飞不进去」——而且这种问题极难定位。
    /// 取最新一版（<c>CreateTime</c> 倒序）即可：新版本同步成功后它同时也是启用版本。
    /// </para>
    /// <para>
    /// <b>拿不到地址时回错误码，绝不回空数组</b>：空数组的语义是「云端没有配置任何飞行区」，
    /// 设备会据此<b>清空本地已有的作业区域</b>；而本平台的实际情况只是「暂时签不出地址」（存储未启用、
    /// 对象被误删）。回错误码设备会保留现状并稍后重试，回空数组则直接把设备的安全围栏抹掉。
    /// </para>
    /// <para>该报文 <c>data</c> 为 <c>null</c>，不需要解析请求参数。</para>
    /// </remarks>
    [MqttSubscribe(Topics.ThingProductRequests, TopicMethods.FlightAreasGet)]
    public async Task FlightAreasGetAsync(CloudMqData<object> data)
    {
        var dockSn = data.Gateway;
        var files = await _areaRepository.GetByDockAsync(dockSn);

        if (files.Count == 0)
        {
            // 云端确实没有为这台机场配置飞行区，回空数组是正确语义
            _logger.LogInformation("机场 {Gateway} 索取飞行区文件，云端未配置，回空列表", dockSn);
            await _gatewayPublish.PublishAsync(Topics.ThingProductRequestsReply,
                ToPublishOutputData(new FlightAreaGetOutput(), data));
            return;
        }

        var latest = files.OrderByDescending(m => m.CreateTime).ThenByDescending(m => m.Id).First();
        if (latest.ObjectKey.IsNullOrWhiteSpace())
        {
            _logger.LogError("机场 {Gateway} 索取飞行区文件，但最新记录缺少对象 Key（id={Id} → {FileName}），" +
                             "无法生成下载地址，已回错误码。请重新登记该文件",
                dockSn, latest.Id, latest.FileName);

            await _gatewayPublish.PublishAsync(Topics.ThingProductRequestsReply,
                ToPublishOutputDataError(data, Enum.DjiReplyErrorEnum.UNKNOWN_ERROR));
            return;
        }

        // 每次现签：设备可能在下发通知后任意时刻才来取，缓存地址必然过期
        var url = await _storageService.BuildDownloadUrlAsync(latest.ObjectKey);
        if (url.IsNullOrWhiteSpace())
        {
            _logger.LogError("机场 {Gateway} 索取飞行区文件，但无法生成下载地址（key={Key}）。" +
                             "请检查 Upload.json 的 OSSProvider 段是否启用",
                dockSn, latest.ObjectKey);

            await _gatewayPublish.PublishAsync(Topics.ThingProductRequestsReply,
                ToPublishOutputDataError(data, Enum.DjiReplyErrorEnum.UNKNOWN_ERROR));
            return;
        }

        // 落一份地址留痕：设备报「下载失败」时，可比对这里的签名时间判断是不是地址过期
        await _areaRepository.SaveUrlAsync(latest.Id, url);

        var output = new FlightAreaGetOutput
        {
            Files =
            [
                new FlightAreaGetFile
                {
                    Name = latest.FileName,
                    Url = url,
                    Checksum = latest.Checksum,
                    Size = latest.FileSize,
                }
            ]
        };

        _logger.LogInformation("已向机场 {Gateway} 提供飞行区文件：{FileName}（{Size} 字节，摘要 {Checksum}）",
            dockSn, latest.FileName, latest.FileSize, latest.Checksum);

        await _gatewayPublish.PublishAsync(Topics.ThingProductRequestsReply, ToPublishOutputData(output, data));
    }

    /// <summary>
    /// 飞行区文件同步进度（<b>这是判断「有没有真正生效」的唯一依据</b>）。
    /// </summary>
    /// <remarks>
    /// <c>need_reply = 1</c>，无论落库结果如何都必须回 <c>events_reply</c>，
    /// 否则设备会持续重发同一条进度。
    /// </remarks>
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.FlightAreasSyncProgress)]
    public async Task SyncProgressAsync(CloudMqData<FlightAreaSyncProgressData> data)
    {
        var payload = data.Data;
        var status = FlightAreaSyncStatusResolver.Parse(payload?.Status);
        var reason = (FlightAreaSyncReasonEnum)(payload?.Reason ?? 0);

        _logger.LogInformation("机场 {Gateway} 飞行区同步进度：{Status}（原因：{Reason}），文件：{FileName}",
            data.Gateway, status, Dji.Core.EnumExtension.GetDescription(reason), payload?.File?.Name);

        if (payload?.File == null)
        {
            _logger.LogWarning("机场 {Gateway} 的飞行区同步进度缺少 file 字段，无法归属，状态：{Status}", data.Gateway, status);
        }
        else
        {
            await _areaRepository.UpdateSyncAsync(data.Gateway, status, reason, payload.File, data.TimeStamp);
        }

        await _gatewayPublish.PublishAsync(Topics.ThingProductEventsReply, ToEventReply(data));
    }

    /// <summary>
    /// 飞行器与各飞行区的距离推送（高频，<c>need_reply = 0</c>）。
    /// </summary>
    /// <remarks>
    /// <b>不回包</b>：协议明确 <c>need_reply = 0</c>，回一条多余的
    /// <c>events_reply</c> 只会增加链路噪音，部分固件还会把它当作异常。
    /// </remarks>
    [MqttSubscribe(Topics.ThingProductEvents, TopicMethods.FlightAreasDroneLocation)]
    public async Task DroneLocationAsync(CloudMqData<FlightAreaDroneLocationData> data)
    {
        var locations = data.Data?.DroneLocations;
        if (locations is not { Count: > 0 }) return;

        var transitions = await _areaRepository.RefreshLocationsAsync(data.Gateway, locations, data.TimeStamp);

        // 进入作业区域是安全相关事件，用 Warning 级别打出，便于日志系统直接过滤出来
        foreach (var (row, _) in transitions)
        {
            _logger.LogWarning("飞行器已进入自定义飞行区：机场={Gateway} 区域={AreaId} 进入次数={Count} 距边界={Distance}",
                data.Gateway, row.AreaId, row.EnterCount, row.AreaDistance);
        }
    }
}
