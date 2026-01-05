namespace Dji.Application;

/// <summary>
/// 设备输出参数
/// </summary>
public class DjiDeviceOutput
{
    /// <summary>
    /// id
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 编号
    /// </summary>
    public string Sn { get; set; }

    /// <summary>
    /// 名称
    /// </summary>
    public string Model { get; set; }

    /// <summary>
    /// 工作空间
    /// </summary>
    public string WorkspaceId { get; set; }

    /// <summary>
    /// 工作空间 描述
    /// </summary>
    public string WorkspaceIdNickName { get; set; }

    /// <summary>
    /// 机场编号
    /// </summary>
    public string ParentSn { get; set; }

    /// <summary>
    /// 机场编号 描述
    /// </summary>
    public string? ParentSnNick { get; set; }

    /// <summary>
    /// 昵称
    /// </summary>
    public string? Nick { get; set; }

    /// <summary>
    /// 领域
    /// </summary>
    public int? Domain { get; set; }

    /// <summary>
    /// 主类型
    /// </summary>
    public long? Type { get; set; }

    /// <summary>
    /// 子类型
    /// </summary>
    public long? SubType { get; set; }

    /// <summary>
    /// index
    /// </summary>
    public string? Index { get; set; }

    /// <summary>
    /// 网关版本
    /// </summary>
    public string? ThingVersion { get; set; }

    /// <summary>
    /// 固件版本
    /// </summary>
    public string? FirmwareVersion { get; set; }

    /// <summary>
    /// desc
    /// </summary>
    public string? Desc { get; set; }

    /// <summary>
    /// 经度
    /// </summary>
    public double? Longitude { get; set; }

    /// <summary>
    /// 维度
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// 高度
    /// </summary>
    public double? Altitude { get; set; }

    /// <summary>
    /// bind_time
    /// </summary>
    public DateTime? BindTime { get; set; }

    /// <summary>
    /// binded
    /// </summary>
    public bool? Binded { get; set; }

    /// <summary>
    /// avatar_url
    /// </summary>
    public string? AvatarUrl { get; set; }
    public SysFile AvatarUrlAttachment { get; set; }

    /// <summary>
    /// create_time
    /// </summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>
    /// is_delete
    /// </summary>
    public bool IsDelete { get; set; }

    public List<DjiDeviceOutput> Children { get; set; }

    //配置Adapt
    public static DjiDeviceOutput ToDto(DjiDevice entity, string spaceName)
    {
        var output = entity.Adapt<DjiDeviceOutput>();
        output.WorkspaceIdNickName = spaceName;
        return output;
    }

}


