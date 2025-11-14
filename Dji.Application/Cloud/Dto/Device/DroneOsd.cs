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

namespace Dji.Application.Cloud.Dto.Device;
using System.Collections.Generic;

#pragma warning disable CS1591 // 禁用缺少 XML 注释警告（如需保留可删除）

/// <summary>
/// 飞行器物模型根对象
/// </summary>
public partial class DroneOsd
{
    /// <summary>
    /// 蛙跳任务场景下，当飞行器对频了两个机场后，推送连接最好的机场SN
    /// </summary>
    public string BestLinkGateway { get; set; }

    /// <summary>
    /// 图传连接拓扑
    /// </summary>
    public WirelessLinkTopoDrone WirelessLinkTopo { get; set; }

    /// <summary>
    /// 飞行器相机信息
    /// </summary>
    public List<CameraInfo> Cameras { get; set; }

    /// <summary>
    /// 飞行安全数据库版本
    /// </summary>
    public string FlysafeDatabaseVersion { get; set; }

    /// <summary>
    /// 当离线地图关闭后，离线地图同步不再进行自动同步
    /// </summary>
    public bool OfflineMapEnable { get; set; }

    /// <summary>
    /// 4G Dongle信息
    /// </summary>
    public List<DongleInfoDrone> DongleInfos { get; set; }

    /// <summary>
    /// 大疆机场当前实际使用的返航高度模式
    /// </summary>
    public RthModeEnum CurrentRthMode { get; set; }

    /// <summary>
    /// 智能返航模式下，飞行器将自动规划最佳返航高度。
    /// 大疆机场当前不支持设置返航高度模式，只能选择'设定高度'模式。
    /// 当环境，光线不满足视觉系统要求时（譬如傍晚阳光直射、夜间弱光无光），
    /// 飞行器将使用您设定的返航高度进行直线返航
    /// </summary>
    public RthModeEnum RthMode { get; set; }

    /// <summary>
    /// 飞行器避障状态
    /// </summary>
    public ObstacleAvoidance ObstacleAvoidance { get; set; }

    /// <summary>
    /// 是否接近限飞区
    /// </summary>
    public NearLimitStateEnum IsNearAreaLimit { get; set; }

    /// <summary>
    /// 是否接近设定的限制高度
    /// </summary>
    public NearLimitStateEnum IsNearHeightLimit { get; set; }

    /// <summary>
    /// 飞行器限高（单位：米 / m）
    /// </summary>
    public int HeightLimit { get; set; }

    /// <summary>
    /// 飞行器夜航灯状态
    /// </summary>
    public NightLightStateEnum NightLightsState { get; set; }

    /// <summary>
    /// 飞行器激活时间(unix 时间戳)（单位：秒 / s）
    /// </summary>
    public long ActivationTime { get; set; }

    /// <summary>
    /// 保养信息
    /// </summary>
    public MaintainStatusDrone MaintainStatus { get; set; }

    /// <summary>
    /// 飞行器累计飞行总架次
    /// </summary>
    public int TotalFlightSorties { get; set; }

    /// <summary>
    /// 负载编号（与字段 payload_index 保持一致）
    /// </summary>
    public PayloadGimbalInfo TypeSubtypeGimbalindex { get; set; }

    /// <summary>
    /// 航迹 ID
    /// </summary>
    public string TrackId { get; set; }

    /// <summary>
    /// 搜星状态
    /// </summary>
    public PositionStateDrone PositionState { get; set; }

    /// <summary>
    /// 存储容量（单位：KB）
    /// </summary>
    public StorageInfo Storage { get; set; }

    /// <summary>
    /// 飞行器电池信息
    /// </summary>
    public BatteryInfo Battery { get; set; }

    /// <summary>
    /// 飞行器累计飞行总里程（单位：米 / m）
    /// </summary>
    public double TotalFlightDistance { get; set; }

    /// <summary>
    /// 飞行器累计飞行航时（单位：秒 / s）
    /// </summary>
    public double TotalFlightTime { get; set; }

    /// <summary>
    /// 用户设置的电池严重低电量告警百分比
    /// </summary>
    public int SeriousLowBatteryWarningThreshold { get; set; }

    /// <summary>
    /// 用户设置的电池低电量告警百分比
    /// </summary>
    public int LowBatteryWarningThreshold { get; set; }

