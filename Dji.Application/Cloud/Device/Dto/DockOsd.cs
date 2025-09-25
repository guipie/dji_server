// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

namespace Dji.Application.Cloud.Device.Dto;
using System.Collections.Generic;


/// <summary>
/// 机场OSD信息
/// </summary>
public class DockOsd
{
    /// <summary>
    /// Home 点有效性 (home_position_is_valid) - enum_int: 0=无效, 1=有效
    /// </summary> 
    public int HomePositionIsValid { get; set; }

    /// <summary>
    /// 机场朝向角 (heading) - double: -180 ~ 180 度
    /// </summary>
    public double Heading { get; set; }

    /// <summary>
    /// 机场 RTK 标定源 (rtcm_info) - struct
    /// </summary>
    public RtcMInfo RtcmInfo { get; set; } = new RtcMInfo();

    /// <summary>
    /// 图传连接拓扑 (wireless_link_topo) - struct
    /// </summary>
    public WirelessLinkTopo WirelessLinkTopo { get; set; } = new WirelessLinkTopo();

    /// <summary>
    /// 机场空调工作状态信息 (air_conditioner) - struct
    /// </summary>
    public AirConditioner AirConditioner { get; set; } = new AirConditioner();

    /// <summary>
    /// 空中回传 (air_transfer_enable) - bool: 0=关闭, 1=开启
    /// </summary>
    public bool AirTransferEnable { get; set; }

    /// <summary>
    /// 机场静音模式 (silent_mode) - enum_int: 0=非静音, 1=静音模式
    /// </summary>
    public int SilentMode { get; set; }

    /// <summary>
    /// 用户体验改善计划 (user_experience_improvement) - enum_int: 0=初始, 1=拒绝, 2=同意
    /// </summary>
    public int UserExperienceImprovement { get; set; }

    /// <summary>
    /// 4G Dongle信息 (dongle_infos) - array of struct
    /// </summary>
    public List<DongleInfo> DongleInfos { get; set; } = new List<DongleInfo>();

    /// <summary>
    /// 飞行器电池保养信息 (drone_battery_maintenance_info) - struct
    /// </summary>
    public DroneBatteryMaintenanceInfo DroneBatteryMaintenanceInfo { get; set; } = new DroneBatteryMaintenanceInfo();

    /// <summary>
    /// 保养信息 (maintain_status) - struct
    /// </summary>
    public MaintainStatus MaintainStatus { get; set; } = new MaintainStatus();

    /// <summary>
    /// 搜星状态 (position_state) - struct
    /// </summary>
    public PositionState PositionState { get; set; } = new PositionState();

    /// <summary>
    /// 紧急停止按钮状态 (emergency_stop_state) - enum_int: 0=关闭, 1=开启
    /// </summary>
    public int EmergencyStopState { get; set; }

    /// <summary>
    /// 飞行器充电状态 (drone_charge_state) - struct
    /// </summary>
    public DroneChargeState DroneChargeState { get; set; } = new DroneChargeState();

    /// <summary>
    /// 机场备用电池信息 (backup_battery) - struct
    /// </summary>
    public BackupBattery BackupBattery { get; set; } = new BackupBattery();

    /// <summary>
    /// 机场声光报警状态 (alarm_state) - enum_int: 0=关闭, 1=开启
    /// </summary>
    public int AlarmState { get; set; }

    /// <summary>
    /// 电池运行模式 (battery_store_mode) - enum_int: 1=计划模式, 2=待命模式
    /// </summary>
    public int BatteryStoreMode { get; set; }

    /// <summary>
    /// 机场激活时间 (activation_time) - int (unix 秒)
    /// </summary>
    public int ActivationTime { get; set; }

    /// <summary>
    /// 椭球高度 (height) - double (米)
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// 备降点 (alternate_land_point) - struct
    /// </summary>
    public AlternateLandPoint AlternateLandPoint { get; set; } = new AlternateLandPoint();

    /// <summary>
    /// 固件一致性 (compatible_status) - enum_int: 0=不需要升级, 1=需要升级
    /// </summary>
    public int CompatibleStatus { get; set; }

