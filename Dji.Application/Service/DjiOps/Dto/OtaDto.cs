// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Core.Enum.DjiEnum.Ops;

namespace Dji.Application.Service.DjiOps.Dto;

/// <summary>
/// 固件升级下发入参。
/// </summary>
/// <remarks>
/// <para>
/// <b>设备是否可以混批次</b>：可以。协议允许 <c>devices[]</c> 同时包含机场与飞行器，
/// 这是官方的推荐用法（一致性升级需要两者版本互相匹配，分开升会反复触发版本不一致告警）。
/// </para>
/// <para>
/// <b>为什么必须二次确认</b>：升级会中断作业、耗时可达数十分钟，且部分老固件无法回退。
/// 这道确认在服务端强制（<see cref="Confirm"/>），不依赖前端弹窗 —— 前端可以被绕过。
/// </para>
/// </remarks>
public class OtaCreateInput
{
    /// <summary>目标机场 SN（下发网关）</summary>
    [Required(ErrorMessage = "机场 SN 不能为空")]
    public string DockSn { get; set; }

    /// <summary>待升级的设备列表（至少一台）</summary>
    [Required(ErrorMessage = "请至少选择一台待升级设备")]
    public List<OtaDeviceInput> Devices { get; set; }

    /// <summary>确认标记</summary>
    public bool Confirm { get; set; }
}

/// <summary>单个待升级设备</summary>
public class OtaDeviceInput
{
    /// <summary>设备 SN（机场本体或该机场下的飞行器）</summary>
    [Required(ErrorMessage = "设备 SN 不能为空")]
    public string DeviceSn { get; set; }

    /// <summary>目标固件版本（如 <c>1.00.223</c>）</summary>
    [Required(ErrorMessage = "目标版本不能为空")]
    public string TargetVersion { get; set; }

    /// <summary>升级类型（默认普通升级）</summary>
    public OtaUpgradeTypeEnum UpgradeType { get; set; } = OtaUpgradeTypeEnum.Normal;

    /// <summary>固件包下载地址（普通升级 / PSDK 升级必填）</summary>
    public string FileUrl { get; set; }

    /// <summary>固件包 MD5（普通升级 / PSDK 升级必填）</summary>
    public string Md5 { get; set; }

    /// <summary>固件包大小（字节，普通升级 / PSDK 升级必填）</summary>
    public long? FileSize { get; set; }

    /// <summary>固件包文件名（普通升级 / PSDK 升级必填）</summary>
    public string FileName { get; set; }
}

/// <summary>固件升级任务查询</summary>
public class OtaTaskSearchInput : BasePageInput
{
    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>机场 SN</summary>
    public string DockSn { get; set; }

    /// <summary>任务状态</summary>
    public OtaTaskStatusEnum? Status { get; set; }

    /// <summary>设备 SN（模糊匹配批次中的设备列表）</summary>
    public string DeviceSn { get; set; }

    /// <summary>批次 ID</summary>
    public string BatchId { get; set; }

    /// <summary>升级类型</summary>
    public OtaUpgradeTypeEnum? UpgradeType { get; set; }

    /// <summary>时间范围起点</summary>
    public DateTime? StartTime { get; set; }

    /// <summary>时间范围终点</summary>
    public DateTime? EndTime { get; set; }
}

/// <summary>固件升级任务输出</summary>
public class OtaTaskOutput
{
    /// <summary>主键</summary>
    public long Id { get; set; }

    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>机场 SN</summary>
    public string DockSn { get; set; }

    /// <summary>机场昵称</summary>
    public string DockNick { get; set; }

    /// <summary>批次 ID</summary>
    public string BatchId { get; set; }

    /// <summary>业务 ID</summary>
    public string Bid { get; set; }

    /// <summary>设备数</summary>
    public int DeviceCount { get; set; }

    /// <summary>设备 SN 列表（逗号分隔）</summary>
    public string DeviceSns { get; set; }

    /// <summary>是否包含机场本体</summary>
    public bool IncludeDock { get; set; }

    /// <summary>是否包含飞行器</summary>
    public bool IncludeDrone { get; set; }

    /// <summary>升级范围名称（机场 / 飞行器 / 机场 + 飞行器）</summary>
    public string ScopeName { get; set; }

    /// <summary>目标版本</summary>
    public string TargetVersion { get; set; }

    /// <summary>下发时的实际版本快照</summary>
    public string CurrentVersion { get; set; }

    /// <summary>升级类型</summary>
    public OtaUpgradeTypeEnum UpgradeType { get; set; }

    /// <summary>升级类型名称</summary>
    public string UpgradeTypeName { get; set; }

    /// <summary>任务状态</summary>
    public OtaTaskStatusEnum Status { get; set; }

    /// <summary>任务状态名称</summary>
    public string StatusName { get; set; }

    /// <summary>是否仍在进行中</summary>
    public bool IsRunning { get; set; }

    /// <summary>进度百分比</summary>
    public int? Percent { get; set; }

    /// <summary>当前步骤（协议原值）</summary>
    public string CurrentStep { get; set; }

    /// <summary>当前步骤名称</summary>
    public string CurrentStepName { get; set; }

    /// <summary>返回码</summary>
    public int? Result { get; set; }

    /// <summary>错误信息</summary>
    public string ErrorMessage { get; set; }

    /// <summary>操作人</summary>
    public string OperatorName { get; set; }

    /// <summary>创建时间</summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>结束时间</summary>
    public DateTime? FinishTime { get; set; }

    /// <summary>本次升级的设备明细</summary>
    public List<OtaTaskDeviceOutput> Devices { get; set; } = [];
}

/// <summary>升级任务中的单个设备明细</summary>
public class OtaTaskDeviceOutput
{
    /// <summary>设备 SN</summary>
    public string Sn { get; set; }

    /// <summary>设备名称（机场昵称 / 飞行器型号）</summary>
    public string DeviceName { get; set; }

    /// <summary>是否为机场本体</summary>
    public bool IsDock { get; set; }

    /// <summary>该设备的目标版本</summary>
    public string ProductVersion { get; set; }

    /// <summary>固件包地址</summary>
    public string FileUrl { get; set; }

    /// <summary>固件包 MD5</summary>
    public string Md5 { get; set; }

    /// <summary>固件包大小（字节）</summary>
    public long? FileSize { get; set; }

    /// <summary>固件包文件名</summary>
    public string FileName { get; set; }

    /// <summary>升级类型</summary>
    public OtaUpgradeTypeEnum UpgradeType { get; set; }
}

/// <summary>可升级设备下拉项（机场下的全部可升级设备）</summary>
public class OtaDeviceOptionOutput
{
    /// <summary>设备 SN</summary>
    public string Sn { get; set; }

    /// <summary>设备名称</summary>
    public string Name { get; set; }

    /// <summary>是否为机场本体</summary>
    public bool IsDock { get; set; }

    /// <summary>机型</summary>
    public string Model { get; set; }

    /// <summary>当前固件版本</summary>
    public string FirmwareVersion { get; set; }

    /// <summary>显示文本</summary>
    public string Label { get; set; }
}
