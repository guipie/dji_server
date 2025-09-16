

using Admin.NET.Application.Const;
using Microsoft.Extensions.Options;
using OnceMi.AspNetCore.OSS;

namespace Admin.NET.Application.JuAI;
/// <summary>
/// 七牛文件服务扩展
/// </summary>
[ApiDescriptionSettings(ApplicationConst.DjiCloud, Order = 800)]
public class QiNiuFileService : IDynamicApiController, ITransient
{

}
