// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Cloud;
using Dji.Application.Cloud.Core;
using Dji.Application.Cloud.Entity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using System.Linq;
using System.Reflection;

namespace Dji.Application;
public static class DjiApplicationSetup
{

    public static IServiceCollection AddMqttSetup(this IServiceCollection services)
    {
        services.AddSingleton<IMqttClient>(sp =>
        {
            var mqttFactory = new MqttClientFactory();
            return mqttFactory.CreateMqttClient();
        });
        services.AddSingleton<IMqttService, MqttService>();
        services.AddSingleton<ModuleManager>();
        var baseType = typeof(BaseModuleService);
        var types = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsClass &&
                        !t.IsAbstract &&
                        t.IsSubclassOf(baseType));

        foreach (var type in types)
        {
            services.Add(new ServiceDescriptor(type, type, ServiceLifetime.Scoped));
            // 如果你想通过接口注册，比如 IModal 接口，也可以这样：
            // services.AddScoped<IModal, type>();
        }
        return services;
    }
    public static IApplicationBuilder UseMqttBuilder(this IApplicationBuilder builder)
    {

        var controllerTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsClass && t.GetCustomAttribute<MqttControllerAttribute>() != null &&
                        t.IsSubclassOf(typeof(BaseModuleService)));
        var moduleManage = builder.ApplicationServices.GetRequiredService<ModuleManager>();

        // MQTT 处理器需要长期存活，而其依赖（仓储等）注册为 Scoped，
        // 因此显式创建一个与应用同生命周期的作用域，并在应用停止时释放，
        // 而不是像历史实现那样在循环里反复 CreateScope() 且从不释放。
        var handlerScope = builder.ApplicationServices.CreateScope();
        builder.ApplicationServices.GetRequiredService<IHostApplicationLifetime>()
            .ApplicationStopping.Register(handlerScope.Dispose);

        foreach (var type in controllerTypes)
        {
            var instance = handlerScope.ServiceProvider.GetRequiredService(type);
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttributes<MqttSubscribeAttribute>().Any());

            foreach (var method in methods)
            {
                // 特性可叠加：同一个处理方法上挂多个 MqttSubscribe 会注册成多条订阅记录。
                // 机场控制类指令（开合舱盖 / 充电 / 重启 / 格式化 …）的进度报文结构完全一致，
                // 叠加特性可让一个方法统一处理，避免写几十个只做转发的样板方法。
                var attrs = method.GetCustomAttributes<MqttSubscribeAttribute>().ToList();
                var parameters = method.GetParameters();

                if (parameters.Length != 1)
                    throw new ArgumentException($"MQTT 处理方法 参数数量不正确");
                var paramType = parameters[0].ParameterType;
                if (!paramType.IsGenericType || paramType.GetGenericTypeDefinition() != typeof(CloudMqData<>))
                    throw new ArgumentException($"MQTT 处理方法 {method.Name} 的参数必须是 CloudMqData<T>");

                var dataType = paramType.GetGenericArguments()[0]; // T 

                foreach (var attr in attrs)
                {
                    moduleManage.AddModule(new SubscriptionModel
                    {
                        Topic = attr.Topic,
                        Domain = attr.Domain,
                        Method = attr.Method,
                        MethodInfo = method,
                        Instance = instance,
                        DataType = dataType,
                        DeclaringType = type
                    });
                }
            }
        }

        Console.WriteLine($"[MQTT] 已注册 {moduleManage._modules.Count} 个 MQTT 订阅方法。");
        var mqttService = builder.ApplicationServices.GetRequiredService<IMqttService>();
        mqttService.StartAsync();
        return builder;
    }
}
