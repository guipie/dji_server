// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

namespace Dji.Application.Cloud.Core;

/// <summary>
/// MQTT 主题（通配符形式，下发时由 <c>DeviceExtension.BindGateway</c> 把 <c>+</c> 替换为具体 SN）。
/// </summary>
public static class Topics
{
    /// <summary>物模型属性推送（OSD，0.5Hz 定频）</summary>
    public const string ThingProductOsd = "thing/product/+/osd";

    /// <summary>
    /// 物模型状态推送（<b>仅变化时上报</b>）。
    /// </summary>
    /// <remarks>
    /// 直播能力（<c>live_capacity</c>）与机场整体直播状态（<c>live_status</c>）只在这个主题里，
    /// OSD 定频报文里没有，因此直播功能必须依赖此订阅。
    /// </remarks>
    public const string ThingProductState = "thing/product/+/state";

    /// <summary>设备生命周期（拓扑上下线）</summary>
    public const string ThingProductStatus = "sys/product/+/status";

    /// <summary>设备主动请求云端</summary>
    public const string ThingProductRequests = "thing/product/+/requests";

    /// <summary>云端对设备请求的应答</summary>
    public const string ThingProductRequestsReply = "thing/product/+/requests_reply";

    /// <summary>云端下发的服务指令</summary>
    public const string ThingProductServices = "thing/product/+/services";

    /// <summary>设备对服务指令的应答</summary>
    public const string ThingProductServicesReply = "thing/product/+/services_reply";

    /// <summary>设备主动上报的事件（航线进度 / HMS / 媒体回调 / 固件进度等都在这里）</summary>
    public const string ThingProductEvents = "thing/product/+/events";

    /// <summary>云端对设备事件的应答</summary>
    public const string ThingProductEventsReply = "thing/product/+/events_reply";

    /// <summary>物模型属性设置应答</summary>
    public const string ThingProductPropertySetReply = "thing/product/+/property/set_reply";

    /// <summary>DRC 上行（远程控制）</summary>
    public const string ThingProductDrcUp = "thing/product/+/drc/up";
}

/// <summary>
/// 协议方法名（<c>method</c> 字段取值）。
/// </summary>
/// <remarks>
/// 常量集中在此便于 grep 定位；方法名大小写敏感，必须与官方文档逐字一致。
/// </remarks>
public static class TopicMethods
{
    #region 设备生命周期（sys/product/{sn}/status）

    /// <summary>拓扑更新（设备上线时上报）</summary>
    public const string UpdateTopo = "update_topo";

    /// <summary>设备离线通知</summary>
    public const string Offline = "offline";

    /// <summary>设备上线通知</summary>
    public const string Online = "online";

    #endregion

    #region 设备主动请求（thing/product/{sn}/requests）

    /// <summary>查询设备组织绑定状态</summary>
    public const string AirportBindStatus = "airport_bind_status";

    /// <summary>设备绑定到组织</summary>
    public const string AirportOrganizationBind = "airport_organization_bind";

    /// <summary>获取组织信息</summary>
    public const string AirportOrganizationGet = "airport_organization_get";

    /// <summary>机场请求云端配置（App 凭证 / NTP 服务器）；不回复会导致机场无法校时</summary>
    public const string Config = "config";

    /// <summary>机场请求航线任务资源（KMZ 下载地址与签名）</summary>
    public const string FlightTaskResourceGet = "flighttask_resource_get";

    /// <summary>蛙跳任务中查询另一机场的任务执行状态</summary>
    public const string FlightTaskProgressGet = "flighttask_progress_get";

    /// <summary>
    /// 机场主动请求对象存储临时凭证（<b>媒体回传链路的起点</b>）。
    /// </summary>
    /// <remarks>
    /// 机场拿到凭证后直传对象存储，不走云端中转；不回复机场就无法回传媒体。
    /// </remarks>
    public const string StorageConfigGet = "storage_config_get";

    #endregion

    #region 媒体回传（thing/product/{sn}/events）

    /// <summary>
    /// 单个媒体文件上传结果上报。
    /// </summary>
    /// <remarks>
    /// 报文带 <c>need_reply = 1</c>，必须回 <c>events_reply</c>，否则机场会重发。
    /// </remarks>
    public const string FileUploadCallback = "file_upload_callback";

    /// <summary>告知云端当前优先级最高的上传任务（机场侧自动选出）</summary>
    public const string HighestPriorityUploadFlighttaskMedia = "highest_priority_upload_flighttask_media";

    #endregion