    /// <summary>
    /// 机场累计运行时长 (acc_time) - int (秒)
    /// </summary>
    public int AccTime { get; set; }

    /// <summary>
    /// 首次上电时间 (first_power_on) - int (毫秒)
    /// </summary>
    public long FirstPowerOn { get; set; }

    /// <summary>
    /// 存储容量 (storage) - struct
    /// </summary>
    public Storage Storage { get; set; } = new Storage();

    /// <summary>
    /// 工作电流 (working_current) - float (毫安)
    /// </summary>
    public float WorkingCurrent { get; set; }

    /// <summary>
    /// 工作电压 (working_voltage) - int (毫伏)
    /// </summary>
    public int WorkingVoltage { get; set; }

    /// <summary>
    /// 舱内湿度 (humidity) - float (%RH)
    /// </summary>
    public float Humidity { get; set; }

    /// <summary>
    /// 舱内温度 (temperature) - float (°C)
    /// </summary>
    public float Temperature { get; set; }

    /// <summary>
    /// 环境温度 (environment_temperature) - float (°C)
    /// </summary>
    public float EnvironmentTemperature { get; set; }

    /// <summary>
    /// 风速 (wind_speed) - float (m/s)
    /// </summary>
    public float WindSpeed { get; set; }

    /// <summary>
    /// 降雨量 (rainfall) - enum_int: 0=无雨, 1=小雨, 2=中雨, 3=大雨
    /// </summary>
    public int Rainfall { get; set; }

    /// <summary>
    /// 网关设备直播能力 (live_capacity) - struct
    /// </summary>
    public LiveCapacity LiveCapacity { get; set; } = new LiveCapacity();

    /// <summary>
    /// 网关当前整体直播状态推送 (live_status) - array of struct
    /// </summary>
    public List<LiveStatus> LiveStatus { get; set; } = new List<LiveStatus>();

    /// <summary>
    /// 图传链路 (wireless_link) - struct
    /// </summary>
    public WirelessLink WirelessLink { get; set; } = new WirelessLink();

    /// <summary>
    /// 媒体文件上传细节 (media_file_detail) - struct
    /// </summary>
    public MediaFileDetail MediaFileDetail { get; set; } = new MediaFileDetail();

    /// <summary>
    /// 机场累计作业次数 (job_number) - int
    /// </summary>
    public int JobNumber { get; set; }

    /// <summary>
    /// 飞行器是否在舱 (drone_in_dock) - enum_int: 0=舱外, 1=舱内
    /// </summary>
    public int DroneInDock { get; set; }

    /// <summary>
    /// 网络状态 (network_state) - struct
    /// </summary>
    public NetworkState NetworkState { get; set; } = new NetworkState();

    /// <summary>
    /// 补光灯状态 (supplement_light_state) - enum_int: 0=关闭, 1=打开
    /// </summary>
    public int SupplementLightState { get; set; }

    /// <summary>
    /// 舱盖状态 (cover_state) - enum_int: 0=关闭, 1=打开, 2=半开, 3=异常
    /// </summary>
    public int CoverState { get; set; }

    /// <summary>
    /// 子设备状态 (sub_device) - struct
    /// </summary>
    public SubDevice SubDevice { get; set; } = new SubDevice();

    /// <summary>
    /// 机场任务状态 (flighttask_step_code) - enum_int
    /// </summary>
    public int FlighttaskStepCode { get; set; }

    /// <summary>
    /// 机场状态 (mode_code) - enum_int
    /// </summary>
    public int ModeCode { get; set; }

    /// <summary>
    /// 固件升级状态 (firmware_upgrade_status) - enum_int: 0=未升级, 1=升级中
    /// </summary>
    public int FirmwareUpgradeStatus { get; set; }

    /// <summary>
    /// 固件版本 (firmware_version) - text
    /// </summary>
    public string FirmwareVersion { get; set; } = string.Empty;

