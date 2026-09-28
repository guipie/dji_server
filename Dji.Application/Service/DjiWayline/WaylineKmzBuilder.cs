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
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Dji.Application.Service.DjiWayline.Dto;
using Dji.Application.Service.DjiWayline.Dto.Temp;
using Dji.Core.Enum.DjiEnum.Wayline;
using Newtonsoft.Json.Linq;

namespace Dji.Application.Service.DjiWayline;

/// <summary>
/// 航线 KMZ 构建器
/// </summary>
/// <remarks>
/// 负责把前端提交的航线参数转换为大疆机场可识别的 KMZ 文件：
/// <list type="number">
/// <item>生成 <c>wpmz/template.kml</c>（航线模板，供编辑器读取）；</item>
/// <item>生成 <c>wpmz/waylines.wpml</c>（可执行航线，供飞行器执行）；</item>
/// <item>打包为 zip（KMZ）。</item>
/// </list>
/// 结构与字段顺序严格对齐大疆官方导出的 KMZ 文件与 WPML 规范。
/// </remarks>
public static class WaylineKmzBuilder
{
    /// <summary>KMZ 内模板文件路径</summary>
    public const string TemplateEntryName = "wpmz/template.kml";

    /// <summary>KMZ 内可执行航线文件路径</summary>
    public const string WaylinesEntryName = "wpmz/waylines.wpml";

    /// <summary>
    /// 模板 ID（template.kml 与 waylines.wpml 内一致）。
    /// </summary>
    /// <remarks>本平台每个 KMZ 只承载一条航线，按官方示例固定为 0。</remarks>
    public const int SingleTemplateId = 0;

    /// <summary>
    /// 航线 ID（waylines.wpml 内）。
    /// </summary>
    /// <remarks>
    /// 每个 KMZ 仅含一条航线，固定为 0 与官方示例一致；该值会随 <c>flighttask_progress</c> 上报回来，
    /// 断点续飞时的 <c>break_point.wayline_id</c> 必须与此一致。
    /// </remarks>
    public const int SingleWaylineId = 0;

    private static readonly XmlSerializer TemplateSerializer = new(typeof(WaylineTemplateKml));
    private static readonly XmlSerializer WaylinesSerializer = new(typeof(WaylinesWpml));

    /// <summary>支持“航线绕行”（autoRerouteInfo）的机型主类型枚举值：M3D / M4D / M4E 系列</summary>
    private static readonly HashSet<int> AutoRerouteDroneTypes = [91, 100, 99];

    #region 对外入口

