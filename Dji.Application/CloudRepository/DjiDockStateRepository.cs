// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using System.Collections.Concurrent;
using Dji.Application.Cloud.Dto.Device;
using Dji.JsonSerialization;

namespace Dji.Application.CloudRepository;

/// <summary>
/// 机场 OSD 快照仓储（写 <c>dji_dock_state</c> 的 OSD 列）。
/// </summary>
/// <remarks>
/// <para>
/// <b>与 <c>DjiLiveRepository</c> 的分工</b>：两者读写同一张 <c>dji_dock_state</c> 表，
/// 但列集合完全不重叠 —— 本类只碰 <c>osd_*</c> / 模式 / 状态类列，
/// 直播仓储只碰 <c>live_capacity</c> / <c>live_status</c> / <c>media_file_detail</c>。
/// 两边都用 <c>UpdateColumns</c> 显式列出自己负责的列，因此<b>并发写入不会互相覆盖</b>
/// （机场的 <c>state</c> 与 <c>osd</c> 是两条独立主题，天然并发）。
/// </para>
/// <para>
/// <b>为什么必须做变化检测</b>：OSD 是 <b>0.5Hz 定频上报</b>的，一个机场每 2 秒就有一帧。
/// 若每帧都写库，10 个机场就是每秒 5 次 UPDATE，SQLite 会迅速成为瓶颈，
/// 而实际上绝大多数帧的内容与上一帧完全相同（状态类属性只在变化时才变）。
/// 因此这里按「关键字段指纹」判断：指纹未变则跳过写库，
/// 但超过 <see cref="RefreshIntervalSeconds"/> 秒仍会强制写一次，
/// 以免前端的「最后更新于」永远停在很久以前，掩盖「设备其实还在正常上报」这一事实。
/// </para>
/// </remarks>
public class DjiDockStateRepository(
    SqlSugarRepository<DjiDockState> stateRes,
    ILogger<DjiDockStateRepository> logger) : BaseRepository
{
    private readonly SqlSugarRepository<DjiDockState> _stateRes = stateRes;
    private readonly ILogger<DjiDockStateRepository> _logger = logger;

    /// <summary>
    /// 即使内容没变化，也至少每隔这么久落库一次（秒）。
    /// </summary>
    /// <remarks>纯粹为了刷新「接收时间」，让前端能区分「设备正常但数据未变」与「设备已失联」。</remarks>
    private const int RefreshIntervalSeconds = 60;

    /// <summary>
    /// 各机场最近一次落库的指纹与时间。
    /// </summary>
    /// <remarks>
    /// 用静态字典而非实例字段：仓储由 DI 按请求创建（瞬时/作用域），实例字段无法跨请求保留状态。
    /// 键为机场 SN，条目数等于机场数量级（几十），无需担心内存。
    /// </remarks>
    private static readonly ConcurrentDictionary<string, (int Hash, DateTime Time)> _fingerprints = new();

    /// <summary>
    /// 写入一帧 OSD 快照。
    /// </summary>
    /// <param name="dockSn">机场 SN</param>
    /// <param name="osd">OSD 报文</param>
    /// <param name="timestamp">报文时间戳（毫秒）</param>
    /// <returns>true 表示本次实际写入了数据库，false 表示被变化检测跳过</returns>
    public async Task<bool> SaveOsdAsync(string dockSn, DockOsd osd, long timestamp)
    {
        if (dockSn.IsNullOrWhiteSpace() || osd == null) return false;

        var fingerprint = BuildFingerprint(osd);
        var now = DateTime.Now;

        if (_fingerprints.TryGetValue(dockSn, out var last)
            && last.Hash == fingerprint
            && (now - last.Time).TotalSeconds < RefreshIntervalSeconds)
        {
            return false;
        }

        var json = SafeSerialize(osd);
        // 三份子结构原文：插入与更新两条路径共用，避免重复序列化
        var subDeviceJson = SafeSerialize(osd.SubDevice);
        var positionStateJson = SafeSerialize(osd.PositionState);
        var networkStateJson = SafeSerialize(osd.NetworkState);

        var existing = await _stateRes.GetFirstAsync(m => m.DockSn == dockSn);
        if (existing == null)
        {
            var entity = new DjiDockState
            {
                DockSn = dockSn,
                OsdJson = json,
                ModeCode = osd.ModeCode,
                FlighttaskStepCode = osd.FlighttaskStepCode,
                CoverState = osd.CoverState,
                PutterState = osd.PutterState,
                DroneInDock = osd.DroneInDock,
                SupplementLightState = osd.SupplementLightState,
                BatteryStoreMode = osd.BatteryStoreMode,
                AlarmState = osd.AlarmState,
                AirConditionerState = osd.AirConditioner?.AirConditionerState,
                AirConditionerSwitchTime = osd.AirConditioner?.SwitchTime,
                DroneChargeState = osd.DroneChargeState?.State,
                DroneChargePercent = osd.DroneChargeState?.CapacityPercent,
                EmergencyStopState = osd.EmergencyStopState,
                SilentMode = osd.SilentMode,
                SdrLinkWorkmode = osd.WirelessLink?.LinkWorkmode,
                FirmwareVersion = osd.FirmwareVersion,
                FirmwareUpgradeStatus = osd.FirmwareUpgradeStatus,
                SubDeviceJson = subDeviceJson,
                PositionStateJson = positionStateJson,
                NetworkStateJson = networkStateJson,
                JobNumber = osd.JobNumber,
                AccTime = osd.AccTime,
                StorageTotal = osd.Storage?.Total,
                StorageUsed = osd.Storage?.Used,
                Temperature = osd.Temperature,
                Humidity = osd.Humidity,
                EnvironmentTemperature = osd.EnvironmentTemperature,
                WindSpeed = osd.WindSpeed,
                Rainfall = osd.Rainfall,
                ElectricSupplyVoltage = osd.ElectricSupplyVoltage,
                WorkingVoltage = osd.WorkingVoltage,
                WorkingCurrent = osd.WorkingCurrent,
                Longitude = osd.Longitude,
                Latitude = osd.Latitude,
                Height = osd.Height,
                OsdTimestamp = timestamp,
                OsdReceivedTime = now,
            };

            await _stateRes.InsertAsync(entity);
        }
        else
        {
            // 先取到局部变量：SqlSugar 的表达式树对 ?. 的翻译不可靠，
            // 而 OSD 的子结构（空调 / 充电 / 图传 / 存储）在部分固件上确实可能整段缺失。
            var airState = osd.AirConditioner?.AirConditionerState;
            var airSwitchTime = osd.AirConditioner?.SwitchTime;
            var chargeState = osd.DroneChargeState?.State;
            var chargePercent = osd.DroneChargeState?.CapacityPercent;
            var linkWorkmode = osd.WirelessLink?.LinkWorkmode;
            var storageTotal = (long?)osd.Storage?.Total;
            var storageUsed = (long?)osd.Storage?.Used;

            await _stateRes.AsUpdateable()
                .SetColumns(m => new DjiDockState
                {
                    OsdJson = json,
                    ModeCode = osd.ModeCode,
                    FlighttaskStepCode = osd.FlighttaskStepCode,
                    CoverState = osd.CoverState,
                    PutterState = osd.PutterState,
                    DroneInDock = osd.DroneInDock,
                    SupplementLightState = osd.SupplementLightState,
                    BatteryStoreMode = osd.BatteryStoreMode,
                    AlarmState = osd.AlarmState,
                    AirConditionerState = airState,
                    AirConditionerSwitchTime = airSwitchTime,
                    DroneChargeState = chargeState,
                    DroneChargePercent = chargePercent,
                    EmergencyStopState = osd.EmergencyStopState,
                    SilentMode = osd.SilentMode,
                    SdrLinkWorkmode = linkWorkmode,
                    FirmwareVersion = osd.FirmwareVersion,
                    FirmwareUpgradeStatus = osd.FirmwareUpgradeStatus,
                    SubDeviceJson = subDeviceJson,
                    PositionStateJson = positionStateJson,
                    NetworkStateJson = networkStateJson,
                    JobNumber = osd.JobNumber,
                    AccTime = osd.AccTime,
                    StorageTotal = storageTotal,
                    StorageUsed = storageUsed,
                    Temperature = osd.Temperature,
                    Humidity = osd.Humidity,
                    EnvironmentTemperature = osd.EnvironmentTemperature,
                    WindSpeed = osd.WindSpeed,
                    Rainfall = osd.Rainfall,
                    ElectricSupplyVoltage = osd.ElectricSupplyVoltage,
                    WorkingVoltage = osd.WorkingVoltage,
                    WorkingCurrent = osd.WorkingCurrent,
                    Longitude = osd.Longitude,
                    Latitude = osd.Latitude,
                    Height = osd.Height,
                    OsdTimestamp = timestamp,
                    OsdReceivedTime = now,
                })
                .Where(m => m.DockSn == dockSn)
                .ExecuteCommandAsync();
        }

        _fingerprints[dockSn] = (fingerprint, now);
        return true;
    }

    /// <summary>取机场快照（控制面板用）</summary>
    public async Task<DjiDockState> GetAsync(string dockSn)
        => dockSn.IsNullOrWhiteSpace() ? null : await _stateRes.GetFirstAsync(m => m.DockSn == dockSn);

    /// <summary>批量取机场快照（列表页用，避免 N+1 查询）</summary>
    public async Task<List<DjiDockState>> GetByDockSnsAsync(List<string> dockSns)
        => dockSns == null || dockSns.Count == 0
            ? []
            : await _stateRes.AsQueryable().Where(m => dockSns.Contains(m.DockSn)).ToListAsync();

    #region 私有实现

    /// <summary>
    /// 计算「会影响落库结果」的字段指纹。
    /// </summary>
    /// <remarks>
    /// 只覆盖本类实际写入的列。刻意不加入 <c>timestamp</c> 一类每帧都变的值，
    /// 否则指纹每帧都不同，变化检测会彻底失效。
    /// </remarks>
    private static int BuildFingerprint(DockOsd osd)
    {
        var raw = string.Join("|",
            osd.ModeCode, osd.FlighttaskStepCode, osd.CoverState, osd.PutterState, osd.DroneInDock,
            osd.SupplementLightState, osd.BatteryStoreMode, osd.AlarmState,
            osd.AirConditioner?.AirConditionerState, osd.AirConditioner?.SwitchTime,
            osd.DroneChargeState?.State, osd.DroneChargeState?.CapacityPercent,
            osd.EmergencyStopState, osd.SilentMode, osd.WirelessLink?.LinkWorkmode,
            osd.FirmwareVersion, osd.FirmwareUpgradeStatus,
            osd.JobNumber, osd.AccTime, osd.Storage?.Total, osd.Storage?.Used,
            osd.Temperature, osd.Humidity, osd.EnvironmentTemperature, osd.WindSpeed, osd.Rainfall,
            osd.ElectricSupplyVoltage, osd.WorkingVoltage, osd.WorkingCurrent,
            osd.Longitude, osd.Latitude, osd.Height);

        return raw.GetHashCode();
    }

    /// <summary>安全序列化：失败返回 null 而不是抛异常（快照的兜底字段不值得中断主流程）</summary>
    private string SafeSerialize(object value)
    {
        if (value == null) return null;
        try
        {
            return JSON.Serialize(value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "机场 OSD 子结构序列化失败，该字段将留空");
            return null;
        }
    }

    #endregion
}