    /// <summary>
    /// 纬度 (latitude) - double
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// 经度 (longitude) - double
    /// </summary>
    public double Longitude { get; set; }
}

public class RtcMInfo
{
    /// <summary>
    /// 网络 RTK 挂载点信息 (mount_point) - text
    /// </summary>
    public string MountPoint { get; set; } = string.Empty;

    /// <summary>
    /// 网络端口信息 (port) - text
    /// </summary>
    public string Port { get; set; } = string.Empty;

    /// <summary>
    /// 网络 host 信息 (host) - text
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// 设备类型 (rtcm_device_type) - enum_int: 1=机场
    /// </summary>
    public int RtcMDeviceType { get; set; }

    /// <summary>
    /// 标定类型 (source_type) - enum_int: 0=未标定, 1=自收敛, 2=手动, 3=网络RTK
    /// </summary>
    public int SourceType { get; set; }
}

public class WirelessLinkTopo
{
    /// <summary>
    /// 加密编码 (secret_code) - array of int (size:28)
    /// </summary>
    public List<int> SecretCode { get; set; } = new List<int>();

    /// <summary>
    /// 飞行器对频信息 (center_node) - struct
    /// </summary>
    public CenterNode CenterNode { get; set; } = new CenterNode();

    /// <summary>
    /// 机场或遥控器对频信息 (leaf_nodes) - array of struct
    /// </summary>
    public List<LeafNode> LeafNodes { get; set; } = new List<LeafNode>();
}

public class CenterNode
{
    /// <summary>
    /// 扰码信息 (sdr_id) - int
    /// </summary>
    public int SdrId { get; set; }

    /// <summary>
    /// 设备sn (sn) - text
    /// </summary>
    public string Sn { get; set; } = string.Empty;
}

public class LeafNode
{
    /// <summary>
    /// 扰码信息 (sdr_id) - int
    /// </summary>
    public int SdrId { get; set; }

    /// <summary>
    /// 设备sn (sn) - text
    /// </summary>
    public string Sn { get; set; } = string.Empty;

    /// <summary>
    /// 控制源序号 (control_source_index) - int: 1~2
    /// </summary>
    public int ControlSourceIndex { get; set; }
}

public class AirConditioner
{
    /// <summary>
    /// 机场空调状态 (air_conditioner_state) - enum_int
    /// 0=空闲, 1=制冷, 2=制热, 3=除湿, 4=制冷退出, 5=制热退出, 6=除湿退出, 7=制冷准备, 8=制热准备, 9=除湿准备
    /// </summary>
    public int AirConditionerState { get; set; }

    /// <summary>
    /// 剩余等待可切换时间 (switch_time) - int (秒)
    /// </summary>
    public int SwitchTime { get; set; }
}

public class DongleInfo
{
    /// <summary>
    /// dongle imei (imei) - text
    /// </summary>
    public string Imei { get; set; } = string.Empty;

    /// <summary>
    /// Dongle 类型 (dongle_type) - enum_int: 6=旧, 10=新（eSIM）
    /// </summary>
    public int DongleType { get; set; }

    /// <summary>
    /// dongle eid (eid) - text
    /// </summary>
    public string Eid { get; set; } = string.Empty;

    /// <summary>
    /// eSIM 激活状态 (esim_activate_state) - enum_int: 0=未激活, 1=已激活
    /// </summary>
    public int EsimActivateState { get; set; }

    /// <summary>
    /// SIM 卡状态 (sim_card_state) - enum_int: 0=未插入, 1=已插入
    /// </summary>
    public int SimCardState { get; set; }

    /// <summary>
    /// SIM 卡槽使能状态 (sim_slot) - enum_int: 0=未知, 1=实体SIM, 2=eSIM
    /// </summary>
    public int SimSlot { get; set; }

    /// <summary>
    /// eSIM 信息 (esim_infos) - array of struct
    /// </summary>
    public List<EsimInfo> EsimInfos { get; set; } = new List<EsimInfo>();

    /// <summary>
    /// SIM 卡信息 (sim_info) - struct
    /// </summary>
    public SimInfo SimInfo { get; set; } = new SimInfo();
}

