// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

namespace Dji.Core.Enum.DjiEnum.Ops;

#region 固件升级

/// <summary>
/// 固件升级类型（<c>ota_create.devices[].firmware_upgrade_type</c>）。
/// </summary>
/// <remarks>
/// <b>取值从 2 开始，没有 0 和 1</b>（官方保留但未启用）。
/// 「一致性升级」「普通升级」的区别决定了升级包从哪来：
/// 一致性升级通常由设备侧自行从 DJI 服务器取包（只给 <c>product_version</c> 即可），
/// 普通升级则需要云端提供 <c>file_url</c> / <c>md5</c> 等完整信息。
/// </remarks>
public enum OtaUpgradeTypeEnum
{
    /// <summary>一致性升级（补齐与系统版本不匹配的模块，如未升级的电池）</summary>
    [Description("一致性升级")]
    Consistency = 2,

    /// <summary>普通升级（把设备所有模块升到指定版本）</summary>
    [Description("普通升级")]
    Normal = 3,

    /// <summary>PSDK 负载升级</summary>
    [Description("PSDK 升级")]
    Psdk = 4
}

/// <summary>
/// 固件升级当前步骤（<c>ota_progress.output.progress.current_step</c>）。
/// </summary>
/// <remarks>
/// 协议给的是字符串，不是整数。整个升级过程有且只有这两步。
/// </remarks>
public enum OtaStepEnum
{
    /// <summary>下载固件</summary>
    [Description("下载固件")]
    DownloadFirmware,

    /// <summary>更新固件</summary>
    [Description("更新固件")]
    UpgradeFirmware
}

/// <summary>
/// 固件升级任务状态（平台内部态）。
/// </summary>
/// <remarks>
/// 协议里升级进度只给 <c>output.status</c>（sent / in_progress / ok / failed ...）与百分比，
/// 没有「云端已建单、还没下发」这一态，因此单独定义，便于前端展示「待下发」。
/// </remarks>
public enum OtaTaskStatusEnum
{
    /// <summary>待下发（已建单，尚未发指令）</summary>
    [Description("待下发")]
    Pending = -1,

    /// <summary>下发中（等待 services_reply）</summary>
    [Description("下发中")]
    Sending = 0,

    /// <summary>升级中</summary>
    [Description("升级中")]
    Upgrading = 1,

    /// <summary>升级成功</summary>
    [Description("升级成功")]
    Success = 2,

    /// <summary>升级失败</summary>
    [Description("升级失败")]
    Failed = 3,

    /// <summary>已取消</summary>
    [Description("已取消")]
    Canceled = 4,
}

/// <summary>
/// 固件升级当前步骤的协议字符串 ↔ 显示文案。
/// </summary>
/// <remarks>
/// 协议给的是字符串（<c>download_firmware</c>）；<see cref="OtaStepEnum"/> 的成员名是 PascalCase，
/// 无法直接 <c>Enum.Parse</c> 到协议值，故在此集中转换。
/// </remarks>
public static class OtaStepResolver
{
    /// <summary>把协议字符串转成枚举；未知值返回 null。</summary>
    public static OtaStepEnum? Parse(string step)
    {
        return step switch
        {
            "download_firmware" => OtaStepEnum.DownloadFirmware,
            "upgrade_firmware" => OtaStepEnum.UpgradeFirmware,
            _ => null
        };
    }

    /// <summary>把协议字符串转成中文步骤名；未知值原样返回，便于暴露固件新增的步骤。</summary>
    public static string Label(string step)
    {
        var parsed = Parse(step);
        return parsed.HasValue ? Dji.Core.EnumExtension.GetDescription(parsed.Value) : step;
    }
}

