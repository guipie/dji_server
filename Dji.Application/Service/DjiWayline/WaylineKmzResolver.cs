// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Cloud.Dto.Wayline;
using Dji.Application.Option;
using Microsoft.Extensions.Options;
using System.Linq;

namespace Dji.Application.Service.DjiWayline;

/// <summary>
/// KMZ 资源定位器：解析出机场可直接下载的 KMZ 地址与 MD5 签名。
/// </summary>
/// <remarks>
/// <para>
/// 该逻辑被两条链路共用：① 下发 <c>flighttask_prepare</c> 时填 <c>file</c>；
/// ② 机场通过 <c>flighttask_resource_get</c> 重取资源时回填同一个 <c>file</c>。
/// 二者必须给出**完全一致**的结果，否则机场会因签名不匹配拒绝执行，因此集中在此实现。
/// </para>
/// <para>
/// <b>为什么不能直接用 <c>KmzFileUrl</c></b>：该地址由上传时的 HTTP 请求上下文生成
/// （<c>CommonUtil.GetLocalhost()</c>），线上通常是 <c>localhost</c> 或内网回环地址，机场无法访问。
/// 因此配置了 <c>Dji.FileBaseUrl</c> 时统一替换为主体部分。
/// </para>
/// </remarks>
public class WaylineKmzResolver(
    SqlSugarRepository<SysFile> fileRep,
    SqlSugarRepository<DjiWaylineEntity> waylineRep,
    IOptions<DjiOptions> options,
    ILogger<WaylineKmzResolver> logger) : ITransient
{
    private readonly SqlSugarRepository<SysFile> _fileRep = fileRep;
    private readonly SqlSugarRepository<DjiWaylineEntity> _waylineRep = waylineRep;
    private readonly DjiOptions _options = options.Value;
    private readonly ILogger<WaylineKmzResolver> _logger = logger;

    /// <summary>
    /// 解析 KMZ 下载地址与签名。
    /// </summary>
    /// <returns>解析失败（地址或签名缺失）时返回 null，由调用方给出明确错误提示</returns>
    public async Task<FlightTaskFile> ResolveAsync(DjiWaylineEntity wayline)
    {
        if (wayline == null) return null;

        var url = BuildDownloadUrl(wayline.KmzFileUrl);
        if (url.IsNullOrWhiteSpace())
        {
            _logger.LogError("航线 {WaylineId} 缺少 KMZ 访问地址，无法下发任务", wayline.Id);
            return null;
        }

        var fingerprint = await ResolveFingerprintAsync(wayline);
        if (fingerprint.IsNullOrWhiteSpace())
        {
            _logger.LogError("航线 {WaylineId} 缺少 KMZ 签名，无法下发任务", wayline.Id);
            return null;
        }

        return new FlightTaskFile { Url = url, Fingerprint = fingerprint };
    }

    /// <summary>
    /// 把 KMZ 地址的主体替换为配置的对外前缀（保留路径与查询串）。
    /// </summary>
    /// <remarks>未配置 <c>Dji.FileBaseUrl</c> 时原样返回，便于本机联调。</remarks>
    private string BuildDownloadUrl(string kmzFileUrl)
    {
        if (kmzFileUrl.IsNullOrWhiteSpace()) return null;

        var baseUrl = _options.FileBaseUrl;
        if (baseUrl.IsNullOrWhiteSpace()) return kmzFileUrl;

        if (!Uri.TryCreate(kmzFileUrl, UriKind.Absolute, out var fileUri))
        {
            _logger.LogWarning("KMZ 地址不是合法的绝对地址，将按前缀直接拼接：{Url}", kmzFileUrl);
            return $"{baseUrl.TrimEnd('/')}/{kmzFileUrl.TrimStart('/')}";
        }

        if (!Uri.TryCreate(baseUrl.TrimEnd('/'), UriKind.Absolute, out var baseUri))
        {
            _logger.LogWarning("Dji.FileBaseUrl 配置不是合法的绝对地址，已忽略：{BaseUrl}", baseUrl);
            return kmzFileUrl;
        }

        return new Uri(baseUri, fileUri.PathAndQuery).ToString();
    }

    /// <summary>
    /// 取 KMZ 的 MD5 签名。
    /// </summary>
    /// <remarks>
    /// 历史航线（<c>Sign</c> 字段上线前创建）没有签名，这里从 <c>SysFile.FileMd5</c> 回填：
    /// 上传组件计算的是 **Base64**，而协议要求十六进制，因此做一次转换后写回，避免每次下发都重算。
    /// </remarks>
    private async Task<string> ResolveFingerprintAsync(DjiWaylineEntity wayline)
    {
        if (!wayline.Sign.IsNullOrWhiteSpace()) return wayline.Sign.Trim().ToLowerInvariant();
        if (wayline.KmzFileId is null or 0) return null;

        var file = await _fileRep.GetFirstAsync(m => m.Id == wayline.KmzFileId);
        var md5 = file?.FileMd5;
        if (md5.IsNullOrWhiteSpace()) return null;

        var hex = NormalizeMd5(md5);
        if (hex.IsNullOrWhiteSpace()) return null;

        wayline.Sign = hex;
        await _waylineRep.AsUpdateable(new DjiWaylineEntity { Id = wayline.Id, Sign = hex })
            .UpdateColumns(m => m.Sign)
            .Where(m => m.Id == wayline.Id)
            .ExecuteCommandAsync();

        _logger.LogInformation("已为历史航线回填 KMZ 签名，waylineId:{WaylineId}", wayline.Id);
        return hex;
    }

    /// <summary>把 Base64 / 十六进制形式的 MD5 统一为小写十六进制</summary>
    private static string NormalizeMd5(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 32 && trimmed.All(Uri.IsHexDigit)) return trimmed.ToLowerInvariant();

        try
        {
            var bytes = Convert.FromBase64String(trimmed);
            return bytes.Length == 16 ? Convert.ToHexString(bytes).ToLowerInvariant() : null;
        }
        catch (FormatException)
        {
            // 既不是 32 位十六进制也不是 Base64，无法判定，交由上层报错
            return null;
        }
    }
}
