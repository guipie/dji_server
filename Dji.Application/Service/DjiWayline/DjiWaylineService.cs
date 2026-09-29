// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Dji.Application.Service.DjiWayline;
using Dji.Application.Service.DjiWayline.Dto;
using Dji.Application.Service.DjiWayline.Dto.Temp;
using Dji.Core.Entity.DjiEntity;
using Dji.Core.Enum.DjiEnum.Wayline;
using Dji.Core.Service;
using Dji.JsonSerialization;
using Microsoft.AspNetCore.Http;

namespace Dji.Application;

/// <summary>
/// 航线服务
/// </summary>
/// <remarks>
/// 航线数据以“主表 + JSON 大字段”方式存储：主表冗余列表展示字段，
/// JSON 字段保存前端完整航线参数并作为 KMZ 的唯一数据源；
/// 每次新增/修改/复制都会重新生成并上传大疆机场可识别的 KMZ 文件。
/// </remarks>
[ApiDescriptionSettings(ApplicationConst.GroupName, Order = 100)]
public class DjiWaylineService : IDynamicApiController, ITransient
{
    /// <summary>KMZ 文件上传目录</summary>
    private const string KmzUploadPath = "Upload/Wayline";

    /// <summary>KMZ MIME 类型</summary>
    private const string KmzContentType = "application/vnd.google-earth.kmz";

    private readonly SqlSugarRepository<DjiWaylineEntity> _rep;
    private readonly SqlSugarRepository<DjiDeviceCameraEnum> _cameraRep;
    private readonly SqlSugarRepository<DjiWorkspaceUser> _workspaceUserRep;
    private readonly DjiDeviceEnumService _deviceEnumService;
    private readonly UserManager _userManager;

    public DjiWaylineService(
        SqlSugarRepository<DjiWaylineEntity> rep,
        SqlSugarRepository<DjiDeviceCameraEnum> cameraRep,
        SqlSugarRepository<DjiWorkspaceUser> workspaceUserRep,
        DjiDeviceEnumService deviceEnumService,
        UserManager userManager)
    {
        _rep = rep;
        _cameraRep = cameraRep;
        _workspaceUserRep = workspaceUserRep;
        _deviceEnumService = deviceEnumService;
        _userManager = userManager;
    }

    #region 接口

    /// <summary>
    /// 新增航线
    /// </summary>
    /// <param name="request">航线参数</param>
    /// <returns>航线Id</returns>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Create")]
    public async Task<long> Create(CreateWaypointWaylineRequest request)
    {
        ValidateRequest(request);

        var workspaceId = await ResolveWorkspaceIdAsync(request.WorkspaceId);
        var waylineName = request.WaylineName.Trim();
        await EnsureNameUniqueAsync(workspaceId, waylineName, null);

        var device = await ResolveDeviceInfoAsync(request.DomainTypeSubType);
        var entity = new DjiWaylineEntity { WorkspaceId = workspaceId, WaylineName = waylineName };
        await FillEntityAsync(entity, request, device);

        await _rep.InsertAsync(entity);
        return entity.Id;
    }