/// <summary>
/// 协议 <c>ota_progress.output.status</c> 字符串 ↔ 平台任务状态。
/// </summary>
/// <remarks>
/// <b><c>paused</c> 归到「升级中」</b>：暂停只是中断而非终态，设备仍可继续；
/// 若映射成失败，运维会误以为需要重新下发。
/// <b><c>rejected</c> / <c>timeout</c> 归到「失败」</b>：两者都不会再自动恢复，
/// 必须人工介入（前者通常是前置条件不满足，后者是链路问题）。
/// </remarks>
public static class OtaTaskStatusResolver
{
    /// <summary>把协议字符串映射为平台状态；未知值按「升级中」处理。</summary>
    public static OtaTaskStatusEnum Parse(string status)
    {
        return status switch
        {
            "sent" => OtaTaskStatusEnum.Sending,
            "ok" => OtaTaskStatusEnum.Success,
            "failed" => OtaTaskStatusEnum.Failed,
            "canceled" => OtaTaskStatusEnum.Canceled,
            "rejected" or "timeout" => OtaTaskStatusEnum.Failed,
            _ => OtaTaskStatusEnum.Upgrading
        };
    }

    /// <summary>是否为终态（不会再有后续进度推送）。</summary>
    public static bool IsFinal(this OtaTaskStatusEnum status)
        => status is OtaTaskStatusEnum.Success or OtaTaskStatusEnum.Failed or OtaTaskStatusEnum.Canceled;
}

#endregion

#region 远程日志

/// <summary>
/// 日志所属模块（<c>fileupload_*</c> 的 <c>module</c> / <c>module_list</c>）。
/// </summary>
/// <remarks>
/// 协议里 <c>module</c> 以<b>字符串</b>（<c>"0"</c> / <c>"3"</c>）出现，不是整数，
/// 落库时统一转成 int 便于筛选；对外输出再转回。
/// 注意这里与媒体回传的 module（0 媒体 / 1 日志）是<b>两套完全不同的枚举</b>。
/// </remarks>
public enum LogModuleEnum
{
    /// <summary>飞行器</summary>
    [Description("飞行器")]
    Drone = 0,

    /// <summary>机场</summary>
    [Description("机场")]
    Dock = 3
}

/// <summary>
/// 日志文件上传状态（平台内部态）。
/// </summary>
public enum LogUploadStatusEnum
{
    /// <summary>待上传（已从设备列出，尚未发起上传）</summary>
    [Description("待上传")]
    Pending = 0,

    /// <summary>上传中</summary>
    [Description("上传中")]
    Uploading = 1,

    /// <summary>已上传</summary>
    [Description("已上传")]
    Uploaded = 2,

    /// <summary>上传失败</summary>
    [Description("上传失败")]
    Failed = 3,

    /// <summary>已取消</summary>
    [Description("已取消")]
    Canceled = 4
}

/// <summary>
/// 协议 <c>fileupload_progress</c> 里 <c>progress.status</c> 的字符串取值。
/// </summary>
public static class LogUploadStateResolver
{
    /// <summary>把协议字符串映射为上传状态；未知值按「上传中」处理。</summary>
    public static LogUploadStatusEnum Parse(string status)
    {
        return status switch
        {
            "ok" => LogUploadStatusEnum.Uploaded,
            "failed" => LogUploadStatusEnum.Failed,
            "cancel" or "canceled" => LogUploadStatusEnum.Canceled,
            _ => LogUploadStatusEnum.Uploading
        };
    }

    /// <summary>是否为终态（不会再收到该文件的后续进度）。</summary>
    public static bool IsFinalState(this LogUploadStatusEnum status)
        => status is LogUploadStatusEnum.Uploaded or LogUploadStatusEnum.Failed or LogUploadStatusEnum.Canceled;
}

#endregion

#region 自定义飞行区

/// <summary>
/// 自定义飞行区文件同步状态（<c>flight_areas_sync_progress.status</c>）。
/// </summary>
/// <remarks>
/// 协议给的是字符串枚举。整个同步链路是：
/// <c>flight_areas_update</c>（下发通知）→ 设备 <c>flight_areas_get</c>（来要文件）
/// → 云端给地址 → 设备下载并启用 → 持续上报 <c>flight_areas_sync_progress</c>。
/// </remarks>
public enum FlightAreaSyncStatusEnum
{
    /// <summary>待同步</summary>
    [Description("待同步")]
    WaitSync = 0,

