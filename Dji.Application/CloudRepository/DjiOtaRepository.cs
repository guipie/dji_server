// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Cloud.Dto.Dock;
using Dji.Core.Enum.DjiEnum.Ops;

namespace Dji.Application.CloudRepository;

/// <summary>
/// 固件升级任务仓储（建单 + 下游进度收口）。
/// </summary>
/// <remarks>
/// <para>
/// <b>进度归并靠 <c>bid</c></b>：设备用与 <c>ota_create</c> 相同的 <c>bid</c> 推送 <c>ota_progress</c>，
/// 而进度报文里<b>没有设备 SN</b>，因此一次下发只能对应一行（见 <c>DjiOtaTask</c> 的类注释）。
/// </para>
/// <para>
/// <b>与机场控制指令仓储的差别</b>：控制指令的进度是「下发 → 逐步推进 → 终态」的短过程，
/// 固件升级可能持续几十分钟且中间会长时间停在同一个百分比。
/// 因此这里不做「僵死收口」的激进判断（<see cref="CloseStaleAsync"/> 只收口「从未收到任何进度」的单子，
/// 而不是按时间一刀切），否则会把正在正常升级的任务误判为超时。
/// </para>
/// </remarks>
public class DjiOtaRepository(
    SqlSugarRepository<DjiOtaTask> taskRes,
    ILogger<DjiOtaRepository> logger) : BaseRepository
{
    private readonly SqlSugarRepository<DjiOtaTask> _taskRes = taskRes;
    private readonly ILogger<DjiOtaRepository> _logger = logger;

    #region 读取

    /// <summary>按主键取任务</summary>
    public async Task<DjiOtaTask> GetAsync(long id)
        => id <= 0 ? null : await _taskRes.GetByIdAsync(id);

    /// <summary>按业务 ID 取任务</summary>
    public async Task<DjiOtaTask> GetByBidAsync(string bid)
        => bid.IsNullOrWhiteSpace() ? null : await _taskRes.GetFirstAsync(m => m.Bid == bid);

    /// <summary>取某机场仍在进行中的升级任务（同一机场不允许并行升级）</summary>
    public async Task<List<DjiOtaTask>> GetRunningAsync(string dockSn)
        => dockSn.IsNullOrWhiteSpace()
            ? []
            : await _taskRes.AsQueryable()
                .Where(m => m.DockSn == dockSn
                    && m.Status != OtaTaskStatusEnum.Success
                    && m.Status != OtaTaskStatusEnum.Failed
                    && m.Status != OtaTaskStatusEnum.Canceled)
                .OrderBy(m => m.CreateTime, OrderByType.Desc)
                .ToListAsync();

    #endregion

    #region 写入

    /// <summary>建单（按 <c>bid</c> 幂等）</summary>
    public async Task<DjiOtaTask> CreateAsync(DjiOtaTask task)
    {
        var exist = await GetByBidAsync(task.Bid);
        if (exist != null)
        {
            // 极端情况下设备推得快：进度已先建行，这里只补齐下发侧信息，不覆盖状态
            exist.DevicesJson ??= task.DevicesJson;
            exist.TargetVersion ??= task.TargetVersion;
            exist.CurrentVersion ??= task.CurrentVersion;
            exist.DeviceSns ??= task.DeviceSns;
            exist.Result ??= task.Result;
            exist.ErrorMessage ??= task.ErrorMessage;
            exist.OperatorId ??= task.OperatorId;
            exist.OperatorName ??= task.OperatorName;

            await _taskRes.AsUpdateable(exist)
                .UpdateColumns(m => new
                {
                    m.DevicesJson, m.TargetVersion, m.CurrentVersion, m.DeviceSns,
                    m.Result, m.ErrorMessage, m.OperatorId, m.OperatorName
                })
                .ExecuteCommandAsync();
            return exist;
        }

        task.CreateTime ??= DateTime.Now;
        await _taskRes.InsertAsync(task);
        return task;
    }

    /// <summary>
    /// 用 <c>ota_progress</c> 的 <c>output</c> 刷新任务进度。
    /// </summary>
    /// <param name="bid">业务 ID</param>
    /// <param name="output">协议 <c>output</c> 结构</param>
    /// <param name="reportTimestamp">报文时间戳（毫秒）</param>
    /// <returns>是否命中已有记录</returns>
    /// <remarks>
    /// 找不到记录时<b>直接忽略</b>而不是补建一行：与机场控制指令不同，
    /// 固件升级不可能由现场触发（必须由平台下发固件包），因此「无记录」只可能是
    /// 平台重启后数据库被清过之类的异常；此时补出来的行会缺失设备与版本信息，没有价值。
    /// </remarks>
    public async Task<bool> UpdateProgressAsync(string bid, DockCommandOutput output, long reportTimestamp)
    {
        var task = await GetByBidAsync(bid);
        if (task == null)
        {
            _logger.LogWarning("收到无法归属的固件升级进度，已忽略：bid={Bid}，status={Status}",
                bid, output?.Status ?? "未知");
            return false;
        }

        var status = OtaTaskStatusResolver.Parse(output?.Status);
        var percent = output?.Progress?.Percent;
        var step = output?.Progress?.CurrentStep ?? output?.Progress?.StepKey;

        await _taskRes.AsUpdateable()
            .SetColumns(m => new DjiOtaTask
            {
                Status = status,
                Percent = percent,
                CurrentStep = step,
                ReportTimestamp = reportTimestamp,
                FinishTime = status.IsFinal() ? DateTime.Now : task.FinishTime,
            })
            .Where(m => m.Id == task.Id)
            .ExecuteCommandAsync();

        return true;
    }

    /// <summary>
    /// 把「下发后从未收到任何进度」的僵死单子收口为失败。
    /// </summary>
    /// <remarks>
    /// 判据是 <c>Status == Sending</c>（连 <c>services_reply</c> 之后的第一个进度都没等到），
    /// <b>而不是</b>「超过多久没更新」—— 固件下载阶段可能几十分钟不刷新百分比，
    /// 按时间收口会把正常升级误判成失败。真正的进度卡死只能靠人看百分比判断。
    /// </remarks>
    public async Task<int> CloseStaleAsync(DateTime before)
        => await _taskRes.AsUpdateable()
            .SetColumns(m => new DjiOtaTask
            {
                Status = OtaTaskStatusEnum.Failed,
                FinishTime = DateTime.Now,
                ErrorMessage = "下发后未收到设备任何升级进度，请检查设备在线状态后重新下发",
            })
            .Where(m => m.Status == OtaTaskStatusEnum.Sending
                && m.CreateTime != null && m.CreateTime < before)
            .ExecuteCommandAsync();

    #endregion
}
