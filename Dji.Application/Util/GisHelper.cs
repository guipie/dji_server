// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dji.Application.Util;
public static class GisHelper
{


    // 将度数转换为弧度
    public static double DegreesToRadians(this double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    // 判断坐标是否有效
    public static bool IsValidCoordinate(this Point point)
    {
        // 经度范围 -180 到 +180
        // 纬度范围 -90 到 +90
        if (point.X < -180 || point.X > 180 || point.X == 0)  // 经度
        {
            return false;
        }

        if (point.Y < -90 || point.Y > 90 || point.Y == 0)  // 纬度
        {
            return false;
        }

        return true;  // 如果经度和纬度都在有效范围内，则认为坐标有效
    }
    public static bool IsValidCoordinate(float lon, float lat)
    {
        // 经度范围 -180 到 +180
        // 纬度范围 -90 到 +90
        if (lon < -180 || lon > 180 || lon == 0)  // 经度
        {
            return false;
        }

        if (lat < -90 || lat > 90 || lat == 0)  // 纬度
        {
            return false;
        }

        return true;  // 如果经度和纬度都在有效范围内，则认为坐标有效
    }
}
