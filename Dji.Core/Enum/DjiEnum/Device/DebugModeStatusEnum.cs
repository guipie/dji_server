// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023 yanyi  联系电话/微信：18600766045  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dji.Core;

[Description("Domain枚举")]
public enum DebugModeStatusEnum
{
    [Description("取消或终止")]
    Canceled = 0,

    [Description("失败")]
    Failed = 1,

    [Description("执行中")]
    InProgress = 2,

    [Description("执行成功")]
    Ok = 3,

    [Description("暂停")]
    Paused = 4,

    [Description("拒绝")]
    Rejected = 5,

    [Description("已下发")]
    Sent = 6,

    [Description("超时")]
    Timeout = 7,
}