    /// <summary>
    /// 可以为设备，也可以为某个浏览器。设备使用 A/B 表示 A 控，B 控，浏览器以自生成的 uuid 作为标识符
    /// </summary>
    public string ControlSource { get; set; }

    /// <summary>
    /// 当前风向
    /// </summary>
    public WindDirectionEnum WindDirection { get; set; }

    /// <summary>
    /// 风速估计，该风速是通过飞行器姿态推测出的，有一定的误差，仅供参考，不能作为气象数据使用（单位：0.1 米每秒 / m/s）
    /// </summary>
    public double WindSpeed { get; set; }

    /// <summary>
    /// 距离 Home 点的距离
    /// </summary>
    public double HomeDistance { get; set; }

    /// <summary>
    /// Home 点纬度
    /// </summary>
    public double HomeLatitude { get; set; }

    /// <summary>
    /// Home 点经度
    /// </summary>
    public double HomeLongitude { get; set; }

    /// <summary>
    /// 偏航轴角度与真北角（经线）的角度，0到6点钟方向为正值，6到12点钟方向为负值
    /// </summary>
    public double AttitudeHead { get; set; }

    /// <summary>
    /// 横滚轴角度
    /// </summary>
    public double AttitudeRoll { get; set; }

    /// <summary>
    /// 俯仰轴角度
    /// </summary>
    public double AttitudePitch { get; set; }

    /// <summary>
    /// 相对起飞点高度
    /// </summary>
    public double Elevation { get; set; }

    /// <summary>
    /// 相对地球椭球面高度, 计算：相对起飞点的高度 + 起飞点的椭球高
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// 当前位置纬度
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// 当前位置经度
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// 垂直速度（单位：米每秒 / m/s）
    /// </summary>
    public double VerticalSpeed { get; set; }

    /// <summary>
    /// 水平速度
    /// </summary>
    public double HorizontalSpeed { get; set; }

    /// <summary>
    /// 固件升级状态
    /// </summary>
    public FirmwareUpgradeStatusEnum FirmwareUpgradeStatus { get; set; }

    /// <summary>
    /// 一致性升级：指飞行器某些模块的固件版本与系统匹配版本不一致，需要进行升级。
    /// 常见的情况例如：飞行器与遥控器已经升级至最新版本，但替换电池时发现电池未升级，此时一致性升级将被提示。
    /// 普通升级：开发者将飞行器所有模块升级至指定固件版本。
    /// </summary>
    public CompatibleStatusEnum CompatibleStatus { get; set; }

    /// <summary>
    /// 固件版本
    /// </summary>
    public string FirmwareVersion { get; set; }

    /// <summary>
    /// 档位
    /// </summary>
    public GearModeEnum Gear { get; set; }

    /// <summary>
    /// 飞行器进入当前状态的原因
    /// </summary>
    public int ModeCodeReason { get; set; }

    /// <summary>
    /// 指点飞行高度（相对(机场)起飞点的高度，相对高 ALT）（单位：米 / m）
    /// </summary>
    public double CommanderFlightHeight { get; set; }

    /// <summary>
    /// 指点飞行模式设置值
    /// </summary>
    public CommanderFlightModeEnum CommanderFlightMode { get; set; }

    /// <summary>
    /// 执行指点飞行时失控了，选择继续执行完，还是执行普通失控行为
    /// </summary>
    public CommanderModeLostActionEnum CommanderModeLostAction { get; set; }

    /// <summary>
    /// 用户对相机拍摄的照片和录像文件进行水印配置, 目前暂不支持直播画面水印
    /// </summary>
    public CameraWatermarkSettings CameraWatermarkSettings { get; set; }

    /// <summary>
    /// 机场选址指飞行器在空中悬停，为机场选址以及检查 RTK 信号质量
    /// </summary>
    public FlightModeEnum ModeCode { get; set; }

    /// <summary>
    /// 飞行器限远状态
    /// </summary>
    public DistanceLimitStatus DistanceLimitStatus { get; set; }

    /// <summary>
    /// psdk ui 资源包
    /// </summary>
    public List<PsdkUiResourceItem> PsdkUiResource { get; set; }

    /// <summary>
    /// psdk 负载设备属性值
    /// </summary>
    public List<PsdkWidgetValue> PsdkWidgetValues { get; set; }
}

// =============== Enums ===============

