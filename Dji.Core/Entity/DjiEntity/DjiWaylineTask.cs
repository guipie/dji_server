// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Core.Enum.DjiEnum.Wayline;

namespace Dji.Core.Entity.DjiEntity;

/// <summary>
/// 航线任务（一次「下发 → 执行 → 结束」的完整记录）。
/// </summary>
/// <remarks>
/// <para>
/// 主键语义：<see cref="FlightId"/> 即上云协议的 <c>flight_id</c>（计划 ID），由云端生成，
/// 机场侧全程以它引用同一任务，因此必须全局唯一（<see cref="FlightId"/> 上建有唯一索引）。
/// </para>
/// <para>
/// 与 <see cref="DjiWaylineEntity"/> 的关系：一条航线可被多次执行，每次执行生成一条任务记录；
/// 这里用 <see cref="WaylineEntityId"/> 做内键关联，同时冗余 <see cref="WaylineId"/>（协议内的
/// workspace 级航线 ID）与 <see cref="WaylineName"/> 快照，保证航线被改名或删除后任务历史仍可读。
/// </para>
/// <para>
/// 状态流转由 <c>MqWaylineService</c> 依据 <c>flighttask_progress</c> 上报驱动：
/// <c>sent → (ready) → in_progress → (paused ⇄ in_progress) → ok / canceled / failed / timeout / partially_done</c>。
/// </para>
/// </remarks>
[SugarTable(null, "航线任务")]
[SugarIndex("index_DjiWaylineTask_FlightId", nameof(FlightId), OrderByType.Asc, true)]
[SugarIndex("index_DjiWaylineTask_DockSn_Status", nameof(DockSn), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public class DjiWaylineTask : EntityWorkspaceBase
{
    /// <summary>计划 ID（协议 flight_id，云端生成，全局唯一）</summary>
    [SugarColumn(ColumnDescription = "计划ID", Length = 64, IsNullable = false)]
    public string FlightId { get; set; }

    /// <summary>任务类型（0 立即 / 1 定时 / 2 条件）</summary>
    [SugarColumn(ColumnDescription = "任务类型", IsNullable = false)]
    public TaskTypeCodeEnum TaskType { get; set; }

    /// <summary>计划执行时间（毫秒时间戳）</summary>
    [SugarColumn(ColumnDescription = "计划执行时间", IsNullable = false)]
    public long ExecuteTime { get; set; }

    /// <summary>任务名称（前端展示用）</summary>
    [SugarColumn(ColumnDescription = "任务名称", Length = 64, IsNullable = true)]
    public string JobName { get; set; }

    /// <summary>执行机场 SN</summary>
    [SugarColumn(ColumnDescription = "执行机场", Length = 64, IsNullable = true)]
    public string DockSn { get; set; }

    /// <summary>执行飞行器 SN（执行前为空，由拓扑/OSD 或进度上报回填）</summary>
    [SugarColumn(ColumnDescription = "执行飞行器", Length = 64, IsNullable = true)]
    public string DroneSn { get; set; }

    /// <summary>平台航线主键（<c>dji_wayline.Id</c>）</summary>
    [SugarColumn(ColumnDescription = "平台航线Id", IsNullable = true)]
    public long? WaylineEntityId { get; set; }

    /// <summary>协议内航线 ID（同一 workspace 内唯一，机场据此在 KMZ 中定位航线）</summary>
    [SugarColumn(ColumnDescription = "航线ID", IsNullable = true, DefaultValue = "0")]
    public int WaylineId { get; set; }

    /// <summary>航线名称快照</summary>
    [SugarColumn(ColumnDescription = "航线名称", Length = 64, IsNullable = true)]
    public string WaylineName { get; set; }

    /// <summary>KMZ 文件名快照</summary>
    [SugarColumn(ColumnDescription = "KMZ文件名", Length = 256, IsNullable = true)]
    public string KmzFileName { get; set; }

    /// <summary>KMZ 内容 MD5（协议 <c>file.fingerprint</c>，下发时快照，便于事后核对机场实际执行的文件）</summary>
    [SugarColumn(ColumnDescription = "KMZ文件签名", Length = 64, IsNullable = true)]
    public string Fingerprint { get; set; }

    /// <summary>任务状态（协议字符串，取值见 <see cref="WaylineJobStatus"/>）</summary>
    [SugarColumn(ColumnDescription = "任务状态", Length = 32, IsNullable = true)]
    public string Status { get; set; }

    /// <summary>当前执行步骤（协议 <c>progress.current_step</c>，取值见 <see cref="FlightTaskStep"/>）</summary>
    [SugarColumn(ColumnDescription = "当前步骤", IsNullable = true, DefaultValue = "0")]
    public int StepCode { get; set; }

    /// <summary>当前步骤中文描述（冗余，避免列表查询时为每个任务换算描述）</summary>
    [SugarColumn(ColumnDescription = "步骤描述", Length = 64, IsNullable = true)]
    public string StepText { get; set; }

    /// <summary>步骤内进度百分比（0–100）</summary>
    [SugarColumn(ColumnDescription = "进度百分比", IsNullable = true, DefaultValue = "0")]
    public int ProgressPercent { get; set; }

    /// <summary>当前执行到的航点序号</summary>
    [SugarColumn(ColumnDescription = "当前航点序号", IsNullable = true, DefaultValue = "0")]
    public int CurrentWaypointIndex { get; set; }

    /// <summary>航线任务细分状态（<see cref="WaylineMissionStateEnum"/>）</summary>
    [SugarColumn(ColumnDescription = "航线任务状态", IsNullable = true, DefaultValue = "0")]
    public int MissionState { get; set; }

    /// <summary>本次任务产生的媒体文件数量</summary>
    [SugarColumn(ColumnDescription = "媒体文件数", IsNullable = true, DefaultValue = "0")]
    public int MediaCount { get; set; }

    /// <summary>满足准备条件的时刻（收到 <c>flighttask_ready</c> 时写入）</summary>
    [SugarColumn(ColumnDescription = "就绪时间", IsNullable = true)]
    public DateTime? ReadyTime { get; set; }

    /// <summary>
    /// 执行指令的发送时刻。
    /// </summary>
    /// <remarks>
    /// 用于后台任务的幂等控制：定时/条件任务的 <c>flighttask_execute</c> 由定时任务触发，
    /// 若不记录发送时刻，定时任务每分钟都会重发一次执行指令，机场会连续拒绝并污染任务状态。
    /// </remarks>
    [SugarColumn(ColumnDescription = "执行指令发送时间", IsNullable = true)]
    public DateTime? ExecuteSentTime { get; set; }

    /// <summary>实际开始执行时间（首次进入 in_progress）</summary>
    [SugarColumn(ColumnDescription = "开始时间", IsNullable = true)]
    public DateTime? BeginTime { get; set; }

    /// <summary>实际结束时间（进入终态）</summary>
    [SugarColumn(ColumnDescription = "结束时间", IsNullable = true)]
    public DateTime? EndTime { get; set; }

    /// <summary>错误码（协议 <c>break_reason</c> 或 <c>services_reply.result</c>）</summary>
    [SugarColumn(ColumnDescription = "错误码", IsNullable = true)]
    public int? ErrorCode { get; set; }

    /// <summary>错误描述</summary>
    [SugarColumn(ColumnDescription = "错误描述", Length = 256, IsNullable = true)]
    public string ErrorMessage { get; set; }

    /// <summary>断点信息 JSON（支持断点续飞，结构见协议的 break_point）</summary>
    [SugarColumn(ColumnDescription = "断点信息", ColumnDataType = "TEXT", IsNullable = true)]
    public string BreakPointJson { get; set; }

    /// <summary>返航高度（米）</summary>
    [SugarColumn(ColumnDescription = "返航高度", IsNullable = true)]
    public int? RthAltitude { get; set; }

    /// <summary>遥控器失控动作（0 返航 / 1 悬停 / 2 降落）</summary>
    [SugarColumn(ColumnDescription = "失控动作", IsNullable = true)]
    public int? OutOfControlAction { get; set; }
}
