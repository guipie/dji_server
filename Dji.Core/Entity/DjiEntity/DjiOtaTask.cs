// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Core.Enum.DjiEnum.Ops;

namespace Dji.Core.Entity.DjiEntity;

/// <summary>
/// 固件升级任务（一次 <c>ota_create</c> 下发 = 一条记录）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么是「一次下发一行」而不是「一个设备一行」</b>：
/// 看协议两侧的信息量就清楚了 ——
/// 下行 <c>ota_create</c> 带 <c>devices[]</c>（每台设备的版本与固件包），
/// 但上行 <c>ota_progress</c> <b>只带 <c>bid</c>、<c>status</c>、<c>progress</c>，完全没有设备 SN</b>。
/// 也就是说设备上报的进度是<b>批次级</b>的，云端<b>无法</b>知道「这一帧 40% 是机场还是飞行器」。
/// 若硬按设备拆行，就会出现「同一批次的两行显示完全相同的进度」——
/// 那是把不存在的信息编出来了。因此这里如实按批次建模：
/// 请求明细存 <see cref="DevicesJson"/>，进度落在批次这一行上。
/// </para>
/// <para>
/// <b>进度归并键是 <c>bid</c></b>：设备用与 <c>ota_create</c> 相同的 <c>bid</c> 推送进度，
/// 因此全表仅 <see cref="Bid"/> 需要唯一索引。
/// </para>
/// <para>
/// <b>两套状态的含义不同</b>：<see cref="Status"/> 是设备的升级进度状态（协议 <c>output.status</c>），
/// 而平台在「已建单、还没发出去」时需要一个额外的中间态（见 <see cref="OtaTaskStatusEnum.Pending"/>），
/// 协议里没有对应值。
/// </para>
/// <para>
/// <b>时间列</b>：创单时刻复用基类的 <c>CreateTime</c>（基类语义就是「插入时写入、更新时不覆盖」），
/// 不再重复声明同义列 —— 重复声明会隐藏基类成员（CS0114）且两列内容完全一致。
/// </para>
/// </remarks>
[SugarTable(null, "固件升级任务")]
[SugarIndex("index_DjiOtaTask_Bid", nameof(Bid), OrderByType.Asc, true)]
[SugarIndex("index_DjiOtaTask_DockTime", nameof(DockSn), OrderByType.Asc, nameof(CreateTime), OrderByType.Desc)]
[SugarIndex("index_DjiOtaTask_Status", nameof(Status), OrderByType.Asc)]
public class DjiOtaTask : EntityWorkspaceBase
{
    /// <summary>目标机场 SN（下发网关）</summary>
    [SugarColumn(ColumnDescription = "机场SN", Length = 64, IsNullable = false)]
    public string DockSn { get; set; }

    /// <summary>批次 ID（一次页面提交可能产生多批，用于分组展示）</summary>
    [SugarColumn(ColumnDescription = "批次ID", Length = 64, IsNullable = true)]
    public string BatchId { get; set; }

    /// <summary>协议 <c>bid</c>（进度报文的归并键，唯一）</summary>
    [SugarColumn(ColumnDescription = "业务ID", Length = 64, IsNullable = true)]
    public string Bid { get; set; }

    /// <summary>本次升级的设备数（<c>devices[]</c> 长度）</summary>
    [SugarColumn(ColumnDescription = "设备数", IsNullable = true)]
    public int DeviceCount { get; set; }

    /// <summary>
    /// 本次升级涉及的设备 SN（逗号分隔）。
    /// </summary>
    /// <remarks>
    /// 冗余一列而不只在 JSON 里存：运维最常见的问题是「这台飞行器到底被升级过没有」，
    /// 逗号拼接后可直接用 <c>LIKE</c> 检索，不必依赖数据库的 JSON 函数（SQLite / MySQL 语法还不一样）。
    /// </remarks>
    [SugarColumn(ColumnDescription = "设备SN列表", Length = 512, IsNullable = true)]
    public string DeviceSns { get; set; }

    /// <summary>是否包含机场本体</summary>
    [SugarColumn(ColumnDescription = "含机场", IsNullable = true)]
    public bool IncludeDock { get; set; }

    /// <summary>是否包含飞行器</summary>
    [SugarColumn(ColumnDescription = "含飞行器", IsNullable = true)]
    public bool IncludeDrone { get; set; }

    /// <summary>目标版本（多设备版本不一致时以逗号分隔）</summary>
    [SugarColumn(ColumnDescription = "目标版本", Length = 256, IsNullable = true)]
    public string TargetVersion { get; set; }

    /// <summary>下发时的实际版本快照（用于事后比对「升了没升上去」，可能为空）</summary>
    [SugarColumn(ColumnDescription = "当前版本", Length = 256, IsNullable = true)]
    public string CurrentVersion { get; set; }

    /// <summary>升级类型（一致性 / 普通 / PSDK）</summary>
    [SugarColumn(ColumnDescription = "升级类型", IsNullable = true)]
    public OtaUpgradeTypeEnum UpgradeType { get; set; }

    /// <summary>
    /// <c>devices[]</c> 请求原文（JSON）。
    /// </summary>
    /// <remarks>
    /// 固件包地址 / MD5 / 大小 / 文件名这些字段只在「重试同一次升级」时才需要，
    /// 平铺成列会让表宽失控且大部分行为空，因此整份存原文。
    /// </remarks>
    [SugarColumn(ColumnDescription = "设备明细", ColumnDataType = "TEXT", IsNullable = true)]
    public string DevicesJson { get; set; }

    /// <summary>任务状态</summary>
    [SugarColumn(ColumnDescription = "任务状态", IsNullable = true)]
    public OtaTaskStatusEnum Status { get; set; }

    /// <summary>进度百分比（0~100）</summary>
    [SugarColumn(ColumnDescription = "进度百分比", IsNullable = true)]
    public int? Percent { get; set; }

    /// <summary>当前步骤（<c>download_firmware</c> / <c>upgrade_firmware</c>）</summary>
    [SugarColumn(ColumnDescription = "当前步骤", Length = 64, IsNullable = true)]
    public string CurrentStep { get; set; }

    /// <summary>业务返回码（协议 <c>result</c>，0 表示成功）</summary>
    [SugarColumn(ColumnDescription = "返回码", IsNullable = true)]
    public int? Result { get; set; }

    /// <summary>失败原因（返回码非 0 或超时时由服务端补的说明）</summary>
    [SugarColumn(ColumnDescription = "错误信息", Length = 512, IsNullable = true)]
    public string ErrorMessage { get; set; }

    /// <summary>操作人 ID</summary>
    [SugarColumn(ColumnDescription = "操作人ID", IsNullable = true)]
    public long? OperatorId { get; set; }

    /// <summary>操作人账号</summary>
    [SugarColumn(ColumnDescription = "操作人", Length = 64, IsNullable = true)]
    public string OperatorName { get; set; }

    /// <summary>结束时刻（进入终态的时间）</summary>
    [SugarColumn(ColumnDescription = "结束时间", IsNullable = true)]
    public DateTime? FinishTime { get; set; }

    /// <summary>报文时间戳（毫秒，设备时钟）</summary>
    [SugarColumn(ColumnDescription = "上报时间戳", IsNullable = true)]
    public long ReportTimestamp { get; set; }
}