public enum RthModeEnum
{
    Smart = 0,
    Fixed = 1
}

public enum NearLimitStateEnum
{
    NotReached = 0,
    Approaching = 1
}

public enum NightLightStateEnum
{
    Off = 0,
    On = 1
}

public enum WindDirectionEnum
{
    North = 1,
    Northeast = 2,
    East = 3,
    Southeast = 4,
    South = 5,
    Southwest = 6,
    West = 7,
    Northwest = 8
}

public enum FirmwareUpgradeStatusEnum
{
    NotUpgrading = 0,
    Upgrading = 1
}

public enum CompatibleStatusEnum
{
    NoUpgradeNeeded = 0,
    UpgradeNeeded = 1
}

public enum GearModeEnum
{
    A = 0,
    P = 1,
    NAV = 2,
    FPV = 3,
    FARM = 4,
    S = 5,
    F = 6,
    M = 7,
    G = 8,
    T = 9
}

public enum CommanderFlightModeEnum
{
    SmartHeight = 0,
    FixedHeight = 1
}

public enum CommanderModeLostActionEnum
{
    ContinueTask = 0,
    ExitToNormal = 1
}

public enum FlightModeEnum
{
    Standby = 0,
    TakeoffPreparing = 1,
    TakeoffReady = 2,
    ManualFlying = 3,
    AutoTakeoff = 4,
    WaypointFlying = 5,
    Panorama = 6,
    FollowMe = 7,
    AdsbAvoid = 8,
    AutoRTH = 9,
    AutoLanding = 10,
    ForcedLanding = 11,
    ThreePropLanding = 12,
    Upgrading = 13,
    Disconnected = 14,
    APAS = 15,
    VirtualJoystick = 16,
    CommandFlying = 17,
    RTKConverging = 18,
    DockSiteSelection = 19
}

// =============== Structs / Classes ===============

public partial class WirelessLinkTopoDrone
{
    /// <summary>
    /// 加密编码（固定长度28）
    /// </summary>
    public int[] SecretCode { get; set; } = new int[28];

    /// <summary>
    /// 飞行器对频信息
    /// </summary>
    public SdrNode CenterNode { get; set; }

    /// <summary>
    /// 当前连接的机场或遥控器对频信息
    /// </summary>
    public List<SdrNode> LeafNodes { get; set; }
}

public partial class SdrNode
{
    /// <summary>
    /// 扰码信息
    /// </summary>
    public int SdrId { get; set; }

    /// <summary>
    /// 设备sn
    /// </summary>
    public string Sn { get; set; }

    /// <summary>
    /// 控制源序号（1~2）
    /// </summary>
    public int? ControlSourceIndex { get; set; }
}

public partial class CameraInfo
{
    /// <summary>
    /// 剩余拍照张数
    /// </summary>
    public int RemainPhotoNum { get; set; }

    /// <summary>
    /// 剩余录像时间（单位：秒 / s）
    /// </summary>
    public int RemainRecordDuration { get; set; }

    /// <summary>
    /// 视频录制时长（单位：秒 / s）
    /// </summary>
    public int RecordTime { get; set; }

    /// <summary>
    /// 负载编号，相机枚举值。非标准的 device_mode_key，格式为 {type-subtype-gimbalindex}
    /// </summary>
    public string PayloadIndex { get; set; }

    /// <summary>
    /// 相机模式
    /// </summary>
    public CameraModeEnum CameraMode { get; set; }

    /// <summary>
    /// 拍照状态
    /// </summary>
    public PhotoStateEnum PhotoState { get; set; }

    /// <summary>
    /// 分屏是否使能
    /// </summary>
    public bool ScreenSplitEnable { get; set; }

    /// <summary>
    /// 录像状态
    /// </summary>
    public RecordingStateEnum RecordingState { get; set; }

    /// <summary>
    /// 变焦倍数
    /// </summary>
    public double ZoomFactor { get; set; }

    /// <summary>
    /// 红外变焦倍数
    /// </summary>
    public double IrZoomFactor { get; set; }

    /// <summary>
    /// 变焦相机的视场角相对于广角相机或者红外相机的视场角，在 liveview 中会有所不同。坐标原点为镜头左上角。
    /// </summary>
    public LiveviewRegion LiveviewWorldRegion { get; set; }

    /// <summary>
    /// 照片存储设置集合，取值范围{current, vision, ir}
    /// </summary>
    public List<string> PhotoStorageSettings { get; set; }