    #region 媒体回传下行（thing/product/{sn}/services）

    /// <summary>人工调整某任务的媒体上传优先级</summary>
    public const string UploadFlighttaskMediaPrioritize = "upload_flighttask_media_prioritize";

    #endregion

    #region 直播下行（thing/product/{sn}/services）

    /// <summary>开始直播（下行携带协议类型、推流地址、清晰度）</summary>
    public const string LiveStartPush = "live_start_push";

    /// <summary>停止直播</summary>
    public const string LiveStopPush = "live_stop_push";

    /// <summary>设置直播清晰度</summary>
    public const string LiveSetQuality = "live_set_quality";

    /// <summary>切换直播镜头（红外/广角/变焦/默认）</summary>
    public const string LiveLensChange = "live_lens_change";

    /// <summary>切换 FPV 相机位置（舱内/舱外）</summary>
    public const string LiveCameraChange = "live_camera_change";

    #endregion

    #region 航线任务下行（thing/product/{sn}/services）

    /// <summary>下发航线任务</summary>
    public const string FlightTaskPrepare = "flighttask_prepare";

    /// <summary>执行航线任务</summary>
    public const string FlightTaskExecute = "flighttask_execute";

    /// <summary>暂停航线任务</summary>
    public const string FlightTaskPause = "flighttask_pause";

    /// <summary>恢复航线任务</summary>
    public const string FlightTaskRecovery = "flighttask_recovery";

    /// <summary>取消航线任务</summary>
    public const string FlightTaskUndo = "flighttask_undo";

    /// <summary>结束航线任务</summary>
    public const string FlightTaskStop = "flighttask_stop";

    #endregion

    #region 航线任务上行（thing/product/{sn}/events）

    /// <summary>上报航线任务进度（含状态、步骤、断点）</summary>
    public const string FlightTaskProgress = "flighttask_progress";

    /// <summary>通知任务已满足准备条件（仅条件任务）</summary>
    public const string FlightTaskReady = "flighttask_ready";

    #endregion

    #region 远程调试

    /// <summary>开启远程调试</summary>
    public const string DebugModeOpen = "debug_mode_open";

    /// <summary>关闭远程调试</summary>
    public const string DebugModeClose = "debug_mode_close";

    #endregion

    #region 健康告警 HMS（thing/product/{sn}/events）

    /// <summary>
    /// 设备健康告警上报。
    /// </summary>
    /// <remarks>
    /// <b>报文是「当前全部活跃告警」的全量快照</b>：上一次上报里有、本次没有的告警 = 已解除。
    /// 报文带 <c>need_reply = 1</c>，须回 <c>events_reply</c>。
    /// </remarks>
    public const string Hms = "hms";

    #endregion

    #region 机场控制（下行 services / 上行 events）

    /// <summary>打开舱盖</summary>
    public const string CoverOpen = "cover_open";

    /// <summary>关闭舱盖</summary>
    public const string CoverClose = "cover_close";

    /// <summary>
    /// 强制关闭舱盖（不校验 <c>drone_in_dock</c>，可用于飞行器不在舱内时强行合盖）。
    /// </summary>
    /// <remarks>官方明确警告：仅在确认飞行器不在舱内时调用，否则可能夹伤桨叶。</remarks>
    public const string CoverForceClose = "cover_force_close";

    /// <summary>打开充电</summary>
    public const string ChargeOpen = "charge_open";

    /// <summary>关闭充电</summary>
    public const string ChargeClose = "charge_close";

    /// <summary>飞行器开机</summary>
    public const string DroneOpen = "drone_open";

    /// <summary>飞行器关机</summary>
    public const string DroneClose = "drone_close";

    /// <summary>机场重启（高危：会中断当前一切作业）</summary>
    public const string DeviceReboot = "device_reboot";

    /// <summary>机场数据格式化（高危：清空机场存储，不可恢复）</summary>
    public const string DeviceFormat = "device_format";

    /// <summary>飞行器数据格式化（高危：清空飞行器存储，不可恢复）</summary>
    public const string DroneFormat = "drone_format";

    /// <summary>打开补光灯</summary>
    public const string SupplementLightOpen = "supplement_light_open";

    /// <summary>关闭补光灯</summary>
    public const string SupplementLightClose = "supplement_light_close";

    /// <summary>电池保养状态切换（<c>action</c>：0 关闭 / 1 开启）</summary>
    public const string BatteryMaintenanceSwitch = "battery_maintenance_switch";

