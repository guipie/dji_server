// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using System.Reflection;

namespace Dji.Application.Cloud.Core;
/// <summary>
/// 模块生命周期管理
/// </summary>
internal class ModuleManager
{
    public readonly List<SubscriptionModel> _modules = [];

    public void AddModule<T>() where T : SubscriptionModel, new()
    {
        _modules.Add(new T());
    }

    public void AddModule(SubscriptionModel module)
    {
        _modules.Add(module);
    }

}

public class SubscriptionModel
{
    public string Topic { get; set; } = null!;
    public int? Type { get; set; } = null;
    public string Method { get; set; }
    public MethodInfo MethodInfo { get; set; } = null!;
    public object Instance { get; set; } = null!;
    public Type DataType { get; set; } = null!; // T in CloudMqData<T>
    public Type DeclaringType { get; set; } = null!;
}