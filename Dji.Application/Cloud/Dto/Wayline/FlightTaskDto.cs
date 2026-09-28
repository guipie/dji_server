// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

namespace Dji.Application.Cloud.Dto.Wayline;

#region 下行（云 → 机场）

/// <summary>
/// 下发航线任务（<c>flighttask_prepare</c>）。
/// </summary>
/// <remarks>
/// 协议字段说明（摘自官方「下发任务」）：
/// <list type="bullet">
/// <item><see cref="ExecuteTime"/> —— 毫秒时间戳；<c>task_type</c> 为 0/1 时必填。立即任务要求机场收到指令的时间与
/// <see cref="ExecuteTime"/> 相差不超过 30 秒，否则机场直接报错拒绝，因此下发前必须用**当前时间**而非计划时间；</item>
/// <item><see cref="File"/> —— KMZ 的下载地址与 MD5 签名，机场据此校验文件完整性；</item>
/// <item><see cref="ReadyConditions"/> —— 条件任务（<c>task_type=2</c>）必填，其余类型忽略；</item>
/// <item>已废弃的 <c>flighttask_create</c> 不再实现，官方明确要求改用 prepare + execute。</item>
/// </list>
/// </remarks>
public class FlightTaskPrepareInput
{
    /// <summary>计划 ID（云端生成，全局唯一）</summary>
    public string FlightId { get; set; }

    /// <summary>任务执行时间（毫秒时间戳）；条件任务可为空</summary>
    public long? ExecuteTime { get; set; }

    /// <summary>任务类型：0 立即 / 1 定时 / 2 条件</summary>
    public int TaskType { get; set; }

    /// <summary>航线文件（KMZ）信息</summary>
    public FlightTaskFile File { get; set; }

    /// <summary>条件任务的准备条件（task_type=2 时必填）</summary>
    public FlightTaskReadyConditions ReadyConditions { get; set; }

    /// <summary>执行前检查条件，任一不满足即任务失败</summary>
    public FlightTaskExecutableConditions ExecutableConditions { get; set; }

    /// <summary>断点续飞信息（指定后任务从断点继续）</summary>
    public FlightTaskBreakPoint BreakPoint { get; set; }

    /// <summary>返航高度（米），取值范围 [20, 1500]</summary>
    public int? RthAltitude { get; set; }

    /// <summary>返航高度模式：0 智能高度 / 1 预设高度（机场当前仅支持预设高度）</summary>
    public int? RthMode { get; set; }

    /// <summary>失控动作：0 返航 / 1 悬停 / 2 降落（当前固定 0）</summary>
    public int? OutOfControlAction { get; set; }

    /// <summary>航线失控动作：0 继续执行航线 / 1 退出航线并执行失控动作（需与 KMZ 内配置一致）</summary>
    public int? ExitWaylineWhenRcLost { get; set; }

    /// <summary>航线精度类型：0 GPS 任务 / 1 高精度 RTK 任务（推荐 1）</summary>
    public int? WaylinePrecisionType { get; set; }

    /// <summary>模拟任务参数（室内调试用，需先拆除桨叶）</summary>
    public FlightTaskSimulateMission SimulateMission { get; set; }

    /// <summary>起飞与航线任务前是否进行飞行安全检查</summary>
    public int? FlightSafetyAdvanceCheck { get; set; }
}

/// <summary>航线文件（KMZ）信息</summary>
public class FlightTaskFile
{
    /// <summary>文件下载地址（机场可直接访问的绝对地址）</summary>
    public string Url { get; set; }

    /// <summary>文件内容 MD5 签名</summary>
    public string Fingerprint { get; set; }
}

/// <summary>条件任务的准备条件</summary>
public class FlightTaskReadyConditions
{
    /// <summary>可执行任务的最低电量百分比（飞行器电量需大于该值）</summary>
    public int? BatteryCapacity { get; set; }

    /// <summary>任务可执行时段起始时间（毫秒时间戳）</summary>
    public long? BeginTime { get; set; }

    /// <summary>任务可执行时段截止时间（毫秒时间戳）</summary>
    public long? EndTime { get; set; }
}

/// <summary>任务执行条件</summary>
public class FlightTaskExecutableConditions
{
    /// <summary>可执行任务所需的机场/飞行器最小剩余存储空间</summary>
    public int? StorageCapacity { get; set; }
}

