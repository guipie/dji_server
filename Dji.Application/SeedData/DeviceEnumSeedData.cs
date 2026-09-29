// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。



namespace Dji.Application.SeedData;
public class DeviceEnumSeedData : ISqlSugarEntitySeedData<DjiDeviceEnum>
{
    public IEnumerable<DjiDeviceEnum> HasData()
    {
        return
        [
        new DjiDeviceEnum() { Id = 1, Name = "Matrice 400",Model="M4", Domain = DomainEnum.Drone, Type = 103, SubType = 0, Desc = "" },
        new DjiDeviceEnum() { Id = 2, Name = "Matrice 350 RTK",Model="M350 RTK", Domain = DomainEnum.Drone, Type = 89, SubType = 0, Desc = "" },
        new DjiDeviceEnum() { Id = 3, Name = "Matrice 300 RTK",Model="M300 RTK", Domain = DomainEnum.Drone, Type = 60, SubType = 0, Desc = "" },
        new DjiDeviceEnum() { Id = 4, Name = "Matrice 30",Model="M30", Domain = DomainEnum.Drone, Type = 67, SubType = 0, Desc = "", },
        new DjiDeviceEnum() { Id = 5, Name = "Matrice 30T",Model="M30T", Domain = DomainEnum.Drone, Type = 67, SubType = 1, Desc = "" },
        new DjiDeviceEnum() { Id = 6, Name = "Mavic 3 行业系列（M3E 相机）",Model="M3E", Domain = DomainEnum.Drone, Type = 77, SubType = 0, Desc = "" },
        new DjiDeviceEnum() { Id = 7, Name = "Mavic 3 行业系列（M3T 相机）",Model="M3T", Domain = DomainEnum.Drone, Type = 77, SubType = 1, Desc = "" },
        new DjiDeviceEnum() { Id = 8, Name = "Mavic 3 行业系列（M3TA 相机）",Model="M3TA", Domain = DomainEnum.Drone, Type = 77, SubType = 3, Desc = "" },
        new DjiDeviceEnum() { Id = 9, Name = "Matrice 3D",Model="M3D", Domain = DomainEnum.Drone, Type = 91, SubType = 0, Desc = "" },
        new DjiDeviceEnum() { Id = 10, Name = "Matrice 3TD",Model="M3TD", Domain = DomainEnum.Drone, Type = 91, SubType = 1, Desc = "" },
        new DjiDeviceEnum() { Id = 11, Name = "Matrice 4D",Model="M4D", Domain = DomainEnum.Drone, Type = 100, SubType = 0, Desc = "" },
        new DjiDeviceEnum() { Id = 12, Name = "Matrice 4TD",Model="M4TD", Domain = DomainEnum.Drone, Type = 100, SubType = 1, Desc = "" },
        new DjiDeviceEnum() { Id = 13, Name = "DJI Matrice 4 系列（M4E 相机）",Model="M4E", Domain = DomainEnum.Drone, Type = 99, SubType = 0, Desc = "" },
        new DjiDeviceEnum() { Id = 14, Name = "DJI Matrice 4 系列（M4T 相机）",Model="M4T", Domain = DomainEnum.Drone, Type = 99, SubType = 1, Desc = "" },
        new DjiDeviceEnum() { Id = 15, Name = "DJI 带屏遥控器行业版",Model="", Domain = DomainEnum.RemoteControl, Type = 56, SubType = 0, Desc = "搭配 Matrice 300 RTK" },
        // Model 是 NOT NULL（实体上标了 [Required]）：遥控器没有像 M350 RTK / Dock3 那样的短型号码，
        // 也必须给值 —— 漏了会让整批种子插入撞 SQLite Error 19，服务直接起不来。
        new DjiDeviceEnum() { Id = 16, Name = "DJI RC Plus", Model = "RC Plus", Domain = DomainEnum.RemoteControl, Type = 119, SubType = 0, Desc = "搭配 Matrice 350 RTK\nMatrice 300 RTK\nMatrice 30/30T" },
        new DjiDeviceEnum() { Id = 17, Name = "DJI RC Plus 2", Model = "RC Plus 2", Domain = DomainEnum.RemoteControl, Type = 174, SubType = 0, Desc = "搭配 DJI Matrice 4 系列" },
        new DjiDeviceEnum() { Id = 18, Name = "DJI RC Pro 行业版", Model = "RC Pro", Domain = DomainEnum.RemoteControl, Type = 144, SubType = 0, Desc = "搭配 Mavic 3 行业系列" },
        new DjiDeviceEnum() { Id = 19, Name = "大疆机场",Model="Dock", Domain = DomainEnum.Dock, Type = 1, SubType = 0, Desc = "" },
        new DjiDeviceEnum() { Id = 20, Name = "大疆机场 2",Model="Dock2", Domain = DomainEnum.Dock, Type = 2, SubType = 0, Desc = "" },
        new DjiDeviceEnum() { Id = 21, Name = "大疆机场 3",Model="Dock3", Domain = DomainEnum.Dock, Type = 3, SubType = 0, Desc = "" },
        new DjiDeviceEnum() { Id = 22, Name = "中继器",Model="", Domain = DomainEnum.Repeater, Type = 600, SubType = 0, Desc = "" },
    ];
    }
}
