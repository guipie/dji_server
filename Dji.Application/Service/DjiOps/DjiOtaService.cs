// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using System.Linq;
using Dji.Application.Cloud.Core;
using Dji.Application.Cloud.Dto.Ops;
using Dji.Application.Cloud.Entity;
using Dji.Application.CloudRepository;
using Dji.Application.Service.Common;
using Dji.Application.Service.DjiOps.Dto;
using Dji.Core.Enum.DjiEnum.Dock;
using Dji.Core.Enum.DjiEnum.Ops;
using Furion.JsonSerialization;

namespace Dji.Application.Service.DjiOps;

/// <summary>
/// 固件升级服务（升级任务下发 + 进度查看）。
/// </summary>
/// <remarks>
/// <para>
/// <b>数据流向</b>：本服务负责「校验 → 下发 <c>ota_create</c> → 建单」；
/// 设备随后推送的 <c>ota_progress</c> 由 <c>MqOtaService</c> 按 <c>bid</c> 落回同一行。
/// 因此这里没有「刷新进度」接口 —— 前端轮询分页列表即可看到实时进度。
/// </para>
/// <para>
/// <b>为什么没有「可升级固件列表」接口</b>：上云 API 不提供「查询设备可用固件版本」的能力，
/// 固件包地址与 MD5 必须由运维侧提供（DJI 官网下载后放到自己的对象存储）。
/// 这是协议的能力边界，不是本服务的缺失。
/// </para>
/// <para>
/// <b>升级前的前置条件为什么要自己判一遍</b>：机场对不满足条件的升级请求会返回错误码，
/// 但错误码不直观（运维拿到 <c>1600xx</c> 看不出「飞行器不在舱」）。
/// 本地先判一次能给出可执行的中文提示，同时省掉一次十几秒的空等。
/// </para>
/// </remarks>
[ApiDescriptionSettings(ApplicationConst.GroupName, Order = 150)]
public class DjiOtaService(
    SqlSugarRepository<Dji.Core.Entity.DjiDevice> deviceRep,
    SqlSugarRepository<DjiOtaTask> taskRep,
    DjiOtaRepository otaRepository,
    DjiDockStateRepository dockStateRepository,
    MqttGatewayPublish publish,
    UserManager userManager,
    ILogger<DjiOtaService> logger) : IDynamicApiController, ITransient
{
    /// <summary>下发回包等待时长（固件升级指令要先做本地一致性检查，响应偏慢）</summary>
    private const int CommandReplyTimeoutSeconds = 20;

    private readonly SqlSugarRepository<Dji.Core.Entity.DjiDevice> _deviceRep = deviceRep;
    private readonly SqlSugarRepository<DjiOtaTask> _taskRep = taskRep;
    private readonly DjiOtaRepository _otaRepository = otaRepository;
    private readonly DjiDockStateRepository _dockStateRepository = dockStateRepository;
    private readonly MqttGatewayPublish _publish = publish;
    private readonly UserManager _userManager = userManager;
    private readonly ILogger<DjiOtaService> _logger = logger;

    #region 查询

    /// <summary>升级任务分页</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Page")]
    public async Task<SqlSugarPagedList<OtaTaskOutput>> Page(OtaTaskSearchInput input)
    {
        input ??= new OtaTaskSearchInput();

        var query = _taskRep.AsQueryable()
            .WhereIF(!input.WorkspaceId.IsNullOrWhiteSpace(), m => m.WorkspaceId == input.WorkspaceId)
            .WhereIF(!input.DockSn.IsNullOrWhiteSpace(), m => m.DockSn == input.DockSn)
            .WhereIF(input.Status.HasValue, m => m.Status == input.Status)
            .WhereIF(input.UpgradeType.HasValue, m => m.UpgradeType == input.UpgradeType)
            .WhereIF(!input.BatchId.IsNullOrWhiteSpace(), m => m.BatchId == input.BatchId)
            .WhereIF(!input.DeviceSn.IsNullOrWhiteSpace(), m => m.DeviceSns.Contains(input.DeviceSn.Trim()))
            .WhereIF(input.StartTime.HasValue, m => m.CreateTime >= input.StartTime)
            .WhereIF(input.EndTime.HasValue, m => m.CreateTime <= input.EndTime)
            .OrderBy(m => m.CreateTime, OrderByType.Desc);

        var paged = await query.Select(m => new OtaTaskOutput
        {
            Id = m.Id,
            WorkspaceId = m.WorkspaceId,
            DockSn = m.DockSn,
            BatchId = m.BatchId,
            Bid = m.Bid,
            DeviceCount = m.DeviceCount,
            DeviceSns = m.DeviceSns,
            IncludeDock = m.IncludeDock,
            IncludeDrone = m.IncludeDrone,
            TargetVersion = m.TargetVersion,
            CurrentVersion = m.CurrentVersion,
            UpgradeType = m.UpgradeType,
            Status = m.Status,
            Percent = m.Percent,
            CurrentStep = m.CurrentStep,
            Result = m.Result,
            ErrorMessage = m.ErrorMessage,
            OperatorName = m.OperatorName,
            CreateTime = m.CreateTime,
            FinishTime = m.FinishTime,
        }).ToPagedListAsync(input.Page, input.PageSize);

        await FillAsync(paged.Items.ToList());
        return paged;
    }

    /// <summary>升级任务详情</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "Detail")]
    public async Task<OtaTaskOutput> Detail([FromQuery] long id)
    {
        var entity = await _otaRepository.GetAsync(id);
        if (entity == null) return null;

        var output = ToOutput(entity);
        await FillAsync(new List<OtaTaskOutput> { output });
        return output;
    }

    /// <summary>
    /// 某机场下可选的可升级设备（机场本体 + 已绑定的飞行器）。
    /// </summary>
    /// <remarks>
    /// 只返回该机场下的设备：跨机场选设备是毫无意义的操作，与其在提交时拒绝，
    /// 不如在数据源上就限制住 —— 前端只需渲染下发的列表即可，不必自己按 ParentSn 过滤。
    /// </remarks>
    [HttpGet]
    [ApiDescriptionSettings(Name = "Devices")]
    public async Task<List<OtaDeviceOptionOutput>> Devices([FromQuery] string dockSn)
    {
        var dock = await ResolveDockAsync(dockSn);

        var children = await _deviceRep.AsQueryable()
            .Where(m => m.ParentSn == dock.Sn)
            .OrderBy(m => m.Domain)
            .ToListAsync();

        var list = new List<OtaDeviceOptionOutput>
        {
            new()
            {
                Sn = dock.Sn,
                Name = dock.Nick,
                IsDock = true,
                Model = dock.Model,
                FirmwareVersion = dock.FirmwareVersion,
                Label = $"机场本体｜{dock.Nick ?? dock.Sn}",
            },
        };

        list.AddRange(children.Select(m => new OtaDeviceOptionOutput
        {
            Sn = m.Sn,
            Name = m.Nick ?? m.Model,
            IsDock = false,
            Model = m.Model,
            FirmwareVersion = m.FirmwareVersion,
            Label = $"{(m.Domain == DomainEnum.Repeater ? "中继" : "飞行器")}｜{m.Nick ?? m.Model ?? m.Sn}",
        }));

        return list;
    }

    /// <summary>任务状态字典</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "StatusOptions")]
    public List<OpsDictOptionOutput> StatusOptions()
        => typeof(OtaTaskStatusEnum).GetEnumDescDictionary()
            .OrderBy(m => m.Key)
            .Select(m => new OpsDictOptionOutput { Value = m.Key, Label = m.Value })
            .ToList();

    /// <summary>升级类型字典</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "UpgradeTypeOptions")]
    public List<OpsDictOptionOutput> UpgradeTypeOptions()
        => typeof(OtaUpgradeTypeEnum).GetEnumDescDictionary()
            .OrderBy(m => m.Key)
            .Select(m => new OpsDictOptionOutput { Value = m.Key, Label = m.Value })
            .ToList();

    /// <summary>升级步骤字典</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "StepOptions")]
    public List<OpsDictOptionOutput> StepOptions()
        => typeof(OtaStepEnum).GetEnumDescDictionary()
            .OrderBy(m => m.Key)
            .Select(m => new OpsDictOptionOutput { Value = m.Key, Label = m.Value })
            .ToList();

    /// <summary>批次下拉（取最近若干批，供筛选）</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "BatchOptions")]
    public async Task<List<OpsBatchOptionOutput>> BatchOptions([FromQuery] string dockSn)
    {
        // 先在数据库侧按时间取最近的一批候选，再在内存里分组 ——
        // 避免依赖 SQL 的 GROUP BY + 聚合函数（SQLite 与 MySQL 的写法与空值行为并不一致）。
        var rows = await _taskRep.AsQueryable()
            .WhereIF(!dockSn.IsNullOrWhiteSpace(), m => m.DockSn == dockSn)
            .Where(m => m.BatchId != null && m.BatchId != "")
            .OrderBy(m => m.CreateTime, OrderByType.Desc)
            .Take(500)
            .Select(m => new { m.BatchId, m.CreateTime })
            .ToListAsync();

        return rows.GroupBy(m => m.BatchId)
            .Select(g => new { BatchId = g.Key, Time = g.Min(m => m.CreateTime) })
            .OrderByDescending(m => m.Time)
            .Take(50)
            .Select(m => new OpsBatchOptionOutput
            {
                BatchId = m.BatchId,
                Label = m.Time.HasValue ? $"{m.BatchId}（{m.Time:yyyy-MM-dd HH:mm}）" : m.BatchId,
            })
            .ToList();
    }

    #endregion

    #region 下发

    /// <summary>
    /// 下发固件升级任务。
    /// </summary>
    /// <remarks>
    /// <para>前置校验（全部通过才发 MQTT）：</para>
    /// <list type="number">
    /// <item>机场已建档、属机场类型、已绑定工作空间、在线；</item>
    /// <item>机场空闲（<c>mode_code = 0</c>）、急停未按下、当前不在固件升级中；</item>
    /// <item>该机场没有进行中的升级任务；</item>
    /// <item>设备必须属于该机场（机场本体或其子设备），同一设备不重复；</item>
    /// <item>非一致性升级必须提供完整固件包信息；</item>
    /// <item>含飞行器时，飞行器必须在舱内；</item>
    /// <item>一次任务只允许一种升级类型。</item>
    /// </list>
    /// <para>
    /// <b>为什么限制「一次任务一种升级类型」</b>：任务表只有一个升级类型列，
    /// 混用会让这条记录无法如实描述自己。而且混用本身没有业务意义 ——
    /// 一致性升级的含义就是「把机场与飞行器一起补到互相匹配的版本」，
    /// 与普通升级混批只会让设备侧困惑。
    /// </para>
    /// </remarks>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Create")]
    public async Task<OtaTaskOutput> Create([FromBody] OtaCreateInput input)
    {
        var dock = await ResolveDockAsync(input?.DockSn);

        if (input.Devices == null || input.Devices.Count == 0)
            throw Oops.Oh("请至少选择一台待升级设备");

        if (!input.Confirm)
            throw Oops.Oh("固件升级会中断作业且耗时较长，请确认后再下发");

        // 防连点：从下发到建单之间的空窗里，数据库查不到「已有升级任务」
        using var _ = InFlightGuard.Enter($"ota|{dock.Sn}", "固件升级");

        var state = await _dockStateRepository.GetAsync(dock.Sn);
        EnsureUpgradable(dock, state);

        var running = await _otaRepository.GetRunningAsync(dock.Sn);
        if (running.Count > 0)
            throw Oops.Oh($"该机场已有进行中的升级任务（{running[0].TargetVersion}），请等待完成后再下发");

        var children = await _deviceRep.AsQueryable()
            .Where(m => m.ParentSn == dock.Sn)
            .ToListAsync();

        var upgradeTypes = input.Devices.Select(m => m.UpgradeType).Distinct().ToList();
        if (upgradeTypes.Count > 1)
            throw Oops.Oh("一次任务只能使用一种升级类型，请分开下发");

        var devices = new List<OtaCreateDevice>();
        var targetVersions = new List<string>();
        var currentVersions = new List<string>();
        var seenSns = new HashSet<string>();
        var includeDock = false;
        var includeDrone = false;

        foreach (var item in input.Devices)
        {
            var sn = item.DeviceSn.Trim();

            if (!seenSns.Add(sn))
                throw Oops.Oh($"设备 {sn} 重复选择，请检查");

            var isDock = sn == dock.Sn;
            var child = children.FirstOrDefault(m => m.Sn == sn);
            if (!isDock && child == null)
                throw Oops.Oh($"设备 {sn} 不属于机场 {dock.Sn}，请重新选择");

            if (item.TargetVersion.IsNullOrWhiteSpace())
                throw Oops.Oh($"设备 {sn} 未填写目标版本");

            // 一致性升级由设备自行从 DJI 服务器取包，只需给目标版本；
            // 普通 / PSDK 升级必须由云端提供完整包信息，否则设备无从下载
            if (item.UpgradeType != OtaUpgradeTypeEnum.Consistency)
            {
                if (item.FileUrl.IsNullOrWhiteSpace() || item.Md5.IsNullOrWhiteSpace()
                    || item.FileSize is null or <= 0 || item.FileName.IsNullOrWhiteSpace())
                {
                    throw Oops.Oh($"设备 {sn} 选择「{Desc(item.UpgradeType)}」时，必须提供固件包的下载地址、MD5、大小与文件名");
                }
            }

            devices.Add(new OtaCreateDevice
            {
                Sn = sn,
                ProductVersion = item.TargetVersion.Trim(),
                FileUrl = item.FileUrl,
                Md5 = item.Md5,
                FileSize = item.FileSize,
                FileName = item.FileName,
                FirmwareUpgradeType = item.UpgradeType,
            });

            targetVersions.Add(item.TargetVersion.Trim());
            currentVersions.Add((isDock ? dock.FirmwareVersion : child.FirmwareVersion) ?? "-");

            if (isDock) includeDock = true;
            else if (child.Domain != DomainEnum.Repeater) includeDrone = true;
        }

        if (includeDrone && state.DroneInDock != (int)DroneInDockEnum.Inside)
            throw Oops.Oh("升级飞行器需要飞行器在舱内（当前不在舱），请先让飞行器回舱并等待上电完成");

        var payload = new OtaCreatePayload { Devices = devices };
        var request = new CloudMqRequest<OtaCreatePayload>(TopicMethods.OtaCreate, payload, dock.Sn);

        var result = await SendAsync(dock.Sn, request);

        var task = await _otaRepository.CreateAsync(new DjiOtaTask
        {
            WorkspaceId = dock.WorkspaceId,
            DockSn = dock.Sn,
            BatchId = Guid.NewGuid().ToString("N"),
            Bid = request.Bid,
            DeviceCount = devices.Count,
            DeviceSns = string.Join(",", devices.Select(m => m.Sn)),
            IncludeDock = includeDock,
            IncludeDrone = includeDrone,
            TargetVersion = string.Join(",", targetVersions.Distinct()),
            CurrentVersion = string.Join(",", currentVersions.Distinct()),
            UpgradeType = upgradeTypes[0],
            DevicesJson = JSON.Serialize(devices),
            Status = result == 0 ? OtaTaskStatusEnum.Sending : OtaTaskStatusEnum.Failed,
            Result = result,
            ErrorMessage = result == 0 ? null : DescribeResult(result),
            OperatorId = _userManager.UserId,
            OperatorName = _userManager.RealName ?? _userManager.Account,
            CreateTime = DateTime.Now,
            FinishTime = result == 0 ? null : DateTime.Now,
        });

        if (result != 0)
            throw Oops.Oh($"机场拒绝固件升级，{DescribeResult(result)}");

        _logger.LogInformation("已下发固件升级：gateway:{Gateway}，devices:{Devices}，version:{Version}，bid:{Bid}",
            dock.Sn, task.DeviceSns, task.TargetVersion, request.Bid);

        var output = ToOutput(task);
        await FillAsync(new List<OtaTaskOutput> { output });
        return output;
    }

    #endregion

    #region 私有实现

    /// <summary>升级前置条件校验（不满足则抛业务异常，异常文案即可直接展示给运维）</summary>
    private static void EnsureUpgradable(Dji.Core.Entity.DjiDevice dock, DjiDockState state)
    {
        if (!dock.IsOnline) throw Oops.Oh("机场离线，无法下发固件升级");

        if (state == null)
            throw Oops.Oh("尚未收到机场上报的状态，暂时无法判断能否升级，请稍后重试");

        if (state.EmergencyStopState == (int)SwitchStateEnum.On)
            throw Oops.Oh("机场急停按钮已按下，请先复位后再升级");

        if (state.FirmwareUpgradeStatus == (int)DockFirmwareUpgradeStatusEnum.Upgrading)
            throw Oops.Oh("机场当前正在固件升级中，请等待完成");

        if (state.ModeCode != (int)DockModeCodeEnum.Idle)
            throw Oops.Oh($"机场当前处于「{Name(state.ModeCode, typeof(DockModeCodeEnum)) ?? "非空闲"}」，固件升级需要机场空闲");
    }

    private async Task<int> SendAsync<T>(string dockSn, CloudMqRequest<T> request)
    {
        try
        {
            var reply = await _publish.PublishWithReplyAsync<T, object>(
                Topics.ThingProductServices, request, CommandReplyTimeoutSeconds);

            var result = reply?.Data?.Result ?? -1;
            if (result != 0)
            {
                _logger.LogWarning("机场拒绝固件升级：gateway:{Gateway}，result:{Result}", dockSn, result);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "固件升级指令无应答：gateway:{Gateway}", dockSn);
            return -1;
        }
    }

    /// <summary>
    /// 把 <c>services_reply</c> 的返回码翻译成可读说明。
    /// </summary>
    /// <remarks>
    /// 官方对 <c>ota_create</c> 只约定「非 0 代表错误」，未公开逐错误码的字典，
    /// 因此不做猜测式翻译；超时（平台内部约定为 -1）单独给出明确解释。
    /// </remarks>
    private static string DescribeResult(int result)
        => result == -1 ? "设备未在超时时间内回包（指挥链路可能中断）" : $"错误码：{result}";

    /// <summary>填充派生字段（机场昵称、枚举名、设备明细）</summary>
    private async Task FillAsync(List<OtaTaskOutput> items)
    {
        if (items.Count == 0) return;

        foreach (var item in items)
        {
            item.StatusName = Desc(item.Status);
            item.UpgradeTypeName = Desc(item.UpgradeType);
            item.CurrentStepName = OtaStepResolver.Label(item.CurrentStep);
            item.IsRunning = !item.Status.IsFinal();
            item.ScopeName = (item.IncludeDock, item.IncludeDrone) switch
            {
                (true, true) => "机场 + 飞行器",
                (true, false) => "机场",
                (false, true) => "飞行器",
                _ => "未知",
            };
        }

        // 设备明细只存在实体的 JSON 列里，列表投影不携带它。
        // 这里按当前页的 ID 回查一次（行数上限就是 PageSize），而不是给每行做一次查询。
        var ids = items.Select(m => m.Id).ToList();
        var jsonMap = (await _taskRep.AsQueryable()
                .Where(m => ids.Contains(m.Id))
                .Select(m => new { m.Id, m.DevicesJson })
                .ToListAsync())
            .ToDictionary(m => m.Id, m => m.DevicesJson);

        foreach (var item in items)
            item.Devices = ParseDevices(jsonMap.GetValueOrDefault(item.Id), item);

        // 设备名与机场昵称：一次查全，避免逐行查库
        var sns = items.Select(m => m.DockSn).Concat(items.SelectMany(m => m.Devices).Select(m => m.Sn))
            .Where(m => !m.IsNullOrWhiteSpace()).Distinct().ToList();

        if (sns.Count == 0) return;

        var devices = await _deviceRep.AsQueryable().Where(m => sns.Contains(m.Sn)).ToListAsync();
        var nameMap = devices.ToDictionary(m => m.Sn, m => m.Nick.IsNullOrWhiteSpace() ? m.Model : m.Nick);

        foreach (var item in items)
        {
            item.DockNick = nameMap.GetValueOrDefault(item.DockSn);
            foreach (var device in item.Devices)
                device.DeviceName = device.IsDock
                    ? $"机场本体｜{nameMap.GetValueOrDefault(device.Sn) ?? device.Sn}"
                    : nameMap.GetValueOrDefault(device.Sn) ?? device.Sn;
        }
    }

    /// <summary>
    /// 解析任务的设备明细。
    /// </summary>
    /// <remarks>
    /// JSON 可能因历史数据或人工改动而缺失 / 损坏：此时按设备 SN 列表生成占位明细，
    /// 保证单据本身仍能展示，不让一条坏数据把整页列表打挂。
    /// </remarks>
    private static List<OtaTaskDeviceOutput> ParseDevices(string json, OtaTaskOutput item)
    {
        if (!json.IsNullOrWhiteSpace())
        {
            try
            {
                var devices = JSON.Deserialize<List<OtaCreateDevice>>(json);
                if (devices is { Count: > 0 })
                {
                    return devices.Select(m => new OtaTaskDeviceOutput
                    {
                        Sn = m.Sn,
                        IsDock = m.Sn == item.DockSn,
                        ProductVersion = m.ProductVersion,
                        FileUrl = m.FileUrl,
                        Md5 = m.Md5,
                        FileSize = m.FileSize,
                        FileName = m.FileName,
                        UpgradeType = m.FirmwareUpgradeType,
                    }).ToList();
                }
            }
            catch
            {
                // 落到下面的降级分支
            }
        }

        return (item.DeviceSns ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(sn => new OtaTaskDeviceOutput
            {
                Sn = sn,
                IsDock = sn == item.DockSn,
                ProductVersion = item.TargetVersion,
                UpgradeType = item.UpgradeType,
            })
            .ToList();
    }

    private static OtaTaskOutput ToOutput(DjiOtaTask entity) => new()
    {
        Id = entity.Id,
        WorkspaceId = entity.WorkspaceId,
        DockSn = entity.DockSn,
        BatchId = entity.BatchId,
        Bid = entity.Bid,
        DeviceCount = entity.DeviceCount,
        DeviceSns = entity.DeviceSns,
        IncludeDock = entity.IncludeDock,
        IncludeDrone = entity.IncludeDrone,
        TargetVersion = entity.TargetVersion,
        CurrentVersion = entity.CurrentVersion,
        UpgradeType = entity.UpgradeType,
        Status = entity.Status,
        Percent = entity.Percent,
        CurrentStep = entity.CurrentStep,
        Result = entity.Result,
        ErrorMessage = entity.ErrorMessage,
        OperatorName = entity.OperatorName,
        CreateTime = entity.CreateTime,
        FinishTime = entity.FinishTime,
        StatusName = Desc(entity.Status),
        UpgradeTypeName = Desc(entity.UpgradeType),
        CurrentStepName = OtaStepResolver.Label(entity.CurrentStep),
        IsRunning = !entity.Status.IsFinal(),
    };

    private async Task<Dji.Core.Entity.DjiDevice> ResolveDockAsync(string dockSn)
    {
        if (dockSn.IsNullOrWhiteSpace()) throw Oops.Oh("机场不能为空");

        var dock = await _deviceRep.GetFirstAsync(m => m.Sn == dockSn) ?? throw Oops.Oh($"未找到机场：{dockSn}");
        if (dock.Domain != DomainEnum.Dock) throw Oops.Oh($"{dockSn} 不是机场设备");
        if (dock.WorkspaceId.IsNullOrWhiteSpace()) throw Oops.Oh($"机场【{dock.Nick ?? dock.Sn}】尚未绑定工作空间");

        return dock;
    }

    /// <summary>取枚举描述；未知取值返回 null</summary>
    private static string Name(int? value, Type enumType)
    {
        if (!value.HasValue) return null;
        return enumType.GetEnumDescDictionary().TryGetValue(value.Value, out var label) ? label : null;
    }

    /// <summary>取枚举的 <c>[Description]</c>；显式写全限定名以避免与 NewLife 的同名扩展二义</summary>
    private static string Desc<T>(T value) where T : struct, System.Enum
        => Dji.Core.EnumExtension.GetDescription(value);

    #endregion
}