/// <summary>断点信息</summary>
public class FlightTaskBreakPoint
{
    /// <summary>断点序号</summary>
    public int Index { get; set; }

    /// <summary>断点状态：0 航段上 / 1 航点上</summary>
    public int State { get; set; }

    /// <summary>当前航段进度（0–1）</summary>
    public double? Progress { get; set; }

    /// <summary>航线 ID</summary>
    public int WaylineId { get; set; }
}

/// <summary>模拟任务参数</summary>
public class FlightTaskSimulateMission
{
    /// <summary>是否启用模拟任务：0 不启用 / 1 启用</summary>
    public int IsEnable { get; set; }

    /// <summary>模拟起始点纬度</summary>
    public double Latitude { get; set; }

    /// <summary>模拟起始点经度</summary>
    public double Longitude { get; set; }
}

/// <summary>
/// 执行航线任务（<c>flighttask_execute</c>）。
/// </summary>
/// <remarks>
/// 多机场（蛙跳）任务需要 <see cref="MultiDockTask"/>；普通航线任务只传 <see cref="FlightId"/> 即可。
/// 本期只做单机场，因此 <see cref="MultiDockTask"/> 仅保留协议位，不主动填充。
/// </remarks>
public class FlightTaskExecuteInput
{
    /// <summary>计划 ID</summary>
    public string FlightId { get; set; }

    /// <summary>多机场任务参数（蛙跳任务必填，本期不使用）</summary>
    public object MultiDockTask { get; set; }
}

/// <summary>
/// 取消任务（<c>flighttask_undo</c>）。
/// </summary>
public class FlightTaskUndoInput
{
    /// <summary>计划 ID 集合</summary>
    public List<string> FlightIds { get; set; } = [];
}

/// <summary>
/// 结束任务（<c>flighttask_stop</c>）。
/// </summary>
public class FlightTaskStopInput
{
    /// <summary>任务 ID</summary>
    public string FlightId { get; set; }

    /// <summary>结束原因：0 正常结束 / 1 另一机场状态机异常</summary>
    public int Reason { get; set; }
}

#endregion

#region 上行（机场 → 云）

/// <summary>
/// 上报航线任务进度（<c>flighttask_progress</c>）。
/// </summary>
/// <remarks>
/// 报文结构为 <c>data: { output: {...}, result: 0 }</c>，即外层是 <see cref="Cloud.Entity.MqOutput{T}"/>，
/// 内层才是本类型；与 <c>flighttask_ready</c>（<c>data: { flight_ids: [...] }</c>，无 output 包裹）不同。
/// </remarks>
public class FlightTaskProgressOutput
{
    /// <summary>扩展内容</summary>
    public FlightTaskProgressExt Ext { get; set; }

    /// <summary>执行进度</summary>
    public FlightTaskProgress Progress { get; set; }

    /// <summary>
    /// 任务状态（字符串枚举，取值见 <c>WaylineJobStatus</c>）。
    /// </summary>
    /// <remarks>取 string 而非枚举：机场可能上报尚未收录的新状态，用 string 不丢信息。</remarks>
    public string Status { get; set; }
}

/// <summary>进度上报的扩展内容</summary>
public class FlightTaskProgressExt
{
    /// <summary>当前执行到的航点序号</summary>
    public int? CurrentWaypointIndex { get; set; }

    /// <summary>航线任务细分状态（取值见 WaylineMissionStateEnum）</summary>
    public int? WaylineMissionState { get; set; }

    /// <summary>本次任务产生的媒体文件数量</summary>
    public int? MediaCount { get; set; }

    /// <summary>航迹 ID</summary>
    public string TrackId { get; set; }

    /// <summary>计划 ID（部分固件在 ext 内冗余上报）</summary>
    public string FlightId { get; set; }

    /// <summary>航线 ID（部分固件在 ext 内冗余上报）</summary>
    public int? WaylineId { get; set; }

    /// <summary>断点信息（任务中断时上报，供云端续飞）</summary>
    public FlightTaskProgressBreakPoint BreakPoint { get; set; }
}

/// <summary>进度上报中的断点信息（比下发时多出经纬度/高度/偏航与中断原因）</summary>
public class FlightTaskProgressBreakPoint
{
    /// <summary>断点序号</summary>
    public int? Index { get; set; }