    /// <summary>
    /// 视频存储设置集合，取值范围{current, vision, ir}
    /// </summary>
    public List<string> VideoStorageSettings { get; set; }

    public ExposureModeEnum WideExposureMode { get; set; }
    public IsoEnum WideIso { get; set; }
    public ShutterSpeedEnum WideShutterSpeed { get; set; }
    public ExposureValueEnum WideExposureValue { get; set; }

    public ExposureModeEnum ZoomExposureMode { get; set; }
    public IsoEnum ZoomIso { get; set; }
    public ShutterSpeedEnum ZoomShutterSpeed { get; set; }
    public ExposureValueEnum ZoomExposureValue { get; set; }

    public FocusModeEnum ZoomFocusMode { get; set; }
    public int ZoomFocusValue { get; set; }
    public int ZoomMaxFocusValue { get; set; }
    public int ZoomMinFocusValue { get; set; }
    public int ZoomCalibrateFarthestFocusValue { get; set; }
    public int ZoomCalibrateNearestFocusValue { get; set; }
    public FocusStateEnum ZoomFocusState { get; set; }

    public IrMeteringModeEnum IrMeteringMode { get; set; }
    public IrMeteringPoint IrMeteringPoint { get; set; }
    public IrMeteringArea IrMeteringArea { get; set; }
}

public enum CameraModeEnum { Photo = 0, Video = 1, LowLight = 2, Panorama = 3 }
public enum PhotoStateEnum { Idle = 0, Taking = 1 }
public enum RecordingStateEnum { Idle = 0, Recording = 1 }
public enum ExposureModeEnum { Auto = 1, ShutterPriority = 2, AperturePriority = 3, Manual = 4 }
public enum IsoEnum
{
    Auto = 0,
    AutoHighSense = 1,
    ISO50 = 2,
    ISO100 = 3,
    ISO200 = 4,
    ISO400 = 5,
    ISO800 = 6,
    ISO1600 = 7,
    ISO3200 = 8,
    ISO6400 = 9,
    ISO12800 = 10,
    ISO25600 = 11,
    FIXED = 255
}

public enum ShutterSpeedEnum
{
    _1_8000 = 0, _1_6400 = 1, _1_6000 = 2, _1_5000 = 3, _1_4000 = 4, _1_3200 = 5, _1_3000 = 6, _1_2500 = 7,
    _1_2000 = 8, _1_1600 = 9, _1_1500 = 10, _1_1250 = 11, _1_1000 = 12, _1_800 = 13, _1_725 = 14, _1_640 = 15,
    _1_500 = 16, _1_400 = 17, _1_350 = 18, _1_320 = 19, _1_250 = 20, _1_240 = 21, _1_200 = 22, _1_180 = 23,
    _1_160 = 24, _1_125 = 25, _1_120 = 26, _1_100 = 27, _1_90 = 28, _1_80 = 29, _1_60 = 30, _1_50 = 31,
    _1_40 = 32, _1_30 = 33, _1_25 = 34, _1_20 = 35, _1_15 = 36, _1_12_5 = 37, _1_10 = 38, _1_8 = 39,
    _1_6_25 = 40, _1_5 = 41, _1_4 = 42, _1_3 = 43, _1_2_5 = 44, _1_2 = 45, _1_1_67 = 46, _1_1_25 = 47,
    _1_0 = 48, _1_3s = 49, _1_6s = 50, _2_0s = 51, _2_5s = 52, _3_0s = 53, _3_2s = 54, _4_0s = 55,
    _5_0s = 56, _6_0s = 57, _7_0s = 58, _8_0s = 59, Auto = 65534
}

public enum ExposureValueEnum
{
    EV_N5_0 = 1, EV_N4_7 = 2, EV_N4_3 = 3, EV_N4_0 = 4, EV_N3_7 = 5, EV_N3_3 = 6, EV_N3_0 = 7, EV_N2_7 = 8,
    EV_N2_3 = 9, EV_N2_0 = 10, EV_N1_7 = 11, EV_N1_3 = 12, EV_N1_0 = 13, EV_N0_7 = 14, EV_N0_3 = 15, EV_0 = 16,
    EV_P0_3 = 17, EV_P0_7 = 18, EV_P1_0 = 19, EV_P1_3 = 20, EV_P1_7 = 21, EV_P2_0 = 22, EV_P2_3 = 23, EV_P2_7 = 24,
    EV_P3_0 = 25, EV_P3_3 = 26, EV_P3_7 = 27, EV_P4_0 = 28, EV_P4_3 = 29, EV_P4_7 = 30, EV_P5_0 = 31, FIXED = 255
}

