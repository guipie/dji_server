using Dji.Application.Cloud.Core;
using Dji.Application.Const;
using Dji.Application.Option;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using MQTTnet;

namespace Dji.Web.Entry.Controllers
{

    [Route("api/demo"), ApiDescriptionSettings(ApplicationConst.DjiCloud, Order = 1000)]
    public class DemoController : AppBaseController
    {
        private readonly IMqttClient _mqttClient;
        private readonly IOptions<MqttOptions> _mqttOptions;

        public DemoController(IMqttClient mqttClient, IOptions<MqttOptions> mqttOptions)
        {
            _mqttClient = mqttClient;
            _mqttOptions = mqttOptions;
        }

        /// <summary>MQTT 诊断端点：检查连接状态、订阅、收到的应答数</summary>
        [HttpGet("mqtt-diag"), AllowAnonymous]
        public object MqttDiag()
        {
            return new
            {
                connected = _mqttClient.IsConnected,
                server = $"{_mqttOptions.Value.Server}:{_mqttOptions.Value.Port}",
                clientId = _mqttOptions.Value.ClientId,
                subscribedTopics = _mqttOptions.Value.SubscribedTopics,
                totalMessageCount = MqttDiagnostics.TotalMessageCount,
                replyReceivedCount = MqttDiagnostics.ReplyMessageCount,
                lastMessageTopic = MqttDiagnostics.LastTopic,
                lastMessageTime = MqttDiagnostics.LastMessageTime,
                lastReply = MqttDiagnostics.LastReply,
                subscribeResults = MqttDiagnostics.SubscribeResults,
                uniqueTopics = MqttDiagnostics.UniqueTopics,
            };
        }

        [HttpPost("upload"), RequestSizeLimit(100000000000000)]
        public async Task<dynamic> Demo(IFormFile file)
        {
            await Task.Delay(3000);
            return "demo OK";
        }

        [HttpPost("api/sse"), AllowAnonymous]
        public async Task CreateSseDemo()
        {
            // 设置响应头，指定 SSE 的内容类型
            HttpContext.Response.Headers.Append("Content-Type", "text/event-stream");
            HttpContext.Response.Headers.Append("Cache-Control", "no-cache");
            HttpContext.Response.Headers.Append("Connection", "keep-alive");

            // 写入 SSE 消息到响应流
            for (int i = 0; i < 10; i++)
            {
                var message = $"消息{i}";
                await HttpContext.Response.WriteAsync(message);
                await HttpContext.Response.Body.FlushAsync();
                await Console.Out.WriteLineAsync(message);
                Task.Delay(1000).Wait();
            }
            await HttpContext.Response.CompleteAsync();
        }

    }
}