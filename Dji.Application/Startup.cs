// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023 yanyi  联系电话/微信：18600766045  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Cloud.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Dji.Application;

[AppStartup(100)]
public class Startup : AppStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        //var chatGPTOptions = App.GetOptions<AzureOpenAIOptions>();
        //services.AddOpenAIService(settings =>
        //{
        //    settings.ApiKey = chatGPTOptions.ApiKey;
        //    settings.DeploymentId = chatGPTOptions.DeploymentId;
        //    settings.ResourceName= chatGPTOptions.ResourceName; 
        //    settings.ProviderType = Betalgo.Ranul.OpenAI.ProviderType.Azure; 
        //});

        services.AddScoped<MqttGatewayPublish>();
        var assembly = AppDomain.CurrentDomain.GetAssemblies();
        var baseType = typeof(BaseRepository);
        var repositoryTypes = assembly.SelectMany(a => a.GetTypes())
                                       .Where(t => baseType.IsAssignableFrom(t) && t != baseType && !t.IsAbstract);

        foreach (var type in repositoryTypes)
        {
            // 获取当前类型的构造函数
            var constructor = type.GetConstructors().FirstOrDefault();
            if (constructor != null)
            {
                // 获取构造函数的参数类型
                var parameterTypes = constructor.GetParameters().Select(p => p.ParameterType).ToArray();

                // 如果存在参数，则动态地为每个参数注册对应的依赖服务
                if (parameterTypes.Any())
                {
                    services.AddScoped(type, serviceProvider =>
                    {
                        var parameters = parameterTypes.Select(serviceProvider.GetService).ToArray();
                        return constructor.Invoke(parameters);
                    });
                }
                else
                {
                    // 若无参数则直接注册
                    services.AddScoped(baseType, type);
                }
            }
        }
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
    }
}