    /// <summary>
    /// 分页查询航线
    /// </summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Page")]
    public async Task<SqlSugarPagedList<DjiWaylineOutput>> Page(DjiWaylineSearchInput input)
    {
        var query = _rep.AsQueryable()
            .WhereIF(!string.IsNullOrWhiteSpace(input.SearchKey), u => u.WaylineName.Contains(input.SearchKey.Trim()))
            .WhereIF(!string.IsNullOrWhiteSpace(input.Name), u => u.WaylineName.Contains(input.Name.Trim()))
            .WhereIF(!string.IsNullOrWhiteSpace(input.WorkspaceId), u => u.WorkspaceId == input.WorkspaceId)
            .WhereIF(!string.IsNullOrWhiteSpace(input.Drone), u => u.Drone.Contains(input.Drone.Trim()) || u.DroneModel.Contains(input.Drone.Trim()))
            .WhereIF(input.WaylineType.HasValue, u => u.WaylineType == input.WaylineType)
            .LeftJoin<DjiWorkspace>((u, w) => u.WorkspaceId == w.WorkspaceId)
            .Select<DjiWaylineOutput>();

        return await query.OrderBuilder(input).ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 获取航线详情（含完整航线参数，供编辑器回填）
    /// </summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "Detail")]
    public async Task<DjiWaylineDetailOutput> Detail([FromQuery] QueryByIdDjiWaylineInput input)
    {
        var entity = await GetEntityAsync(input.Id);
        var output = entity.Adapt<DjiWaylineDetailOutput>();
        output.Param = DeserializeParam(entity);
        output.WorkspaceNickName = await GetWorkspaceNickNameAsync(entity.WorkspaceId);
        return output;
    }

    /// <summary>
    /// 更新航线（重新生成 KMZ）
    /// </summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Update")]
    public async Task<bool> Update(UpdateDjiWaylineInput input)
    {
        var entity = await GetEntityAsync(input.Id);
        ValidateRequest(input);

        var workspaceId = await ResolveWorkspaceIdAsync(string.IsNullOrWhiteSpace(input.WorkspaceId) ? entity.WorkspaceId : input.WorkspaceId);
        var waylineName = input.WaylineName.Trim();
        await EnsureNameUniqueAsync(workspaceId, waylineName, entity.Id);

        var device = await ResolveDeviceInfoAsync(input.DomainTypeSubType);
        entity.WorkspaceId = workspaceId;
        entity.WaylineName = waylineName;
        await FillEntityAsync(entity, input, device);

        return await _rep.AsUpdateable(entity).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// 删除航线
    /// </summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Delete")]
    public async Task Delete(DeleteDjiWaylineInput input)
    {
        var entity = await GetEntityAsync(input.Id);
        await _rep.FakeDeleteAsync(entity);
    }

    /// <summary>
    /// 航线重命名
    /// </summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Rename")]
    public async Task<bool> Rename(RenameDjiWaylineInput input)
    {
        var entity = await GetEntityAsync(input.Id);
        var newName = (input.NewName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(newName)) throw Oops.Oh("航线名称不能为空");
        if (newName == entity.WaylineName) return true;

        await EnsureNameUniqueAsync(entity.WorkspaceId, newName, entity.Id);
        entity.WaylineName = newName;
        entity.KmzFileName = $"{newName}.kmz";
        return await _rep.AsUpdateable(entity).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// 复制航线
    /// </summary>
    /// <param name="input">源航线Id、新名称（可选）、目标空间（可选）</param>
    /// <returns>新航线Id</returns>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Copy")]
    public async Task<long> Copy(CopyDjiWaylineInput input)
    {
        var source = await GetEntityAsync(input.Id);
        var request = DeserializeParam(source) ?? throw Oops.Oh("航线参数缺失，无法复制，请重新编辑保存该航线");

        var workspaceId = string.IsNullOrWhiteSpace(input.WorkspaceId)
            ? source.WorkspaceId
            : await ResolveWorkspaceIdAsync(input.WorkspaceId);

        var waylineName = await BuildCopyNameAsync(workspaceId, input.NewName, source.WaylineName);
        request.WaylineName = waylineName;
        request.WorkspaceId = workspaceId;

        var device = await ResolveDeviceInfoAsync(source.DomainTypeSubType);
        var entity = new DjiWaylineEntity { WorkspaceId = workspaceId, WaylineName = waylineName };
        await FillEntityAsync(entity, request, device);

        await _rep.InsertAsync(entity);
        return entity.Id;
    }

    /// <summary>
    /// 导出 KMZ（按当前航线参数实时重新生成）
    /// </summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "Export")]
    public async Task<IActionResult> Export([FromQuery] QueryByIdDjiWaylineInput input)
    {
        var entity = await GetEntityAsync(input.Id);
        var request = DeserializeParam(entity) ?? throw Oops.Oh("航线参数缺失，无法导出，请重新编辑保存该航线");
        var device = await ResolveDeviceInfoAsync(entity.DomainTypeSubType);

        var bytes = BuildKmzBytes(request, device, GetCurrentUserName());
        return new FileStreamResult(new MemoryStream(bytes), KmzContentType)
        {
            FileDownloadName = $"{entity.WaylineName}.kmz",
        };
    }

    #endregion

    #region 私有实现

    private async Task<DjiWaylineEntity> GetEntityAsync(long id)
    {
        return await _rep.GetFirstAsync(u => u.Id == id) ?? throw Oops.Oh(ErrorCodeEnum.D1002);
    }

    /// <summary>把航线参数写入实体（含冗余展示字段、JSON 大字段与 KMZ 文件）</summary>
    private async Task FillEntityAsync(DjiWaylineEntity entity, CreateWaypointWaylineRequest request, DeviceInfo device)
    {
        var folder = request.Folder ??= new Folder();
        var mission = request.MissionConfig ??= new MissionConfig();
        var (distance, duration) = WaylineKmzBuilder.CalculateDistanceAndDuration(folder);
        var heightMode = WaylineKmzBuilder.ResolveHeightMode(request.Ext?.WaylinePointHeightMode);

        entity.WaylineType = WaylineKmzBuilder.ResolveWaylineType(request.TemplateType);
        entity.TemplateType = WaylineKmzBuilder.ResolveTemplateType(request);
        entity.TemplateStr = request.TemplateStr;
        entity.Drone = device.DeviceEnum.Name;
        entity.DroneModel = request.DroneModel;
        entity.DomainTypeSubType = request.DomainTypeSubType;
        entity.Acc = request.Acc;
        entity.TemplateId = WaylineKmzBuilder.SingleTemplateId;
        entity.WaylineId = WaylineKmzBuilder.SingleWaylineId;
        entity.AutoFlightSpeed = folder.AutoFlightSpeed;
        entity.ExecuteHeightMode = heightMode.ExecuteHeightMode;
        entity.TakeOffSecurityHeight = mission.TakeOffSecurityHeight;
        entity.GlobalRTHHeight = mission.GlobalRTHHeight ?? 100;
        entity.FinishAction = mission.FinishAction;
        entity.GlobalHeight = folder.GlobalHeight is > 0 ? folder.GlobalHeight.Value : folder.Placemarks.FirstOrDefault()?.ExecuteHeight ?? 0;
        entity.PointCount = folder.Placemarks.Count;
        entity.Distance = Math.Round(distance, 6);
        entity.Duration = Math.Round(duration, 6);
        entity.WaylineParamJson = JSON.Serialize(request);

        var bytes = BuildKmzBytes(request, device, GetCurrentUserName());
        entity.Sign = ComputeMd5(bytes);

        var file = await UploadKmzAsync(bytes, entity.WaylineName);
        entity.KmzFileId = file.Id;
        entity.KmzFileName = $"{entity.WaylineName}.kmz";
        entity.KmzFilePath = file.FilePath;
        entity.KmzFileUrl = file.Url;
    }

    /// <summary>
    /// 计算 KMZ 内容 MD5（小写十六进制）。
    /// </summary>
    /// <remarks>
    /// 该值即协议 <c>flighttask_prepare.file.fingerprint</c>，机场据此校验下载到的 KMZ 是否完整。
    /// 与 <c>SysFile.FileMd5</c> 的区别：后者由上传组件按存储实现计算（部分 OSS 返回 Base64），
    /// 不一定符合协议要求，因此这里按协议规范统一算一次十六进制值。
    /// </remarks>
    private static string ComputeMd5(byte[] bytes)
    {
        var hash = MD5.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static byte[] BuildKmzBytes(CreateWaypointWaylineRequest request, DeviceInfo device, string author)
    {
        var folder = request.Folder ?? new Folder();
        var (distance, duration) = WaylineKmzBuilder.CalculateDistanceAndDuration(folder);
        var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        var template = WaylineKmzBuilder.BuildTemplate(request, device.DroneInfo, device.PayloadInfo, author, timestamp);
        var waylines = WaylineKmzBuilder.BuildWaylines(request, device.DroneInfo, device.PayloadInfo, distance, duration);

        return WaylineKmzBuilder.BuildKmz(
            WaylineKmzBuilder.SerializeTemplate(template),
            WaylineKmzBuilder.SerializeWaylines(waylines));
    }

    /// <summary>上传 KMZ 到文件服务</summary>
    private static async Task<FileOutput> UploadKmzAsync(byte[] bytes, string waylineName)
    {
        var fileName = $"{SanitizeFileName(waylineName)}.kmz";
        using var stream = new MemoryStream(bytes);
        // 大疆 KMZ 无标准 MIME 登记，统一使用 application/octet-stream（与上传白名单一致）
        var formFile = new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream",
        };
        return await App.GetService<SysFileService>().UploadFile(formFile, KmzUploadPath);
    }

    private CreateWaypointWaylineRequest? DeserializeParam(DjiWaylineEntity entity)
    {
        return string.IsNullOrWhiteSpace(entity.WaylineParamJson)
            ? null
            : JSON.Deserialize<CreateWaypointWaylineRequest>(entity.WaylineParamJson);
    }

    private async Task<string> GetWorkspaceNickNameAsync(string workspaceId)
    {
        if (string.IsNullOrWhiteSpace(workspaceId)) return null;
        return await _rep.Context.Queryable<DjiWorkspace>()
            .Where(m => m.WorkspaceId == workspaceId)
            .Select(m => m.WorkspaceNickName)
            .FirstAsync();
    }

    private static string GetCurrentUserName()
    {
        var name = App.User?.FindFirst(ClaimConst.RealName)?.Value
                   ?? App.User?.FindFirst(ClaimConst.Account)?.Value;
        return string.IsNullOrWhiteSpace(name) ? "航线规划平台" : name;
    }

    /// <summary>解析空间：未指定时使用当前用户的默认空间</summary>
    private async Task<string> ResolveWorkspaceIdAsync(string workspaceId)
    {
        if (!string.IsNullOrWhiteSpace(workspaceId)) return workspaceId.Trim();

        var spaces = await _workspaceUserRep.GetListAsync(m => m.UserId == _userManager.UserId);
        var current = spaces.FirstOrDefault(m => m.IsDefault) ?? spaces.FirstOrDefault();
        if (current == null) throw Oops.Oh("当前用户未分配工作空间，请先在“工作空间用户”中配置后再创建航线");
        return current.WorkspaceId;
    }

    /// <summary>同一空间内航线名称唯一（软删除数据不参与校验）</summary>
    private async Task EnsureNameUniqueAsync(string workspaceId, string waylineName, long? excludeId)
    {
        var query = _rep.AsQueryable().Where(m => m.WorkspaceId == workspaceId && m.WaylineName == waylineName);
        if (excludeId is > 0) query = query.Where(m => m.Id != excludeId!.Value);
        if (await query.AnyAsync()) throw Oops.Oh($"同一空间下已存在名为“{waylineName}”的航线");
    }

    /// <summary>生成复制后的航线名称：未指定时自动追加“-副本”序号</summary>
    private async Task<string> BuildCopyNameAsync(string workspaceId, string newName, string sourceName)
    {
        if (!string.IsNullOrWhiteSpace(newName))
        {
            var target = newName.Trim();
            await EnsureNameUniqueAsync(workspaceId, target, null);
            return target;
        }

        for (var i = 1; i <= 100; i++)
        {
            var suffix = i == 1 ? "-副本" : $"-副本{i}";
            var candidate = $"{sourceName}{suffix}";
            var exists = await _rep.AsQueryable().AnyAsync(m => m.WorkspaceId == workspaceId && m.WaylineName == candidate);
            if (!exists) return candidate;
        }

        throw Oops.Oh("副本数量已达上限，请手动指定新的航线名称");
    }

    /// <summary>
    /// 解析机型/负载枚举信息
    /// </summary>
    /// <remarks>
    /// 前端提交的 <c>domainTypeSubType</c> 形如 <c>0-100-1</c>（Domain-Type-SubType）。
    /// 负载枚举取自相机枚举的 <c>TsgIndex</c>（type-subtype-gimbalindex）。
    /// </remarks>
    private async Task<DeviceInfo> ResolveDeviceInfoAsync(string domainTypeSubType)
    {
        var deviceEnum = _deviceEnumService.GetDeviceEnum(domainTypeSubType)
                         ?? throw Oops.Oh($"未找到设备机型枚举配置：{domainTypeSubType}，请先在“设备枚举”中维护");

        var cameraEnum = await ResolveCameraEnumAsync(deviceEnum);
        var segments = (cameraEnum.TsgIndex ?? string.Empty).Split('-', StringSplitOptions.RemoveEmptyEntries);

        return new DeviceInfo
        {
            DeviceEnum = deviceEnum,
            CameraEnum = cameraEnum,
            DroneInfo = new DroneInfo
            {
                DroneEnumValue = deviceEnum.Type,
                DroneSubEnumValue = deviceEnum.SubType,
            },
            PayloadInfo = new PayloadInfo
            {
                PayloadEnumValue = ParseSegment(segments, 0),
                PayloadSubEnumValue = ParseSegment(segments, 1),
                PayloadPositionIndex = ParseSegment(segments, 2),
            },
        };
    }

    /// <summary>
    /// 获取机型对应的负载枚举
    /// </summary>
    /// <remarks>
    /// 优先使用已维护的绑定关系；未绑定时按“机型名 ↔ 相机名”的命名约定匹配主云台相机，
    /// 匹配成功后自动回填绑定关系，避免每次都要人工维护。
    /// </remarks>
    private async Task<DjiDeviceCameraEnum> ResolveCameraEnumAsync(DjiDeviceEnumOutput deviceEnum)
    {
        var bound = await _cameraRep.GetFirstAsync(m => m.DeviceEnumId == deviceEnum.Id);
        if (bound != null) return bound;

        var tokens = BuildMatchTokens(deviceEnum.Name);
        var candidates = await _cameraRep.GetListAsync(m => m.IsMainGimbal == true && m.TsgIndex != null);
        var matched = candidates
            .Select(m => new { Camera = m, Key = NormalizeCameraName(m.Name) })
            .Where(x => !string.IsNullOrEmpty(x.Key))
            .Select(x => new
            {
                x.Camera,
                Token = tokens.Where(t => x.Key.EndsWith(t, StringComparison.Ordinal))
                              .OrderByDescending(t => t.Length)
                              .FirstOrDefault(),
            })
            .Where(x => x.Token != null)
            // 匹配到的型号标记越长越精确，优先采用
            .OrderByDescending(x => x.Token.Length)
            .Select(x => x.Camera)
            .FirstOrDefault();

        if (matched == null)
            throw Oops.Oh($"机型【{deviceEnum.Name}】尚未绑定相机枚举，请在“相机枚举”中为该机型配置主云台相机后重试");

        // 自动回填绑定关系
        await _cameraRep.AsUpdateable(new DjiDeviceCameraEnum { Id = matched.Id, DeviceEnumId = deviceEnum.Id })
            .UpdateColumns(m => m.DeviceEnumId)
            .ExecuteCommandAsync();

        return matched;
    }

    /// <summary>从机型名称提取用于匹配相机的候选型号标记</summary>
    private static List<string> BuildMatchTokens(string deviceName)
    {
        var tokens = new List<string>();
        var name = deviceName ?? string.Empty;

        // 括号内的型号标记，如“Mavic 3 行业系列（M3E 相机）”→“M3E”
        var bracket = Regex.Match(name, @"[（(]([^）)]+)[）)]");
        if (bracket.Success) AddToken(tokens, bracket.Groups[1].Value, allowStripModelPrefix: true);

        // 名称最后一个词，如“Matrice 3TD”→“3TD”
        var words = name.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 0) AddToken(tokens, words[^1], allowStripModelPrefix: true);

        AddToken(tokens, name, allowStripModelPrefix: false);
        return tokens;
    }

    private static void AddToken(List<string> tokens, string raw, bool allowStripModelPrefix)
    {
        var normalized = NormalizeAlphanumeric(raw);
        if (normalized.Length < 2) return;

        if (!tokens.Contains(normalized)) tokens.Add(normalized);

        // DJI 相机型号常省略 M 前缀（M3E ↔ 3E），补充无前缀别名
        if (allowStripModelPrefix && normalized.StartsWith('m') && normalized.Length > 2)
        {
            var trimmed = normalized[1..];
            if (!tokens.Contains(trimmed)) tokens.Add(trimmed);
        }
    }

    /// <summary>相机名称归一化：去掉“Camera/相机”后缀并只保留字母数字</summary>
    private static string NormalizeCameraName(string cameraName)
    {
        var normalized = NormalizeAlphanumeric(cameraName);
        foreach (var suffix in new[] { "camera", "相机" })
        {
            if (normalized.Length > suffix.Length && normalized.EndsWith(suffix, StringComparison.Ordinal))
            {
                normalized = normalized[..^suffix.Length];
                break;
            }
        }
        return normalized;
    }

    private static string NormalizeAlphanumeric(string value)
    {
        var builder = new StringBuilder();
        foreach (var ch in (value ?? string.Empty).ToLowerInvariant())
            if (ch is >= 'a' and <= 'z' or >= '0' and <= '9')
                builder.Append(ch);
        return builder.ToString();
    }

    private static int ParseSegment(string[] segments, int index)
    {
        if (segments.Length <= index) return 0;
        return int.TryParse(segments[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }

    private static void ValidateRequest(CreateWaypointWaylineRequest request)
    {
        if (request == null) throw Oops.Oh("请求参数不能为空");
        if (string.IsNullOrWhiteSpace(request.WaylineName)) throw Oops.Oh("航线名称不能为空");
        if (request.WaylineName.Trim().Length > 64) throw Oops.Oh("航线名称长度不能超过 64 个字符");
        if (string.IsNullOrWhiteSpace(request.DomainTypeSubType)) throw Oops.Oh("请先选择飞行器");
        if (request.Folder?.Placemarks is not { Count: > 0 }) throw Oops.Oh("请至少添加一个航点");

        if (request.Folder.AutoFlightSpeed is < 1 or > 15) throw Oops.Oh("全局航线速度取值范围为 [1, 15] m/s");

        for (var i = 0; i < request.Folder.Placemarks.Count; i++)
        {
            var parts = (request.Folder.Placemarks[i].Point ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2
                || !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var lon)
                || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat))
                throw Oops.Oh($"第 {i + 1} 个航点坐标格式不正确，应为“经度,纬度[,高度]”");

            if (lon is < -180 or > 180 || lat is < -90 or > 90)
                throw Oops.Oh($"第 {i + 1} 个航点坐标超出有效范围");
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder();
        foreach (var ch in name ?? string.Empty)
            builder.Append(invalid.Contains(ch) ? '_' : ch);
        var result = builder.ToString().Trim();
        return string.IsNullOrWhiteSpace(result) ? "wayline" : result;
    }

    #endregion

    /// <summary>机型与负载枚举解析结果</summary>
    private sealed class DeviceInfo
    {
        public DjiDeviceEnumOutput DeviceEnum { get; set; }

        public DjiDeviceCameraEnum CameraEnum { get; set; }

        public DroneInfo DroneInfo { get; set; }

        public PayloadInfo PayloadInfo { get; set; }
    }
}