    /// <summary>断点状态：0 航段上 / 1 航点上</summary>
    public int? State { get; set; }

    /// <summary>当前航段进度（0–1）</summary>
    public double? Progress { get; set; }

    /// <summary>航线 ID</summary>
    public int? WaylineId { get; set; }

    /// <summary>中断原因码（业务错误码，取值见官方 break_reason 表）</summary>
    public int? BreakReason { get; set; }

    /// <summary>断点纬度</summary>
    public double? Latitude { get; set; }

    /// <summary>断点经度</summary>
    public double? Longitude { get; set; }

    /// <summary>断点椭球高（米）</summary>
    public double? Height { get; set; }

    /// <summary>断点偏航角</summary>
    public double? AttitudeHead { get; set; }
}

/// <summary>执行进度</summary>
public class FlightTaskProgress
{
    /// <summary>当前执行步骤（取值见 FlightTaskStep）</summary>
    public int? CurrentStep { get; set; }

    /// <summary>进度值（0–100）</summary>
    public int? Percent { get; set; }
}

/// <summary>
/// 任务满足准备条件通知（<c>flighttask_ready</c>，仅条件任务会收到）。
/// </summary>
public class FlightTaskReadyOutput
{
    /// <summary>当前满足条件的计划 ID 集合</summary>
    public List<string> FlightIds { get; set; } = [];
}

/// <summary>
/// 机场请求任务资源（<c>flighttask_resource_get</c>，上行 <c>requests</c>）。
/// </summary>
/// <remarks>机场在获取不到 KMZ 或航线文件失效时主动向云端重取，云端需回 <see cref="FlightTaskResourceGetOutput"/>。</remarks>
public class FlightTaskResourceGetInput
{
    /// <summary>计划 ID</summary>
    public string FlightId { get; set; }
}

/// <summary>任务资源应答（下行 <c>requests_reply</c>）</summary>
public class FlightTaskResourceGetOutput
{
    /// <summary>航线文件（KMZ）信息</summary>
    public FlightTaskFile File { get; set; }
}

/// <summary>
/// 查询目标机场当前任务（<c>flighttask_progress_get</c>，上行 <c>requests</c>）。
/// </summary>
/// <remarks>多机场（蛙跳）任务中，起飞机场需要知道降落机场的任务状态；单机场场景下也可用于运维排障。</remarks>
public class FlightTaskProgressGetInput
{
    /// <summary>目标设备 SN</summary>
    public string Sn { get; set; }

    /// <summary>目标计划的 flight_id（部分固件版本会携带）</summary>
    public string FlightId { get; set; }
}

/// <summary>查询目标机场当前任务的应答（下行 <c>requests_reply</c>）</summary>
public class FlightTaskProgressGetOutput
{
    /// <summary>设备最新任务 ID</summary>
    public string FlightId { get; set; }
}

#endregion

#region 云端配置下发（config）

/// <summary>
/// 机场请求云端配置（上行 <c>requests</c>，Method = <c>config</c>）。
/// </summary>
/// <remarks>
/// 机场在启动时会索取 App 凭证与 NTP 服务器地址；**若不回复，机场时钟无法校准，
/// 而航线任务对时间差极其敏感（立即任务要求 30 秒内），会导致任务下发失败**。
/// </remarks>
public class CloudConfigRequest
{
    /// <summary>配置维度：目前只有 "product"</summary>
    public string ConfigScope { get; set; }

    /// <summary>配置格式：目前只有 "json"</summary>
    public string ConfigType { get; set; }
}

/// <summary>云端配置应答（下行 <c>requests_reply</c>）</summary>
public class CloudConfigReply
{
    /// <summary>NTP 服务地址（为空则机场不校时，务必配置）</summary>
    public string NtpServerHost { get; set; }

    /// <summary>NTP 服务端口，缺省 123</summary>
    public int? NtpServerPort { get; set; }

    /// <summary>大疆开发者 App ID</summary>
    public string AppId { get; set; }

    /// <summary>大疆开发者 App Key</summary>
    public string AppKey { get; set; }

    /// <summary>大疆开发者 App License</summary>
    public string AppLicense { get; set; }
}

#endregion
