// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

namespace Dji.Application.Cloud.Dto.Ops;

#region 下行：列举可上传的日志

/// <summary>
/// 列举可上传日志的下发载荷（<c>fileupload_list</c>）。
/// </summary>
/// <remarks>
/// <b>注意 <c>module_list</c> 是字符串数组而不是整数数组</b>：协议里 <c>module</c> 这一族字段
/// 在不同报文中的类型并不一致 —— <c>fileupload_list</c> 的请求与 <c>fileupload_progress</c> 的文件项
/// 用的是字符串（<c>"0"</c> / <c>"3"</c>），而 <c>fileupload_list</c> 回包里也是字符串。
/// 因此这里统一用 <c>string</c>，避免序列化出 <c>[0,3]</c> 被设备拒绝。
/// </remarks>
public class FileuploadListPayload
{
    /// <summary>文件所属模块过滤列表（<c>"0"</c> 飞行器 / <c>"3"</c> 机场）</summary>
    public List<string> ModuleList { get; set; } = [];
}

/// <summary>
/// 列举可上传日志的回包（<c>services_reply</c>）。
/// </summary>
/// <remarks>回包结构比较特殊：<c>data</c> 里没有 <c>output</c>，而是直接把 <c>files</c> 与 <c>result</c> 平铺。</remarks>
public class FileuploadListReply
{
    /// <summary>各设备的日志索引集合</summary>
    public List<FileuploadListDevice> Files { get; set; } = [];

    /// <summary>返回码（0 表示成功）</summary>
    public int? Result { get; set; }
}

/// <summary>某设备的可上传日志索引</summary>
public class FileuploadListDevice
{
    /// <summary>设备序列号</summary>
    public string DeviceSn { get; set; }

    /// <summary>该设备的返回码（0 表示成功）</summary>
    public int? Result { get; set; }

    /// <summary>所属模块（<c>"0"</c> 飞行器 / <c>"3"</c> 机场），协议用字符串</summary>
    public string Module { get; set; }

    /// <summary>文件索引列表</summary>
    public List<FileuploadListIndex> List { get; set; } = [];
}

/// <summary>单个日志索引</summary>
public class FileuploadListIndex
{
    /// <summary>日志索引（设备上按开机轮次编号）</summary>
    public int? BootIndex { get; set; }

    /// <summary>日志开始时间（协议标注单位秒，但实测回包给的是毫秒，见仓储中的换算说明）</summary>
    public long? StartTime { get; set; }

    /// <summary>日志结束时间（同上）</summary>
    public long? EndTime { get; set; }

    /// <summary>文件大小（字节）</summary>
    public long? Size { get; set; }
}

#endregion

#region 下行：发起上传 / 取消上传

/// <summary>
/// 发起日志上传的下发载荷（<c>fileupload_start</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>与 <c>storage_config_get</c> 的应答高度重合但不完全相同</b>：后者只给桶 / 凭证 / 前缀，
/// 由设备自行决定文件名；本报文还要额外给出 <c>params.files[]</c>，
/// <b>由云端指定 <c>object_key</c> 与要传哪些 <c>boot_index</c></b>。
/// 因此不能直接复用 <c>StorageConfigGetOutput</c>，但其中桶 / 凭证等字段由同一份配置构造，保证两者不会打架。
/// </para>
/// <para>
/// <c>credentials.expire</c> 单位在文档里写「秒」，示例里却是毫秒级时间戳。
/// 本项目使用长期 AK/SK，恒填 <c>0</c>（与媒体回传链路一致），因此不涉及该歧义。
/// </para>
/// </remarks>
public class FileuploadStartPayload
{
    /// <summary>对象存储桶名称</summary>
    public string Bucket { get; set; }

    /// <summary>数据中心所在的地域</summary>
    public string Region { get; set; }

    /// <summary>凭证信息</summary>
    public FileuploadCredentials Credentials { get; set; }

    /// <summary>对外服务的访问域名</summary>
    public string Endpoint { get; set; }

    /// <summary>云厂商枚举值（<c>ali</c> / <c>aws</c> / <c>minio</c>）</summary>
    public string Provider { get; set; }

    /// <summary>待上传文件参数</summary>
    public FileuploadParams Params { get; set; }
}

/// <summary>日志上传凭证</summary>
public class FileuploadCredentials
{
    /// <summary>访问密钥 ID</summary>
    public string AccessKeyId { get; set; }

    /// <summary>秘密访问密钥</summary>
    public string AccessKeySecret { get; set; }

    /// <summary>凭证过期时间（长期凭证填 0）</summary>
    public int Expire { get; set; }

    /// <summary>会话凭证（长期 AK 留空）</summary>
    public string SecurityToken { get; set; }
}

