// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。




using AngleSharp.Dom;
using Dji.Application.Cloud.Dto.Device;
using Dji.Application.Cloud.Dto.Org;
using System.Linq;

namespace Dji.Application.CloudRepository;


internal class WorkspaceRepository(SqlSugarRepository<DjiWorkspace> rep, ILogger<DeviceRepository> logger, SysCacheService sysCacheService) : BaseRepository
{
    private readonly SqlSugarRepository<DjiWorkspace> _rep = rep;
    private readonly SysCacheService _sysCache = sysCacheService;
    private readonly ILogger<DeviceRepository> _logger = logger;

    private static readonly SemaphoreSlim _workspaceLock = new(1, 1);

    public async Task SaveWorkspace(DeviceOrganization data)
    {
        await _workspaceLock.WaitAsync();
        try
        {
            var isExist = await _rep.IsAnyAsync(m => m.WorkspaceId == data.OrganizationId);
            if (!isExist)
            {
                Console.WriteLine("insert workspace:", data.ToJson());
                await _rep.InsertAsync(new DjiWorkspace()
                {
                    WorkspaceName = data.OrganizationName,
                    //WorkspaceBindCode = data,
                    WorkspaceNickName = data.OrganizationName,
                    WorkspaceDesc = "自动绑定",
                    WorkspaceId = data.OrganizationId,
                    TenantId = SqlSugarConst.MainConfigId.ToLong(),
                });
            }
        }
        finally
        {
            _workspaceLock.Release();
        }
    }

    public async Task<bool> UpdateWorkspaceBindCode(string spaceId, string code)
    {
        return await _rep.UpdateAsync(m => new DjiWorkspace() { WorkspaceBindCode = code }, m => m.WorkspaceId == spaceId);
    }

    public async Task<DjiWorkspace> GetWorkspaceBySpaceId(string spaceId)
    {
        return await _rep.GetFirstAsync(m => m.WorkspaceId == spaceId);
    }
}
