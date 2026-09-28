// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Application.Cloud.Core;

namespace Dji.Application.Cloud.Dto.Media;

/// <summary>
/// 媒体回传链路的协议 DTO。
/// </summary>
/// <remarks>
/// <para>
/// <b>机场侧的完整链路（只有 MQTT，没有 HTTP）</b>：
/// <list type="number">
/// <item>机场 <c>requests</c> → <c>storage_config_get</c>：索取对象存储临时凭证；</item>
/// <item>云端 <c>requests_reply</c> → <c>storage_config_get</c>：下发 bucket / endpoint / 凭证 / Key 前缀；</item>
/// <item>机场直传对象存储（<b>不经过云端</b>）；</item>
/// <item>机场 <c>events</c> → <c>file_upload_callback</c>：逐个上报文件结果；</item>
/// <item>云端 <c>events_reply</c> → <c>file_upload_callback</c>：应答（该报文 <c>need_reply=1</c>，不回会重发）。</item>
/// </list>
/// </para>
/// <para>
/// <b>为什么不能照搬 Pilot 的 HTTP 接口</b>：文档里 <c>/media/api/v1/workspaces/{id}/fast-upload</c>、
/// <c>.../tiny-fingerprints</c>、<c>.../upload-callback</c> 等属于左侧菜单的
/// <b>「Pilot 上云」</b>章节（遥控器端），机场不调用它们。机场的秒传/去重靠
/// <c>storage_config_get</c> 返回的 <c>object_key_prefix</c> 与设备侧自主判断完成。
/// </para>
/// </remarks>
public static class MediaConstants
{
    /// <summary><c>storage_config_get.data.module</c>：媒体模块（另一个取值 1 为日志，属 P3 远程日志）</summary>
    public const int ModuleMedia = 0;

    /// <summary><c>storage_config_get.data.module</c>：日志模块</summary>
    public const int ModuleLog = 1;
}

/// <summary>机场索取对象存储凭证（上行 <c>requests</c>，method = <c>storage_config_get</c>）</summary>
public class StorageConfigGetInput
{
    /// <summary>模块枚举值：0 媒体 / 1 日志</summary>
    public int Module { get; set; }
}

/// <summary>云端下发对象存储凭证（下行 <c>requests_reply</c>）</summary>
public class StorageConfigGetOutput
{
    /// <summary>对象存储桶名称</summary>
    public string Bucket { get; set; }

    /// <summary>临时凭证</summary>
    public StorageCredentials Credentials { get; set; }

    /// <summary>对外服务的访问域名（<b>必须能同时被机场和浏览器访问</b>，不能填内网回环地址）</summary>
    public string Endpoint { get; set; }

    /// <summary>云厂商枚举："ali" 阿里云 / "aws" 亚马逊云 / "minio" MinIO</summary>
    public string Provider { get; set; }

    /// <summary>数据中心所在的地域</summary>
    public string Region { get; set; }

    /// <summary>对象存储 Key 的前缀（机场会把文件放在该前缀下，本平台用工作空间 ID 作为前缀）</summary>
    public string ObjectKeyPrefix { get; set; }
}

/// <summary>对象存储临时凭证</summary>
/// <remarks>
/// <c>security_token</c> 与 <c>expire</c> 是「临时凭证」语义；
/// 若使用长期 AK/SK（如本项目的 MinIO 配置），<c>security_token</c> 留空即可，
/// 机场端会按「无会话令牌」处理。
/// </remarks>
public class StorageCredentials
{
    /// <summary>访问密钥 ID</summary>
    public string AccessKeyId { get; set; }

    /// <summary>秘密访问密钥</summary>
    public string AccessKeySecret { get; set; }

    /// <summary>凭证过期时间（秒）。长期凭证填 0</summary>
    public int Expire { get; set; }

    /// <summary>会话凭证（STS 才有，长期 AK 留空）</summary>
    public string SecurityToken { get; set; }
}

