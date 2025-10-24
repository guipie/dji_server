// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Entity;
using NewLife;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dji.Application.Repository;


internal class DeviceRepository(SqlSugarRepository<DjiDevice> sugarRepository, ILogger<DeviceRepository> logger)
{
    private readonly SqlSugarRepository<DjiDevice> _sugarRepository = sugarRepository;
    private readonly ILogger<DeviceRepository> _logger = logger;

    public bool DeviceExists(string sn)
    {
        return _sugarRepository.IsAny(m => m.Sn == sn);
    }
    public async Task<DjiDevice> GetDjiDeviceBySn(string sn)
    {
        return await _sugarRepository.GetFirstAsync(m => m.Sn == sn);
    }
    public async Task<DjiDevice> Insert(DjiDevice device)
    {
        if (device.Sn.IsNullOrWhiteSpace() || device.WorkspaceId.IsNullOrWhiteSpace())
        {
            _logger.LogError("device data is empty,data:{}", device.ToJson());
            return null;
        }
        var data = await GetDjiDeviceBySn(device.Sn);
        if (data != null) return data;
        return await _sugarRepository.InsertReturnEntityAsync(device);
    }
    public async Task<DjiDevice> Insert(string sn, string spaceId)
    {
        var entity = new DjiDevice() { Sn = sn, WorkspaceId = spaceId };
        return await Insert(entity);
    }
}