public enum FocusModeEnum { MF = 0, AFS = 1, AFC = 2 }
public enum FocusStateEnum { Idle = 0, Focusing = 1, Success = 2, Failed = 3 }
public enum IrMeteringModeEnum { Off = 0, Point = 1, Area = 2 }

public partial class LiveviewRegion
{
    public double Left { get; set; }
    public double Top { get; set; }
    public double Right { get; set; }
    public double Bottom { get; set; }
}

public partial class IrMeteringPoint
{
    /// <summary>
    /// 以镜头的左上角为坐标中心点，水平方向为 x（0~1）
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// 以镜头的左上角为坐标中心点，竖直方向为 y（0~1）
    /// </summary>
    public double Y { get; set; }

    public double Temperature { get; set; }
}

public partial class IrMeteringArea
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double AverTemperature { get; set; }
    public TemperaturePoint MinTemperaturePoint { get; set; }
    public TemperaturePoint MaxTemperaturePoint { get; set; }
}

public partial class TemperaturePoint
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Temperature { get; set; }
}

// ====== Dongle 相关 ======

public partial class DongleInfoDrone
{
    public string Imei { get; set; }
    public DongleTypeEnum DongleType { get; set; }
    public string Eid { get; set; }
    public EsimActivateStateEnum EsimActivateState { get; set; }
    public SimCardStateEnum SimCardState { get; set; }
    public SimSlotEnum SimSlot { get; set; }
    public List<EsimInfoDrone> EsimInfos { get; set; }
    public SimInfoDrone SimInfo { get; set; }
}

public enum DongleTypeEnum { Old = 6, NewWithEsims = 10 }
public enum EsimActivateStateEnum { Unknown = 0, NotActivated = 1, Activated = 2 }
public enum SimCardStateEnum { NotInserted = 0, Inserted = 1 }
public enum SimSlotEnum { Unknown = 0, PhysicalSim = 1, Esim = 2 }

public partial class EsimInfoDrone
{
    public TelecomOperatorEnum TelecomOperator { get; set; }
    public bool Enabled { get; set; }
    public string Iccid { get; set; }
}

public partial class SimInfoDrone
{
    public TelecomOperatorEnum TelecomOperator { get; set; }
    public SimTypeEnum SimType { get; set; }
    public string Iccid { get; set; }
}

public enum TelecomOperatorEnum { Unknown = 0, ChinaMobile = 1, ChinaUnicom = 2, ChinaTelecom = 3 }
public enum SimTypeEnum { Unknown = 0, Normal = 1, TriNetwork = 2 }

// ====== 其他结构体 ======

public partial class ObstacleAvoidance
{
    public ObstacleStateEnum Horizon { get; set; }
    public ObstacleStateEnum Upside { get; set; }
    public ObstacleStateEnum Downside { get; set; }
}

public enum ObstacleStateEnum { Off = 0, On = 1 }

public partial class MaintainStatusDrone
{
    public List<MaintainStatusItemDrone> MaintainStatusArray { get; set; }
}

public partial class MaintainStatusItemDrone
{
    public MaintenanceStateEnum State { get; set; }
    public LastMaintainTypeEnum LastMaintainType { get; set; }
    public long LastMaintainTime { get; set; } // Unix timestamp
    public int LastMaintainFlightTime { get; set; } // hours
    public int LastMaintainFlightSorties { get; set; }
}

public enum MaintenanceStateEnum { None = 0, Exists = 1 }
public enum LastMaintainTypeEnum { Basic = 1, Routine = 2, Deep = 3 }

