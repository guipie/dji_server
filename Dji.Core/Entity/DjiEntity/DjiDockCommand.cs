// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Core.Enum.DjiEnum.Dock;

namespace Dji.Core.Entity.DjiEntity;

/// <summary>
/// 机场控制指令 / 运维操作记录（下发 + 执行结果 + 操作人）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么必须有这张表</b>：机场控制指令（开合舱盖、重启、格式化）的链路是「下发即返回 + 异步进度」：
/// <c>services</c> 下发后机场先回一个 <c>services_reply</c>（表示「收到了」），
/// 真正的执行结果与百分比随后通过 <c>events</c> 陆续推送（<c>cover_open</c>、<c>device_reboot</c> …）。
/// 两次回包之间可能间隔数分钟（格式化尤其慢），靠内存状态无法跨请求、更无法跨服务重启，
/// 因此必须落库。
/// </para>
/// <para>
/// <b>同时充当操作审计</b>：重启、格式化这类高危操作必须能追溯「谁、什么时候、对哪台机场做了什么」。
/// 官方协议不提供操作人信息，<see cref="OperatorId"/> / <see cref="OperatorName"/> 由云端在接收请求时补齐。
/// </para>
/// <para>
/// <b>唯一键选择</b>：用协议 <c>bid</c>（业务会话 ID）作为幂等键 —— 同一条 <c>events</c> 进度会被设备多次推送，
/// 带同一个 <c>bid</c>，必须归并到同一行而不是每次新增。
/// </para>
/// <para>
/// <b>时间列</b>：下发时刻直接复用基类的 <c>CreateTime</c>（基类已是「插入时写入、更新时不覆盖」语义，
/// 对这条记录而言它就等价于「下发时间」），不再重复声明一个同义列 ——
/// 重复声明会隐藏基类成员并触发 CS0114，且两列内容完全一致。
/// </para>
/// </remarks>
[SugarTable(null, "机场操作记录")]
[SugarIndex("index_DjiDockCommand_Bid", nameof(Bid), OrderByType.Asc, true)]
[SugarIndex("index_DjiDockCommand_DockTime", nameof(DockSn), OrderByType.Asc, nameof(CreateTime), OrderByType.Desc)]
[SugarIndex("index_DjiDockCommand_Status", nameof(Status), OrderByType.Asc)]
public class DjiDockCommand : EntityWorkspaceBase
{
    /// <summary>目标机场 SN</summary>
    [SugarColumn(ColumnDescription = "机场SN", Length = 64, IsNullable = false)]
    public string DockSn { get; set; }

    /// <summary>协议方法名（如 <c>cover_open</c>，与 <c>TopicMethods</c> 常量一致）</summary>
    [SugarColumn(ColumnDescription = "协议方法", Length = 64, IsNullable = false)]
    public string Method { get; set; }

    /// <summary>操作中文名（如「打开舱盖」），落库保存以免后续改文案影响历史记录</summary>
    [SugarColumn(ColumnDescription = "操作名称", Length = 64, IsNullable = true)]
    public string ActionName { get; set; }

    /// <summary>业务会话 ID（协议 <c>bid</c>，进度报文的归并键）</summary>
    [SugarColumn(ColumnDescription = "业务ID", Length = 64, IsNullable = true)]
    public string Bid { get; set; }

    /// <summary>风险等级，前端据此决定是否弹二次确认</summary>
    [SugarColumn(ColumnDescription = "风险等级", IsNullable = true)]
    public DockCommandRiskEnum RiskLevel { get; set; }

    /// <summary>当前状态（下发中 / 执行中 / 成功 / 失败 …）</summary>
    [SugarColumn(ColumnDescription = "执行状态", IsNullable = true)]
    public DockTaskStatusEnum Status { get; set; }

    /// <summary>进度百分比（协议 <c>output.progress.percent</c>，0~100）</summary>
    [SugarColumn(ColumnDescription = "进度百分比", IsNullable = true)]
    public int? Percent { get; set; }

    /// <summary>当前步骤键（协议 <c>output.progress.step_key</c>，如 <c>check_work_mode</c>）</summary>
    [SugarColumn(ColumnDescription = "当前步骤", Length = 64, IsNullable = true)]
    public string StepKey { get; set; }

    /// <summary>下发入参原文 JSON（如空调模式 action、eSIM 的 imei），便于事后追溯传了什么</summary>
    [SugarColumn(ColumnDescription = "入参原文", ColumnDataType = "TEXT", IsNullable = true)]
    public string PayloadJson { get; set; }

    /// <summary>业务返回码（协议 <c>result</c>，0 表示成功）</summary>
    [SugarColumn(ColumnDescription = "返回码", IsNullable = true)]
    public int? Result { get; set; }

    /// <summary>错误说明（返回码非 0 时由错误码字典翻译；查不到则留空）</summary>
    [SugarColumn(ColumnDescription = "错误信息", Length = 512, IsNullable = true)]
    public string ErrorMessage { get; set; }

    /// <summary>操作人 ID，由云端补</summary>
    [SugarColumn(ColumnDescription = "操作人ID", IsNullable = true)]
    public long? OperatorId { get; set; }

    /// <summary>操作人账号，由云端补</summary>
    [SugarColumn(ColumnDescription = "操作人", Length = 64, IsNullable = true)]
    public string OperatorName { get; set; }

    /// <summary>结束时刻（进入终态的时间）</summary>
    [SugarColumn(ColumnDescription = "结束时间", IsNullable = true)]
    public DateTime? FinishTime { get; set; }

    /// <summary>报文时间戳（毫秒，设备时钟）</summary>
    [SugarColumn(ColumnDescription = "上报时间戳", IsNullable = true)]
    public long ReportTimestamp { get; set; }
}