    /// <summary>
    /// 构建 template.kml 模型
    /// </summary>
    /// <param name="request">航线参数</param>
    /// <param name="droneInfo">飞行器机型信息</param>
    /// <param name="payloadInfo">负载机型信息</param>
    /// <param name="author">作者</param>
    /// <param name="timestamp">时间戳（毫秒）</param>
    public static WaylineTemplateKml BuildTemplate(CreateWaypointWaylineRequest request, DroneInfo droneInfo, PayloadInfo payloadInfo, string author, long timestamp)
    {
        var folder = request.Folder ?? new Folder();
        var mission = request.MissionConfig ?? new MissionConfig();
        var heightMode = ResolveHeightMode(request.Ext?.WaylinePointHeightMode);

        var model = new WaylineTemplateKml
        {
            Document = new TemplateDocument
            {
                Author = author,
                CreateTime = timestamp,
                UpdateTime = timestamp,
                MissionConfig = BuildMissionConfig(mission, droneInfo, payloadInfo, request.Ext?.HomeCoordinate, includeTakeOffRefPoint: true),
                Folder = new TemplateFolder
                {
                    TemplateType = ResolveTemplateType(request),
                    TemplateId = SingleTemplateId,
                    AutoFlightSpeed = folder.AutoFlightSpeed,
                    GlobalWaypointTurnMode = folder.GlobalWaypointTurnMode,
                    GlobalUseStraightLine = 1,
                    GimbalPitchMode = folder.GimbalPitchMode,
                    GlobalHeight = ResolveGlobalHeight(folder),
                    PayloadParam = new TemplatePayloadParam
                    {
                        ImageFormat = JoinImageFormat(folder.PayloadParam),
                        ScanningMode = string.IsNullOrWhiteSpace(folder.PayloadParam?.ScanningMode) ? "repetitive" : folder.PayloadParam.ScanningMode,
                        PayloadPositionIndex = folder.PayloadParam?.PayloadPositionIndex ?? payloadInfo.PayloadPositionIndex,
                    },
                    GlobalWaypointHeadingParam = BuildGlobalHeadingParam(folder.GlobalWaypointHeadingParam),
                    WaylineCoordinateSysParam = new WaylineCoordinateSysParam
                    {
                        CoordinateMode = "WGS84",
                        HeightMode = heightMode.TemplateHeightMode,
                    },
                    Placemarks = [],
                },
            },
        };

        var index = 0;
        var actionGroupId = 0;
        foreach (var placemark in folder.Placemarks ?? [])
        {
            var placemarkIndex = index++;
            var target = new TemplatePlacemark
            {
                Index = placemarkIndex,
                IsRisky = placemark.IsRisky == true ? 1 : 0,
                UseGlobalHeight = placemark.UseGlobalHeight == false ? 0 : 1,
                EllipsoidHeight = placemark.EllipsoidHeight ?? placemark.ExecuteHeight,
                Height = placemark.ExecuteHeight,
                WaypointSpeed = placemark.UseGlobalSpeed == false ? placemark.FlyToPointSpeed ?? folder.AutoFlightSpeed : folder.AutoFlightSpeed,
                UseGlobalSpeed = placemark.UseGlobalSpeed == false ? 0 : 1,
                UseGlobalHeadingParam = placemark.UseGlobalHeadingParam ? 1 : 0,
                UseGlobalTurnParam = placemark.UseGlobalTurnParam ? 1 : 0,
                GimbalPitchAngle = placemark.GimbalPitchAngle ?? 0,
                UseStraightLine = placemark.UseStraightLine == false ? 0 : 1,
                Point = PointCoordinates.From(placemark.Point),
                ActionGroups = BuildActionGroups(placemark, placemarkIndex, ref actionGroupId),
            };

            // 使用全局参数时不输出局部参数，避免与全局定义冲突
            if (!placemark.UseGlobalHeadingParam)
                target.WaypointHeadingParam = BuildHeadingParam(placemark.WaypointHeadingParam, folder.GlobalWaypointHeadingParam);

            if (!placemark.UseGlobalTurnParam)
                target.WaypointTurnParam = BuildTurnParam(placemark.WaypointTurnParam, folder.GlobalWaypointTurnMode);

            model.Document.Folder.Placemarks.Add(target);
        }

        return model;
    }

