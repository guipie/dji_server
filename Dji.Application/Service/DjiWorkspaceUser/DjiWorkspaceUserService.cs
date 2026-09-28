using Dji.Core.Service;
using Dji.Application.Const;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Furion.DatabaseAccessor;
namespace Dji.Application;
/// <summary>
/// 空间用户服务
/// </summary>
[ApiDescriptionSettings(ApplicationConst.GroupName, Order = 100)]
public class DjiWorkspaceUserService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<DjiWorkspaceUser> _rep;
    private readonly SqlSugarRepository<SysUser> _userRep;
    private readonly SqlSugarRepository<DjiWorkspace> _spaceRep;
    private readonly ISqlSugarClient _sysDb;
    private readonly UserManager _userManager;
    public DjiWorkspaceUserService(SqlSugarRepository<DjiWorkspaceUser> rep, SqlSugarRepository<SysUser> userRep, SqlSugarRepository<DjiWorkspace> spaceRep, UserManager user, ISqlSugarClient db)
    {
        _rep = rep;
        _userRep = userRep;
        _spaceRep = spaceRep;
        _sysDb = db.AsTenant().GetConnectionScope(SqlSugarConst.MainConfigId);
        _userManager = user;
    }

    /// <summary>
    /// 分页查询空间用户
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Page")]
    public async Task<SqlSugarPagedList<DjiWorkspaceUserOutput>> Page(DjiWorkspaceUserInput input)
    {
        var query = _userRep.AsQueryable()
            .WhereIF(!string.IsNullOrWhiteSpace(input.SearchKey), u =>
                u.NickName.Contains(input.SearchKey.Trim())
                || u.Account.Contains(input.SearchKey.Trim())
            )
            .GroupBy(u => new { u.Id, u.NickName, u.Account })
            .Select((u) => new DjiWorkspaceUserOutput
            {
                UserId = u.Id,
            }, true);
        var data = await query.OrderBuilder(input).ToPagedListAsync(input.Page, input.PageSize);
        data.Items.ToList().ForEach(x =>
        {
            var spaces = _rep.GetList(m => m.UserId == x.UserId);
            var defaulted = spaces?.Where(m => m.IsDefault).FirstOrDefault();
            x.WorkspaceId = defaulted?.WorkspaceId;
            x.WorkspaceNickName = defaulted?.WorkspaceNickName;
            x.Workspaces = spaces?.Select(m => new UserWorkspace() { WorkspaceId = m.WorkspaceId, WorkspaceNickName = m.WorkspaceNickName, IsDefault = m.IsDefault }).ToList();
        });

        return data;
    }
    [HttpGet]
    [ApiDescriptionSettings(Name = "My")]
    public async Task<IList<UserWorkspace>> My()
    {
        var spaces = await _rep.GetListAsync(m => m.UserId == _userManager.UserId);
        return spaces.Select(m => new UserWorkspace() { IsDefault = m.IsDefault, WorkspaceId = m.WorkspaceId, WorkspaceNickName = m.WorkspaceNickName }).ToList();
    }
    [HttpPost]
    [UnitOfWork]
    [ApiDescriptionSettings(Name = "Set")]
    public async Task<bool> Set(SetDjiWorkspaceUserInput input)
    {
        await _rep.DeleteAsync(m => SqlFunc.ContainsArray<long>(input.UserIds, m.UserId));
        var spaces = await _spaceRep.GetListAsync(m => SqlFunc.ContainsArray(input.WorkspaceIds, m.WorkspaceId));
        var users = await _userRep.GetListAsync(m => SqlFunc.ContainsArray<long>(input.UserIds, m.Id));
        var data = new List<DjiWorkspaceUser>();
        for (var i = 0; i < spaces.Count; i++)
        {
            var s = spaces[i];
            data.AddRange(users.Select(x => new DjiWorkspaceUser() { UserId = x.Id, Account = x.Account, NickName = x.NickName, WorkspaceId = s.WorkspaceId, WorkspaceNickName = s.WorkspaceNickName, IsDefault = i == 0 }));
        }
        return await _rep.InsertRangeAsync(data);
    }
    [HttpPost]
    [UnitOfWork]
    [ApiDescriptionSettings(Name = "Set/Default")]
    public async Task<bool> SetDefault(string workspaceId)
    {
        var spaces = await _rep.GetListAsync(m => m.UserId == _userManager.UserId);
        spaces.ForEach(m =>
        {
            if (m.WorkspaceId == workspaceId)
                m.IsDefault = true;
            else
                m.IsDefault = false;
        });
        return await _rep.UpdateRangeAsync(spaces);
    }
    /// <summary>
    /// 增加空间用户
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Add")]
    public async Task<long> Add(AddDjiWorkspaceUserInput input)
    {
        var entity = input.Adapt<DjiWorkspaceUser>();
        await _rep.InsertAsync(entity);
        return entity.Id;
    }

    /// <summary>
    /// 删除空间用户
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Delete")]
    public async Task Delete(DeleteDjiWorkspaceUserInput input)
    {
        var entity = await _rep.GetFirstAsync(u => u.UserId == input.UserId) ?? throw Oops.Oh(ErrorCodeEnum.D1002);
        await _rep.FakeDeleteAsync(entity);   //假删除
        //await _rep.DeleteAsync(entity);   //真删除
    }

    /// <summary>
    /// 更新空间用户
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Update")]
    public async Task Update(UpdateDjiWorkspaceUserInput input)
    {
        var entity = input.Adapt<DjiWorkspaceUser>();
        await _rep.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommandAsync();
    }

    /// <summary>
    /// 获取空间用户
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet]
    [ApiDescriptionSettings(Name = "Detail")]
    public async Task<DjiWorkspaceUser> Detail([FromQuery] QueryByIdDjiWorkspaceUserInput input)
    {
        return await _rep.GetFirstAsync(u => u.Id == input.Id);
    }

    /// <summary>
    /// 获取空间用户列表
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet]
    [ApiDescriptionSettings(Name = "List")]
    public async Task<List<DjiWorkspaceUserOutput>> List([FromQuery] DjiWorkspaceUserInput input)
    {
        return await _rep.AsQueryable().Select<DjiWorkspaceUserOutput>().ToListAsync();
    }

    /// <summary>
    /// 获取user_id列表
    /// </summary>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "SysUserUserIdDropdown"), HttpGet]
    public async Task<dynamic> SysUserUserIdDropdown()
    {
        return await _rep.Context.Queryable<SysUser>()
                .Select(u => new
                {
                    Label = u.Id,
                    Value = u.Id
                }
                ).ToListAsync();
    }




}

