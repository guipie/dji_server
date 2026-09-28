// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using System.IO;
using System.Text.Json;
using Dji.Application.Cloud.Dto.Hms;

namespace Dji.Application.Service.Common;

/// <summary>
/// HMS 告警文案解析（把协议里的 <c>code</c> + <c>args</c> 翻译成可读中文）。
/// </summary>
/// <remarks>
/// <para>
/// <b>文案来源</b>：官方发布的 <c>hms.json</c>（约 3900 条，含中英文），
/// 随本项目放在 <c>Configuration/hms.json</c>，启动后<b>懒加载一次</b>常驻内存。
/// 之所以不放进数据库：它是纯静态的只读字典，放库里反而要在每次部署时做数据同步；
/// 之所以做懒加载而非启动即读：文件缺失不应阻断整个服务启动（HMS 只是告警展示，
/// 解析不到文案时退化为展示原始告警码，不影响其它功能）。
/// </para>
/// <para>
/// <b>文案 Key 的拼接规则</b>（官方原文）：
/// 机场设备为 <c>dock_tip_{code}</c>；飞行器为 <c>fpv_tip_{code}</c>，
/// 且当 <c>in_the_sky = 1</c> 时应优先取 <c>fpv_tip_{code}_in_the_sky</c>。
/// 判断「是不是机场」看 <c>device_type</c> 的首段（domain）：3 为机场，0 为飞行器。
/// </para>
/// <para>
/// <b>占位符回填</b>：官方文案里含 <c>%alarmid</c>、<c>%index</c>、<c>%component_index</c>、
/// <c>%battery_index</c>、<c>%dock_cover_index</c>、<c>%charging_rod_index</c> 六种占位符，
/// 全部依赖 <c>args</c> 里的两个索引。规则见 <see cref="Fill"/> 的注释。
/// </para>
/// </remarks>
public class HmsTextService : ISingleton
{
    private readonly ILogger<HmsTextService> _logger;
    private readonly Lazy<Dictionary<string, HmsTextItem>> _dict;

