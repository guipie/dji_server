// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using System.Linq;
using Dji.Application.Cloud.Dto.Hms;
using Dji.Application.Service.Common;
using Dji.Core.Enum.DjiEnum.Hms;
using Furion.JsonSerialization;

namespace Dji.Application.CloudRepository;

/// <summary>
/// HMS 告警仓储（全量快照 → 增量维护活跃状态）。
/// </summary>
/// <remarks>
/// <para>
/// <b>核心难点在于「快照」与「记录」的转换</b>：设备每次都推「我现在的全部告警」，
/// 但业务上需要的是「这条告警从什么时候开始、到什么时候结束」。
/// 因此每次同步要做三件事（缺一不可）：
/// ① 本次出现的 → 新增或刷新 <c>LastTime</c>，状态置为活跃；
/// ② 上次活跃但本次未出现的 → 状态置为已恢复，记录 <c>RecoverTime</c>；
/// ③ 已恢复的行<b>保留</b>，供事后追溯。
/// </para>
/// <para>
/// <b>为什么以「机场 SN」而不是「设备 SN」为分组边界</b>：HMS 报文来自网关（机场），
/// 设备侧并不上报自己的 SN，只能通过 <c>device_type</c> 判断是机场还是飞行器。
/// 而一个机场下至多挂一台飞行器，因此按机场分组即可保证不误清其它设备的告警。
/// </para>
/// </remarks>
public class DjiHmsRepository(
    SqlSugarRepository<DjiHmsAlarm> alarmRes,
    SqlSugarRepository<DjiDevice> deviceRes,
    HmsTextService textService,
    ILogger<DjiHmsRepository> logger) : BaseRepository
{
    private readonly SqlSugarRepository<DjiHmsAlarm> _alarmRes = alarmRes;
    private readonly SqlSugarRepository<DjiDevice> _deviceRes = deviceRes;
    private readonly HmsTextService _textService = textService;
    private readonly ILogger<DjiHmsRepository> _logger = logger;

    #region 读取

    /// <summary>取某机场当前活跃的告警</summary>
    public async Task<List<DjiHmsAlarm>> GetActiveAsync(string dockSn)
        => await _alarmRes.AsQueryable()
            .Where(m => m.DockSn == dockSn && m.Status == HmsAlarmStatusEnum.Active)
            .OrderBy(m => m.Level, OrderByType.Desc)
            .OrderBy(m => m.LastTime, OrderByType.Desc)
            .ToListAsync();

    /// <summary>按主键取单条</summary>
    public async Task<DjiHmsAlarm> GetAsync(long id)
        => await _alarmRes.GetByIdAsync(id);

    #endregion

    #region 写入

    /// <summary>
    /// 用一次全量报文同步某机场的告警状态。
    /// </summary>
    /// <param name="dockSn">网关（机场）SN</param>
    /// <param name="items">本次报文里的全部活跃告警</param>
    /// <param name="reportTimestamp">报文时间戳（毫秒）</param>
    /// <returns>本次新增的活跃告警数；无法落库（设备未建档）时返回 -1</returns>
    /// <remarks>
    /// 报文为空列表是<b>合法且常见</b>的（所有告警都恢复了），此时应把该机场下所有活跃告警
    /// 一并标记为已恢复，而不是当作无效报文忽略 —— 这恰恰是最需要落库的一次。
    /// </remarks>
    public async Task<int> SyncAsync(string dockSn, List<HmsAlarmItem> items, long reportTimestamp)
    {
        if (dockSn.IsNullOrWhiteSpace()) return -1;

        var device = await _deviceRes.GetFirstAsync(m => m.Sn == dockSn);
        if (device == null)
        {
            _logger.LogWarning("机场 {DockSn} 上报 HMS 告警但尚未建档，本次告警未落库", dockSn);
            return -1;
        }

        items ??= [];
        var now = DateTime.Now;

        // 现有活跃记录：以「码 + 设备型号 + 部件 + 传感器」为身份，与下面的 upsert 口径保持一致
        var actives = await GetActiveAsync(dockSn);
        var activeMap = actives.ToDictionary(KeyOf);

        var incomingKeys = new HashSet<string>();
        var newCount = 0;

        foreach (var item in items)
        {
            if (item?.Code.IsNullOrWhiteSpace() != false) continue;

            var key = KeyOf(item);
            incomingKeys.Add(key);

            var (zh, en) = _textService.Resolve(item.Code, item.DeviceType, item.InTheSky ?? 0, item.Args);

            if (activeMap.TryGetValue(key, out var exist))
            {
                // 仍然活跃：只刷新时间与文案，保留 FirstTime 不动（它记录的是这条告警最早的起点）
                exist.LastTime = now;
                exist.ReportTimestamp = reportTimestamp;
                exist.Level = (HmsLevelEnum)(item.Level ?? (int)exist.Level);
                exist.Module = (HmsModuleEnum)(item.Module ?? (int)exist.Module);
                exist.InTheSky = item.InTheSky ?? exist.InTheSky;
                exist.Imminent = item.Imminent ?? exist.Imminent;
                if (!zh.IsNullOrWhiteSpace()) exist.TextZh = zh;
                if (!en.IsNullOrWhiteSpace()) exist.TextEn = en;

                await _alarmRes.AsUpdateable(exist)
                    .UpdateColumns(m => new
                    {
                        m.LastTime, m.ReportTimestamp, m.Level, m.Module,
                        m.InTheSky, m.Imminent, m.TextZh, m.TextEn
                    })
                    .ExecuteCommandAsync();
                continue;
            }

            var entity = new DjiHmsAlarm
            {
                WorkspaceId = device.WorkspaceId,
                DockSn = dockSn,
                DeviceType = item.DeviceType,
                DeviceSn = ResolveDeviceSn(item, dockSn, device),
                Code = item.Code,
                Level = (HmsLevelEnum)(item.Level ?? 0),
                Module = (HmsModuleEnum)(item.Module ?? (int)HmsModuleEnum.Hms),
                InTheSky = item.InTheSky ?? 0,
                Imminent = item.Imminent ?? 0,
                ComponentIndex = item.Args?.ComponentIndex ?? 0,
                SensorIndex = item.Args?.SensorIndex ?? 0,
                ArgsJson = item.Args == null ? null : JSON.Serialize(item.Args),
                TextZh = zh,
                TextEn = en,
                Status = HmsAlarmStatusEnum.Active,
                FirstTime = now,
                LastTime = now,
                ReportTimestamp = reportTimestamp,
            };

            await _alarmRes.InsertAsync(entity);
            newCount++;
        }

        // 本次未出现的活跃告警 → 已恢复
        var recovered = actives.Where(m => !incomingKeys.Contains(KeyOf(m))).ToList();
        if (recovered.Count > 0)
        {
            var ids = recovered.Select(m => m.Id).ToList();
            await _alarmRes.AsUpdateable()
                .SetColumns(m => new DjiHmsAlarm
                {
                    Status = HmsAlarmStatusEnum.Recovered,
                    RecoverTime = now,
                })
                .Where(m => ids.Contains(m.Id))
                .ExecuteCommandAsync();

            _logger.LogInformation("机场 {DockSn} 的 {Count} 条 HMS 告警已恢复：{Codes}",
                dockSn, recovered.Count, string.Join(",", recovered.Select(m => m.Code)));
        }

        return newCount;
    }

    /// <summary>手动清除（删除）单条告警记录</summary>
    public async Task<int> DeleteAsync(List<long> ids)
    {
        if (ids == null || ids.Count == 0) return 0;
        return await _alarmRes.AsDeleteable().Where(m => ids.Contains(m.Id)).ExecuteCommandAsync();
    }

    /// <summary>删除某时间点之前已恢复的历史告警（由定时任务调用，控制表体积）</summary>
    public async Task<int> CleanRecoveredAsync(DateTime before)
        => await _alarmRes.AsDeleteable()
            .Where(m => m.Status == HmsAlarmStatusEnum.Recovered && m.RecoverTime != null && m.RecoverTime < before)
            .ExecuteCommandAsync();

    #endregion

    #region 私有实现

    /// <summary>
    /// 告警身份键：码 + 设备型号 + 部件索引 + 传感器索引。
    /// </summary>
    /// <remarks>
    /// 与表上的唯一索引口径一致。同一告警码会在不同部件上同时出现（左右电池、前后充电杆），
    /// 只用 code 做键会把它们错误地合并成一条。
    /// </remarks>
    private static string KeyOf(HmsAlarmItem item)
        => BuildKey(item.Code, item.DeviceType, item.Args?.ComponentIndex ?? 0, item.Args?.SensorIndex ?? 0);

    private static string KeyOf(DjiHmsAlarm alarm)
        => BuildKey(alarm.Code, alarm.DeviceType, alarm.ComponentIndex, alarm.SensorIndex);

    private static string BuildKey(string code, string deviceType, int component, int sensor)
        => $"{code}|{deviceType}|{component}|{sensor}";

    /// <summary>
    /// 推断告警来源的物理设备 SN。
    /// </summary>
    /// <remarks>
    /// 协议不随告警报文给出设备 SN，只能按 <c>device_type</c> 的 domain 段推断：
    /// 机场本体的告警归网关 SN，其余（飞行器）优先取该机场下已建档的子设备 SN。
    /// 推断不出来时留空 —— 这一列只用于展示与筛选，空缺不影响告警本身的价值。
    /// </remarks>
    private static string ResolveDeviceSn(HmsAlarmItem item, string dockSn, DjiDevice dock)
    {
        var deviceType = item.DeviceType;
        if (deviceType.IsNullOrWhiteSpace()) return dockSn;

        var domain = deviceType.Split('-', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return domain == "3" ? dockSn : dock?.Sn ?? dockSn;
    }

    #endregion
}
