// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using System.Text.RegularExpressions;

namespace Dji.Application.Cloud.Core;
/// <summary>
/// 主题路由
/// </summary>
internal class TopicRouter : ITopicRouter
{
    // 支持通配符的主题模式匹配项
    private readonly List<(Regex Pattern, Type MessageType, Delegate Handler)> _routes = new();

    private static readonly Dictionary<string, string> StandardHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        ["{product_id}"] = @"[a-zA-Z0-9_-]+",
        ["{sn}"] = @"[a-zA-Z0-9_-]+",
        ["{device_id}"] = @"[a-zA-Z0-9_-]+"
    };

    public void RegisterHandler<T>(string topicPattern, Func<T, Task> handler) where T : class
    {
        var regexPattern = "^" + Regex.Escape(topicPattern)
            .Replace(@"\{product_id\}", "(?<product_id>[a-zA-Z0-9_-]+)")
            .Replace(@"\{sn\}", "(?<sn>[a-zA-Z0-9_-]+)")
            .Replace(@"\{device_id\}", "(?<device_id>[a-zA-Z0-9_-]+)")
            .Replace(@"\+", @"[^/]+")
            .Replace(@"\#", @".*") + "$";

        var regex = new Regex(regexPattern, RegexOptions.Compiled);

        _routes.Add((regex, typeof(T), handler));
    }

    public async Task RouteAsync(string topic, string payload, CancellationToken ct = default)
    {
        foreach (var (Pattern, MessageType, Handler) in _routes)
        {
            var match = Pattern.Match(topic);
            if (match.Success)
            {
                try
                {
                    // 反序列化
                    var data = System.Text.Json.JsonSerializer.Deserialize(payload, MessageType);
                    if (data is not null)
                    {
                        // 调用处理器：Func<T, Task>
                        var task = (Task)Handler.DynamicInvoke(data)!;
                        if (task != null)
                            await task.ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    // 应该有日志注入，这里简化处理
                    Console.WriteLine($"[TopicRouter] 处理消息失败: {topic} -> {ex.Message}");
                }
                break; // 找到第一个匹配项即停止（可改为支持多播）
            }
        }
    }
}
