// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023 yanyi  联系电话/微信：18600766045  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

namespace Admin.NET.Core.Service;

/// <summary>
/// 系统数据库管理服务
/// </summary>
[ApiDescriptionSettings(Order = 251)]
public class SysDatabaseExtendService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly IViewEngine _viewEngine;
    private readonly CodeGenOptions _codeGenOptions;
    private readonly UserManager _userManager;

    public SysDatabaseExtendService(ISqlSugarClient db,
        IViewEngine viewEngine,
        IOptions<CodeGenOptions> codeGenOptions,
        UserManager userManager)
    {
        _db = db;
        _viewEngine = viewEngine;
        _codeGenOptions = codeGenOptions.Value;
        _userManager = userManager;
    }

    /// <summary>
    /// 查询sql
    /// </summary> 
    /// <param name="configId">ConfigId</param>
    /// <returns></returns> 
    [DisplayName("查询sql")]
    public DataTable PostQuery([FromRoute] string configId = SqlSugarConst.MainConfigId, [FromBody] string sql = "select 1")
    {
        if (!_userManager.SuperAdmin) throw Oops.Oh("您没有权限操作");
        var db = _db.AsTenant().GetConnectionScope(configId);
        return db.Ado.GetDataTable(sql);
    }
    /// <summary>
    /// 操作sql
    /// </summary>
    /// <param name="configId"></param>
    /// <param name="sql"></param>
    /// <returns></returns>
    [DisplayName("查询sql")]
    public int PostExecute([FromRoute] string configId = SqlSugarConst.MainConfigId, [FromBody] string sql = "select 1")
    {
        if (!_userManager.SuperAdmin) throw Oops.Oh("您没有权限操作");
        var db = _db.AsTenant().GetConnectionScope(configId);
        return db.Ado.ExecuteCommand(sql);
    }
}