    public HmsTextService(ILogger<HmsTextService> logger)
    {
        _logger = logger;
        _dict = new Lazy<Dictionary<string, HmsTextItem>>(Load, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>字典是否已成功加载（可用于接口返回「文案字典不可用」的提示）</summary>
    public bool IsAvailable => _dict.Value.Count > 0;

    /// <summary>
    /// 解析一条告警的中英文文案。
    /// </summary>
    /// <param name="code">告警码，如 <c>0x16100083</c></param>
    /// <param name="deviceType">设备产品枚举值，如 <c>3-1-0</c>（机场）/ <c>0-67-0</c>（飞行器）</param>
    /// <param name="inTheSky">上报时刻是否在空中</param>
    /// <param name="args">告警参数（部件 / 传感器索引）</param>
    /// <returns>中英文文案；字典里查不到时两项均为空</returns>
    public (string Zh, string En) Resolve(string code, string deviceType, int inTheSky, HmsAlarmArgs args)
    {
        var (zh, en) = ("", "");
        if (code.IsNullOrWhiteSpace()) return (zh, en);

        var dict = _dict.Value;
        if (dict.Count == 0) return (zh, en);

        var isDock = IsDockDevice(deviceType);
        foreach (var key in BuildKeys(code, isDock, inTheSky))
        {
            if (!dict.TryGetValue(key, out var item)) continue;
            zh = Fill(item.Zh, code, args);
            en = Fill(item.En, code, args);
            break;
        }

        return (zh, en);
    }

    #region 私有实现

    /// <summary>
    /// 按「最具体 → 最兜底」的顺序给出候选文案 Key。
    /// </summary>
    /// <remarks>
    /// 飞行器告警带 <c>_in_the_sky</c> 后缀的条目只在「确实在空中」时才应命中，
    /// 但字典里并非每个告警码都存在该后缀版本，因此给出降级链：
    /// 先试带后缀的，再退回不带后缀的，避免因为缺一条文案而整条告警显示为空白。
    /// </remarks>
    private static IEnumerable<string> BuildKeys(string code, bool isDock, int inTheSky)
    {
        if (isDock)
        {
            yield return $"dock_tip_{code}";
            yield break;
        }

        if (inTheSky == 1) yield return $"fpv_tip_{code}_in_the_sky";
        yield return $"fpv_tip_{code}";
    }

    /// <summary>
    /// 判断是否机场本体发出的告警。
    /// </summary>
    /// <remarks>
    /// <c>device_type</c> 形如 <c>{domain}-{type}-{sub_type}</c>，domain = 3 为机场。
    /// 值缺失时按机场处理：机场告警的文案 Key 规则更简单（无 <c>in_the_sky</c> 分支），
    /// 且实际报文中机场告警占比更高，误判的代价更小。
    /// </remarks>
    private static bool IsDockDevice(string deviceType)
    {
        if (deviceType.IsNullOrWhiteSpace()) return true;
        var seg = deviceType.Split('-', StringSplitOptions.RemoveEmptyEntries);
        return seg.Length == 0 || seg[0] != "0";
    }

    /// <summary>
    /// 回填文案里的占位符。
    /// </summary>
    /// <remarks>
    /// 官方规则：
    /// <list type="bullet">
    /// <item><c>%alarmid</c> → 告警码本身；</item>
    /// <item><c>%index</c> → <c>sensor_index + 1</c>；</item>
    /// <item><c>%component_index</c> → <c>component_index + 1</c>，且限定在 1~2（协议目前最多 3 个云台）；</item>
    /// <item><c>%battery_index</c> → <c>sensor_index = 0</c> 时为「左」，否则为「右」；</item>
    /// <item><c>%dock_cover_index</c> → 规则同上（左 / 右）；</item>
    /// <item><c>%charging_rod_index</c> → <c>sensor_index</c> 0/1/2/3 依次对应 前 / 后 / 左 / 右。</item>
    /// </list>
    /// 注意「+1 后再限定范围」的顺序：<c>%component_index</c> 要把越界值夹到 1~2，
    /// 而不是先夹索引再加一，否则 <c>component_index = 0</c> 会算成 1（正确）、
    /// 而 <c>component_index = 5</c> 会被夹成 2（也正确）—— 两者结果一致，但语义不同，
    /// 这里按官方描述「最终范围限定在 1 和 2 之间」即对最终值夹取。
    /// </remarks>
    private static string Fill(string text, string code, HmsAlarmArgs args)
    {
        if (text.IsNullOrWhiteSpace()) return text;

        var component = args?.ComponentIndex ?? 0;
        var sensor = args?.SensorIndex ?? 0;

        var componentText = Math.Clamp(component + 1, 1, 2).ToString();
        var chargingRod = sensor switch
        {
            0 => "前",
            1 => "后",
            2 => "左",
            _ => "右"
        };

        return text
            .Replace("%alarmid", code)
            .Replace("%component_index", componentText)
            .Replace("%charging_rod_index", chargingRod)
            .Replace("%dock_cover_index", sensor == 0 ? "左" : "右")
            .Replace("%battery_index", sensor == 0 ? "左" : "右")
            .Replace("%index", (sensor + 1).ToString());
    }

    /// <summary>加载文案字典；失败时返回空字典并记录警告，不抛异常</summary>
    private Dictionary<string, HmsTextItem> Load()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Configuration", "hms.json");
            if (!File.Exists(path))
            {
                _logger.LogWarning("HMS 文案字典不存在（{Path}），告警将只显示原始告警码。" +
                                   "该文件随项目发布，若缺失通常是未把 Configuration\\hms.json 设为复制到输出目录", path);
                return [];
            }

            var json = File.ReadAllText(path);
            // 字典的键是中文/英文小写字段名（zh / en），而属性用的是 Pascal 命名，故需忽略大小写。
            // 显式写全限定名：项目全局 using 里已有 Newtonsoft.Json，直接写 JsonSerializer 会二义。
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var raw = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, HmsTextItem>>(json, options);
            if (raw == null || raw.Count == 0)
            {
                _logger.LogWarning("HMS 文案字典内容为空：{Path}", path);
                return [];
            }

            // 查表必须大小写不敏感：字典里的告警码已统一为小写十六进制（0x1a420bc0），
            // 但设备上报的写法不固定（同一码可能是 0x1A420BC0）。若用默认的大小写敏感字典，
            // 大写码会整条落空、页面上只剩原始告警码。
            var dict = new Dictionary<string, HmsTextItem>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in raw) dict[pair.Key] = pair.Value;

            _logger.LogInformation("HMS 文案字典已加载，共 {Count} 条", dict.Count);
            return dict;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载 HMS 文案字典失败，告警将只显示原始告警码");
            return [];
        }
    }

    #endregion

    /// <summary>字典条目（仅保留中英文，见项目内 Configuration/hms.json）</summary>
    public class HmsTextItem
    {
        /// <summary>中文文案</summary>
        public string Zh { get; set; }

        /// <summary>英文文案</summary>
        public string En { get; set; }
    }
}