public class EsimInfo
{
    /// <summary>
    /// 支持的运营商 (telecom_operator) - enum_int: 0=未知, 1=移动, 2=联通, 3=电信
    /// </summary>
    public int TelecomOperator { get; set; }

    /// <summary>
    /// eSIM 使能状态 (enabled) - bool
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// sim iccid (iccid) - text
    /// </summary>
    public string Iccid { get; set; } = string.Empty;
}

public class SimInfo
{
    /// <summary>
    /// 支持的运营商 (telecom_operator) - enum_int
    /// </summary>
    public int TelecomOperator { get; set; }

    /// <summary>
    /// SIM 卡类型 (sim_type) - enum_int: 0=未知, 1=普通卡, 2=三网卡
    /// </summary>
    public int SimType { get; set; }

    /// <summary>
    /// sim iccid (iccid) - text
    /// </summary>
    public string Iccid { get; set; } = string.Empty;
}

public class DroneBatteryMaintenanceInfo
{
    /// <summary>
    /// 保养状态 (maintenance_state) - enum_int: 0=无需, 1=待保养, 2=正在保养
    /// </summary>
    public int MaintenanceState { get; set; }

    /// <summary>
    /// 电池保养剩余时间 (maintenance_time_left) - int (小时)
    /// </summary>
    public int MaintenanceTimeLeft { get; set; }

    /// <summary>
    /// 电池加热保温状态 (heat_state) - enum_int: 0=未加热, 1=加热中, 2=保温中
    /// </summary>
    public int HeatState { get; set; }

    /// <summary>
    /// 电池详细信息 (batteries) - array of struct
    /// </summary>
    public List<BatteryDetail> Batteries { get; set; } = new List<BatteryDetail>();
}

public class BatteryDetail
{
    /// <summary>
    /// 电池剩余电量 (capacity_percent) - int: 0~100
    /// </summary>
    public int CapacityPercent { get; set; }

    /// <summary>
    /// 电池序号 (index) - enum_int: 0=左, 1=右
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// 电压 (voltage) - int (mV)
    /// </summary>
    public int Voltage { get; set; }

    /// <summary>
    /// 温度 (temperature) - float (°C)
    /// </summary>
    public float Temperature { get; set; }
}

public class MaintainStatus
{
    /// <summary>
    /// 保养信息数组 (maintain_status_array) - array of struct
    /// </summary>
    public List<MaintainStatusItem> MaintainStatusArray { get; set; } = new List<MaintainStatusItem>();
}

public class MaintainStatusItem
{
    /// <summary>
    /// 保养状态 (state) - enum_int: 0=无保养, 1=有保养
    /// </summary>
    public int State { get; set; }

    /// <summary>
    /// 上一次保养类型 (last_maintain_type) - enum_int: 0=无, 17=常规, 18=深度
    /// </summary>
    public int LastMaintainType { get; set; }

    /// <summary>
    /// 上一次保养时间 (last_maintain_time) - date (秒)
    /// </summary>
    public int LastMaintainTime { get; set; }

    /// <summary>
    /// 上一次保养时作业架次 (last_maintain_work_sorties) - int
    /// </summary>
    public int LastMaintainWorkSorties { get; set; }
}

public class PositionState
{
    /// <summary>
    /// 是否标定 (is_calibration) - enum_int: 0=未标定, 1=已标定
    /// </summary>
    public int IsCalibration { get; set; }

    /// <summary>
    /// 是否收敛 (is_fixed) - enum_int: 0=未开始, 1=收敛中, 2=成功, 3=失败
    /// </summary>
    public int IsFixed { get; set; }

    /// <summary>
    /// 搜星档位 (quality) - enum_int: 1~5档, 10=RTK fixed
    /// </summary>
    public int Quality { get; set; }

    /// <summary>
    /// GPS 搜星数量 (gps_number) - int
    /// </summary>
    public int GpsNumber { get; set; }

    /// <summary>
    /// RTK 搜星数量 (rtk_number) - int
    /// </summary>
    public int RtkNumber { get; set; }
}