public partial class PayloadGimbalInfo
{
    public double GimbalPitch { get; set; } // degree
    public double GimbalRoll { get; set; } // degree
    public double GimbalYaw { get; set; } // degree
    public string PayloadIndex { get; set; }
    public double ZoomFactor { get; set; }
    public ThermalPaletteStyleEnum ThermalCurrentPaletteStyle { get; set; }
    public List<ThermalPaletteStyleEnum> ThermalSupportedPaletteStyles { get; set; }
    public ThermalGainModeEnum ThermalGainMode { get; set; }
    public ThermalIsothermStateEnum ThermalIsothermState { get; set; }
    public int ThermalIsothermUpperLimit { get; set; } // °C
    public int ThermalIsothermLowerLimit { get; set; } // °C
    public float ThermalGlobalTemperatureMin { get; set; } // °C
    public float ThermalGlobalTemperatureMax { get; set; } // °C
}

public enum ThermalPaletteStyleEnum
{
    WhiteHot = 0, BlackHot = 1, RedHot = 2, Medical = 3, Rainbow1 = 5, IronRed = 6,
    Arctic = 8, Lava = 11, HotIron = 12, Rainbow2 = 13
}

public enum ThermalGainModeEnum
{
    Auto = 0,
    LowGain = 1, // 0°C-500°C
    HighGain = 2  // -20°C-150°C
}

public enum ThermalIsothermStateEnum { Off = 0, On = 1 }

public partial class PositionStateDrone
{
    public ConvergenceStateEnum IsFixed { get; set; }
    public GpsQualityEnum Quality { get; set; }
    public int GpsNumber { get; set; }
    public int RtkNumber { get; set; }
}

public enum ConvergenceStateEnum { NotStarted = 0, Converging = 1, Success = 2, Failed = 3 }
public enum GpsQualityEnum { Level1 = 1, Level2 = 2, Level3 = 3, Level4 = 4, Level5 = 5, RtkFixed = 10 }

public partial class StorageInfo
{
    public int Total { get; set; } // KB
    public int Used { get; set; } // KB
}

public partial class BatteryInfo
{
    public int CapacityPercent { get; set; }
    public int RemainFlightTime { get; set; } // seconds
    public int ReturnHomePower { get; set; }
    public int LandingPower { get; set; }
    public List<BatteryDetailDrone> Batteries { get; set; }
}

public partial class BatteryDetailDrone
{
    public int CapacityPercent { get; set; }
    public int Index { get; set; }
    public string Sn { get; set; }
    public int Type { get; set; }
    public int SubType { get; set; }
    public string FirmwareVersion { get; set; }
    public int LoopTimes { get; set; }
    public int Voltage { get; set; } // mV
    public float Temperature { get; set; } // °C
    public int HighVoltageStorageDays { get; set; } // days
}

public partial class DistanceLimitStatus
{
    public DistanceLimitStateEnum State { get; set; }
    public int DistanceLimit { get; set; } // meters
    public NearLimitStateEnum IsNearDistanceLimit { get; set; }
}

public enum DistanceLimitStateEnum { NotSet = 0, Set = 1 }

public partial class PsdkUiResourceItem
{
    public int PsdkIndex { get; set; }
    public PsdkReadyStateEnum PsdkReady { get; set; }
    public string ObjectKey { get; set; }
}

public enum PsdkReadyStateEnum { NotReady = 0, Ready = 1 }

public partial class PsdkWidgetValue
{
    public int PsdkIndex { get; set; }
    public string PsdkName { get; set; }
    public string PsdkSn { get; set; }
    public string PsdkVersion { get; set; }
    public string PsdkLibVersion { get; set; }
    public SpeakerInfo Speaker { get; set; }
    // 注意：原始数据截断，此处仅保留已知字段
}

public partial class SpeakerInfo
{
    public SpeakerWorkModeEnum WorkMode { get; set; }
}

public enum SpeakerWorkModeEnum { TTS = 0, AudioPlayback = 1 }

public partial class CameraWatermarkSettings
{
    public WatermarkSwitchEnum GlobalEnable { get; set; }
    public WatermarkSwitchEnum DroneTypeEnable { get; set; }
    public WatermarkSwitchEnum DroneSnEnable { get; set; }
    public WatermarkSwitchEnum DatetimeEnable { get; set; }
    public WatermarkSwitchEnum GpsEnable { get; set; }
    public WatermarkSwitchEnum UserCustomStringEnable { get; set; }
    public string UserCustomString { get; set; } // 最多250字节
    public WatermarkLayoutEnum Layout { get; set; }
}

public enum WatermarkSwitchEnum { Off = 0, On = 1 }
public enum WatermarkLayoutEnum { TopLeft = 0, BottomLeft = 1, TopRight = 2, BottomRight = 3 }