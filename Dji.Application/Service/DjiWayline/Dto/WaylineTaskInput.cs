// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Cloud.Dto.Wayline;
using Dji.Core.Enum.DjiEnum.Wayline;

namespace Dji.Application.Service.DjiWayline.Dto;

/// <summary>航线任务分页查询输入</summary>
public class DjiWaylineTaskSearchInput : BasePageInput
{
    /// <summary>关键字（任务名称 / flight_id / 航线名称）</summary>
    public string SearchKey { get; set; }

    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>执行机场 SN</summary>
    public string DockSn { get; set; }

    /// <summary>任务状态（协议字符串，见 WaylineJobStatus）</summary>
    public string Status { get; set; }

    /// <summary>任务类型</summary>
    public TaskTypeCodeEnum? TaskType { get; set; }

    /// <summary>仅看未结束任务</summary>
    public bool? OnlyActive { get; set; }
}

/// <summary>
/// 下发航线任务输入。
/// </summary>
/// <remarks>
/// 对应协议 <c>flighttask_prepare</c>（以及紧随其后的 <c>flighttask_execute</c>）的入参。
/// 已废弃的 <c>flighttask_create</c> 官方明确不再使用，因此不提供对应入口。
/// </remarks>
public class CreateWaylineTaskInput
{
    /// <summary>平台航线主键（dji_wayline.Id）</summary>
    [Required(ErrorMessage = "请选择要执行的航线")]
    public long WaylineEntityId { get; set; }

    /// <summary>执行机场 SN</summary>
    [Required(ErrorMessage = "请选择执行机场")]
    public string DockSn { get; set; }

    /// <summary>任务名称（留空时用航线名称）</summary>
    public string JobName { get; set; }

    /// <summary>任务类型：0 立即 / 1 定时 / 2 条件</summary>
    public TaskTypeCodeEnum TaskType { get; set; } = TaskTypeCodeEnum.Immediate;

    /// <summary>
    /// 计划执行时间。
    /// </summary>
    /// <remarks>
    /// <c>task_type = 0</c> 时忽略此值（服务端取当前时间，避免超出机场 30 秒容差）；
    /// <c>1</c> 时必填；<c>2</c> 可选。
    /// </remarks>
    public DateTime? ExecuteTime { get; set; }

    /// <summary>返航高度（米），取值范围 [20, 1500]</summary>
    public int? RthAltitude { get; set; }

    /// <summary>失控动作：0 返航 / 1 悬停 / 2 降落</summary>
    public int? OutOfControlAction { get; set; }

    /// <summary>航线失控动作：0 继续执行航线 / 1 退出航线并执行失控动作</summary>
    public int? ExitWaylineWhenRcLost { get; set; }

    /// <summary>航线精度类型：0 GPS 任务 / 1 高精度 RTK 任务</summary>
    public int? WaylinePrecisionType { get; set; }

    /// <summary>条件任务的准备条件（task_type = 2 时必填）</summary>
    public FlightTaskReadyConditions ReadyConditions { get; set; }

    /// <summary>执行前检查条件</summary>
    public FlightTaskExecutableConditions ExecutableConditions { get; set; }

    /// <summary>模拟任务参数（室内调试，需先拆桨）</summary>
    public FlightTaskSimulateMission SimulateMission { get; set; }

    /// <summary>是否开启飞行安全检查</summary>
    public int? FlightSafetyAdvanceCheck { get; set; }
}

/// <summary>航线任务控制输入（暂停 / 恢复 / 取消 / 结束）</summary>
public class WaylineTaskControlInput
{
    /// <summary>任务主键（dji_wayline_task.Id）</summary>
    [Required(ErrorMessage = "任务Id不能为空")]
    public long Id { get; set; }
}

/// <summary>结束任务输入</summary>
public class StopWaylineTaskInput : WaylineTaskControlInput
{
    /// <summary>结束原因：0 正常结束 / 1 另一机场状态机异常（单机场固定 0）</summary>
    public int Reason { get; set; }
}

/// <summary>任务主键查询输入</summary>
public class QueryByIdWaylineTaskInput : BaseIdInput
{
}

/// <summary>任务进度流水查询输入</summary>
public class WaylineTaskProgressInput : BaseIdInput
{
    /// <summary>最多返回条数</summary>
    public int Take { get; set; } = 200;
}

/// <summary>按机场查询活跃任务输入</summary>
public class ActiveWaylineTaskInput
{
    /// <summary>机场 SN</summary>
    [Required(ErrorMessage = "机场SN不能为空")]
    public string DockSn { get; set; }
}
