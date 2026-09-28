// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

namespace Dji.Core.Entity.DjiEntity;

/// <summary>
/// 航线任务进度流水（机场每次上报 <c>flighttask_progress</c> 落一条）。
/// </summary>
/// <remarks>
/// <para>
/// 为什么要单独建流水表：<see cref="DjiWaylineTask"/> 只保留任务**最新**状态，
/// 用于列表展示；而排障时需要还原「哪个步骤卡了多久」，因此把每次上报原样留痕。
/// </para>
/// <para>
/// 写入频率受机场上报节奏控制（任务执行期间约 1 次/秒量级），属只增不改的追加表，
/// 建议按 <see cref="TaskId"/> 建立索引，并按需做定期归档。
/// </para>
/// </remarks>
[SugarTable(null, "航线任务进度流水")]
[SugarIndex("index_DjiWaylineTaskProgress_TaskId", nameof(TaskId), OrderByType.Asc)]
public class DjiWaylineTaskProgress : EntityAppBase
{
    /// <summary>任务主键（<c>dji_wayline_task.Id</c>）</summary>
    [SugarColumn(ColumnDescription = "任务Id", IsNullable = false)]
    public long TaskId { get; set; }

    /// <summary>计划 ID（协议 flight_id）</summary>
    [SugarColumn(ColumnDescription = "计划ID", Length = 64, IsNullable = true)]
    public string FlightId { get; set; }

    /// <summary>上报机场 SN</summary>
    [SugarColumn(ColumnDescription = "机场SN", Length = 64, IsNullable = true)]
    public string DockSn { get; set; }

    /// <summary>任务状态（协议字符串）</summary>
    [SugarColumn(ColumnDescription = "任务状态", Length = 32, IsNullable = true)]
    public string Status { get; set; }

    /// <summary>当前步骤编码</summary>
    [SugarColumn(ColumnDescription = "步骤编码", IsNullable = true)]
    public int StepCode { get; set; }

    /// <summary>当前步骤描述</summary>
    [SugarColumn(ColumnDescription = "步骤描述", Length = 64, IsNullable = true)]
    public string StepText { get; set; }

    /// <summary>步骤内进度百分比（0–100）</summary>
    [SugarColumn(ColumnDescription = "进度百分比", IsNullable = true)]
    public int ProgressPercent { get; set; }

    /// <summary>当前执行到的航点序号</summary>
    [SugarColumn(ColumnDescription = "当前航点序号", IsNullable = true)]
    public int CurrentWaypointIndex { get; set; }

    /// <summary>航线任务细分状态</summary>
    [SugarColumn(ColumnDescription = "航线任务状态", IsNullable = true)]
    public int MissionState { get; set; }

    /// <summary>媒体文件数量</summary>
    [SugarColumn(ColumnDescription = "媒体文件数", IsNullable = true)]
    public int MediaCount { get; set; }

    /// <summary>断点信息 JSON（协议 ext.break_point 原文）</summary>
    [SugarColumn(ColumnDescription = "断点信息", ColumnDataType = "TEXT", IsNullable = true)]
    public string BreakPointJson { get; set; }

    /// <summary>报文时间戳（毫秒，取协议 timestamp，便于按设备时钟对齐）</summary>
    [SugarColumn(ColumnDescription = "报文时间戳", IsNullable = true)]
    public long ReportTimestamp { get; set; }
}
