

using Dji.Application.Const;

namespace Dji.Application.JuAI;
/// <summary>
/// 聚AI用户服务
/// </summary>
[ApiDescriptionSettings(ApplicationConst.DjiCloud, Order = 100)]
public class AppUserService : IDynamicApiController, ITransient
{

}

