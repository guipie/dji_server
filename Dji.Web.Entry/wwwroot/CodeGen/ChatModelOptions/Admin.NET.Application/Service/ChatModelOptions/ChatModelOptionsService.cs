using Admin.NET.Core.Service;
using Admin.NET.Application.Const;
using Admin.NET.Application.Entity;
using Microsoft.AspNetCore.Http;
namespace Admin.NET.Application;
/// <summary>
/// 模型配置服务
/// </summary>
[ApiDescriptionSettings(ApplicationConst.GroupName, Order = 100)]
public class ChatModelOptionsService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<ChatModelOptions> _rep;
    public ChatModelOptionsService(SqlSugarRepository<ChatModelOptions> rep)
    {
        _rep = rep;
    }

    /// <summary>
    /// 分页查询模型配置
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Page")]
    public async Task<SqlSugarPagedList<ChatModelOptionsOutput>> Page(ChatModelOptionsInput input)
    {
        var query = _rep.AsQueryable()
            .WhereIF(!string.IsNullOrWhiteSpace(input.SearchKey), u =>
                u.ModelId.Contains(input.SearchKey.Trim())
                || u.Name.Contains(input.SearchKey.Trim())
                || u.Field.Contains(input.SearchKey.Trim())
            )
            .WhereIF(!string.IsNullOrWhiteSpace(input.ModelId), u => u.ModelId.Contains(input.ModelId.Trim()))
            .WhereIF(!string.IsNullOrWhiteSpace(input.Name), u => u.Name.Contains(input.Name.Trim()))
            .WhereIF(input.OptionType.HasValue, u => u.OptionType == input.OptionType)
            .WhereIF(!string.IsNullOrWhiteSpace(input.Field), u => u.Field.Contains(input.Field.Trim()))
            //处理外键和TreeSelector相关字段的连接
            .LeftJoin<ChatModel>((u, modelid) => u.ModelId == modelid.Id )
            .OrderBy(u => u.CreateTime)
            .Select((u, modelid) => new ChatModelOptionsOutput
            {
                Id = u.Id,
                ModelId = u.ModelId, 
                ModelIdName = modelid.Name,
                Name = u.Name,
                OptionType = (AIModelOptionEnum)u.OptionType,
                MaxNum = u.MaxNum,
                MinNum = u.MinNum,
                SelectedValues = u.SelectedValues,
                Desc = u.Desc,
                DefaultVal = u.DefaultVal,
                Extends = u.Extends,
                CreateTime = u.CreateTime,
                UpdateTime = u.UpdateTime,
                CreateUserId = u.CreateUserId,
                UpdateUserId = u.UpdateUserId,
                IsDelete = u.IsDelete,
                Field = u.Field,
            });
        return await query.OrderBuilder(input).ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 增加模型配置
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Add")]
    public async Task<long> Add(AddChatModelOptionsInput input)
    {
        var entity = input.Adapt<ChatModelOptions>();
        await _rep.InsertAsync(entity);
        return entity.Id;
    }

    /// <summary>
    /// 删除模型配置
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Delete")]
    public async Task Delete(DeleteChatModelOptionsInput input)
    {
        var entity = await _rep.GetFirstAsync(u => u.Id == input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D1002);
        await _rep.FakeDeleteAsync(entity);   //假删除
        //await _rep.DeleteAsync(entity);   //真删除
    }

    /// <summary>
    /// 更新模型配置
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Update")]
    public async Task Update(UpdateChatModelOptionsInput input)
    {
        var entity = input.Adapt<ChatModelOptions>();
        await _rep.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommandAsync();
    }

    /// <summary>
    /// 获取模型配置
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet]
    [ApiDescriptionSettings(Name = "Detail")]
    public async Task<ChatModelOptions> Detail([FromQuery] QueryByIdChatModelOptionsInput input)
    {
        return await _rep.GetFirstAsync(u => u.Id == input.Id);
    }

    /// <summary>
    /// 获取模型配置列表
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet]
    [ApiDescriptionSettings(Name = "List")]
    public async Task<List<ChatModelOptionsOutput>> List([FromQuery] ChatModelOptionsInput input)
    {
        return await _rep.AsQueryable().Select<ChatModelOptionsOutput>().ToListAsync();
    }

    /// <summary>
    /// 获取对应的模型列表
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "ChatModelModelIdDropdown"), HttpGet]
    public async Task<dynamic> ChatModelModelIdDropdown()
    {
        return await _rep.Context.Queryable<ChatModel>()
                .Select(u => new
                {
                    Label = u.Name,
                    Value = u.Id
                }
                ).ToListAsync();
    }




}

