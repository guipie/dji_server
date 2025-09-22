// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

namespace Dji.Application.Entity;
[SugarTable("DjiDevice", "设备表")]
public class DjiDevice : EntityAppBase
{

    [SugarColumn(ColumnDescription = "sn号", Length = 20)]
    [Required]
    public string Sn { get; set; }
    [SugarColumn(ColumnDescription = "设备推送名称", Length = 20)]
    [Required]
    public string Name { get; set; }

    [SugarColumn(ColumnDescription = "昵称", Length = 20, IsNullable = true)]
    public string Nick { get; set; }

    [SugarColumn(ColumnDescription = "空间ID", Length = 20, IsNullable = true)]
    public string WorkspaceId { get; set; }

    [SugarColumn(ColumnDescription = "网关设备的命名空间", Length = 20, IsNullable = true)]
    public string Domain { get; set; }

    [SugarColumn(ColumnDescription = "网关设备的产品类型\t", Length = 20, IsNullable = true)]
    public int Type { get; set; }

    [SugarColumn(ColumnDescription = "网关子设备的产品子类型", Length = 20, IsNullable = true)]
    public int SubType { get; set; }


    [SugarColumn(ColumnDescription = "连接网关设备的通道索引\t", Length = 20, IsNullable = true)]
    public string Index { get; set; }


    [SugarColumn(ColumnDescription = "网关版本", Length = 20, IsNullable = true)]
    public string ThingVersion { get; set; }

    [SugarColumn(ColumnDescription = "固件版本", Length = 20, IsNullable = true)]
    public string FirmwareVersion { get; set; }

    [SugarColumn(ColumnDescription = "描述", Length = 2000, IsNullable = true)]
    public string Desc { get; set; }

    [SugarColumn(ColumnDescription = "经度", IsNullable = true)]
    public float Longitude { get; set; }

    [SugarColumn(ColumnDescription = "维度", IsNullable = true)]
    public float Latitude { get; set; }

    [SugarColumn(ColumnDescription = "高度", IsNullable = true)]
    public float Altitude { get; set; }


    [SugarColumn(ColumnDescription = "绑定时间", IsNullable = true)]
    public DateTime BindTime { get; set; }

    [SugarColumn(ColumnDescription = "是否绑定", IsNullable = true)]
    public bool Binded { get; set; }

    [SugarColumn(ColumnDescription = "头像", Length = 200, IsNullable = true)]
    public string AvatarUrl { get; set; }

}
