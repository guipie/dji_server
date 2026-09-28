// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Cloud.Dto.Dock;
using Dji.Core.Enum.DjiEnum.Dock;

namespace Dji.Application.CloudRepository;

/// <summary>
/// 机场控制指令仓储（下发记录 + 异步进度收口）。
/// </summary>
/// <remarks>
/// <para>
/// <b>一条指令的生命周期跨越两个 MQTT 报文</b>：
/// 下发 <c>services</c> → 机场回 <c>services_reply</c>（只说明「收到了」）→
/// 机场陆续推 <c>events</c>（说明「做到哪一步了」）。
/// 本仓储负责把这两个阶段都归并到同一行（以协议 <c>bid</c> 为键）。
/// </para>
/// <para>
/// <b>为什么要容忍「找不到记录」</b>：控制指令也可能来自其它渠道（例如机场现场的遥控器操作），
/// 那时云端只有 events 没有下发记录。此时按需补建一行，宁可记录不完整也不要丢事件 ——
/// 现场排查时「设备做过这个动作」比「谁下的指令」更关键。
/// </para>
/// </remarks>
public class DjiDockCommandRepository(
    SqlSugarRepository<DjiDockCommand> commandRes,
    ILogger<DjiDockCommandRepository> logger) : BaseRepository
{
    private readonly SqlSugarRepository<DjiDockCommand> _commandRes = commandRes;
    private readonly ILogger<DjiDockCommandRepository> _logger = logger;

    #region 读取

    /// <summary>按业务 ID 取指令记录</summary>
    public async Task<DjiDockCommand> GetByBidAsync(string bid)
        => bid.IsNullOrWhiteSpace() ? null : await _commandRes.GetFirstAsync(m => m.Bid == bid);

    /// <summary>取某机场最近的指令记录（控制面板的活动日志）</summary>
    public async Task<List<DjiDockCommand>> GetRecentAsync(string dockSn, int limit = 20)
        => await _commandRes.AsQueryable()
            .WhereIF(!dockSn.IsNullOrWhiteSpace(), m => m.DockSn == dockSn)
            .OrderBy(m => m.CreateTime, OrderByType.Desc)
            .Take(limit)
            .ToListAsync();

    /// <summary>取某机场仍在执行中的指令（控制面板的活动日志 / 按钮互斥判断）</summary>
    public async Task<List<DjiDockCommand>> GetRunningAsync(string dockSn)
        => dockSn.IsNullOrWhiteSpace()
            ? []
            : await _commandRes.AsQueryable()
                .Where(m => m.DockSn == dockSn
                    && m.Status != DockTaskStatusEnum.Ok && m.Status != DockTaskStatusEnum.Failed
                    && m.Status != DockTaskStatusEnum.Canceled && m.Status != DockTaskStatusEnum.Rejected
                    && m.Status != DockTaskStatusEnum.Timeout)
                .OrderBy(m => m.CreateTime, OrderByType.Desc)
                .ToListAsync();

    /// <summary>该机场是否已有同类指令在执行中（用于避免重复下发）</summary>
    public async Task<bool> HasRunningAsync(string dockSn, string method)
        => await _commandRes.AsQueryable()
            .Where(m => m.DockSn == dockSn && m.Method == method
                && m.Status != DockTaskStatusEnum.Ok && m.Status != DockTaskStatusEnum.Failed
                && m.Status != DockTaskStatusEnum.Canceled && m.Status != DockTaskStatusEnum.Rejected
                && m.Status != DockTaskStatusEnum.Timeout)
            .AnyAsync();

    #endregion

    #region 写入

    /// <summary>
    /// 落库一条下发记录（按 <c>bid</c> 幂等）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么是「下发之后」才落库</b>：协议要求下发报文里的 <c>bid</c> 唯一且由发送方生成，
    /// 而生成动作发生在发送方法内部，调用方在拿到回包之前无法预知该值。
    /// 若先落库再下发，则两次 <c>bid</c> 不一致，后续 <c>events</c> 进度就找不到记录。
    /// 因此这里采用「先下发 → 取到回包与 bid → 再落库」的顺序；
    /// 调用方在创建时就按回包结果写入 <see cref="DjiDockCommand.Status"/>
    /// （<c>result = 0</c> 为「已下发」等待进度，非 0 为「失败」并收口）。
    /// </para>
    /// <para>
    /// <b>为什么还要判存在</b>：极端情况下设备在 <c>services_reply</c> 之后、本方法执行之前就推来
    /// <c>events</c> 进度，此时 <see cref="UpdateProgressAsync"/> 会先补建一行。
    /// 因此这里按 <c>bid</c> 查一次，存在则只补充下发侧的信息与结果，
    /// <b>不覆盖状态</b> —— 进度报文才是更新的真值。
    /// </para>
    /// </remarks>
    public async Task<DjiDockCommand> CreateAsync(DjiDockCommand command)
    {
        var exist = await GetByBidAsync(command.Bid);
        if (exist != null)
        {
            // 进度已先到：只补齐下发侧的信息，状态以进度报文为准（它是更新的真值）
            exist.ActionName = command.ActionName ?? exist.ActionName;
            exist.RiskLevel = command.RiskLevel;
            exist.PayloadJson = command.PayloadJson ?? exist.PayloadJson;
            exist.Result = command.Result;
            exist.ErrorMessage = command.ErrorMessage ?? exist.ErrorMessage;
            exist.OperatorId ??= command.OperatorId;
            exist.OperatorName ??= command.OperatorName;

            await _commandRes.AsUpdateable(exist)
                .UpdateColumns(m => new
                {
                    m.ActionName, m.RiskLevel, m.PayloadJson, m.Result,
                    m.ErrorMessage, m.OperatorId, m.OperatorName
                })
                .ExecuteCommandAsync();
            return exist;
        }

        command.CreateTime ??= DateTime.Now;
        await _commandRes.InsertAsync(command);
        return command;
    }

    /// <summary>
    /// 用 <c>events</c> 进度报文的 <c>output</c> 刷新指令状态与进度。
    /// </summary>
    /// <returns>是否命中已有记录</returns>
    public async Task<bool> UpdateProgressAsync(string bid, string dockSn, string method,
        DockCommandOutput output, long reportTimestamp)
    {
        var status = DockTaskStatusResolver.Parse(output?.Status);
        var percent = output?.Progress?.Percent;
        var stepKey = output?.Progress?.StepKey.IsNullOrWhiteSpace() == false
            ? output.Progress.StepKey
            : output?.Progress?.CurrentStep;

        var command = await GetByBidAsync(bid);
        if (command == null)
        {
            // 没有下发记录（可能来自现场操作）：补建一行，保证事件不丢
            command = new DjiDockCommand
            {
                DockSn = dockSn,
                Method = method,
                ActionName = method,
                Bid = bid,
                RiskLevel = DockCommandRiskEnum.Normal,
                CreateTime = DateTime.Now,
            };
            command.Status = status;
            command.Percent = percent;
            command.StepKey = stepKey;
            command.ReportTimestamp = reportTimestamp;
            if (status.IsFinal()) command.FinishTime = DateTime.Now;
            await _commandRes.InsertAsync(command);

            // 现场操作（遥控器 / 机场本地面板）不会被云端下发链路记录，只能靠这条事件补档
            _logger.LogInformation("收到无下发记录的机场指令事件，已补档：method={Method}，gateway={Gateway}，bid={Bid}，status={Status}",
                method, dockSn, bid, status);
            return false;
        }

        await _commandRes.AsUpdateable()
            .SetColumns(m => new DjiDockCommand
            {
                Status = status,
                Percent = percent,
                StepKey = stepKey,
                // 进度报文只带 status/progress，不含 result；保持回包阶段拿到的值不被冲掉
                Result = command.Result,
                ReportTimestamp = reportTimestamp,
                FinishTime = status.IsFinal() ? DateTime.Now : command.FinishTime,
            })
            .Where(m => m.Id == command.Id)
            .ExecuteCommandAsync();

        return true;
    }

    /// <summary>把某机场所有「下发中」的僵死记录收口为超时（服务重启后清理，避免前端一直转圈）</summary>
    public async Task<int> CloseStaleAsync(DateTime before)
        => await _commandRes.AsUpdateable()
            .SetColumns(m => new DjiDockCommand
            {
                Status = DockTaskStatusEnum.Timeout,
                FinishTime = DateTime.Now,
                ErrorMessage = "指令下发后未收到设备回包",
            })
            .Where(m => m.Status == DockTaskStatusEnum.Sending && m.CreateTime != null && m.CreateTime < before)
            .ExecuteCommandAsync();

    #endregion
}