    /// <summary>
    /// 构建 waylines.wpml 模型
    /// </summary>
    public static WaylinesWpml BuildWaylines(CreateWaypointWaylineRequest request, DroneInfo droneInfo, PayloadInfo payloadInfo, double distance, double duration)
    {
        var folder = request.Folder ?? new Folder();
        var mission = request.MissionConfig ?? new MissionConfig();
        var heightMode = ResolveHeightMode(request.Ext?.WaylinePointHeightMode);

        var model = new WaylinesWpml
        {
            Document = new WaylinesDocument
            {
                // waylines.wpml 的 missionConfig 不含参考起飞点
                MissionConfig = BuildMissionConfig(mission, droneInfo, payloadInfo, null, includeTakeOffRefPoint: false),
                Folder = new WaylinesFolder
                {
                    TemplateId = SingleTemplateId,
                    AutoFlightSpeed = folder.AutoFlightSpeed,
                    ExecuteHeightMode = heightMode.ExecuteHeightMode,
                    WaylineId = SingleWaylineId,
                    Distance = Math.Round(distance, 6),
                    Duration = Math.Round(duration, 6),
                    Placemarks = [],
                },
            },
        };

        var index = 0;
        var actionGroupId = 0;
        foreach (var placemark in folder.Placemarks ?? [])
        {
            var placemarkIndex = index++;
            model.Document.Folder.Placemarks.Add(new WaylinesPlacemark
            {
                Index = placemarkIndex,
                ExecuteHeight = placemark.ExecuteHeight,
                WaypointSpeed = placemark.UseGlobalSpeed == false ? placemark.FlyToPointSpeed ?? folder.AutoFlightSpeed : folder.AutoFlightSpeed,
                // waylines.wpml 中偏航角/转弯/云台参数始终显式输出，未单独配置时使用全局值
                WaypointHeadingParam = BuildHeadingParam(placemark.WaypointHeadingParam, folder.GlobalWaypointHeadingParam),
                WaypointTurnParam = BuildTurnParam(placemark.WaypointTurnParam, folder.GlobalWaypointTurnMode),
                WaypointGimbalHeadingParam = new WpmlWaypointGimbalHeadingParam
                {
                    WaypointGimbalPitchAngle = placemark.GimbalPitchAngle ?? 0,
                    WaypointGimbalYawAngle = 0,
                },
                UseStraightLine = placemark.UseStraightLine == false ? 0 : 1,
                IsRisky = placemark.IsRisky == true ? 1 : 0,
                WaypointWorkType = placemark.WaypointWorkType ?? 0,
                Point = PointCoordinates.From(placemark.Point),
                ActionGroups = BuildActionGroups(placemark, placemarkIndex, ref actionGroupId),
            });
        }

        return model;
    }

    /// <summary>
    /// 序列化 template.kml
    /// </summary>
    public static string SerializeTemplate(WaylineTemplateKml model) => Serialize(TemplateSerializer, model);

    /// <summary>
    /// 序列化 waylines.wpml
    /// </summary>
    public static string SerializeWaylines(WaylinesWpml model) => Serialize(WaylinesSerializer, model);

