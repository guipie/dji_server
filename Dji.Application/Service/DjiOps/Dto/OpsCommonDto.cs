// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

namespace Dji.Application.Service.DjiOps.Dto;

/// <summary>运维模块共用的枚举字典项</summary>
public class OpsDictOptionOutput
{
    /// <summary>取值</summary>
    public int Value { get; set; }

    /// <summary>显示文本</summary>
    public string Label { get; set; }
}

/// <summary>运维模块共用的「批次」选项（用于按批次号做筛选下拉）。</summary>
/// <remarks>
/// 批次是页面一次提交产生的分组号。把它做成下拉而不是自由输入，
/// 是因为它既不直观（是 GUID）、也没必要让用户记忆 —— 从已有数据里选即可。
/// </remarks>
public class OpsBatchOptionOutput
{
    /// <summary>批次 ID</summary>
    public string BatchId { get; set; }

    /// <summary>显示文本（批次号 + 生成时间）</summary>
    public string Label { get; set; }
}

/// <summary>运维模块共用的机场下拉查询入参</summary>
public class OpsDockOptionInput
{
    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }
}

/// <summary>
/// 运维模块共用的机场下拉项。
/// </summary>
/// <remarks>
/// 现有各模块（直播 / 媒体 / HMS / 航线）各自维护了一份同构的 DTO，这是历史原因。
/// 新模块统一用这一份：字段完全一致，重复定义只会让「同一个机场在不同页面显示的名称不一样」这类
/// 不一致问题更容易出现。
/// </remarks>
public class OpsDockOptionOutput
{
    /// <summary>机场 SN</summary>
    public string Sn { get; set; }

    /// <summary>机场昵称</summary>
    public string Nick { get; set; }

    /// <summary>所属工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>是否在线</summary>
    public bool IsOnline { get; set; }

    /// <summary>下拉显示文本</summary>
    public string Label { get; set; }
}
