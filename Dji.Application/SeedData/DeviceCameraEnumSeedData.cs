// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Entity;

namespace Dji.Application.SeedData;
public class DeviceCameraEnumSeedData : ISqlSugarEntitySeedData<DjiDeviceCameraEnum>
{
    public IEnumerable<DjiDeviceCameraEnum> HasData()
    {
        return
        [
            // 飞行器 FPV
            new DjiDeviceCameraEnum() { Id = 1, Name = "Matrice 300 RTK FPV", ProductType = "飞行器 FPV", Domain = DomainEnum.Payload, TsgIndex = "39-0-7", CameraPosition = null, Desc = "" },
        new DjiDeviceCameraEnum() { Id = 2, Name = "Matrice 350 RTK FPV", ProductType = "飞行器 FPV", Domain = DomainEnum.Payload, TsgIndex = "39-0-7", CameraPosition = null, Desc = "" },
        new DjiDeviceCameraEnum() { Id = 3, Name = "Matrice 30 FPV", ProductType = "飞行器 FPV", Domain = DomainEnum.Payload, TsgIndex = "39-0-7", CameraPosition = null, Desc = "" },
        new DjiDeviceCameraEnum() { Id = 4, Name = "Matrice 30T FPV", ProductType = "飞行器 FPV", Domain = DomainEnum.Payload, TsgIndex = "39-0-7", CameraPosition = null, Desc = "" },
        new DjiDeviceCameraEnum() { Id = 5, Name = "Matrice 3D 辅助影像", ProductType = "飞行器 FPV", Domain = DomainEnum.Payload, TsgIndex = "176-0-0", CameraPosition = null, Desc = "" },
        new DjiDeviceCameraEnum() { Id = 6, Name = "Matrice 3TD 辅助影像", ProductType = "飞行器 FPV", Domain = DomainEnum.Payload, TsgIndex = "176-0-0", CameraPosition = null, Desc = "" },
        new DjiDeviceCameraEnum() { Id = 7, Name = "Matrice 4D 辅助影像", ProductType = "飞行器 FPV", Domain = DomainEnum.Payload, TsgIndex = "176-0-0", CameraPosition = null, Desc = "" },
        new DjiDeviceCameraEnum() { Id = 8, Name = "Matrice 4TD 辅助影像", ProductType = "飞行器 FPV", Domain = DomainEnum.Payload, TsgIndex = "176-0-0", CameraPosition = null, Desc = "" },

        // 相机 - 禅思 Z30
        new DjiDeviceCameraEnum() { Id = 9, Name = "禅思 Z30", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "20-0-0", CameraPosition = CameraPositionEnum.Left, IsMainGimbal = true, Desc = "" },
        new DjiDeviceCameraEnum() { Id = 10, Name = "禅思 Z30", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "20-0-1", CameraPosition = CameraPositionEnum.Right, Desc = "" },
        new DjiDeviceCameraEnum() { Id = 11, Name = "禅思 Z30", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "20-0-2", CameraPosition = CameraPositionEnum.Up, Desc = "" },

        // 禅思 XT2
        new DjiDeviceCameraEnum() { Id = 12, Name = "禅思 XT2", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "26-0-0", CameraPosition = CameraPositionEnum.Left, IsMainGimbal = true, Desc = "" },
        new DjiDeviceCameraEnum() { Id = 13, Name = "禅思 XT2", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "26-0-1", CameraPosition = CameraPositionEnum.Right, Desc = "" },
        new DjiDeviceCameraEnum() { Id = 14, Name = "禅思 XT2", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "26-0-2", CameraPosition = CameraPositionEnum.Up, Desc = "" },

        // 禅思 XTS
        new DjiDeviceCameraEnum() { Id = 15, Name = "禅思 XTS", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "41-0-0", CameraPosition = CameraPositionEnum.Left, IsMainGimbal = true, Desc = "" },
        new DjiDeviceCameraEnum() { Id = 16, Name = "禅思 XTS", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "41-0-1", CameraPosition = CameraPositionEnum.Right, Desc = "" },
        new DjiDeviceCameraEnum() { Id = 17, Name = "禅思 XTS", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "41-0-2", CameraPosition = CameraPositionEnum.Up, Desc = "" },

        // 禅思 H20
        new DjiDeviceCameraEnum() { Id = 18, Name = "禅思 H20", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "42-0-0", CameraPosition = CameraPositionEnum.Left, IsMainGimbal = true, Desc = "" },
        new DjiDeviceCameraEnum() { Id = 19, Name = "禅思 H20", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "42-0-1", CameraPosition = CameraPositionEnum.Right, Desc = "" },
        new DjiDeviceCameraEnum() { Id = 20, Name = "禅思 H20", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "42-0-2", CameraPosition = CameraPositionEnum.Up, Desc = "" },

        // 禅思 H20T
        new DjiDeviceCameraEnum() { Id = 21, Name = "禅思 H20T", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "43-0-0", CameraPosition = CameraPositionEnum.Left, IsMainGimbal = true, Desc = "" },
        new DjiDeviceCameraEnum() { Id = 22, Name = "禅思 H20T", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "43-0-1", CameraPosition = CameraPositionEnum.Right, Desc = "" },
        new DjiDeviceCameraEnum() { Id = 23, Name = "禅思 H20T", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "43-0-2", CameraPosition = CameraPositionEnum.Up, Desc = "" },

        // 禅思 H20N
        new DjiDeviceCameraEnum() { Id = 24, Name = "禅思 H20N", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "61-0-0", CameraPosition = CameraPositionEnum.Left, IsMainGimbal = true, Desc = "" },
        new DjiDeviceCameraEnum() { Id = 25, Name = "禅思 H20N", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "61-0-1", CameraPosition = CameraPositionEnum.Right, Desc = "" },
        new DjiDeviceCameraEnum() { Id = 26, Name = "禅思 H20N", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "61-0-2", CameraPosition = CameraPositionEnum.Up, Desc = "" },

        // 禅思 H30
        new DjiDeviceCameraEnum() { Id = 27, Name = "禅思 H30", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "82-0-0", CameraPosition = CameraPositionEnum.Left, IsMainGimbal = true, Desc = "" },
        new DjiDeviceCameraEnum() { Id = 28, Name = "禅思 H30", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "82-0-1", CameraPosition = CameraPositionEnum.Right, Desc = "" },
        new DjiDeviceCameraEnum() { Id = 29, Name = "禅思 H30", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "82-0-2", CameraPosition = CameraPositionEnum.Up, Desc = "" },

        // 禅思 H30T
        new DjiDeviceCameraEnum() { Id = 30, Name = "禅思 H30T", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "83-0-0", CameraPosition = CameraPositionEnum.Left, IsMainGimbal = true, Desc = "" },
        new DjiDeviceCameraEnum() { Id = 31, Name = "禅思 H30T", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "83-0-1", CameraPosition = CameraPositionEnum.Right, Desc = "" },
        new DjiDeviceCameraEnum() { Id = 32, Name = "禅思 H30T", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "83-0-2", CameraPosition = CameraPositionEnum.Up, Desc = "" },

        // Matrice 30 Camera
        new DjiDeviceCameraEnum() { Id = 33, Name = "Matrice 30 Camera", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "52-0-0", CameraPosition = null, IsMainGimbal = true, Desc = "" },

        // Matrice 30T Camera
        new DjiDeviceCameraEnum() { Id = 34, Name = "Matrice 30T Camera", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "53-0-0", CameraPosition = null, IsMainGimbal = true, Desc = "" },

        // DJI Matrice 4E Camera
        new DjiDeviceCameraEnum() { Id = 35, Name = "DJI Matrice 4E Camera", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "88-0-0", CameraPosition = null, IsMainGimbal = true, Desc = "" },

        // DJI Matrice 4T Camera
        new DjiDeviceCameraEnum() { Id = 36, Name = "DJI Matrice 4T Camera", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "89-0-0", CameraPosition = null, IsMainGimbal = true, Desc = "" },

        // Mavic 3E Camera
        new DjiDeviceCameraEnum() { Id = 37, Name = "Mavic 3E Camera", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "66-0-0", CameraPosition = null, IsMainGimbal = true, Desc = "" },

        // Mavic 3T Camera
        new DjiDeviceCameraEnum() { Id = 38, Name = "Mavic 3T Camera", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "67-0-0", CameraPosition = null, IsMainGimbal = true, Desc = "" },

        // Mavic 3TA Camera
        new DjiDeviceCameraEnum() { Id = 39, Name = "Mavic 3TA Camera", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "129-0-0", CameraPosition = null, IsMainGimbal = true, Desc = "" },

        // Matrice 3D Camera
        new DjiDeviceCameraEnum() { Id = 40, Name = "Matrice 3D Camera", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "80-0-0", CameraPosition = null, IsMainGimbal = true, Desc = "" },

        // Matrice 3TD Camera
        new DjiDeviceCameraEnum() { Id = 41, Name = "Matrice 3TD Camera", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "81-0-0", CameraPosition = null, IsMainGimbal = true, Desc = "" },

        // Matrice 4D Camera
        new DjiDeviceCameraEnum() { Id = 42, Name = "Matrice 4D Camera", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "98-0-0", CameraPosition = null, IsMainGimbal = true, Desc = "" },

        // Matrice 4TD Camera
        new DjiDeviceCameraEnum() { Id = 43, Name = "Matrice 4TD Camera", ProductType = "相机", Domain = DomainEnum.Payload, TsgIndex = "99-0-0", CameraPosition = null, IsMainGimbal = true, Desc = "" },

        // 机场相机
        new DjiDeviceCameraEnum() { Id = 44, Name = "DJI Dock 舱外相机", ProductType = "机场相机", Domain = DomainEnum.Payload, TsgIndex = "165-0-7", CameraPosition = CameraPositionEnum.Out, Desc = "大疆机场\ncamera_position: 1" },
        new DjiDeviceCameraEnum() { Id = 45, Name = "DJI Dock 2 舱内相机", ProductType = "机场相机", Domain = DomainEnum.Payload, TsgIndex = "165-0-7", CameraPosition = CameraPositionEnum.In, Desc = "大疆机场 2\ncamera_position: 0" },
        new DjiDeviceCameraEnum() { Id = 46, Name = "DJI Dock 2 舱外相机", ProductType = "机场相机", Domain = DomainEnum.Payload, TsgIndex = "165-0-7", CameraPosition = CameraPositionEnum.Out, Desc = "大疆机场 2\ncamera_position: 1" },
        new DjiDeviceCameraEnum() { Id = 47, Name = "DJI Dock 3 舱内相机", ProductType = "机场相机", Domain = DomainEnum.Payload, TsgIndex = "165-0-7", CameraPosition = CameraPositionEnum.In, Desc = "大疆机场 3\ncamera_position: 0" },
        new DjiDeviceCameraEnum() { Id = 48, Name = "DJI Dock 3 舱外相机", ProductType = "机场相机", Domain = DomainEnum.Payload, TsgIndex = "165-0-7", CameraPosition = CameraPositionEnum.Out, Desc = "大疆机场 3\ncamera_position: 1" },
    ];
    }
}
