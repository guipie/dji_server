using Admin.NET.Application.Const;
using Microsoft.AspNetCore.Authorization;

namespace Admin.NET.Web.Entry.Controllers
{

    [Route("api/demo"), ApiDescriptionSettings(ApplicationConst.DjiCloud, Order = 1000)]
    public class DemoController : AppBaseController
    {


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