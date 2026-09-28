// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

namespace Dji.Core.Entity;
//[SugarIndex("index_Order_Id",nameof(Order.id),OrderByType.Asc)]     //普通索引--非聚集索引
//[SugarIndex("index_device_sn", nameof(DjiDevice.Sn),OrderByType.Asc, nameof(DjiDevice.Name), OrderByType.Asc,true)]  //复合索引
[SugarIndex("index_DjiDevice_Sn", nameof(Sn), OrderByType.Asc, true)]  //唯一索引(true 表示唯一索引)
[SugarTable("DjiDevice", "设备表")]
public class DjiDevice : EntityWorkspaceBase
{

    [SugarColumn(ColumnDescription = "sn号", Length = 20, IsTreeKey = true)]
    [Required]
    public string Sn { get; set; }

    [SugarColumn(ColumnDescription = "空间ID", Length = 50, IsNullable = true)]
    public override string WorkspaceId { get; set; }

    [SugarColumn(ColumnDescription = "设备模型，型号", Length = 20, IsNullable = true)]
    public string Model { get; set; }


    [SugarColumn(ColumnDescription = "父SN号", Length = 20, IsNullable = true)]
    public string ParentSn { get; set; }


    [SugarColumn(ColumnDescription = "昵称", Length = 20, IsNullable = true)]
    public string Nick { get; set; }

    [SugarColumn(ColumnDescription = "网关设备的命名空间", Length = 20, IsNullable = true)]
    public DomainEnum? Domain { get; set; }

    [SugarColumn(ColumnDescription = "网关设备的产品类型", Length = 20, IsNullable = true)]
    public int? Type { get; set; }

    [SugarColumn(ColumnDescription = "网关子设备的产品子类型", Length = 20, IsNullable = true)]
    public int? SubType { get; set; }


    [SugarColumn(ColumnDescription = "连接网关设备的通道索引", Length = 20, IsNullable = true)]
    public string Index { get; set; }


    [SugarColumn(ColumnDescription = "网关版本", Length = 20, IsNullable = true)]
    public string ThingVersion { get; set; }

    [SugarColumn(ColumnDescription = "固件版本", Length = 20, IsNullable = true)]
    public string FirmwareVersion { get; set; }

    [SugarColumn(ColumnDescription = "描述", Length = 2000, IsNullable = true)]
    public string Desc { get; set; }

    /// <summary>经度（未知时为 null，避免用 0 表达“未知”——(0,0) 是几内亚湾的合法坐标）</summary>
    [SugarColumn(ColumnDescription = "经度", IsNullable = true)]
    public double? Longitude { get; set; }

    /// <summary>纬度（未知时为 null）</summary>
    [SugarColumn(ColumnDescription = "纬度", IsNullable = true)]
    public double? Latitude { get; set; }

    /// <summary>高度（未知时为 null）</summary>
    [SugarColumn(ColumnDescription = "高度", IsNullable = true)]
    public double? Altitude { get; set; }


    /// <summary>是否在线（由 update_topo / offline 事件维护，服务重启后按 LastOnlineTime 判定）</summary>
    [SugarColumn(ColumnDescription = "是否在线", IsNullable = true, DefaultValue = "0")]
    public bool IsOnline { get; set; }

    /// <summary>最近一次在线时间</summary>
    [SugarColumn(ColumnDescription = "最近在线时间", IsNullable = true)]
    public DateTime? LastOnlineTime { get; set; }

    /// <summary>最近一次收到 OSD 的时间</summary>
    [SugarColumn(ColumnDescription = "最近OSD时间", IsNullable = true)]
    public DateTime? LastOsdTime { get; set; }


    [SugarColumn(ColumnDescription = "绑定时间", IsNullable = true)]
    public DateTime BindTime { get; set; }

    [SugarColumn(ColumnDescription = "是否绑定", IsNullable = true)]
    public bool Binded { get; set; }

    [SugarColumn(ColumnDescription = "绑定次数",DefaultValue ="0")]
    public int BindNum { get; set; }

    [SugarColumn(ColumnDescription = "头像", Length = 200, IsNullable = true)]
    public string AvatarUrl { get; set; }


    /// <summary>子设备（飞行器），非数据库字段，按 ParentSn 装配</summary>
    [SugarColumn(IsIgnore = true)]
    public IList<DjiDevice> Children { get; set; } = [];
}
