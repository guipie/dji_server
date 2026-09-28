// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Core.Enum.DjiEnum.Wayline;

namespace Dji.Application.Service.DjiWayline.Dto;

/// <summary>
/// 航线任务列表输出。
/// </summary>
/// <remarks>
/// 状态与步骤除原始值外同时给出中文描述（<see cref="StatusText"/> / <see cref="StepText"/>）：
/// 前端列表不必再维护一份映射表，避免前后端字典不一致导致的显示错误。
/// </remarks>
public class DjiWaylineTaskOutput
{
    /// <summary>主键Id</summary>
    public long Id { get; set; }

    /// <summary>计划ID（协议 flight_id）</summary>
    public string FlightId { get; set; }

    /// <summary>任务名称</summary>
    public string JobName { get; set; }

    /// <summary>任务类型</summary>
    public TaskTypeCodeEnum TaskType { get; set; }

    /// <summary>计划执行时间（毫秒时间戳）</summary>
    public long ExecuteTime { get; set; }

    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>执行机场 SN</summary>
    public string DockSn { get; set; }

    /// <summary>机场昵称</summary>
    public string DockNick { get; set; }

    /// <summary>执行飞行器 SN</summary>
    public string DroneSn { get; set; }

    /// <summary>航线名称快照</summary>
    public string WaylineName { get; set; }

    /// <summary>协议内航线 ID</summary>
    public int WaylineId { get; set; }

    /// <summary>任务状态（协议字符串）</summary>
    public string Status { get; set; }

    /// <summary>任务状态中文描述</summary>
    public string StatusText { get; set; }

    /// <summary>当前步骤编码</summary>
    public int StepCode { get; set; }

    /// <summary>当前步骤中文描述</summary>
    public string StepText { get; set; }

    /// <summary>进度百分比</summary>
    public int ProgressPercent { get; set; }

    /// <summary>当前航点序号</summary>
    public int CurrentWaypointIndex { get; set; }

    /// <summary>航线任务细分状态</summary>
    public int MissionState { get; set; }

    /// <summary>媒体文件数量</summary>
    public int MediaCount { get; set; }

    /// <summary>就绪时间</summary>
    public DateTime? ReadyTime { get; set; }

    /// <summary>开始时间</summary>
    public DateTime? BeginTime { get; set; }

    /// <summary>结束时间</summary>
    public DateTime? EndTime { get; set; }

    /// <summary>错误码</summary>
    public int? ErrorCode { get; set; }

    /// <summary>错误描述</summary>
    public string ErrorMessage { get; set; }

    /// <summary>是否仍在跟踪中（非终态）</summary>
    public bool IsActive { get; set; }

    /// <summary>创建时间</summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>创建者</summary>
    public string CreateUserName { get; set; }
}

/// <summary>航线任务详情输出（含断点信息，供续飞）</summary>
public class DjiWaylineTaskDetailOutput : DjiWaylineTaskOutput
{
    /// <summary>平台航线主键</summary>
    public long? WaylineEntityId { get; set; }

    /// <summary>KMZ 文件名</summary>
    public string KmzFileName { get; set; }

    /// <summary>KMZ 内容 MD5（下发时快照）</summary>
    public string Fingerprint { get; set; }

    /// <summary>返航高度</summary>
    public int? RthAltitude { get; set; }

    /// <summary>失控动作</summary>
    public int? OutOfControlAction { get; set; }

    /// <summary>断点信息 JSON（可从断点续飞）</summary>
    public string BreakPointJson { get; set; }
}

/// <summary>任务进度流水输出</summary>
public class DjiWaylineTaskProgressOutput
{
    /// <summary>主键Id</summary>
    public long Id { get; set; }

    /// <summary>任务状态</summary>
    public string Status { get; set; }

    /// <summary>任务状态中文描述</summary>
    public string StatusText { get; set; }

    /// <summary>步骤编码</summary>
    public int StepCode { get; set; }

    /// <summary>步骤描述</summary>
    public string StepText { get; set; }

    /// <summary>进度百分比</summary>
    public int ProgressPercent { get; set; }

    /// <summary>当前航点序号</summary>
    public int CurrentWaypointIndex { get; set; }

    /// <summary>航线任务细分状态</summary>
    public int MissionState { get; set; }

    /// <summary>媒体文件数量</summary>
    public int MediaCount { get; set; }

    /// <summary>断点信息 JSON</summary>
    public string BreakPointJson { get; set; }

    /// <summary>报文时间戳（毫秒）</summary>
    public long ReportTimestamp { get; set; }

    /// <summary>记录时间</summary>
    public DateTime? CreateTime { get; set; }
}

/// <summary>状态字典项（供前端渲染筛选项与标签）</summary>
public class WaylineJobStatusOption
{
    /// <summary>状态原始值（协议字符串）</summary>
    public string Value { get; set; }

    /// <summary>中文描述</summary>
    public string Label { get; set; }

    /// <summary>是否为终态</summary>
    public bool IsTerminal { get; set; }
}

/// <summary>步骤字典项</summary>
public class FlightTaskStepOption
{
    /// <summary>步骤编码</summary>
    public int Value { get; set; }

    /// <summary>步骤描述</summary>
    public string Label { get; set; }
}

/// <summary>
/// 可选执行机场（下发任务时的机场下拉）。
/// </summary>
/// <remarks>
/// 不复用设备分页接口：后者返回的是「网关 + 子设备」树且无分页、无域过滤，
/// 前端要拿到机场列表还得自己遍历拆树，且拿不到「是否被任务占用」这一关键信息。
/// </remarks>
public class DockOptionOutput
{
    /// <summary>机场 SN</summary>
    public string Sn { get; set; }

    /// <summary>机场昵称</summary>
    public string Nick { get; set; }

    /// <summary>设备型号</summary>
    public string Model { get; set; }

    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>是否在线</summary>
    public bool IsOnline { get; set; }

    /// <summary>当前是否有未结束任务（有则不可再下发）</summary>
    public bool Busy { get; set; }

    /// <summary>占用中任务的当前状态描述</summary>
    public string BusyStatus { get; set; }

    /// <summary>是否可下发（在线且无未结束任务）</summary>
    public bool CanDispatch { get; set; }
}
