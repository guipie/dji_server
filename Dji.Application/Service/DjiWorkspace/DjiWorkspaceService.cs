using Dji.Core.Service;
using Dji.Application.Const;
using Microsoft.AspNetCore.Http;

namespace Dji.Application;
/// <summary>
/// workspace服务
/// </summary>
[ApiDescriptionSettings(ApplicationConst.GroupName, Order = 100)]
public class DjiWorkspaceService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<DjiWorkspace> _rep;
    public DjiWorkspaceService(SqlSugarRepository<DjiWorkspace> rep)
    {
        _rep = rep;
    }

    /// <summary>
    /// 分页查询workspace
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Page")]
    public async Task<SqlSugarPagedList<DjiWorkspaceOutput>> Page(DjiWorkspaceInput input)
    {
        var query = _rep.AsQueryable()
            .WhereIF(!string.IsNullOrWhiteSpace(input.SearchKey), u =>
                u.WorkspaceId.Contains(input.SearchKey.Trim())
                || u.WorkspaceName.Contains(input.SearchKey.Trim())
                || u.NickName.Contains(input.SearchKey.Trim())
                || u.WorkspaceBindCode.Contains(input.SearchKey.Trim())
                || u.WorkspaceDesc.Contains(input.SearchKey.Trim())
            )
            .WhereIF(!string.IsNullOrWhiteSpace(input.WorkspaceId), u => u.WorkspaceId.Contains(input.WorkspaceId.Trim()))
            .WhereIF(!string.IsNullOrWhiteSpace(input.WorkspaceName), u => u.WorkspaceName.Contains(input.WorkspaceName.Trim()))
            .WhereIF(!string.IsNullOrWhiteSpace(input.NickName), u => u.NickName.Contains(input.NickName.Trim()))
            .WhereIF(!string.IsNullOrWhiteSpace(input.WorkspaceBindCode), u => u.WorkspaceBindCode.Contains(input.WorkspaceBindCode.Trim()))
            .WhereIF(!string.IsNullOrWhiteSpace(input.WorkspaceDesc), u => u.WorkspaceDesc.Contains(input.WorkspaceDesc.Trim()))
            .Select<DjiWorkspaceOutput>();
        return await query.OrderBuilder(input).ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 增加workspace
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Add")]
    public async Task<long> Add(AddDjiWorkspaceInput input)
    {
        var entity = input.Adapt<DjiWorkspace>();
        await _rep.InsertAsync(entity);
        return entity.Id;
    }

    /// <summary>
    /// 删除workspace
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Delete")]
    public async Task Delete(DeleteDjiWorkspaceInput input)
    {
        var entity = await _rep.GetFirstAsync(u => u.Id == input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D1002);
        await _rep.FakeDeleteAsync(entity);   //假删除
        //await _rep.DeleteAsync(entity);   //真删除
    }

    /// <summary>
    /// 更新workspace
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Update")]
    public async Task Update(UpdateDjiWorkspaceInput input)
    {
        var entity = input.Adapt<DjiWorkspace>();
        await _rep.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommandAsync();
    }

    /// <summary>
    /// 获取workspace
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet]
    [ApiDescriptionSettings(Name = "Detail")]
    public async Task<DjiWorkspace> Detail([FromQuery] QueryByIdDjiWorkspaceInput input)
    {
        return await _rep.GetFirstAsync(u => u.Id == input.Id);
    }

    /// <summary>
    /// 获取workspace列表
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet]
    [ApiDescriptionSettings(Name = "List")]
    public async Task<List<DjiWorkspaceOutput>> List([FromQuery] DjiWorkspaceInput input)
    {
        return await _rep.AsQueryable().Select<DjiWorkspaceOutput>().ToListAsync();
    } 

}