public class DroneChargeState
{
    /// <summary>
    /// 电量百分比 (capacity_percent) - int: 0~100
    /// </summary>
    public int CapacityPercent { get; set; }

    /// <summary>
    /// 充电状态 (state) - enum_int: 0=空闲, 1=充电中
    /// </summary>
    public int State { get; set; }
}

public class BackupBattery
{
    /// <summary>
    /// 备用电池开关 (switch) - enum_int: 0=关闭, 1=开启
    /// </summary>
    public int Switch { get; set; }

    /// <summary>
    /// 备用电池电压 (voltage) - int (mV)
    /// </summary>
    public int Voltage { get; set; }

    /// <summary>
    /// 备用电池温度 (temperature) - float (°C)
    /// </summary>
    public float Temperature { get; set; }
}

public class Storage
{
    /// <summary>
    /// 总容量 (total) - int (KB)
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// 已使用容量 (used) - int (KB)
    /// </summary>
    public int Used { get; set; }
}

public class AlternateLandPoint
{
    /// <summary>
    /// 经度 (longitude) - float
    /// </summary>
    public float Longitude { get; set; }

    /// <summary>
    /// 纬度 (latitude) - float
    /// </summary>
    public float Latitude { get; set; }

    /// <summary>
    /// 安全高度 (safe_land_height) - float
    /// </summary>
    public float SafeLandHeight { get; set; }

    /// <summary>
    /// 是否设置备降点 (is_configured) - enum_int: 0=未设置, 1=已设置
    /// </summary>
    public int IsConfigured { get; set; }

    /// <summary>
    /// 椭球高度 (height) - float
    /// </summary>
    public float Height { get; set; }
}

public class LiveCapacity
{
    /// <summary>
    /// 可选择推流的码流数量 (available_video_number) - int
    /// </summary>
    public int AvailableVideoNumber { get; set; }

    /// <summary>
    /// 可同时推流的最大码流数量 (coexist_video_number_max) - int
    /// </summary>
    public int CoexistVideoNumberMax { get; set; }

    /// <summary>
    /// 可选择的视频设备源 (device_list) - array of struct
    /// </summary>
    public List<DeviceList> DeviceList { get; set; } = new List<DeviceList>();
}

public class DeviceList
{
    /// <summary>
    /// 飞行器等视频源设备序列号 (sn) - text
    /// </summary>
    public string Sn { get; set; } = string.Empty;

    /// <summary>
    /// 该设备可选择推流的码流数 (available_video_number) - int
    /// </summary>
    public int AvailableVideoNumber { get; set; }

    /// <summary>
    /// 该设备可同时推流的码流数 (coexist_video_number_max) - int
    /// </summary>
    public int CoexistVideoNumberMax { get; set; }

    /// <summary>
    /// 该设备上的相机列表 (camera_list) - array of struct
    /// </summary>
    public List<CameraList> CameraList { get; set; } = new List<CameraList>();
}

public class CameraList
{
    /// <summary>
    /// 相机索引 (camera_index) - text: {type-subtype-gimbalindex}
    /// </summary>
    public string CameraIndex { get; set; } = string.Empty;

    /// <summary>
    /// 该相机可选择推流的码流数 (available_video_number) - int
    /// </summary>
    public int AvailableVideoNumber { get; set; }

    /// <summary>
    /// 该相机可同时推流的码流数 (coexist_video_number_max) - int
    /// </summary>
    public int CoexistVideoNumberMax { get; set; }

    /// <summary>
    /// 该相机可选择的码流列表 (video_list) - array of struct
    /// </summary>
    public List<VideoList> VideoList { get; set; } = new List<VideoList>();
}

public class VideoList
{
    /// <summary>
    /// 码流索引 (video_index) - text
    /// </summary>
    public string VideoIndex { get; set; } = string.Empty;

    /// <summary>
    /// 码流类型 (video_type) - text
    /// </summary>
    public string VideoType { get; set; } = string.Empty;

