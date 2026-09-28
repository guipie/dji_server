// 麻省理工学院许可证
//
// 版权所有 (c) 2021-2023  联系电话/微信：15100305  QQ：15100305
//
// 特此免费授予获得本软件的任何人以处理本软件的权利，但须遵守以下条件：在所有副本或重要部分的软件中必须包括上述版权声明和本许可声明。
//
// 软件按“原样”提供，不提供任何形式的明示或暗示的保证，包括但不限于对适销性、适用性和非侵权的保证。
// 在任何情况下，作者或版权持有人均不对任何索赔、损害或其他责任负责，无论是因合同、侵权或其他方式引起的，与软件或其使用或其他交易有关。

using Dji.Core.Enum.DjiEnum.Media;

namespace Dji.Core.Entity.DjiEntity;

/// <summary>
/// 媒体文件（机场回传的图片 / 视频 / PPK 附件）。
/// </summary>
/// <remarks>
/// <para>
/// <b>写入来源</b>：机场上行 <c>thing/product/{sn}/events</c> 的 <c>file_upload_callback</c>，
/// 由 <c>MqMediaService</c> 落库。机场是「先直传对象存储、再回调云端」，因此本表记录的是
/// <b>上传结果</b>而不是上传任务；文件本体在对象存储里，云端只持有 Key 与元数据。
/// </para>
/// <para>
/// <b>幂等性</b>：机场在弱网下会重发同一条 <c>file_upload_callback</c>（该报文 <c>need_reply = 1</c>），
/// 若不去重会出现重复照片。因此对 <see cref="ObjectKey"/> 建唯一索引，
/// 落库走 upsert，重复报文只刷新元数据不新增行。
/// </para>
/// <para>
/// <b>坐标系</b>：<see cref="Longitude"/> / <see cref="Latitude"/> 是 WGS84（协议 <c>shoot_position</c> 原值）。
/// 前端地图打点若用高德/百度底图需要做一次坐标转换，转换属展示层职责，落库不转以免丢失原始值。
/// </para>
/// </remarks>
[SugarTable(null, "媒体文件")]
[SugarIndex("index_DjiMediaFile_ObjectKey", nameof(ObjectKey), OrderByType.Asc, true)]
[SugarIndex("index_DjiMediaFile_FlightId", nameof(FlightId), OrderByType.Asc)]
[SugarIndex("index_DjiMediaFile_DockSn", nameof(DockSn), OrderByType.Asc, nameof(MediaCreateTime), OrderByType.Desc)]
public class DjiMediaFile : EntityWorkspaceBase
{
    /// <summary>对象存储 Key（协议 <c>file.object_key</c>，全局唯一，用于去重）</summary>
    [SugarColumn(ColumnDescription = "对象存储Key", Length = 512, IsNullable = false)]
    public string ObjectKey { get; set; }

    /// <summary>所属存储桶（冗余落库，便于换桶后仍能定位历史文件）</summary>
    [SugarColumn(ColumnDescription = "存储桶", Length = 128, IsNullable = true)]
    public string Bucket { get; set; }

    /// <summary>原始文件名（协议 <c>file.name</c>，如 DJI_20260928120001_0001_D.JPG）</summary>
    [SugarColumn(ColumnDescription = "文件名", Length = 256, IsNullable = true)]
    public string FileName { get; set; }

    /// <summary>文件后缀（小写，含点）</summary>
    [SugarColumn(ColumnDescription = "文件后缀", Length = 16, IsNullable = true)]
    public string Suffix { get; set; }

    /// <summary>媒体类型（由后缀推断，详见 <see cref="MediaFileTypeResolver"/>）</summary>
    [SugarColumn(ColumnDescription = "媒体类型", IsNullable = true)]
    public MediaFileTypeEnum FileType { get; set; }

    /// <summary>文件大小（字节）。协议不保证携带，未知时为 0</summary>
    [SugarColumn(ColumnDescription = "文件大小", IsNullable = true)]
    public long FileSize { get; set; }

    /// <summary>文件的业务路径（协议 <c>file.path</c>，用于在对象存储里做逻辑分组）</summary>
    [SugarColumn(ColumnDescription = "业务路径", Length = 512, IsNullable = true)]
    public string BizPath { get; set; }

