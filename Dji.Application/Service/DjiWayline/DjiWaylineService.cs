using Dji.Application.Const;
using Dji.Application.Service.DjiWayline.Dto;
using Dji.Core.Entity.DjiEntity;
using Dji.Core.Service;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;
namespace Dji.Application;
/// <summary>
/// 设备服务
/// </summary>
[ApiDescriptionSettings(ApplicationConst.GroupName, Order = 100)]
public class DjiWaylineService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<DjiWaylineEntity> _rep;
    public DjiWaylineService(SqlSugarRepository<DjiWaylineEntity> rep)
    {
        _rep = rep;
    } 

    public async Task<DjiWaylineOutput> CreateWayline(DjiWaylineAddInput input)
    {
        var waylineNameValid=await _rep.IsAnyAsync(m=>m.WaylineName==input.WaylineName.Trim());
        return null;
    }

}

