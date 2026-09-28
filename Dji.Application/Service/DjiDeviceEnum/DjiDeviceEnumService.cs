using AngleSharp.Dom;
using Dji.Application.Const;
using Dji.Core.Service;
using Microsoft.AspNetCore.Http;
using System.Linq;
namespace Dji.Application;
/// <summary>
/// 设备枚举服务
/// </summary>
[ApiDescriptionSettings(ApplicationConst.GroupName, Order = 100)]
public class DjiDeviceEnumService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<DjiDeviceEnum> _rep;
    public DjiDeviceEnumService(SqlSugarRepository<DjiDeviceEnum> rep)
    {
        _rep = rep;
    }

    /// <summary>
    /// 无人机型号列表
    /// </summary>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "DroneDeviceEnums")]
    public async Task<IList<DjiDeviceEnumOutput>> GetDroneDeviceEnums()
    {
        return await _rep.AsQueryable().Where(m=>m.Domain==DomainEnum.Drone).OrderBy(m=>m.Name).Select<DjiDeviceEnumOutput>().ToListAsync();
    }

    /// <summary>
    /// 按“Domain-Type-SubType”标识获取设备枚举
    /// </summary>
    /// <remarks>
    /// 标识分隔符同时兼容下划线与短横线（前端统一使用短横线），
    /// 并按字段查询而非字符串拼接，避免格式差异导致匹配失败。
    /// </remarks>
    [ApiDescriptionSettings(Name = "DeviceEnum")]
    public DjiDeviceEnumOutput GetDeviceEnum(string domain_type_subtype)
    {
        var parts = (domain_type_subtype ?? string.Empty).Split(['_', '-'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3
            || !int.TryParse(parts[0], out var domain)
            || !int.TryParse(parts[1], out var type)
            || !int.TryParse(parts[2], out var subType))
            throw Oops.Oh($"设备枚举标识格式不正确：{domain_type_subtype}，应形如“Domain-Type-SubType”");

        return _rep.AsQueryable()
            .Where(m => m.Domain == (DomainEnum)domain && m.Type == type && m.SubType == subType)
            .Select<DjiDeviceEnumOutput>()
            .First();
    }
    /// <summary>
    /// 分页查询设备枚举
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Page")]
    public async Task<SqlSugarPagedList<DjiDeviceEnumOutput>> Page(DjiDeviceEnumInput input)
    {
        var query = _rep.AsQueryable()
            .WhereIF(!string.IsNullOrWhiteSpace(input.SearchKey), u =>
                u.Name.Contains(input.SearchKey.Trim())
            )
            .WhereIF(!string.IsNullOrWhiteSpace(input.Name), u => u.Name.Contains(input.Name.Trim()))
            .WhereIF(input.Domain.HasValue, u => u.Domain == input.Domain)
            .Select<DjiDeviceEnumOutput>();
        return await query.OrderBuilder(input).ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 增加设备枚举
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Add")]
    public async Task<long> Add(AddDjiDeviceEnumInput input)
    {
        var entity = input.Adapt<DjiDeviceEnum>();
        await _rep.InsertAsync(entity);
        return entity.Id;
    }

    /// <summary>
    /// 删除设备枚举
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Delete")]
    public async Task Delete(DeleteDjiDeviceEnumInput input)
    {
        var entity = await _rep.GetFirstAsync(u => u.Id == input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D1002);
        await _rep.FakeDeleteAsync(entity);   //假删除
        //await _rep.DeleteAsync(entity);   //真删除
    }

    /// <summary>
    /// 更新设备枚举
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Update")]
    public async Task Update(UpdateDjiDeviceEnumInput input)
    {
        var entity = input.Adapt<DjiDeviceEnum>();
        await _rep.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommandAsync();
    }

    /// <summary>
    /// 获取设备枚举
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet]
    [ApiDescriptionSettings(Name = "Detail")]
    public async Task<DjiDeviceEnum> Detail([FromQuery] QueryByIdDjiDeviceEnumInput input)
    {
        return await _rep.GetFirstAsync(u => u.Id == input.Id);
    }

    /// <summary>
    /// 获取设备枚举列表
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet]
    [ApiDescriptionSettings(Name = "List")]
    public async Task<List<DjiDeviceEnumOutput>> List([FromQuery] DjiDeviceEnumInput input)
    {
        return await _rep.AsQueryable().Select<DjiDeviceEnumOutput>().ToListAsync();
    }





}

