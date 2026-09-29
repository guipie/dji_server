using Dji.Core.Service;
using Dji.Application.Const;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Dji.DatabaseAccessor;
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
        var spaces = await EnsureDefaultWorkspaceAsync(_userManager.UserId);
        return spaces.Select(m => new UserWorkspace() { IsDefault = m.IsDefault, WorkspaceId = m.WorkspaceId, WorkspaceNickName = m.WorkspaceNickName }).ToList();
    }

    /// <summary>
    /// 兜底：用户尚未归属任何空间时，自动把他挂到默认空间上。
    /// </summary>
    /// <remarks>
    /// 种子数据只覆盖了初始化账号，之后新建的账号不会自动进空间；而 <c>DjiFlyZoneService</c> /
    /// <c>DjiWaylineService</c> 里的 <c>ResolveWorkspaceIdAsync</c> 又依赖「当前用户的默认空间」，
    /// 取不到时只能抛错，错误还是「NOT NULL constraint failed」这种用户自己根本修不了的形式。
    /// 与其让每个新账号都先去「工作空间用户」页面手工配一遍，不如在取本人空间列表时顺手补齐：
    /// 没有空间记录时才写，有记录时不改动任何既有数据。
    /// </remarks>
    private async Task<List<DjiWorkspaceUser>> EnsureDefaultWorkspaceAsync(long userId)
    {
        var spaces = await _rep.GetListAsync(m => m.UserId == userId);
        if (spaces.Count > 0) return spaces;

        // 优先默认空间；连默认空间都没有（例如手工删空过表）就退回任意一个空间，总之不能返回空
        var workspace = await _spaceRep.GetFirstAsync(m => m.WorkspaceId == ApplicationConst.DefaultWorkspaceId)
                        ?? await _spaceRep.GetFirstAsync(m => m.Id > 0);
        var user = await _userRep.GetFirstAsync(u => u.Id == userId);
        if (workspace == null || user == null) return spaces;

        var entity = new DjiWorkspaceUser
        {
            UserId = user.Id,
            Account = user.Account,
            NickName = user.NickName,
            WorkspaceId = workspace.WorkspaceId,
            WorkspaceNickName = workspace.WorkspaceNickName,
            IsDefault = true,
        };
        await _rep.InsertAsync(entity);

        spaces.Add(entity);
        return spaces;
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