    /// <summary>所属航线任务 ID（协议 <c>file.ext.flight_id</c>）；非任务拍摄时为空</summary>
    [SugarColumn(ColumnDescription = "任务ID", Length = 64, IsNullable = true)]
    public string FlightId { get; set; }

    /// <summary>
    /// 关联的本地任务主键（<c>dji_wayline_task.Id</c>）。
    /// </summary>
    /// <remarks>
    /// 冗余一份数值主键是为了让「按任务查媒体」不用再拿 flight_id 做字符串 join；
    /// 回调到达时若任务尚未入库（回调早于进度上报的极端情况）则为空，由任务侧补齐。
    /// </remarks>
    [SugarColumn(ColumnDescription = "本地任务Id", IsNullable = true)]
    public long? TaskId { get; set; }

    /// <summary>回传机场 SN</summary>
    [SugarColumn(ColumnDescription = "机场SN", Length = 64, IsNullable = true)]
    public string DockSn { get; set; }

    /// <summary>拍摄飞行器 SN。协议回调里没有该字段，由 flight_id 反查任务得到</summary>
    [SugarColumn(ColumnDescription = "飞行器SN", Length = 64, IsNullable = true)]
    public string DroneSn { get; set; }

    /// <summary>飞行器产品枚举值（协议 <c>file.ext.drone_model_key</c>，如 0-67-0）</summary>
    [SugarColumn(ColumnDescription = "飞行器型号", Length = 32, IsNullable = true)]
    public string DroneModelKey { get; set; }

    /// <summary>负载产品枚举值（协议 <c>file.ext.payload_model_key</c>）</summary>
    [SugarColumn(ColumnDescription = "负载型号", Length = 32, IsNullable = true)]
    public string PayloadModelKey { get; set; }

    /// <summary>是否为原图（协议 <c>file.ext.is_original</c>）；机场回传的通常是原图</summary>
    [SugarColumn(ColumnDescription = "是否原图", IsNullable = true)]
    public bool IsOriginal { get; set; }

    /// <summary>
    /// 文件组 ID。
    /// </summary>
    /// <remarks>
    /// 协议 <c>file_upload_callback</c> 里没有这个字段，但机场会在对象存储 Key 前缀里带上
    /// 一次上传批次的 UUID（对应 Pilot 侧 <c>group-upload-callback</c> 的 <c>file_group_id</c>）。
    /// 这里从 <see cref="ObjectKey"/> 的首段解析出来，用于「按批次浏览」与统计批次完整率。
    /// </remarks>
    [SugarColumn(ColumnDescription = "文件组ID", Length = 128, IsNullable = true)]
    public string FileGroupId { get; set; }

    /// <summary>拍摄时间（协议 <c>metadata.create_time</c>，ISO8601）</summary>
    [SugarColumn(ColumnDescription = "拍摄时间", IsNullable = true)]
    public DateTime? MediaCreateTime { get; set; }

    /// <summary>拍摄绝对高度（米）</summary>
    [SugarColumn(ColumnDescription = "绝对高度", IsNullable = true)]
    public double? AbsoluteAltitude { get; set; }

    /// <summary>拍摄相对高度（米）</summary>
    [SugarColumn(ColumnDescription = "相对高度", IsNullable = true)]
    public double? RelativeAltitude { get; set; }

    /// <summary>云台偏航角（度，协议里是字符串，落库转数值）</summary>
    [SugarColumn(ColumnDescription = "云台偏航角", IsNullable = true)]
    public double? GimbalYawDegree { get; set; }

    /// <summary>拍摄位置经度（WGS84）</summary>
    [SugarColumn(ColumnDescription = "拍摄经度", IsNullable = true)]
    public double? Longitude { get; set; }

    /// <summary>拍摄位置纬度（WGS84）</summary>
    [SugarColumn(ColumnDescription = "拍摄纬度", IsNullable = true)]
    public double? Latitude { get; set; }

    /// <summary>对外访问地址（按对象存储配置拼接，便于前端直接预览/下载）</summary>
    [SugarColumn(ColumnDescription = "访问地址", Length = 1024, IsNullable = true)]
    public string Url { get; set; }

    /// <summary>回传报文时间戳（毫秒，设备时钟）</summary>
    [SugarColumn(ColumnDescription = "回传时间戳", IsNullable = true)]
    public long ReportTimestamp { get; set; }
}
