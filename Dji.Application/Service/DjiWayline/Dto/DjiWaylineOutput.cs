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
/// 航线列表输出（不含 JSON 大字段）
/// </summary>
public class DjiWaylineOutput
{
    /// <summary>主键Id</summary>
    public long Id { get; set; }

    /// <summary>航线名称</summary>
    public string WaylineName { get; set; }

    /// <summary>航线类别</summary>
    public WaylineType WaylineType { get; set; }

    /// <summary>WPML 模板类型</summary>
    public string TemplateType { get; set; }

    /// <summary>模板中文名称</summary>
    public string TemplateStr { get; set; }

    /// <summary>飞行器</summary>
    public string Drone { get; set; }

    /// <summary>飞行器型号</summary>
    public string DroneModel { get; set; }

    /// <summary>配件</summary>
    public string Acc { get; set; }

    /// <summary>工作空间</summary>
    public string WorkspaceId { get; set; }

    /// <summary>工作空间名称</summary>
    public string WorkspaceNickName { get; set; }

    /// <summary>航点数量</summary>
    public int PointCount { get; set; }

    /// <summary>航线总距离（米）</summary>
    public double Distance { get; set; }

    /// <summary>预计总时长（秒）</summary>
    public double Duration { get; set; }

    /// <summary>全局航线飞行速度（米/秒）</summary>
    public double AutoFlightSpeed { get; set; }

    /// <summary>航点执行高度模式</summary>
    public string ExecuteHeightMode { get; set; }

    /// <summary>安全起飞高度（米）</summary>
    public double TakeOffSecurityHeight { get; set; }

    /// <summary>全局返航高度（米）</summary>
    public double GlobalRTHHeight { get; set; }

    /// <summary>航线结束动作</summary>
    public string FinishAction { get; set; }

    /// <summary>KMZ 文件名</summary>
    public string KmzFileName { get; set; }

    /// <summary>KMZ 文件访问地址</summary>
    public string KmzFileUrl { get; set; }

    /// <summary>创建时间</summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>创建者</summary>
    public string CreateUserName { get; set; }

    /// <summary>更新时间</summary>
    public DateTime? UpdateTime { get; set; }
}

/// <summary>
/// 航线详情输出（含完整航线参数）
/// </summary>
public class DjiWaylineDetailOutput : DjiWaylineOutput
{
    /// <summary>设备枚举标识</summary>
    public string DomainTypeSubType { get; set; }

    /// <summary>KMZ 文件Id</summary>
    public long? KmzFileId { get; set; }

    /// <summary>KMZ 文件存储路径</summary>
    public string KmzFilePath { get; set; }

    /// <summary>完整航线参数（供前端回填编辑器）</summary>
    public CreateWaypointWaylineRequest Param { get; set; }
}
