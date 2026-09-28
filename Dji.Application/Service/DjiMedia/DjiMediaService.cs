// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
//
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Cloud.Core;
using Dji.Application.Cloud.Dto.Media;
using Dji.Application.Cloud.Entity;
using Dji.Application.CloudRepository;
using Dji.Application.Service.DjiMedia.Dto;
using Dji.Core.Enum.DjiEnum.Media;
using System.Linq;

namespace Dji.Application.Service.DjiMedia;

/// <summary>
/// 媒体库服务。
/// </summary>
/// <remarks>
/// <para>
/// <b>数据来源</b>：机场通过 <c>file_upload_callback</c> 上报的媒体文件元数据
/// （文件本体在对象存储里，本服务只管理元数据与访问地址）。
/// </para>
/// <para>
/// <b>本服务是只读 + 删除</b>：不存在「新建媒体」的接口 —— 媒体只能由机场产生。
/// 唯一的写操作是 <see cref="Prioritize"/>，它不改变文件本身，
/// 只是请求机场把某个任务的媒体提前上传（<c>upload_flighttask_media_prioritize</c>）。
/// </para>
/// <para>
/// <b>坐标不转换</b>：<c>Longitude</c> / <c>Latitude</c> 是 WGS84 原始值，
/// 前端地图打点若用高德 / 百度底图需自行做一次坐标纠偏 —— 转换放在展示层，
/// 避免落库时丢失原始值（测绘场景需要原始 WGS84）。
/// </para>
/// </remarks>
[ApiDescriptionSettings(ApplicationConst.GroupName, Order = 120)]
public class DjiMediaService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<DjiMediaFile> _mediaRep;
    private readonly SqlSugarRepository<Dji.Core.Entity.DjiDevice> _deviceRep;
    private readonly SqlSugarRepository<DjiWaylineTask> _taskRep;
    private readonly DjiMediaRepository _mediaRepository;
    private readonly MqttGatewayPublish _publish;
    private readonly ILogger<DjiMediaService> _logger;

    public DjiMediaService(
        SqlSugarRepository<DjiMediaFile> mediaRep,
        SqlSugarRepository<Dji.Core.Entity.DjiDevice> deviceRep,
        SqlSugarRepository<DjiWaylineTask> taskRep,
        DjiMediaRepository mediaRepository,
        MqttGatewayPublish publish,
        ILogger<DjiMediaService> logger)
    {
        _mediaRep = mediaRep;
        _deviceRep = deviceRep;
        _taskRep = taskRep;
        _mediaRepository = mediaRepository;
        _publish = publish;
        _logger = logger;
    }

    #region 查询

    /// <summary>分页查询媒体文件</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Page")]
    public async Task<SqlSugarPagedList<DjiMediaOutput>> Page(DjiMediaSearchInput input)
    {
        var query = _mediaRep.AsQueryable()
            .WhereIF(!input.WorkspaceId.IsNullOrWhiteSpace(), m => m.WorkspaceId == input.WorkspaceId)
            .WhereIF(!input.DockSn.IsNullOrWhiteSpace(), m => m.DockSn == input.DockSn)
            .WhereIF(!input.FlightId.IsNullOrWhiteSpace(), m => m.FlightId == input.FlightId)
            .WhereIF(input.FileType.HasValue, m => m.FileType == input.FileType)
            .WhereIF(input.OnlyOriginal == true, m => m.IsOriginal)
            .WhereIF(!input.FileName.IsNullOrWhiteSpace(), m => m.FileName.Contains(input.FileName.Trim()))
            .WhereIF(input.StartTime.HasValue, m => m.MediaCreateTime >= input.StartTime)
            .WhereIF(input.EndTime.HasValue, m => m.MediaCreateTime <= input.EndTime)
            // 按拍摄时间倒序：新拍的在前；无拍摄时间（老固件或 PPK 附件）用回传时间兜底
            .OrderBy(m => m.MediaCreateTime == null)
            .OrderBy(m => m.MediaCreateTime, OrderByType.Desc)
            .OrderBy(m => m.CreateTime, OrderByType.Desc);

        var paged = await query.Select(m => new DjiMediaOutput
        {
            Id = m.Id,
            WorkspaceId = m.WorkspaceId,
            ObjectKey = m.ObjectKey,
            FileName = m.FileName,
            Suffix = m.Suffix,
            FileType = m.FileType,
            FileSize = m.FileSize,
            Url = m.Url,
            DockSn = m.DockSn,
            DroneSn = m.DroneSn,
            FlightId = m.FlightId,
            IsOriginal = m.IsOriginal,
            FileGroupId = m.FileGroupId,
            MediaCreateTime = m.MediaCreateTime,
            Longitude = m.Longitude,
            Latitude = m.Latitude,
            AbsoluteAltitude = m.AbsoluteAltitude,
            RelativeAltitude = m.RelativeAltitude,
            GimbalYawDegree = m.GimbalYawDegree,
            CreateTime = m.CreateTime,
        }).ToPagedListAsync(input.Page, input.PageSize);

        return await CompleteAsync(paged);
    }

    /// <summary>媒体详情</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "Detail")]
    public async Task<DjiMediaDetailOutput> Detail([FromQuery] BaseIdInput input)
    {
        var entity = await _mediaRepository.GetAsync(input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D1002);
        var output = entity.Adapt<DjiMediaDetailOutput>();
        FillDerived(output);

        var dock = await _deviceRep.GetFirstAsync(m => m.Sn == entity.DockSn);
        output.DockNick = dock?.Nick;

        if (!entity.FlightId.IsNullOrWhiteSpace())
        {
            var task = await _taskRep.GetFirstAsync(m => m.FlightId == entity.FlightId);
            output.FlightJobName = task?.JobName;
        }

        return output;
    }

    /// <summary>按任务取全部媒体（按拍摄时间正序，便于按航点顺序浏览）</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "ByTask")]
    public async Task<List<DjiMediaOutput>> ByTask(MediaByTaskInput input)
    {
        string flightId = input.FlightId;

        if (flightId.IsNullOrWhiteSpace() && input.TaskId is > 0)
        {
            var task = await _taskRep.GetFirstAsync(m => m.Id == input.TaskId);
            flightId = task?.FlightId;
        }

        if (flightId.IsNullOrWhiteSpace()) return [];

        var list = await _mediaRepository.GetByFlightIdAsync(flightId);
        if (list.Count == 0) return [];

        var output = list.Select(m =>
        {
            var item = m.Adapt<DjiMediaOutput>();
            FillDerived(item);
            return item;
        }).ToList();

        var dockSn = list[0].DockSn;
        var dockNick = dockSn.IsNullOrWhiteSpace()
            ? null
            : (await _deviceRep.GetFirstAsync(m => m.Sn == dockSn))?.Nick;
        foreach (var item in output) item.DockNick = dockNick;

        return output;
    }

    /// <summary>媒体统计（总数 / 总大小 / 分类型）</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Stats")]
    public async Task<MediaStatsOutput> Stats(MediaStatsInput input)
    {
        var list = await _mediaRep.AsQueryable()
            .WhereIF(!input.WorkspaceId.IsNullOrWhiteSpace(), m => m.WorkspaceId == input.WorkspaceId)
            .WhereIF(!input.DockSn.IsNullOrWhiteSpace(), m => m.DockSn == input.DockSn)
            .Select(m => new { m.FileType, m.FileSize })
            .ToListAsync();

        var output = new MediaStatsOutput
        {
            TotalCount = list.Count,
            TotalSize = list.Sum(m => m.FileSize),
            ImageCount = list.Count(m => m.FileType == MediaFileTypeEnum.Image),
            VideoCount = list.Count(m => m.FileType == MediaFileTypeEnum.Video),
        };

        output.Types = list
            .GroupBy(m => m.FileType)
            .Select(g => new MediaTypeStatOutput
            {
                FileType = g.Key,
                FileTypeName = DescribeFileType(g.Key),
                Count = g.Count(),
                TotalSize = g.Sum(m => m.FileSize),
            })
            .OrderByDescending(m => m.Count)
            .ToList();

        return output;
    }

    /// <summary>有媒体的任务列表（筛选下拉用）</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "TaskOptions")]
    public async Task<List<MediaTaskOptionOutput>> TaskOptions(MediaStatsInput input)
    {
        var grouped = await _mediaRep.AsQueryable()
            .Where(m => m.FlightId != null && m.FlightId != "")
            .WhereIF(!input.WorkspaceId.IsNullOrWhiteSpace(), m => m.WorkspaceId == input.WorkspaceId)
            .WhereIF(!input.DockSn.IsNullOrWhiteSpace(), m => m.DockSn == input.DockSn)
            .GroupBy(m => new { m.FlightId, m.DockSn })
            .Select(m => new { m.FlightId, m.DockSn, Count = SqlFunc.AggregateCount(m.Id) })
            .OrderByDescending(m => m.Count)
            .Take(200)
            .ToListAsync();

        if (grouped.Count == 0) return [];

        var flightIds = grouped.Select(m => m.FlightId).ToList();
        var tasks = await _taskRep.AsQueryable().Where(m => flightIds.Contains(m.FlightId)).ToListAsync();

        return grouped.Select(m =>
        {
            var task = tasks.FirstOrDefault(t => t.FlightId == m.FlightId);
            var name = task?.JobName.IsNullOrWhiteSpace() == false ? task.JobName : m.FlightId;
            return new MediaTaskOptionOutput
            {
                FlightId = m.FlightId,
                JobName = task?.JobName,
                DockSn = m.DockSn,
                MediaCount = m.Count,
                Label = $"{name}（{m.Count} 个文件）",
            };
        }).ToList();
    }

    /// <summary>媒体类型字典</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "TypeOptions")]
    public List<MediaTypeOptionOutput> TypeOptions()
    {
        return typeof(MediaFileTypeEnum).GetEnumDescDictionary()
            .OrderBy(m => m.Key)
            .Select(m => new MediaTypeOptionOutput { Value = m.Key, Label = m.Value })
            .ToList();
    }

    /// <summary>回传过媒体的机场下拉（筛选用）</summary>
    /// <remarks>
    /// 返回全部机场（<c>Domain = Dock</c>）并带上各自已回传的媒体数量 —— 
    /// 保留「0 个文件」的机场是为了让用户能确认「这个机场确实还没传上来」，
    /// 而不是以为筛选条件把机场过滤没了。
    /// </remarks>
    [HttpPost]
    [ApiDescriptionSettings(Name = "DockOptions")]
    public async Task<List<MediaDockOptionOutput>> DockOptions(MediaDockOptionInput input)
    {
        var docks = await _deviceRep.AsQueryable()
            .Where(m => m.Domain == DomainEnum.Dock)
            .WhereIF(!input.WorkspaceId.IsNullOrWhiteSpace(), m => m.WorkspaceId == input.WorkspaceId)
            .OrderBy(m => m.Nick)
            .ToListAsync();

        if (docks.Count == 0) return [];

        var counts = await _mediaRep.AsQueryable()
            .Where(m => m.DockSn != null && m.DockSn != "")
            .GroupBy(m => m.DockSn)
            .Select(m => new { m.DockSn, Count = SqlFunc.AggregateCount(m.Id) })
            .ToListAsync();

        return docks.Select(m => new MediaDockOptionOutput
        {
            Sn = m.Sn,
            Nick = m.Nick,
            WorkspaceId = m.WorkspaceId,
            IsOnline = m.IsOnline,
            MediaCount = counts.FirstOrDefault(c => c.DockSn == m.Sn)?.Count ?? 0,
            Label = m.Nick.IsNullOrWhiteSpace() ? m.Sn : $"{m.Nick}（{m.Sn}）",
        }).ToList();
    }

    #endregion

    #region 写入

    /// <summary>删除媒体记录（批量）</summary>
    /// <remarks>
    /// <b>只删元数据，不删对象存储里的文件</b>：机场直传的文件归属对象存储的生命周期策略，
    /// 由云端删桶内对象需要额外授权与失败补偿，风险大于收益。若确实需要释放空间，
    /// 请在对象存储侧按前缀（工作空间 ID）批量清理。
    /// </remarks>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Delete")]
    public async Task<bool> Delete(DeleteMediaInput input)
    {
        var count = await _mediaRepository.DeleteAsync(input.Ids);
        _logger.LogInformation("删除媒体记录 {Count} 条", count);
        return count > 0;
    }

    /// <summary>
    /// 把某任务的媒体调整为最高上传优先级。
    /// </summary>
    /// <remarks>
    /// 机场会自动排出上传队列，但在「这次任务的成果更急」时需要人工插队。
    /// 指令下发到任务所属机场，因此必须先由 flight_id 反查任务拿到机场 SN。
    /// </remarks>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Prioritize")]
    public async Task<bool> Prioritize(MediaPrioritizeInput input)
    {
        var task = await _taskRep.GetFirstAsync(m => m.FlightId == input.FlightId)
                   ?? throw Oops.Oh($"未找到任务：{input.FlightId}");

        if (task.DockSn.IsNullOrWhiteSpace()) throw Oops.Oh("该任务未记录执行机场，无法调整上传优先级");

        try
        {
            var request = new CloudMqRequest<UploadFlighttaskMediaPrioritizeInput>(
                TopicMethods.UploadFlighttaskMediaPrioritize,
                new UploadFlighttaskMediaPrioritizeInput { FlightId = input.FlightId },
                task.DockSn);

            var reply = await _publish.PublishWithReplyAsync<UploadFlighttaskMediaPrioritizeInput, object>(
                Topics.ThingProductServices, request, 15);
            var result = reply?.Data?.Result ?? -1;

            if (result != 0) _logger.LogWarning("调整媒体上传优先级被拒绝，flight_id:{FlightId}，result:{Result}",
                input.FlightId, result);

            return result == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调整媒体上传优先级无应答，flight_id:{FlightId}", input.FlightId);
            throw Oops.Oh("机场未在 15 秒内应答，请检查机场在线状态");
        }
    }

    #endregion

    #region 私有实现

    /// <summary>批量补全派生字段（机场别名 / 任务名 / 类型名 / 大小文本）</summary>
    private async Task<SqlSugarPagedList<DjiMediaOutput>> CompleteAsync(SqlSugarPagedList<DjiMediaOutput> paged)
    {
        // 注意：SqlSugarPagedList.Items 声明为 IEnumerable<T>，不能用 Count 属性
        if (paged?.Items is not { } items || !items.Any()) return paged;

        foreach (var item in items) FillDerived(item);

        var dockSns = items.Select(m => m.DockSn).Where(m => !m.IsNullOrWhiteSpace()).Distinct().ToList();
        var docks = await _deviceRep.AsQueryable().Where(m => dockSns.Contains(m.Sn)).ToListAsync();

        var flightIds = items.Select(m => m.FlightId).Where(m => !m.IsNullOrWhiteSpace()).Distinct().ToList();
        var tasks = flightIds.Count == 0
            ? []
            : await _taskRep.AsQueryable().Where(m => flightIds.Contains(m.FlightId)).ToListAsync();

        foreach (var item in items)
        {
            item.DockNick = docks.FirstOrDefault(m => m.Sn == item.DockSn)?.Nick;
            item.FlightJobName = tasks.FirstOrDefault(m => m.FlightId == item.FlightId)?.JobName;
        }

        return paged;
    }

    /// <summary>补全与实体字段相关的展示派生值</summary>
    private static void FillDerived(DjiMediaOutput item)
    {
        item.FileTypeName = DescribeFileType(item.FileType);
        item.Previewable = MediaFileTypeResolver.IsPreviewable(item.FileType);
        item.SizeText = FormatSize(item.FileSize);
    }

    private static string DescribeFileType(MediaFileTypeEnum type)
    {
        var text = EnumExtension.GetDescription(type);
        return text ?? (type == MediaFileTypeEnum.Unknown ? "其他" : type.ToString());
    }

    /// <summary>字节数转可读文本</summary>
    private static string FormatSize(long bytes)
    {
        if (bytes <= 0) return "-";
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit == 0 ? $"{value:0} {units[unit]}" : $"{value:0.##} {units[unit]}";
    }

    #endregion
}
