// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

namespace Dji.Core.Enum.DjiEnum.Wayline;

/// <summary>
/// 航线任务执行步骤（<c>flighttask_progress.output.progress.current_step</c>）。
/// </summary>
/// <remarks>
/// 机场上报的步骤码数量多（0–49 及 65533 以上的特殊值）且会随固件迭代新增，
/// 因此这里用「常量 + 描述字典」而非 enum：未知步骤码只需原样展示数字，
/// 不会因为枚举缺项而在反序列化时抛错。
/// </remarks>
public static class FlightTaskStep
{
    /// <summary>等待服务响应完成后结束（进入终态前的收尾步骤）</summary>
    public const int WaitServiceResponse = 65533;

    /// <summary>无具体状态</summary>
    public const int None = 65534;

    /// <summary>未知</summary>
    public const int Unknown = 65535;

    private static readonly Dictionary<int, string> Descriptions = new()
    {
        [0] = "初始状态",
        [1] = "起飞前检查：飞行器是否正在执行航线",
        [2] = "起飞前检查：机场是否已退出工作模式",
        [3] = "起飞前检查：航线执行中",
        [4] = "返航前检查：正在返航",
        [5] = "航线执行进入准备状态，等待任务下发",
        [6] = "机场进入工作状态",
        [7] = "进入开机检查与舱盖准备工作",
        [8] = "图传频段配对",
        [9] = "等待飞控就绪，推流连接建立",
        [10] = "等待 RTK 源上报数值",
        [11] = "检查 RTK 源是否来自本机场，不是则重置",
        [12] = "等待飞控通知",
        [13] = "机场无控制权，正在夺取飞行器控制权",
        [14] = "自定义飞行区一致性检查",
        [15] = "离线地图一致性检查",
        [16] = "获取最新 KMZ 下载地址",
        [17] = "下载 KMZ",
        [18] = "上传 KMZ",
        [19] = "着色配置",
        [20] = "飞行器起飞参数设置、备降点设置、起飞高度设置、着色设置",
        [21] = "飞行器 flyto 起飞参数设置",
        [22] = "起飞机场检查降落机场准备状态",
        [23] = "返航点设置",
        [24] = "触发航线执行",
        [26] = "进入返航前检查",
        [27] = "飞行器降落至机场",
        [28] = "降落后关闭舱盖",
        [29] = "机场退出工作模式",
        [30] = "机场异常恢复",
        [31] = "机场上传飞控日志",
        [32] = "检查相机录制状态",
        [33] = "获取媒体文件数量",
        [34] = "机场起飞舱盖异常恢复",
        [35] = "通知任务结果",
        [36] = "日志列表获取：获取飞行器列表",
        [37] = "日志列表获取：获取机场列表",
        [38] = "日志列表获取：上传日志列表结果",
        [39] = "日志获取：获取飞行器日志",
        [40] = "日志获取：获取机场日志",
        [41] = "日志获取：压缩飞行器日志",
        [42] = "日志获取：压缩机场日志",
        [43] = "日志获取：上传飞行器日志",
        [44] = "日志获取：上传机场日志",
        [45] = "日志获取：通知结果",
        [46] = "自定义飞行区文件更新准备",
        [47] = "自定义飞行区更新中",
        [48] = "离线地图更新准备",
        [49] = "离线地图更新中",
        [WaitServiceResponse] = "等待服务响应完成后结束",
        [None] = "无具体状态",
        [Unknown] = "未知",
    };

    /// <summary>取步骤中文描述；未知步骤显示为「步骤 N」，便于对照官方文档排查</summary>
    public static string Describe(int step)
    {
        return Descriptions.TryGetValue(step, out var text) ? text : $"步骤 {step}";
    }

    /// <summary>全部步骤（供前端渲染步骤解释，保证前后端字典一致）</summary>
    public static IReadOnlyDictionary<int, string> All => Descriptions;
}

/// <summary>
/// 航线任务细分状态（<c>flighttask_progress.output.ext.wayline_mission_state</c>）。
/// </summary>
/// <remarks>
/// 与 <see cref="FlightTaskStep"/> 不同，该字段取值固定为 0–9 的整数，语义稳定，
/// 适合用枚举表达；同时保留描述字典供前端展示。
/// </remarks>
public enum WaylineMissionStateEnum
{
    [Description("断连")]
    Disconnected = 0,

    [Description("不支持该航点")]
    WaypointUnsupported = 1,

    [Description("航线准备状态，可上传文件，可执行已有文件")]
    Prepared = 2,

    [Description("航线文件上传中")]
    FileUploading = 3,

    [Description("已触发开始命令，飞行器正在读取航线，任务尚未开始")]
    StartTriggered = 4,

    [Description("进入航线，飞向第一个航点")]
    EnteringWayline = 5,

    [Description("航线执行中")]
    Executing = 6,

    [Description("航线中断（用户主动暂停或飞控异常）")]
    Interrupted = 7,

    [Description("航线恢复")]
    Recovering = 8,

    [Description("航线停止")]
    Stopped = 9,
}
