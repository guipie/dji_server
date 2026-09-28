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
/// 航线
/// </summary>
/// <remarks>
/// 存储策略为“主表 + JSON 大字段”：
/// <list type="bullet">
/// <item>主表保存列表展示与筛选所需的冗余字段（航点数/距离/时长/速度/机型……），避免列表查询反序列化大字段；</item>
/// <item><see cref="WaylineParamJson"/> 保存前端提交的完整航线参数（航点、动作、全局配置），
/// 它是 KMZ 的唯一数据源，更新/复制/导出时据此重新生成 KMZ。</item>
/// </list>
/// 说明：<c>WorkspaceId + WaylineName</c> 的唯一性由应用层保证（查询时已自动过滤软删除数据），
/// 这里只建普通复合索引用于加速；若使用数据库唯一索引，软删除后同名航线将无法再次创建。
/// </remarks>
[SugarTable("dji_wayline", "航线")]
[SugarIndex("index_wayline_workspace_name", nameof(WorkspaceId), OrderByType.Asc, nameof(WaylineName), OrderByType.Asc)]
public class DjiWaylineEntity : EntityWorkspaceBase
{
    /// <summary>空间ID</summary>
    [SugarColumn(ColumnDescription = "空间ID", Length = 50, IsNullable = true)]
    public override string WorkspaceId { get; set; }

    /// <summary>航线名称</summary>
    [SugarColumn(ColumnDescription = "航线名称", Length = 64, IsNullable = false)]
    public string WaylineName { get; set; }

    /// <summary>航线类别（航点飞行/建图航拍/倾斜摄影/航带飞行）</summary>
    [SugarColumn(ColumnDescription = "航线类别", IsNullable = false, DefaultValue = "0")]
    public WaylineType WaylineType { get; set; }

    /// <summary>WPML 模板类型：waypoint / mapping2d / mapping3d / mappingStrip</summary>
    [SugarColumn(ColumnDescription = "模板类型", Length = 32, IsNullable = true)]
    public string TemplateType { get; set; }

    /// <summary>模板中文名称（航点航线/面状航线……），用于列表展示</summary>
    [SugarColumn(ColumnDescription = "模板名称", Length = 32, IsNullable = true)]
    public string TemplateStr { get; set; }

    /// <summary>飞行器名称（展示用）</summary>
    [SugarColumn(ColumnDescription = "飞行器", Length = 64, IsNullable = true)]
    public string Drone { get; set; }

    /// <summary>飞行器型号</summary>
    [SugarColumn(ColumnDescription = "飞行器型号", Length = 64, IsNullable = true)]
    public string DroneModel { get; set; }

    /// <summary>设备枚举标识（Domain_Type_SubType），生成 KMZ 时据此回查机型/负载枚举值</summary>
    [SugarColumn(ColumnDescription = "设备枚举标识", Length = 64, IsNullable = true)]
    public string DomainTypeSubType { get; set; }

    /// <summary>配件</summary>
    [SugarColumn(ColumnDescription = "配件", Length = 32, IsNullable = true)]
    public string Acc { get; set; }

    /// <summary>模板ID（template.kml 内唯一）</summary>
    [SugarColumn(ColumnDescription = "模板ID", IsNullable = true, DefaultValue = "0")]
    public int TemplateId { get; set; }

    /// <summary>航线ID（waylines.wpml 内唯一）</summary>
    [SugarColumn(ColumnDescription = "航线ID", IsNullable = true, DefaultValue = "0")]
    public int WaylineId { get; set; }

    /// <summary>全局航线飞行速度（米/秒）</summary>
    [SugarColumn(ColumnDescription = "全局航线速度", IsNullable = true, DefaultValue = "0")]
    public double AutoFlightSpeed { get; set; }

    /// <summary>航点执行高度模式：WGS84 / relativeToStartPoint / realTimeFollowSurface</summary>
    [SugarColumn(ColumnDescription = "执行高度模式", Length = 32, IsNullable = true)]
    public string ExecuteHeightMode { get; set; }

    /// <summary>安全起飞高度（米）</summary>
    [SugarColumn(ColumnDescription = "安全起飞高度", IsNullable = true, DefaultValue = "0")]
    public double TakeOffSecurityHeight { get; set; }

    /// <summary>全局返航高度（米）</summary>
    [SugarColumn(ColumnDescription = "全局返航高度", IsNullable = true, DefaultValue = "0")]
    public double GlobalRTHHeight { get; set; }

    /// <summary>航线结束动作：goHome / noAction / autoLand / gotoFirstWaypoint</summary>
    [SugarColumn(ColumnDescription = "结束动作", Length = 32, IsNullable = true)]
    public string FinishAction { get; set; }

    /// <summary>全局航线高度（米）</summary>
    [SugarColumn(ColumnDescription = "全局航线高度", IsNullable = true, DefaultValue = "0")]
    public double GlobalHeight { get; set; }

    /// <summary>航点数量</summary>
    [SugarColumn(ColumnDescription = "航点数量", IsNullable = true, DefaultValue = "0")]
    public int PointCount { get; set; }

    /// <summary>航线总距离（米）</summary>
    [SugarColumn(ColumnDescription = "航线总距离", IsNullable = true, DefaultValue = "0")]
    public double Distance { get; set; }

    /// <summary>预计总时长（秒）</summary>
    [SugarColumn(ColumnDescription = "预计时长", IsNullable = true, DefaultValue = "0")]
    public double Duration { get; set; }

    /// <summary>航线参数（JSON 大字段，KMZ 的唯一数据源）</summary>
    [SugarColumn(ColumnDescription = "航线参数", ColumnDataType = "TEXT", IsNullable = true)]
    public string WaylineParamJson { get; set; }

    /// <summary>KMZ 文件Id（SysFile 主键）</summary>
    [SugarColumn(ColumnDescription = "KMZ文件Id", IsNullable = true)]
    public long? KmzFileId { get; set; }

    /// <summary>KMZ 文件名</summary>
    [SugarColumn(ColumnDescription = "KMZ文件名", Length = 256, IsNullable = true)]
    public string KmzFileName { get; set; }

    /// <summary>KMZ 文件存储路径</summary>
    [SugarColumn(ColumnDescription = "KMZ文件路径", Length = 512, IsNullable = true)]
    public string KmzFilePath { get; set; }

    /// <summary>KMZ 文件访问地址</summary>
    [SugarColumn(ColumnDescription = "KMZ文件地址", Length = 1024, IsNullable = true)]
    public string KmzFileUrl { get; set; }

    /// <summary>
    /// KMZ 内容 MD5（小写十六进制，32 位）。
    /// </summary>
    /// <remarks>
    /// 对应上云协议 <c>flighttask_prepare.file.fingerprint</c>：机场下载 KMZ 后用它校验文件完整性。
    /// 生成 KMZ 时一并算出并落库，避免每次下发任务都重新读取文件计算。
    /// </remarks>
    [SugarColumn(ColumnDescription = "KMZ文件签名", Length = 64, IsNullable = true)]
    public string Sign { get; set; }
}
