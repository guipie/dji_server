using Dji.Core.Service;
using Dji.Application.Const;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Linq;
namespace Dji.Application;
/// <summary>
/// 设备服务
/// </summary>
[ApiDescriptionSettings(ApplicationConst.GroupName, Order = 100)]
public class DjiDeviceService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<DjiDevice> _rep;
    public DjiDeviceService(SqlSugarRepository<DjiDevice> rep)
    {
        _rep = rep;
    }

    /// <summary>
    /// 分页查询设备
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Page")]
    public async Task<IList<DjiDeviceOutput>> Page(DjiDeviceInput input)
    {
        var query = await _rep.AsQueryable()
            .WhereIF(!string.IsNullOrWhiteSpace(input.SearchKey), u =>
                u.Sn.Contains(input.SearchKey.Trim())
                || u.Model.Contains(input.SearchKey.Trim())
                || u.ParentSn.Contains(input.SearchKey.Trim())
                || u.Nick.Contains(input.SearchKey.Trim())
            )
            .WhereIF(!string.IsNullOrWhiteSpace(input.Sn), u => u.Sn.Contains(input.Sn.Trim()))
            .WhereIF(!string.IsNullOrWhiteSpace(input.Name), u => u.Model.Contains(input.Name.Trim()))
            .WhereIF(!string.IsNullOrWhiteSpace(input.WorkspaceId), u => u.WorkspaceId == input.WorkspaceId)
            .WhereIF(!string.IsNullOrWhiteSpace(input.Nick), u => u.Nick.Contains(input.Nick.Trim()))
            //处理外键和TreeSelector相关字段的连接
            .LeftJoin<DjiWorkspace>((u, workspaceid) => u.WorkspaceId == workspaceid.WorkspaceId)
            .Select((u, workspaceid) => new DjiDeviceOutput()
            {
                WorkspaceIdNickName = workspaceid.NickName
            }, true)
            .OrderBy(u => new { u.Sn }).ToTreeAsync(u => u.Children, u => u.ParentSn, null, u => u.Sn); 
        return query;
    }

    /// <summary>
    /// 增加设备
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Add")]
    public async Task<long> Add(AddDjiDeviceInput input)
    {
        var entity = input.Adapt<DjiDevice>();
        await _rep.InsertAsync(entity);
        return entity.Id;
    }

    /// <summary>
    /// 删除设备
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Delete")]
    public async Task Delete(DeleteDjiDeviceInput input)
    {
        var entity = await _rep.GetFirstAsync(u => u.Id == input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D1002);
        await _rep.FakeDeleteAsync(entity);   //假删除
        //await _rep.DeleteAsync(entity);   //真删除
    }

    /// <summary>
    /// 更新设备
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Update")]
    public async Task Update(UpdateDjiDeviceInput input)
    {
        var entity = input.Adapt<DjiDevice>();
        await _rep.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommandAsync();
    }

    /// <summary>
    /// 获取设备
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet]
    [ApiDescriptionSettings(Name = "Detail")]
    public async Task<DjiDevice> Detail([FromQuery] QueryByIdDjiDeviceInput input)
    {
        return await _rep.GetFirstAsync(u => u.Id == input.Id);
    }

    /// <summary>
    /// 获取设备列表
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet]
    [ApiDescriptionSettings(Name = "List")]
    public async Task<List<DjiDeviceOutput>> List([FromQuery] DjiDeviceInput input)
    {
        return await _rep.AsQueryable().Select<DjiDeviceOutput>().ToListAsync();
    }

    /// <summary>
    /// 获取工作空间列表
    /// </summary>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "DjiWorkspaceWorkspaceIdDropdown"), HttpGet]
    public async Task<dynamic> DjiWorkspaceWorkspaceIdDropdown()
    {
        return await _rep.Context.Queryable<DjiWorkspace>()
                .Select(u => new
                {
                    Label = u.NickName,
                    Value = u.WorkspaceId
                }
                ).ToListAsync();
    }
    /// <summary>
    /// 获取机场编号列表
    /// </summary>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "DjiDeviceParentSnDropdown"), HttpGet]
    public async Task<dynamic> DjiDeviceParentSnDropdown()
    {
        return await _rep.Context.Queryable<DjiDevice>()
                .Select(u => new
                {
                    Label = u.Nick,
                    Value = u.Sn
                }
                ).ToListAsync();
    }

    /// <summary>
    /// 上传avatar_url
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "UploadAvatarUrl"), HttpPost]
    public async Task<FileOutput> UploadAvatarUrl([Required] IFormFile file)
    {
        var service = App.GetService<SysFileService>();
        return await service.UploadFile(file, "upload/AvatarUrl");
    }



}

