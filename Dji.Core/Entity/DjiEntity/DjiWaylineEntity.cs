// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Core.Enum.DjiEnum.Wayline;
using Elastic.Clients.Elasticsearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dji.Core.Entity.DjiEntity;

[SugarIndex("index_WaylineName_Name", nameof(WaylineName), OrderByType.Asc, true)]  //唯一索引(true 表示唯一索引)
[SugarTable(null, "航线")]
public class DjiWaylineEntity : EntityWorkspaceBase
{ 
    //[SugarColumn(ColumnDescription = "模板ID,[0, 65535]", IsNullable = false)]
    //public int TemplateId { get; set; }

    //[SugarColumn(ColumnDescription = "航线ID,[0, 65535]", IsNullable = false)]
    //public int WaylineId { get; set; }

    [SugarColumn(ColumnDescription = "航线名称", Length = 20, IsNullable = false)]
    public string WaylineName { get; set; }

    [SugarColumn(ColumnDescription = "航线类别", IsNullable = false)]
    public WaylineType WaylineType { get; set; }



    [SugarColumn(ColumnDescription = "飞行器", Length = 20, IsNullable = false)]
    public string Drone { get; set; }

    [SugarColumn(ColumnDescription = "飞行器型号", Length = 20, IsNullable = false)]
    public string DroneModel { get; set; }

    [SugarColumn(ColumnDescription = "配件", Length = 20)]
    public string Acc { get; set; }

    //[SugarColumn(ColumnDescription = "全局航线飞行速度[1,15]", IsNullable = false)]
    //public int AutoFlightSpeed { get; set; }

    //[SugarColumn(ColumnDescription = "WGS84：椭球高模式\r\nrelativeToStartPoint：相对起飞点高度模式\r\nrealTimeFollowSurface: 使用实时仿地模式，仅支持M3E/M3T/M3M", IsNullable = false)]
    //public string ExecuteHeightMode { get; set; } = "WGS84";


    //[SugarColumn(ColumnDescription = "安全起飞高度,遥控器场景 [1.2,1500]，机场场景 [8,1500] （高度模式：相对起飞点高度）\r\n* 注：飞行器起飞后，先爬升至该高度，再根据“飞向首航点模式”的设置飞至首航点。该元素仅在飞行器未起飞时生效。", IsNullable = false)]
    //public double TakeOffSecurityHeight { get; set; }
    //    [SugarColumn(ColumnDescription = "全局返航高度,[2,1500] *注：飞行器返航时，先爬升至该高度，再进行返航。", IsNullable = false)]
    //    public double GlobalRTHHeight { get; set; }

    ////goHome：飞行器完成航线任务后，退出航线模式并返航。
    ////noAction：飞行器完成航线任务后，退出航线模式。
    ////autoLand：飞行器完成航线任务后，退出航线模式并原地降落。
    ////gotoFirstWaypoint：飞行器完成航线任务后，立即飞向航线起始点，到达后退出航线模式。* 注：以上动作执行过程，若飞行器退出了航线模式且进入失控状态，则会优先执行失控动作。
    //[SugarColumn(ColumnDescription = "全局返航高度,[2,1500] *注：飞行器返航时，先爬升至该高度，再进行返航。", IsNullable = false)]
    //public string FinishAction { get; set; } = "goHome"; 
}