    /// <summary>
    /// 支持切换的视频镜头类型 (switchable_video_types) - array of text
    /// </summary>
    public List<string> SwitchableVideoTypes { get; set; } = new List<string>();
}

public class LiveStatus
{
    /// <summary>
    /// 直播码流标识符 (video_id) - text: {sn}/{camera_index}/{video_index}
    /// </summary>
    public string VideoId { get; set; } = string.Empty;

    /// <summary>
    /// 视频类型 (video_type) - text
    /// </summary>
    public string VideoType { get; set; } = string.Empty;

    /// <summary>
    /// 直播码流的质量 (video_quality) - enum_int: 0=自适应, 1=流畅, 2=标清, 3=高清, 4=超清
    /// </summary>
    public int VideoQuality { get; set; }

    /// <summary>
    /// 直播状态 (status) - enum_int: 0=未直播, 1=在直播
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// 错误码 (error_status) - int
    /// </summary>
    public int ErrorStatus { get; set; }
}

public class WirelessLink
{
    /// <summary>
    /// 飞行器上 Dongle 数量 (dongle_number) - int
    /// </summary>
    public int DongleNumber { get; set; }

    /// <summary>
    /// 4G 链路连接状态 (4g_link_state) - enum_int: 0=断开, 1=连接
    /// </summary>
    public int _4GLinkState { get; set; }

    /// <summary>
    /// SDR 链路连接状态 (sdr_link_state) - enum_int: 0=断开, 1=连接
    /// </summary>
    public int SdrLinkState { get; set; }

    /// <summary>
    /// 机场的图传链路模式 (link_workmode) - enum_int: 0=SDR, 1=4G融合
    /// </summary>
    public int LinkWorkmode { get; set; }

    /// <summary>
    /// SDR 信号质量 (sdr_quality) - int: 0~5
    /// </summary>
    public int SdrQuality { get; set; }

    /// <summary>
    /// 总体 4G 信号质量 (4g_quality) - int: 0~5
    /// </summary>
    public int _4GQuality { get; set; }

    /// <summary>
    /// 天端 4G 信号质量 (4g_uav_quality) - int: 0~5
    /// </summary>
    public int _4GUavQuality { get; set; }

    /// <summary>
    /// 地端 4G 信号质量 (4g_gnd_quality) - int: 0~5
    /// </summary>
    public int _4GGndQuality { get; set; }

    /// <summary>
    /// SDR 频段 (sdr_freq_band) - float
    /// </summary>
    public float SdrFreqBand { get; set; }

    /// <summary>
    /// 4G 频段 (4g_freq_band) - float
    /// </summary>
    public float _4GFreqBand { get; set; }
}

public class MediaFileDetail
{
    /// <summary>
    /// 媒体文件上传细节 (remain_upload) - int
    /// </summary>
    public int RemainUpload { get; set; }
}

public class NetworkState
{
    /// <summary>
    /// 网络类型 (type) - enum_int: 1=4G, 2=以太网
    /// </summary>
    public int Type { get; set; }

    /// <summary>
    /// 网络质量 (quality) - enum_int: 0~5
    /// </summary>
    public int Quality { get; set; }

    /// <summary>
    /// 网络速率 (rate) - float (KB/s)
    /// </summary>
    public float Rate { get; set; }
}

public class SubDevice
{
    /// <summary>
    /// 子设备序列号 (device_sn) - text
    /// </summary>
    public string DeviceSn { get; set; } = string.Empty;

    /// <summary>
    /// 子设备枚举值 (device_model_key) - text: {domain-type-subtype}
    /// </summary>
    public string DeviceModelKey { get; set; } = string.Empty;

    /// <summary>
    /// 机场停机坪上的飞行器开机状态 (device_online_status) - enum_int: 0=关机, 1=开机
    /// </summary>
    public int DeviceOnlineStatus { get; set; }

    /// <summary>
    /// 机场停机坪上的飞行器是否与机场对频 (device_paired) - enum_int: 0=未对频, 1=已对频
    /// </summary>
    public int DevicePaired { get; set; }
}