    /// <summary>同步中</summary>
    [Description("同步中")]
    Synchronizing = 1,

    /// <summary>已同步</summary>
    [Description("已同步")]
    Synchronized = 2,

    /// <summary>同步失败</summary>
    [Description("同步失败")]
    Fail = 3,

    /// <summary>使能开关失败</summary>
    [Description("使能开关失败")]
    SwitchFail = 4
}

/// <summary>
/// 同步状态字符串 ↔ 枚举。
/// </summary>
public static class FlightAreaSyncStatusResolver
{
    /// <summary>解析协议字符串；未知值返回 <see cref="FlightAreaSyncStatusEnum.Fail"/>。</summary>
    public static FlightAreaSyncStatusEnum Parse(string status)
    {
        return status switch
        {
            "wait_sync" => FlightAreaSyncStatusEnum.WaitSync,
            "synchronizing" => FlightAreaSyncStatusEnum.Synchronizing,
            "synchronized" => FlightAreaSyncStatusEnum.Synchronized,
            "switch_fail" => FlightAreaSyncStatusEnum.SwitchFail,
            _ => FlightAreaSyncStatusEnum.Fail
        };
    }
}

/// <summary>
/// 自定义飞行区同步失败原因码（<c>flight_areas_sync_progress.reason</c>）。
/// </summary>
/// <remarks>
/// 只有 <c>status</c> 为 <c>fail</c> 时该字段才有意义；<c>0</c> 表示无错误。
/// </remarks>
public enum FlightAreaSyncReasonEnum
{
    /// <summary>无错误</summary>
    [Description("无错误")]
    None = 0,

    /// <summary>解析云端返回的文件信息失败</summary>
    [Description("解析云端返回的文件信息失败")]
    ParseCloudFile = 1,

    /// <summary>获取飞行器端文件信息失败</summary>
    [Description("获取飞行器端文件信息失败")]
    GetDroneFile = 2,

    /// <summary>从云端下载文件失败</summary>
    [Description("从云端下载文件失败")]
    DownloadFromCloud = 3,

    /// <summary>链路翻转失败</summary>
    [Description("链路翻转失败")]
    LinkSwitch = 4,

    /// <summary>传输文件失败</summary>
    [Description("传输文件失败")]
    TransferFile = 5,

    /// <summary>disable 失败</summary>
    [Description("关闭作业区域失败")]
    DisableFail = 6,

    /// <summary>自定义飞行区删除失败</summary>
    [Description("自定义飞行区删除失败")]
    DeleteFail = 7,

    /// <summary>飞行器端加载作业区域数据失败</summary>
    [Description("飞行器端加载作业区域数据失败")]
    LoadOnDrone = 8,

    /// <summary>enable 失败</summary>
    [Description("启用作业区域失败")]
    EnableFail = 9,

    /// <summary>机场增强图传无法关闭，作业区域数据同步失败</summary>
    [Description("机场增强图传无法关闭，同步失败")]
    EnhanceLinkBlocked = 10,

    /// <summary>飞行器开机失败，无法同步作业区域数据</summary>
    [Description("飞行器开机失败，无法同步")]
    DronePowerOnFail = 11,

    /// <summary>checksum 校验失败</summary>
    [Description("文件校验失败")]
    ChecksumFail = 12,

    /// <summary>同步异常超时</summary>
    [Description("同步超时")]
    Timeout = 13
}

#endregion

#region AirSense

