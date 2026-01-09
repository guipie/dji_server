// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Cloud.Core;
using Elastic.Clients.Elasticsearch;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dji.Application.Cloud.Entity;
public class CloudMqRequest<T>
{
    public string Tid { get; set; }

    public string Bid { get; set; }

    public long Timestamp { get; set; }
    public string Method { get; set; }
    public string Gateway { get; set; }
    public virtual T Data { get; set; }
    public CloudMqRequest(string method, T data, string gateway, string tid, string bid)
    {
        Tid = tid;
        Bid = bid;
        Method = method;
        Data = data;
        Timestamp = DateTimeUtil.ToUnixTimestampByMilliseconds(DateTime.Now);
        Gateway = gateway;
    }
    public CloudMqRequest(string method, T data, string gateway)
    {
        Tid = Guid.NewGuid().ToString();
        Bid = Guid.NewGuid().ToString();
        Method = method;
        Data = data;
        Timestamp = DateTimeUtil.ToUnixTimestampByMilliseconds(DateTime.Now);
        Gateway = gateway;
    }

    public CloudMqRequest(string method, string gateway)
    {
        Tid = Guid.NewGuid().ToString();
        Bid = Guid.NewGuid().ToString();
        Method = method;
        Data = default;
        Timestamp = DateTimeUtil.ToUnixTimestampByMilliseconds(DateTime.Now);
        Gateway = gateway;
    }
}