/// <summary>上传参数</summary>
public class FileuploadParams
{
    /// <summary>按「模块 + 对象存储 Key」分组的待传文件</summary>
    public List<FileuploadFileGroup> Files { get; set; } = [];
}

/// <summary>一组待上传文件（同一模块、同一 object_key 前缀）</summary>
public class FileuploadFileGroup
{
    /// <summary>文件在对象存储桶的 Key</summary>
    public string ObjectKey { get; set; }

    /// <summary>日志所属模块（<c>"0"</c> 飞行器 / <c>"3"</c> 机场），协议用字符串</summary>
    public string Module { get; set; }

    /// <summary>本组要上传的日志索引</summary>
    public List<FileuploadBootIndex> List { get; set; } = [];
}

/// <summary>单个待上传的日志索引</summary>
public class FileuploadBootIndex
{
    /// <summary>日志索引</summary>
    public int? BootIndex { get; set; }
}

/// <summary>
/// 上传状态更新（<c>fileupload_update</c>）。
/// </summary>
/// <remarks>目前协议只支持取消（<c>status = "cancel"</c>），且必须同时给出要取消的模块列表。</remarks>
public class FileuploadUpdatePayload
{
    /// <summary>上传状态（固定 <c>cancel</c>）</summary>
    public string Status { get; set; } = "cancel";

    /// <summary>要取消的日志模块列表（<c>"0"</c> / <c>"3"</c>）</summary>
    public List<string> ModuleList { get; set; } = [];
}

#endregion

#region 上行：上传进度

/// <summary>
/// 日志上传进度（<c>fileupload_progress</c>，<c>events</c> 上行）。
/// </summary>
/// <remarks>
/// <para>
/// <b>本报文 <c>need_reply = 0</c></b>（官方示例明确给出），因此处理完毕后<b>不需要</b>回
/// <c>events_reply</c> —— 与媒体回传的 <c>file_upload_callback</c>（<c>need_reply = 1</c>）不同。
/// 多发一条无用的应答反而会干扰设备侧的消息计数。
/// </para>
/// <para>
/// <b>进度是「按文件」给的</b>：<c>output.ext.files[]</c> 一次可带多个文件，
/// 每个文件都有自己的 <c>progress</c>。这比固件升级的进度信息丰富得多（那边没有设备 SN）。
/// </para>
/// </remarks>
public class FileUploadProgressData
{
    /// <summary>返回码（0 表示无错误）</summary>
    public int? Result { get; set; }

    /// <summary>输出</summary>
    public FileUploadProgressOutput Output { get; set; }
}

/// <summary>上传进度输出</summary>
public class FileUploadProgressOutput
{
    /// <summary>扩展信息（文件列表在 <c>ext</c> 里）</summary>
    public FileUploadProgressExt Ext { get; set; }

    /// <summary>整体状态（与各文件的 <c>progress.status</c> 并列）</summary>
    public string Status { get; set; }
}

/// <summary>上传进度扩展信息</summary>
public class FileUploadProgressExt
{
    /// <summary>文件进度列表</summary>
    public List<FileUploadProgressFile> Files { get; set; } = [];
}

/// <summary>单个文件的上传进度</summary>
public class FileUploadProgressFile
{
    /// <summary>所属模块（<c>"0"</c> 飞行器 / <c>"3"</c> 机场），协议用字符串</summary>
    public string Module { get; set; }

    /// <summary>文件大小（字节）</summary>
    public long? Size { get; set; }

    /// <summary>设备序列号</summary>
    public string DeviceSn { get; set; }

    /// <summary>对象存储桶 Key</summary>
    public string Key { get; set; }

    /// <summary>文件指纹（用于校验完整性；同一文件多次进度帧保持一致）</summary>
    public string Fingerprint { get; set; }

    /// <summary>进度信息</summary>
    public FileUploadProgressDetail Progress { get; set; }
}

/// <summary>上传进度明细</summary>
public class FileUploadProgressDetail
{
    /// <summary>进度百分比（0~100）</summary>
    public int? Progress { get; set; }

    /// <summary>上传完成时间（<b>毫秒</b>）</summary>
    public long? FinishTime { get; set; }

    /// <summary>上传速率（字节/秒）</summary>
    public long? UploadRate { get; set; }

    /// <summary>返回码（非 0 表示该文件上传失败）</summary>
    public int? Result { get; set; }

    /// <summary>上传状态字符串（<c>ok</c> / <c>failed</c> / <c>cancel</c> / 进行中）</summary>
    public string Status { get; set; }

    /// <summary>当前步骤序号（部分固件提供）</summary>
    public int? CurrentStep { get; set; }

    /// <summary>总步骤数（部分固件提供）</summary>
    public int? TotalStep { get; set; }
}

#endregion