/// <summary>
/// AirSense 告警等级（<c>airsense_warning[].warning_level</c>）。
/// </summary>
/// <remarks>
/// 官方口径：<b>等级 ≥ 3 时建议无人机主动避让</b>。前端据此决定是否用醒目的红色告警。
/// </remarks>
public enum AirSenseWarningLevelEnum
{
    /// <summary>无危险</summary>
    [Description("无危险")]
    None = 0,

    /// <summary>等级一</summary>
    [Description("等级一")]
    Level1 = 1,

    /// <summary>等级二</summary>
    [Description("等级二")]
    Level2 = 2,

    /// <summary>等级三</summary>
    [Description("等级三")]
    Level3 = 3,

    /// <summary>等级四</summary>
    [Description("等级四")]
    Level4 = 4
}

/// <summary>
/// 民航客机绝对高度类型。
/// </summary>
public enum AirSenseAltitudeTypeEnum
{
    /// <summary>椭球高</summary>
    [Description("椭球高")]
    Ellipsoid = 0,

    /// <summary>海拔高</summary>
    [Description("海拔高")]
    Asl = 1
}

/// <summary>
/// 民航客机相对本机的垂直趋势。
/// </summary>
public enum AirSenseVertTrendEnum
{
    /// <summary>相对高度不变</summary>
    [Description("高度不变")]
    Stable = 0,

    /// <summary>相对高度上升</summary>
    [Description("高度上升")]
    Up = 1,

    /// <summary>相对高度下降</summary>
    [Description("高度下降")]
    Down = 2
}

#endregion

#region RTK 一键标定

/// <summary>
/// RTK 标定的设备模块（<c>rtk_calibration.devices[].module</c>）。
/// </summary>
/// <remarks>协议用字符串表示；本项目目前只做机场本体标定。</remarks>
public enum RtkCalibrationModuleEnum
{
    /// <summary>机场</summary>
    [Description("机场")]
    Dock = 3,

    /// <summary>中继</summary>
    [Description("中继")]
    Relay = 6
}

/// <summary>
/// RTK 标定类型（<c>rtk_calibration.devices[].type</c>）。
/// </summary>
public enum RtkCalibrationTypeEnum
{
    /// <summary>手动标定（当前唯一取值）</summary>
    [Description("手动标定")]
    Manual = 1
}

#endregion

#region 飞行区 / 禁飞区（平台侧自绘）

/// <summary>
/// 自定义飞行区类型。
/// </summary>
/// <remarks>
/// 对应下发文件里的 <c>geofence_type</c>：
/// <list type="bullet">
/// <item><c>dfence</c> —— 自定义作业区（圈内可飞，飞机会被约束在边界内，越界即返航）</item>
/// <item><c>nfz</c> —— 自定义禁飞区（圈外可飞，飞机进入前会自动绕行）</item>
/// </list>
/// <b>两者的边界条件是不对称的</b>：机场必须落在所有 dfence 内部、落在所有 nfz 外部，
/// 且距任一区域边界 ≥ 10m。这正是保存前必须做校验的原因，否则文件下发到设备会被直接拒绝。
/// </remarks>
public enum FlyZoneTypeEnum
{
    /// <summary>自定义作业区（geofence，圈内可飞）</summary>
    [Description("作业区")]
    CustomFlyZone = 0,

    /// <summary>自定义禁飞区（no-fly zone，圈外可飞）</summary>
    [Description("禁飞区")]
    NoFlyZone = 1
}

/// <summary>
/// 飞行区的几何形状。
/// </summary>
/// <remarks>
/// GeoJSON 原生没有圆，所以圆形区域在文件里是一个 <c>Point</c> + <c>properties.radius</c>
/// 的组合表达，绘制与预览时由前端按中心点与半径临时生成。
/// </remarks>
public enum FlyZoneShapeEnum
{
    /// <summary>多边形（首尾顶点需重合形成闭合环）</summary>
    [Description("多边形")]
    Polygon = 0,

    /// <summary>圆形（中心点 + 半径）</summary>
    [Description("圆形")]
    Circle = 1
}

#endregion
