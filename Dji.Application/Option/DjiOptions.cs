
namespace Dji.Application.Option;

/// <summary>
/// 上云平台对接参数（对应配置节点 <c>Dji</c>）。
/// </summary>
/// <remarks>
/// 这些参数会通过 MQTT 的 <c>config</c> 应答下发给机场，或用于构造机场可访问的 KMZ 下载地址，
/// 因此必须配置为**机场侧可达**的地址（不能用 localhost）。
/// </remarks>
public sealed class DjiOptions : IConfigurableOptions
{
    /// <summary>
    /// 大疆开发者 App ID。
    /// </summary>
    /// <remarks>在 <c>https://developer.dji.com/user/apps/</c> 创建应用后获得，机场用它与云端完成鉴权。</remarks>
    public string AppId { get; set; }

    /// <summary>大疆开发者 App Key</summary>
    public string AppKey { get; set; }

    /// <summary>大疆开发者 App License</summary>
    public string AppLicense { get; set; }

    /// <summary>
    /// NTP 服务器地址（下发给机场用于校时）。
    /// </summary>
    /// <remarks>
    /// 强烈建议配置：机场与云端时钟不一致会导致「立即任务」因超过 30 秒误差被拒绝，
    /// 且定时任务的 <c>execute_time</c> 会全部偏移。
    /// </remarks>
    public string NtpServerHost { get; set; }

    /// <summary>NTP 服务端口，缺省 123</summary>
    public int NtpServerPort { get; set; } = 123;

    /// <summary>
    /// KMZ 文件对外访问前缀（如 <c>http://cloud.example.com:5005</c>）。
    /// </summary>
    /// <remarks>
    /// 机场通过 <c>flighttask_prepare.file.url</c> 主动下载 KMZ，该地址由机场发起访问，
    /// 因此必须是机场网络可达的绝对地址；留空时退化为 <c>CommonUtil.GetLocalhost()</c>（仅适用于本机联调）。
    /// </remarks>
    public string FileBaseUrl { get; set; }

    /// <summary>直播（自建 SRS）相关参数</summary>
    public LiveOptions Live { get; set; } = new();
}

/// <summary>
/// 直播流媒体参数（对应配置节点 <c>Dji:Live</c>），本项目采用<b>自建 SRS</b>。
/// </summary>
/// <remarks>
/// <para>
/// 三个地址的角色不同，<b>不能混用同一个域名</b>：
/// <list type="bullet">
/// <item><see cref="RtmpPushBaseUrl"/> —— 由<b>机场</b>访问（推流），必须是机场网络可达的地址；</item>
/// <item><see cref="FlvPlayBaseUrl"/> / <see cref="HlsPlayBaseUrl"/> —— 由<b>浏览器</b>访问（拉流），
/// 必须是终端用户网络可达的地址。</item>
/// </list>
/// 联调时两者常常相同，但生产环境里机场走内网、浏览器走公网，此时需要分别配置。
/// </para>
/// <para>
/// <b>协议只允许一个 <c>url</c> 字段</b>：下发给机场的仅 <see cref="RtmpPushBaseUrl"/>，
/// 播放地址是云端自己按流名拼出来给前端用的，与机场无关。
/// </para>
/// </remarks>
public sealed class LiveOptions
{
    /// <summary>
    /// 是否启用直播能力。
    /// </summary>
    /// <remarks>关闭时直播接口会直接返回“未配置”提示，而不是下发一个无效地址让机场报错。</remarks>
    public bool Enabled { get; set; }

    /// <summary>
    /// 机场推流基地址（SRS 的 RTMP 入口），如 <c>rtmp://192.168.1.10:1935/live</c>。
    /// </summary>
    /// <remarks>末尾不要带斜杠，云端会自动拼接流名。</remarks>
    public string RtmpPushBaseUrl { get; set; }

    /// <summary>浏览器拉流基地址（SRS 的 HTTP-FLV 出口），如 <c>http://192.168.1.10:8080/live</c></summary>
    public string FlvPlayBaseUrl { get; set; }

    /// <summary>浏览器拉流备用地址（SRS 的 HLS 出口），如 <c>http://192.168.1.10:8080/live</c></summary>
    public string HlsPlayBaseUrl { get; set; }

    /// <summary>
    /// 流名前缀，用于把大疆的流与其他业务的流区分开（如 <c>dji_</c>）。
    /// </summary>
    /// <remarks>SRS 的流名只允许字母、数字、下划线、短横线与点，因此 <c>video_id</c> 里的 <c>/</c> 会被替换。</remarks>
    public string StreamPrefix { get; set; } = "dji_";

    /// <summary>
    /// 单机场允许的最大并发直播路数上限（云端兜底值）。
    /// </summary>
    /// <remarks>
    /// 真正的上限来自设备上报的 <c>live_capacity.coexist_video_number_max</c>；
    /// 该值只在设备尚未上报能力时作为兜底，避免无能力数据时无法做并发校验。
    /// </remarks>
    public int MaxConcurrentPerDock { get; set; } = 1;

    /// <summary>
    /// 单次直播的最长时长（分钟），超时由后台任务自动停播。
    /// </summary>
    /// <remarks>
    /// 机场长时间推流会持续消耗图传带宽，且一旦会话记录与设备实际状态脱节（设备掉线未上报）
    /// 就会出现「幽灵直播」。设置上限可避免资源被永久占用；0 表示不限制。
    /// </remarks>
    public int MaxSessionMinutes { get; set; } = 120;
}

public sealed class MqttOptions : IConfigurableOptions
{
    public string ClientId { get; set; } = Guid.NewGuid().ToString();

    [Required(ErrorMessage = "mqtt server不能为空")]
    public required string Server { get; set; }
    public int Port { get; set; } = 1883;
    public bool UseTls { get; set; } = false;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int KeepAliveSeconds { get; set; } = 60;
    public List<string> SubscribedTopics { get; set; } = [];
    public bool CleanSession { get; set; } = true;
}