#region 媒体文件上传结果（上行 events，method = file_upload_callback）

/// <summary>媒体文件上传结果上报</summary>
/// <remarks>报文 <c>need_reply = 1</c>，必须回 <c>events_reply</c>，否则机场会持续重发。</remarks>
public class FileUploadCallbackInput
{
    /// <summary>文件信息</summary>
    public MediaFilePayload File { get; set; }
}

/// <summary>协议中的单个媒体文件描述</summary>
public class MediaFilePayload
{
    /// <summary>对象存储 Key</summary>
    public string ObjectKey { get; set; }

    /// <summary>文件的业务路径</summary>
    public string Path { get; set; }

    /// <summary>文件名称（含后缀）</summary>
    public string Name { get; set; }

    /// <summary>扩展属性</summary>
    public MediaFileExt Ext { get; set; }

    /// <summary>媒体元数据</summary>
    public MediaFileMetadata Metadata { get; set; }
}

/// <summary>媒体文件扩展属性</summary>
public class MediaFileExt
{
    /// <summary>所属航线任务 ID</summary>
    public string FlightId { get; set; }

    /// <summary>飞行器产品枚举值</summary>
    public string DroneModelKey { get; set; }

    /// <summary>负载产品枚举值</summary>
    public string PayloadModelKey { get; set; }

    /// <summary>是否为原图</summary>
    public bool IsOriginal { get; set; }
}

/// <summary>媒体元数据（拍摄位置与姿态）</summary>
/// <remarks>
/// 数值字段统一挂 <see cref="TolerantDoubleConverter"/>：协议里同一字段既有 JSON 数字也有数字字符串，
/// 不同固件版本两种形式都在用，容错转换可避免一个字段把整条回调报文解析掉。
/// </remarks>
public class MediaFileMetadata
{
    /// <summary>云台偏航角（度）。协议示例为字符串 "-91.40"，故需容错</summary>
    [JsonConverter(typeof(TolerantDoubleConverter))]
    public double? GimbalYawDegree { get; set; }

    /// <summary>拍摄绝对高度（米）</summary>
    [JsonConverter(typeof(TolerantDoubleConverter))]
    public double? AbsoluteAltitude { get; set; }

    /// <summary>拍摄相对高度（米）</summary>
    [JsonConverter(typeof(TolerantDoubleConverter))]
    public double? RelativeAltitude { get; set; }

    /// <summary>
    /// 媒体拍摄时间。
    /// </summary>
    /// <remarks>
    /// 协议标注为 ISO8601，但示例给的是 <c>"2021-05-10 16:04:20"</c>（空格分隔，严格来说不是 ISO8601），
    /// 因此声明为 <see cref="string"/> 由仓储层做多格式容错解析，避免反序列化直接抛异常。
    /// </remarks>
    public string CreateTime { get; set; }

    /// <summary>拍摄位置</summary>
    public MediaShootPosition ShootPosition { get; set; }
}

/// <summary>拍摄位置（WGS84）</summary>
public class MediaShootPosition
{
    /// <summary>纬度</summary>
    [JsonConverter(typeof(TolerantDoubleConverter))]
    public double? Lat { get; set; }

    /// <summary>经度</summary>
    [JsonConverter(typeof(TolerantDoubleConverter))]
    public double? Lng { get; set; }
}

#endregion

#region 上传优先级

/// <summary>机场告知当前优先级最高的上传任务（上行 <c>events</c>）</summary>
public class HighestPriorityUploadInput
{
    /// <summary>当前优先级最高的任务 ID</summary>
    public string FlightId { get; set; }
}

/// <summary>人工调整某任务的媒体上传优先级（下行 <c>services</c>）</summary>
public class UploadFlighttaskMediaPrioritizeInput
{
    /// <summary>需要最高优先级上传的任务 ID</summary>
    public string FlightId { get; set; }
}

#endregion
