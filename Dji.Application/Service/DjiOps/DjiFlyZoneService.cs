// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using System.Linq;
using Dji.Application.Service.DjiOps.Dto;
using Dji.Core.Enum.DjiEnum.Ops;
using Newtonsoft.Json.Linq;

namespace Dji.Application.Service.DjiOps;

/// <summary>
/// 飞行区 / 禁飞区服务（运营端在地图上自绘并保存的空域）。
/// </summary>
/// <remarks>
/// <para>
/// <b>本服务只负责「定义」空域，不负责下发</b>。
/// 下发走的是另一条成熟链路：<see cref="DjiFlightAreaService"/>（把文件登记到某台机场并触发设备拉取）。
/// 二者通过一次 <c>Export</c> 衔接 —— 这里产出符合大疆协议的文件内容，
/// 前端拿着内容去调上传接口，拿到对象 Key 后再去 <c>DjiFlightAreaService</c> 登记。
/// 这样切分是为了让两条链路各自独立演进：空域编辑随时改，下发受设备状态与链路制约。
/// </para>
/// <para>
/// 导出格式严格遵循大疆的自定义飞行区协议（GeoJSON FeatureCollection）：
/// <list type="bullet">
/// <item><c>geofence_type</c>：<c>dfence</c>（作业区，圈内可飞）/ <c>nfz</c>（禁飞区，圈外可飞）</item>
/// <item>圆形没有 GeoJSON 原生类型，用 <c>Point</c> + <c>properties.radius</c> 表达</item>
/// <item><c>enable</c> 当前固件不解析，但协议要求存在且为 <c>true</c></item>
/// </list>
/// </para>
/// </remarks>
[ApiDescriptionSettings(ApplicationConst.GroupName, Order = 171)]
public class DjiFlyZoneService(
    SqlSugarRepository<DjiFlyZone> zoneRep,
    SqlSugarRepository<DjiWorkspaceUser> workspaceUserRep,
    UserManager userManager,
    ILogger<DjiFlyZoneService> logger) : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<DjiFlyZone> _zoneRep = zoneRep;
    private readonly SqlSugarRepository<DjiWorkspaceUser> _workspaceUserRep = workspaceUserRep;
    private readonly UserManager _userManager = userManager;
    private readonly ILogger<DjiFlyZoneService> _logger = logger;

    #region 查询

    /// <summary>飞行区分页</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Page")]
    public async Task<SqlSugarPagedList<FlyZoneOutput>> Page(FlyZoneSearchInput input)
    {
        input ??= new FlyZoneSearchInput();

        var query = _zoneRep.AsQueryable()
            .WhereIF(!input.WorkspaceId.IsNullOrWhiteSpace(), m => m.WorkspaceId == input.WorkspaceId)
            .WhereIF(input.ZoneType.HasValue, m => m.ZoneType == input.ZoneType)
            .WhereIF(input.Shape.HasValue, m => m.Shape == input.Shape)
            .WhereIF(input.IsEnabled.HasValue, m => m.IsEnabled == input.IsEnabled.Value)
            .WhereIF(!input.Keyword.IsNullOrWhiteSpace(), m => m.Name != null && m.Name.Contains(input.Keyword.Trim()))
            .OrderBy(m => m.Sort).OrderBy(m => m.CreateTime, OrderByType.Desc);

        var paged = await query.Select(m => new FlyZoneOutput
        {
            Id = m.Id,
            WorkspaceId = m.WorkspaceId,
            Name = m.Name,
            ZoneType = m.ZoneType,
            Shape = m.Shape,
            Geometry = m.Geometry,
            Radius = m.Radius,
            MinHeight = m.MinHeight,
            MaxHeight = m.MaxHeight,
            Color = m.Color,
            IsEnabled = m.IsEnabled,
            Sort = m.Sort,
            Remark = m.Remark,
            CreateTime = m.CreateTime
        }).ToPagedListAsync(input.Page, input.PageSize);

        // 面积 / 周长 / 顶点数要解析几何后才能算，只能在 SQL 之外补
        foreach (var row in paged.Items) Decorate(row);

        return paged;
    }

    /// <summary>
    /// 不分页列表。
    /// </summary>
    /// <remarks>
    /// 地图页用它：一次性拿到某空间下全部区域直接渲染 ——
    /// 空域数量是可预期的（几十到几百），分页只会带来糟糕的地图交互体验。
    /// </remarks>
    [HttpGet]
    [ApiDescriptionSettings(Name = "List")]
    public async Task<List<FlyZoneOutput>> List([FromQuery] string workspaceId)
    {
        var list = await _zoneRep.AsQueryable()
            .WhereIF(!workspaceId.IsNullOrWhiteSpace(), m => m.WorkspaceId == workspaceId)
            .OrderBy(m => m.Sort).OrderBy(m => m.CreateTime)
            .Select(m => new FlyZoneOutput
            {
                Id = m.Id,
                WorkspaceId = m.WorkspaceId,
                Name = m.Name,
                ZoneType = m.ZoneType,
                Shape = m.Shape,
                Geometry = m.Geometry,
                Radius = m.Radius,
                MinHeight = m.MinHeight,
                MaxHeight = m.MaxHeight,
                Color = m.Color,
                IsEnabled = m.IsEnabled,
                Sort = m.Sort,
                Remark = m.Remark,
                CreateTime = m.CreateTime
            }).ToListAsync();

        list.ForEach(Decorate);
        return list;
    }

    /// <summary>区域详情</summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "Detail")]
    public async Task<FlyZoneOutput> Detail([FromQuery] long id)
    {
        var entity = await _zoneRep.GetByIdAsync(id);
        if (entity == null) throw Oops.Oh("区域不存在或已被删除");

        var output = entity.Adapt<FlyZoneOutput>();
        Decorate(output);
        return output;
    }

    #endregion

    #region 写操作

    /// <summary>新增区域</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Add")]
    public async Task<long> Add(AddFlyZoneInput input)
    {
        ValidateGeometry(input.Shape, input.Radius, input.Geometry);

        var entity = new DjiFlyZone
        {
            // 工作空间是 NOT NULL：运营端的选择器允许为空（=「全部」，便于跨空间查看），
            // 此时退回当前用户的默认空间，而不是把 null 丢给数据库去撞 SQLite 的约束。
            WorkspaceId = await ResolveWorkspaceIdAsync(input.WorkspaceId),
            Name = input.Name.Trim(),
            ZoneType = input.ZoneType,
            Shape = input.Shape,
            Geometry = SerializeGeometry(input.Geometry),
            Radius = input.Shape == FlyZoneShapeEnum.Circle ? input.Radius : 0,
            MinHeight = input.MinHeight,
            MaxHeight = input.MaxHeight,
            Color = input.Color,
            IsEnabled = input.IsEnabled,
            Sort = input.Sort,
            Remark = input.Remark
        };

        var created = await _zoneRep.AsInsertable(entity).ExecuteReturnEntityAsync();
        _logger.LogInformation("新增飞行区 {Name}({Id}) 类型={ZoneType} 形状={Shape}", entity.Name, created.Id, entity.ZoneType, entity.Shape);
        return created.Id;
    }

    /// <summary>修改区域</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Update")]
    public async Task<bool> Update(UpdateFlyZoneInput input)
    {
        var entity = await _zoneRep.GetByIdAsync(input.Id);
        if (entity == null) throw Oops.Oh("区域不存在或已被删除");

        if (input.Geometry != null) ValidateGeometry(input.Shape, input.Radius, input.Geometry);

        entity.Name = input.Name?.Trim() ?? entity.Name;
        entity.ZoneType = input.ZoneType;
        entity.Shape = input.Shape;
        if (input.Geometry != null) entity.Geometry = SerializeGeometry(input.Geometry);
        entity.Radius = input.Shape == FlyZoneShapeEnum.Circle ? input.Radius : 0;
        entity.MinHeight = input.MinHeight;
        entity.MaxHeight = input.MaxHeight;
        entity.Color = input.Color;
        entity.IsEnabled = input.IsEnabled;
        entity.Sort = input.Sort;
        entity.Remark = input.Remark;

        return await _zoneRep.AsUpdateable(entity).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// 批量删除。
    /// </summary>
    /// <remarks>
    /// 软删除（实体实现 <c>IDeletedFilter</c>）：空域一旦误删，
    /// 对应的现场作业限制可能立刻失效，保留记录便于快速找回。
    /// </remarks>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Delete")]
    public async Task<int> Delete(FlyZoneIdsInput input)
    {
        if (input?.Ids == null || input.Ids.Count == 0) throw Oops.Oh("请选择要删除的区域");
        return await _zoneRep.AsDeleteable().Where(m => input.Ids.Contains(m.Id)).ExecuteCommandAsync();
    }

    /// <summary>启停（停用即不参与导出与下发，但保留历史）</summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "SetEnabled")]
    public async Task<bool> SetEnabled(FlyZoneEnableInput input)
    {
        if (input?.Ids == null || input.Ids.Count == 0) throw Oops.Oh("请选择要操作的区域");
        return await _zoneRep.AsUpdateable()
            .SetColumns(m => m.IsEnabled == input.IsEnabled)
            .Where(m => input.Ids.Contains(m.Id))
            .ExecuteCommandAsync() > 0;
    }

    #endregion

    #region 导出

    /// <summary>
    /// 导出符合大疆协议的自定义飞行区文件。
    /// </summary>
    /// <remarks>
    /// 拿到 <c>Content</c> 之后的标准流程：调平台的上传接口 → 拿到对象 Key →
    /// 调 <c>DjiFlightAreaService.Register</c> 登记到目标机场 → 设备异步拉取生效。
    /// 本接口<b>不做上传也不做下发</b>，因为这两步都需要额外上下文（存储目标、目标机场），
    /// 混在一起会让「只想先看看文件内容对不对」这类诉求很难办。
    /// </remarks>
    [HttpGet]
    [ApiDescriptionSettings(Name = "Export")]
    public async Task<FlyZoneExportOutput> Export([FromQuery] string workspaceId)
    {
        var zones = await _zoneRep.AsQueryable()
            .WhereIF(!workspaceId.IsNullOrWhiteSpace(), m => m.WorkspaceId == workspaceId)
            .Where(m => m.IsEnabled)
            .OrderBy(m => m.Sort).OrderBy(m => m.CreateTime)
            .ToListAsync();

        var output = new FlyZoneExportOutput
        {
            FileName = $"geofence_{DateTime.Now:yyyyMMddHHmmss}.json"
        };

        var features = new JArray();

        foreach (var zone in zones)
        {
            // 区域在文件里的 id 必须全局唯一：「类型前缀 + 主键」天然不重复，且肉眼可辨识类型
            var typeCode = zone.ZoneType == FlyZoneTypeEnum.NoFlyZone ? "nfz" : "dfence";
            var areaId = $"{typeCode}-{zone.Id}";

            var validation = ValidateSingle(zone, areaId);
            output.Validations.Add(validation);
            if (!validation.Valid) continue;

            features.Add(BuildFeature(zone, areaId, typeCode));
            output.Count++;
        }

        var root = new JObject
        {
            ["type"] = "FeatureCollection",
            ["features"] = features
        };

        output.Content = root.ToString(Formatting.Indented);
        return output;
    }

    #endregion

    #region 内部辅助

    /// <summary>
    /// 解析工作空间：未指定时使用当前用户的默认空间。
    /// </summary>
    /// <remarks>
    /// 与 <c>DjiWaylineService</c> 保持同一套口径。这里必须兜底的原因很直接：
    /// <c>EntityWorkspaceBase.WorkspaceId</c> 是 <b>NOT NULL</b>，而运营端的空间选择器是「可空 = 全部」，
    /// 直接拿 <c>input.WorkspaceId</c> 落库会在没选空间时抛出
    /// <c>SQLite Error 19: NOT NULL constraint failed</c> —— 一个用户完全看不懂、也无从自行修复的错。
    /// </remarks>
    private async Task<string> ResolveWorkspaceIdAsync(string workspaceId)
    {
        if (!string.IsNullOrWhiteSpace(workspaceId)) return workspaceId.Trim();

        var spaces = await _workspaceUserRep.GetListAsync(m => m.UserId == _userManager.UserId);
        var current = spaces.FirstOrDefault(m => m.IsDefault) ?? spaces.FirstOrDefault();
        if (current == null) throw Oops.Oh("当前用户未分配工作空间，请先在“工作空间用户”中配置，或在页面上选择一个空间后再保存");
        return current.WorkspaceId;
    }

    /// <summary>
    /// 把扁平的点列表还原成 GeoJSON 的 <c>coordinates</c> 结构后落库。
    /// </summary>
    /// <remarks>
    /// 入参刻意用扁平的「点列表」而不是 GeoJSON 原生的多层数组：
    /// 多边形是 <c>[[p],[p]]</c>、圆心是 <c>[[p]]</c>，同一份 DTO 就能表达两种形状，
    /// 前端不必为了画圆而对 JS 数组多套一层。这里再按协议还原回来。
    /// </remarks>
    private static string SerializeGeometry(FlyZoneGeometryInput geometry)
    {
        if (geometry == null) return null;

        object coordinates;
        if (string.Equals(geometry.Type, "Point", StringComparison.OrdinalIgnoreCase))
        {
            var center = geometry.Coordinates?.FirstOrDefault();
            coordinates = center != null && center.Count >= 2
                ? new JArray(center[0], center[1])
                : new JArray(0, 0);
        }
        else
        {
            var ring = new JArray();
            foreach (var point in geometry.Coordinates ?? [])
            {
                if (point.Count < 2) continue;
                ring.Add(new JArray(point[0], point[1]));
            }
            coordinates = new JArray(ring);
        }

        return JsonConvert.SerializeObject(new { type = geometry.Type, coordinates });
    }

    /// <summary>
    /// 构造 Feature。
    /// </summary>
    /// <remarks>
    /// 圆的表达是 <c>Point</c> + <c>properties.subType="Circle"</c> + <c>radius</c>；
    /// 多边形则是 <c>Polygon</c> + <c>radius: 0</c>（协议要求字段存在）。
    /// 多边形<b>必须闭合</b>：首尾顶点坐标要一致。这里选择补齐而不是报错 ——
    /// 多数前端绘制工具产出的环并不闭合，而补齐是无损且唯一正确的处理。
    /// </remarks>
    private static JObject BuildFeature(DjiFlyZone zone, string areaId, string typeCode)
    {
        var geometry = JObject.Parse(zone.Geometry ?? "{\"type\":\"Polygon\",\"coordinates\":[]}");
        var isCircle = zone.Shape == FlyZoneShapeEnum.Circle;

        var props = new JObject { ["enable"] = true };
        if (isCircle)
        {
            props["subType"] = "Circle";
            props["radius"] = Math.Round(zone.Radius, 2);
        }
        else
        {
            props["radius"] = 0;
            geometry = EnsureClosedPolygon(geometry);
        }

        return new JObject
        {
            ["id"] = areaId,
            ["geofence_type"] = typeCode,
            ["type"] = "Feature",
            ["properties"] = props,
            ["geometry"] = geometry
        };
    }

    /// <summary>把 Polygon 的坐标环补齐成首尾相同，并把坐标精度统一到 7 位小数（约 1.1cm，足够且能显著缩小文件体积）</summary>
    private static JObject EnsureClosedPolygon(JObject geometry)
    {
        var coords = geometry["coordinates"] as JArray;
        var ring = coords?.First as JArray;
        if (ring == null || ring.Count < 3) return geometry;

        var points = new JArray();
        foreach (var p in ring)
        {
            var arr = p as JArray;
            if (arr == null || arr.Count < 2) continue;
            points.Add(new JArray(Math.Round((double)arr[0], 7), Math.Round((double)arr[1], 7)));
        }
        if (points.Count < 3) return geometry;

        var first = (JArray)points.First;
        var last = (JArray)points.Last;
        if (Math.Abs((double)first[0] - (double)last[0]) > 1e-9 || Math.Abs((double)first[1] - (double)last[1]) > 1e-9)
            points.Add(new JArray((double)first[0], (double)first[1]));

        return new JObject { ["type"] = "Polygon", ["coordinates"] = new JArray(points) };
    }

    /// <summary>
    /// 单区域的协议合规性校验。
    /// </summary>
    /// <remarks>
    /// 这几条都是大疆设备的<b>硬约束</b>，任一不满足都会让整份文件被拒：
    /// 多边形顶点上限 255、圆形半径下限 10 米、坐标必须有有效值。
    /// 「多个作业区不得重叠」「机场须在作业区内 / 禁飞区外且距边界 ≥ 10m」是跨区域关系，
    /// 更适合前端在绘制时即时提示 —— 等到导出才发现重叠，用户已经白画了。
    /// </remarks>
    private static FlyZoneValidateItem ValidateSingle(DjiFlyZone zone, string areaId)
    {
        var item = new FlyZoneValidateItem { ZoneId = zone.Id, ZoneName = zone.Name, AreaId = areaId, Valid = true };

        if (zone.Shape == FlyZoneShapeEnum.Circle && zone.Radius < 10)
        {
            item.Valid = false;
            item.Message = $"圆形半径 {zone.Radius:0.##}m 小于协议最小值 10m";
            return item;
        }

        var metrics = ComputeMetrics(zone.Geometry, zone.Shape, zone.Radius);
        if (metrics.VertexCount > 255)
        {
            item.Valid = false;
            item.Message = $"多边形顶点数 {metrics.VertexCount} 超过协议上限 255";
            return item;
        }
        if (zone.Shape == FlyZoneShapeEnum.Polygon && metrics.VertexCount < 3)
        {
            item.Valid = false;
            item.Message = "多边形至少需要 3 个顶点";
        }

        return item;
    }

    /// <summary>写入前的入参校验</summary>
    private static void ValidateGeometry(FlyZoneShapeEnum shape, double radius, FlyZoneGeometryInput geometry)
    {
        if (geometry == null) throw Oops.Oh("几何数据不能为空");
        if (string.IsNullOrWhiteSpace(geometry.Type)) throw Oops.Oh("几何类型不能为空");

        var expected = shape == FlyZoneShapeEnum.Circle ? "Point" : "Polygon";
        if (!string.Equals(geometry.Type, expected, StringComparison.OrdinalIgnoreCase))
            throw Oops.Oh($"形状为{(shape == FlyZoneShapeEnum.Circle ? "圆形" : "多边形")}时，几何类型应为 {expected}，实际为 {geometry.Type}");

        if (geometry.Coordinates == null || geometry.Coordinates.Count == 0) throw Oops.Oh("坐标不能为空");

        if (shape == FlyZoneShapeEnum.Circle)
        {
            if (radius < 10) throw Oops.Oh("圆形半径不得小于 10 米（大疆协议硬约束）");
            var center = geometry.Coordinates.FirstOrDefault();
            if (center == null || center.Count < 2) throw Oops.Oh("圆心坐标不完整");
            CheckRange(center[0], center[1]);
            return;
        }

        var points = geometry.Coordinates;
        if (points == null || points.Count < 3) throw Oops.Oh("多边形至少需要 3 个顶点");
        if (points.Count > 255) throw Oops.Oh($"多边形顶点数不得超过 255，当前 {points.Count}");
        foreach (var point in points)
        {
            if (point.Count < 2) throw Oops.Oh("坐标点需包含经度与纬度");
            CheckRange(point[0], point[1]);
        }
    }

    private static void CheckRange(double lon, double lat)
    {
        if (lon < -180 || lon > 180 || lat < -90 || lat > 90)
            throw Oops.Oh($"坐标超出合法范围：经度 {lon}, 纬度 {lat}");
    }

    /// <summary>补齐名称向量与量算结果</summary>
    private static void Decorate(FlyZoneOutput row)
    {
        row.ZoneTypeName = DescribeZoneType(row.ZoneType);
        row.ShapeName = DescribeShape(row.Shape);
        var m = ComputeMetrics(row.Geometry, row.Shape, row.Radius);
        row.AreaSquareMeters = m.Area;
        row.PerimeterMeters = m.Perimeter;
        row.VertexCount = m.VertexCount;
    }

    // 枚举描述用显式分支而不是 GetDescription()：项目同时引入了 Dji.Core.EnumExtension 与
    // NewLife.EnumHelper，同名扩展方法会造成编译二义性。
    private static string DescribeZoneType(FlyZoneTypeEnum t) => t == FlyZoneTypeEnum.NoFlyZone ? "禁飞区" : "作业区";
    private static string DescribeShape(FlyZoneShapeEnum s) => s == FlyZoneShapeEnum.Circle ? "圆形" : "多边形";

    private static (double Area, double Perimeter, int VertexCount) ComputeMetrics(
        string geometryJson, FlyZoneShapeEnum shape, double radius)
    {
        if (shape == FlyZoneShapeEnum.Circle)
        {
            if (radius <= 0) return (0, 0, 0);
            return (Math.PI * radius * radius, 2 * Math.PI * radius, 0);
        }

        var points = ParseRing(geometryJson);
        if (points.Count < 3) return (0, 0, points.Count);

        // 球面多边形面积（球冠多边形公式）：R²·Σ[(λ₂-λ₁)·(2+sinφ₁+sinφ₂)]/2
        double total = 0, perimeter = 0;
        for (int i = 0; i < points.Count; i++)
        {
            var a = points[i];
            var b = points[(i + 1) % points.Count];
            total += ToRad(b.lon - a.lon) * (2 + Math.Sin(ToRad(a.lat)) + Math.Sin(ToRad(b.lat)));
            perimeter += Haversine(a.lon, a.lat, b.lon, b.lat);
        }

        const double earthRadius = 6371008.8;
        var areaSq = Math.Abs(total * earthRadius * earthRadius / 2);

        // 闭合环的首尾重复点不应计入顶点数
        var count = points.Count;
        var first = points[0];
        var last = points[^1];
        if (count > 1 && Math.Abs(first.lon - last.lon) < 1e-9 && Math.Abs(first.lat - last.lat) < 1e-9)
            count -= 1;

        return (areaSq, perimeter, count);
    }

    private static List<(double lon, double lat)> ParseRing(string geometryJson)
    {
        var result = new List<(double, double)>();
        if (string.IsNullOrWhiteSpace(geometryJson)) return result;

        try
        {
            var coords = JObject.Parse(geometryJson)["coordinates"] as JArray;
            var ring = coords?.First as JArray;
            if (ring == null) return result;

            foreach (var p in ring)
            {
                var arr = p as JArray;
                if (arr == null || arr.Count < 2) continue;
                result.Add(((double)arr[0], (double)arr[1]));
            }
        }
        catch
        {
            // 几何串不合法时忽略：列表页仍应可见，只是量算为 0
        }
        return result;
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;

    /// <summary>两点球面距离（米）</summary>
    private static double Haversine(double lon1, double lat1, double lon2, double lat2)
    {
        const double r = 6371008.8;
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return 2 * r * Math.Asin(Math.Sqrt(a));
    }

    #endregion
}
