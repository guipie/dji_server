using Admin.NET.Application.Const;

/// <summary>
/// 超级管理员相关服务
/// </summary>
[ApiDescriptionSettings(ApplicationConst.DjiCloud, Order = 100)]
public class SuperAdminService : IDynamicApiController, ITransient
{

}