    /// <summary>
    /// 打包为 KMZ（zip）。
    /// </summary>
    public static byte[] BuildKmz(string templateXml, string waylinesXml)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            WriteEntry(zip, TemplateEntryName, templateXml);
            WriteEntry(zip, WaylinesEntryName, waylinesXml);
        }
        return stream.ToArray();
    }

    #endregion

    #region 业务映射

    /// <summary>
    /// 模板类型 → WPML templateType 字符串
    /// </summary>
    /// <param name="request">
    /// 航线创建请求，其 <c>TemplateType</c> 与前端 <c>TemplateTypeEnum</c> 一致：
    /// 1=waypoint 2=mapping2d 3=mapping3d 4=mappingStrip
    /// </param>
    public static string ResolveTemplateType(CreateWaypointWaylineRequest request)
    {
        return ResolveWaylineType(request.TemplateType) switch
        {
            WaylineType.Mapping2D => "mapping2d",
            WaylineType.Mapping3D => "mapping3d",
            WaylineType.MappingStrip => "mappingStrip",
            _ => "waypoint",
        };
    }

    /// <summary>前端模板类型（1 起） → 后端航线类别枚举</summary>
    public static WaylineType ResolveWaylineType(int templateType)
    {
        var index = templateType - 1;
        return System.Enum.IsDefined(typeof(WaylineType), index) ? (WaylineType)index : WaylineType.WayPoint;
    }

    /// <summary>
    /// 高度模式映射
    /// </summary>
    /// <param name="mode">前端取值：hb（海拔）/ xdqfd（相对起飞点）/ xddm（相对地面），默认 xdqfd</param>
    public static (string TemplateHeightMode, string ExecuteHeightMode) ResolveHeightMode(string mode)
    {
        return mode switch
        {
            "hb" => ("EGM96", "WGS84"),
            "xddm" => ("aboveGroundLevel", "realTimeFollowSurface"),
            _ => ("relativeToStartPoint", "relativeToStartPoint"),
        };
    }

    /// <summary>
    /// 计算航线总距离（米）与预计时长（秒）
    /// </summary>
    /// <remarks>
    /// 距离按相邻航点间大圆距离累加（不含首尾闭合段）；时长 = 距离 / 全局航线速度。
    /// </remarks>
    public static (double Distance, double Duration) CalculateDistanceAndDuration(Folder folder)
    {
        var placemarks = folder?.Placemarks ?? [];
        var points = placemarks
            .Select(p => ParsePoint(p.Point))
            .Where(p => p.HasValue)
            .Select(p => p!.Value)
            .ToList();

        var distance = 0d;
        for (var i = 1; i < points.Count; i++)
            distance += Haversine(points[i - 1], points[i]);

        var speed = folder?.AutoFlightSpeed ?? 0;
        var duration = speed > 0 ? distance / speed : 0;
        return (distance, duration);
    }

    #endregion

    #region 私有实现

    private static WpmlMissionConfig BuildMissionConfig(MissionConfig mission, DroneInfo droneInfo, PayloadInfo payloadInfo, HomeCoordinate home, bool includeTakeOffRefPoint)
    {
        var config = new WpmlMissionConfig
        {
            FlyToWaylineMode = DefaultIfEmpty(mission.FlyToWaylineMode, "safely"),
            FinishAction = DefaultIfEmpty(mission.FinishAction, "goHome"),
            ExitOnRCLost = DefaultIfEmpty(mission.ExitOnRCLost, "goContinue"),
            ExecuteRCLostAction = DefaultIfEmpty(mission.ExecuteRCLostAction, "goBack"),
            TakeOffSecurityHeight = mission.TakeOffSecurityHeight,
            GlobalTransitionalSpeed = mission.GlobalTransitionalSpeed,
            GlobalRTHHeight = mission.GlobalRTHHeight ?? 100,
            DroneInfo = droneInfo,
            PayloadInfo = payloadInfo,
        };

        if (includeTakeOffRefPoint)
        {
            config.TakeOffRefPoint = FormatTakeOffRefPoint(home, mission.TakeOffRefPointAGLHeight);
            config.TakeOffRefPointAGLHeight = mission.TakeOffRefPointAGLHeight;
        }

        // 航线绕行仅支持 M3D / M4D / M4E 系列机型
        if (mission.AutoRerouteInfoVal && AutoRerouteDroneTypes.Contains(droneInfo.DroneEnumValue))
        {
            config.AutoRerouteInfo = new WpmlAutoRerouteInfo
            {
                MissionAutoRerouteMode = 1,
                TransitionalAutoRerouteMode = 1,
            };
        }

        return config;
    }

    /// <summary>参考起飞点：纬度,经度,高度（椭球高）</summary>
    private static string? FormatTakeOffRefPoint(HomeCoordinate? home, double? fallbackHeight)
    {
        if (home == null) return null;
        var height = home.Height ?? fallbackHeight ?? 0;
        return string.Join(",",
            home.Latitude.ToString("0.000000", CultureInfo.InvariantCulture),
            home.Longitude.ToString("0.000000", CultureInfo.InvariantCulture),
            height.ToString("0.000000", CultureInfo.InvariantCulture));
    }

    private static double ResolveGlobalHeight(Folder folder)
    {
        if (folder.GlobalHeight is > 0) return folder.GlobalHeight.Value;
        var first = (folder.Placemarks ?? []).FirstOrDefault();
        return first?.ExecuteHeight ?? 0;
    }

    private static string? JoinImageFormat(PayloadParam? payloadParam)
    {
        var formats = payloadParam?.ImageFormat?.Where(f => !string.IsNullOrWhiteSpace(f)).ToList();
        return formats is { Count: > 0 } ? string.Join(",", formats) : null;
    }

    private static WpmlWaypointHeadingParam BuildGlobalHeadingParam(GlobalWaypointHeadingParam? source)
    {
        source ??= new GlobalWaypointHeadingParam();
        return new WpmlWaypointHeadingParam
        {
            WaypointHeadingMode = DefaultIfEmpty(source.WaypointHeadingMode, "followWayline"),
            WaypointHeadingAngle = source.WaypointHeadingAngle,
            WaypointPoiPoint = source.WaypointPoiPoint,
            WaypointHeadingPathMode = DefaultIfEmpty(source.WaypointHeadingPathMode, "followBadArc"),
        };
    }

    private static WpmlWaypointHeadingParam BuildHeadingParam(WaypointHeadingParam? source, GlobalWaypointHeadingParam? globalSource)
    {
        if (source == null)
        {
            return new WpmlWaypointHeadingParam
            {
                WaypointHeadingMode = DefaultIfEmpty(globalSource?.WaypointHeadingMode, "followWayline"),
                WaypointHeadingAngle = globalSource?.WaypointHeadingAngle,
                WaypointPoiPoint = globalSource?.WaypointPoiPoint,
                WaypointHeadingPathMode = DefaultIfEmpty(globalSource?.WaypointHeadingPathMode, "followBadArc"),
            };
        }

        return new WpmlWaypointHeadingParam
        {
            WaypointHeadingMode = DefaultIfEmpty(source.WaypointHeadingMode, DefaultIfEmpty(globalSource?.WaypointHeadingMode, "followWayline")),
            WaypointHeadingAngle = source.WaypointHeadingAngle,
            WaypointPoiPoint = source.WaypointPoiPoint,
            WaypointHeadingPathMode = DefaultIfEmpty(source.WaypointHeadingPathMode, DefaultIfEmpty(globalSource?.WaypointHeadingPathMode, "followBadArc")),
        };
    }

    private static WpmlWaypointTurnParam BuildTurnParam(WaypointTurnParam? source, string? globalTurnMode)
    {
        return new WpmlWaypointTurnParam
        {
            WaypointTurnMode = DefaultIfEmpty(source?.WaypointTurnMode, DefaultIfEmpty(globalTurnMode, "toPointAndStopWithDiscontinuityCurvature")),
            WaypointTurnDampingDist = source?.WaypointTurnDampingDist ?? 0,
        };
    }

    /// <summary>
    /// 将前端“一个航点一串动作”的扁平结构，按触发器分组为 WPML 的 actionGroup。
    /// </summary>
    /// <remarks>
    /// 相邻且触发器（类型 + 参数）相同的动作归入同一个动作组，行为与 DJI Pilot 一致。
    /// </remarks>
    private static List<WpmlActionGroup> BuildActionGroups(Placemark placemark, int placemarkIndex, ref int actionGroupId)
    {
        var groups = new List<WpmlActionGroup>();
        var actions = placemark.ActionsGroup ?? [];
        if (actions.Count == 0) return groups;

        var currentTriggerKey = string.Empty;
        var currentGroup = default(WpmlActionGroup);

        foreach (var action in actions)
        {
            if (string.IsNullOrWhiteSpace(action.ActionActuatorFunc)) continue;

            var triggerType = DefaultIfEmpty(action.ActionTrigger?.ActionTriggerType, "reachPoint");
            var triggerParam = action.ActionTrigger?.ActionTriggerParam;
            var triggerKey = $"{triggerType}|{triggerParam?.ToString(CultureInfo.InvariantCulture)}";

            if (currentGroup == null || triggerKey != currentTriggerKey)
            {
                currentGroup = new WpmlActionGroup
                {
                    ActionGroupId = actionGroupId++,
                    ActionGroupStartIndex = placemarkIndex,
                    ActionGroupEndIndex = placemarkIndex,
                    ActionGroupMode = "sequence",
                    ActionTrigger = new WpmlActionTrigger
                    {
                        ActionTriggerType = triggerType,
                        ActionTriggerParam = triggerParam,
                    },
                    Actions = [],
                };
                groups.Add(currentGroup);
                currentTriggerKey = triggerKey;
            }

            var param = new ActionActuatorFuncParam();
            AppendJsonParam(param, action.ActionActuatorFuncParam);

            currentGroup.Actions.Add(new WpmlAction
            {
                ActionId = currentGroup.Actions.Count,
                ActionActuatorFunc = action.ActionActuatorFunc,
                ActionActuatorFuncParam = param.IsEmpty ? null : param,
            });
        }

        return groups;
    }

    /// <summary>
    /// 将前端提交的弱类型动作参数按原始顺序写入 XML 参数节点。
    /// </summary>
    private static void AppendJsonParam(ActionActuatorFuncParam target, JObject? source)
    {
        if (source == null) return;
        foreach (var property in source.Properties())
        {
            var value = ConvertToken(property.Value);
            if (value != null) target.Add(property.Name, value);
        }
    }

    /// <summary>
    /// 动作参数值转换：布尔按大疆规范输出 0/1，浮点至少保留一位小数，数组以英文逗号连接，其余原样输出。
    /// </summary>
    private static string? ConvertToken(JToken? token)
    {
        return token switch
        {
            null => null,
            { Type: JTokenType.Null } => null,
            { Type: JTokenType.Boolean } => token.Value<bool>() ? "1" : "0",
            { Type: JTokenType.Array } => string.Join(",", token.Select(ConvertToken).Where(v => v != null)),
            { Type: JTokenType.Object } => null, // WPML 动作参数为扁平结构，嵌套对象不输出
            { Type: JTokenType.String } => token.Value<string>(),
            { Type: JTokenType.Float } => FormatFloat(token.Value<double>()),
            _ => token.ToString(),
        };
    }

    /// <summary>
    /// 浮点输出格式化：使用最短可无损回读的表示（避免 <c>90.0000000006754</c> 这类精度丢失），
    /// 整数值补 <c>.0</c>，与官方 KMZ 的 <c>45.0</c> 写法保持一致。
    /// </summary>
    private static string FormatFloat(double value)
    {
        var text = value.ToString("R", CultureInfo.InvariantCulture);
        if (text.IndexOf('.') < 0 && text.IndexOf('E') < 0 && text.IndexOf('e') < 0)
            text += ".0";
        return text;
    }

    private static string Serialize<T>(XmlSerializer serializer, T model)
    {
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true,
            IndentChars = "  ",
            OmitXmlDeclaration = false,
        };

        var writer = new Utf8StringWriter();
        using (var xmlWriter = XmlWriter.Create(writer, settings))
            serializer.Serialize(xmlWriter, model!, WpmlNamespaces.Namespaces());

        return writer.ToString();
    }

    private static void WriteEntry(ZipArchive zip, string entryName, string content)
    {
        var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string DefaultIfEmpty(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;

    private static (double Lon, double Lat)? ParsePoint(string? point)
    {
        var parts = (point ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return null;
        if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var lon)) return null;
        if (!double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)) return null;
        return (lon, lat);
    }

    /// <summary>大圆距离（米）</summary>
    private static double Haversine((double Lon, double Lat) a, (double Lon, double Lat) b)
    {
        const double earthRadius = 6371000d;
        var lat1 = a.Lat * Math.PI / 180;
        var lat2 = b.Lat * Math.PI / 180;
        var deltaLat = (b.Lat - a.Lat) * Math.PI / 180;
        var deltaLon = (b.Lon - a.Lon) * Math.PI / 180;

        var h = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2)
                + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
        return 2 * earthRadius * Math.Asin(Math.Min(1, Math.Sqrt(h)));
    }

    /// <summary>StringWriter 默认 UTF-16，会导致 XML 声明写成 utf-16，这里强制 UTF-8</summary>
    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }

    #endregion
}