    /// <summary>机场空调工作模式切换（<c>action</c>：0 空闲 / 1 制冷 / 2 制热 / 3 除湿）</summary>
    public const string AirConditionerModeSwitch = "air_conditioner_mode_switch";

    /// <summary>机场声光报警开关（<c>action</c>：0 关闭 / 1 开启）</summary>
    public const string AlarmStateSwitch = "alarm_state_switch";

    /// <summary>电池运行模式切换（<c>action</c>：1 计划模式 / 2 待命模式）</summary>
    public const string BatteryStoreModeSwitch = "battery_store_mode_switch";

    /// <summary>增强图传开关（<c>link_workmode</c>：0 仅 SDR / 1 4G 增强）</summary>
    public const string SdrWorkmodeSwitch = "sdr_workmode_switch";

    /// <summary>
    /// eSIM 激活。
    /// </summary>
    /// <remarks>仅 Dock 2 / Dock 3 支持（Dock 1 章节未列出）。</remarks>
    public const string EsimActivate = "esim_activate";

    /// <summary>
    /// eSIM 运营商切换（<c>esim_operator</c>：1 移动 / 2 联通 / 3 电信）。
    /// </summary>
    /// <remarks>仅 Dock 2 / Dock 3 支持。</remarks>
    public const string EsimOperatorSwitch = "esim_operator_switch";

    /// <summary>
    /// 实体 SIM 卡与 eSIM 切换（<c>sim_slot</c>：1 实体卡 / 2 eSIM）。
    /// </summary>
    /// <remarks>仅 Dock 2 / Dock 3 支持。</remarks>
    public const string SimSlotSwitch = "sim_slot_switch";

    /// <summary>
    /// RTK 一键标定。
    /// </summary>
    /// <remarks>
    /// 双向同名：下行发 <c>services</c>（携带 <c>devices</c> 标定坐标），
    /// 上行走 <c>events</c> 回报每个设备的标定结果与整体进度。
    /// <b>仅 Dock 2 章节列出</b>（Dock 3 章节未列出，调用前需确认固件支持）。
    /// </remarks>
    public const string RtkCalibration = "rtk_calibration";

    #endregion

    #region 固件升级

    /// <summary>创建固件升级任务（下行 services，<c>devices</c> 数组可含机场 / 飞行器）</summary>
    public const string OtaCreate = "ota_create";

    /// <summary>固件升级进度（上行 events，带 <c>need_reply</c>）</summary>
    public const string OtaProgress = "ota_progress";

    #endregion

    #region 远程日志

    /// <summary>获取设备可上传的日志文件索引（下行 services，回包带 <c>boot_index</c> 列表）</summary>
    public const string FileuploadList = "fileupload_list";

    /// <summary>发起日志文件上传（下行 services，携带对象存储凭证与待传索引）</summary>
    public const string FileuploadStart = "fileupload_start";

    /// <summary>上传状态更新（下行 services，目前仅支持 <c>cancel</c> 取消）</summary>
    public const string FileuploadUpdate = "fileupload_update";

    /// <summary>日志文件上传进度（上行 events）</summary>
    public const string FileuploadProgress = "fileupload_progress";

    #endregion

    #region 自定义飞行区

    /// <summary>
    /// 自定义飞行区文件获取（<b>设备主动 requests 上行</b>，云端回 <c>requests_reply</c> 给出文件下载地址）。
    /// </summary>
    /// <remarks>与 <c>flighttask_resource_get</c> 同类：设备要文件时才来要，云端不能主动推。</remarks>
    public const string FlightAreasGet = "flight_areas_get";

    /// <summary>通知设备去云端拉取并启用最新的自定义飞行区文件（下行 services，无入参）</summary>
    public const string FlightAreasUpdate = "flight_areas_update";

    /// <summary>自定义飞行区文件同步进度（上行 events，带 <c>need_reply</c>）</summary>
    public const string FlightAreasSyncProgress = "flight_areas_sync_progress";

    /// <summary>飞行器与自定义飞行区边界距离推送（上行 events，高频，<c>need_reply = 0</c>）</summary>
    public const string FlightAreasDroneLocation = "flight_areas_drone_location";

    #endregion

    #region AirSense（感知民航客机）

    /// <summary>
    /// AirSense 周边民航客机告警（上行 events）。
    /// </summary>
    /// <remarks>报文 <c>data</c> 本身是<b>数组</b>（一次可推多架飞机），非对象。</remarks>
    public const string AirsenseWarning = "airsense_warning";

    #endregion
}
