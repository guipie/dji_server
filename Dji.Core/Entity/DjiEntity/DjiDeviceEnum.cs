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
[SugarIndex("index_DjiDeviceEnum_Name", nameof(Name), OrderByType.Asc, true)]  //唯一索引(true 表示唯一索引)
[SugarTable("DjiDeviceEnum", "设备表")]
public class DjiDeviceEnum : EntityAppBase
{

    [SugarColumn(ColumnDescription = "产品名称", Length = 20)]
    [Required]
    public string Name { get; set; }


    [SugarColumn(ColumnDescription = "型号", Length = 20)]
    [Required]
    public string Model { get; set; }

    //[SugarColumn(ColumnDescription = "分组所属", Length = 20)]
    //[Required]
    //public string GroupName { get; set; }


    //[SugarColumn(ColumnDescription = "挂载产品", Length = 20)]
    //public long ParentId { get; set; }

    //[SugarColumn(ColumnDescription = "挂载产品名称", Length = 20)]
    //[Required]
    //public string ParentName { get; set; }


    [SugarColumn(ColumnDescription = "领域", Length = 20)]
    public DomainEnum Domain { get; set; }

    [SugarColumn(ColumnDescription = "主类型", Length = 20)]
    public int Type { get; set; }

    [SugarColumn(ColumnDescription = "子类型", Length = 20)]
    public int SubType { get; set; }


    [SugarColumn(ColumnDescription = "描述", Length = 2000, IsNullable = true)]
    public string Desc { get; set; }


}
