using Dji.Core.Service;
using Dji.Application.Const;
using Microsoft.AspNetCore.Http;
namespace Dji.Application;
/// <summary>
/// 相机枚举服务
/// </summary>
[ApiDescriptionSettings(ApplicationConst.GroupName, Order = 100)]
public class DjiDeviceCameraEnumService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<DjiDeviceCameraEnum> _rep;
    public DjiDeviceCameraEnumService(SqlSugarRepository<DjiDeviceCameraEnum> rep)
    {
        _rep = rep;
    }

    /// <summary>
    /// 分页查询相机枚举
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Page")]
    public async Task<SqlSugarPagedList<DjiDeviceCameraEnumOutput>> Page(DjiDeviceCameraEnumInput input)
    {
        var query = _rep.AsQueryable()
            .WhereIF(!string.IsNullOrWhiteSpace(input.SearchKey), u =>
                u.Name.Contains(input.SearchKey.Trim())
                || u.TsgIndex.Contains(input.SearchKey.Trim())
            )
            .WhereIF(!string.IsNullOrWhiteSpace(input.Name), u => u.Name.Contains(input.Name.Trim()))
            .WhereIF(input.Domain.HasValue, u => u.Domain == input.Domain)
            .WhereIF(!string.IsNullOrWhiteSpace(input.TsgIndex), u => u.TsgIndex.Contains(input.TsgIndex.Trim()))
            .Select<DjiDeviceCameraEnumOutput>().OrderBy(m => m.Id);
        return await query.OrderBuilder(input).ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 增加相机枚举
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Add")]
    public async Task<long> Add(AddDjiDeviceCameraEnumInput input)
    {
        var entity = input.Adapt<DjiDeviceCameraEnum>();
        await _rep.InsertAsync(entity);
        return entity.Id;
    }

    /// <summary>
    /// 删除相机枚举
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Delete")]
    public async Task Delete(DeleteDjiDeviceCameraEnumInput input)
    {
        var entity = await _rep.GetFirstAsync(u => u.Id == input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D1002);
        await _rep.FakeDeleteAsync(entity);   //假删除
        //await _rep.DeleteAsync(entity);   //真删除
    }

    /// <summary>
    /// 更新相机枚举
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Update")]
    public async Task Update(UpdateDjiDeviceCameraEnumInput input)
    {
        var entity = input.Adapt<DjiDeviceCameraEnum>();
        await _rep.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommandAsync();
    }

    /// <summary>
    /// 获取相机枚举
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet]
    [ApiDescriptionSettings(Name = "Detail")]
    public async Task<DjiDeviceCameraEnum> Detail([FromQuery] QueryByIdDjiDeviceCameraEnumInput input)
    {
        return await _rep.GetFirstAsync(u => u.Id == input.Id);
    }

    /// <summary>
    /// 获取相机枚举列表
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet]
    [ApiDescriptionSettings(Name = "List")]
    public async Task<List<DjiDeviceCameraEnumOutput>> List([FromQuery] DjiDeviceCameraEnumInput input)
    {
        return await _rep.AsQueryable().Select<DjiDeviceCameraEnumOutput>().ToListAsync();
    }

    public DjiDeviceCameraEnum? GetCameraEnum(long deviceId)
    {
        return _rep.GetFirst(m => m.DeviceEnumId == deviceId);
    }



}

