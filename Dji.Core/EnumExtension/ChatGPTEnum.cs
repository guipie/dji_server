namespace Admin.NET.Core;


[Description("ai模型")]
public enum AIModelEnum
{

    [Description("聊天")]
    Chat,
    [Description("绘画")]
    Image,
    [Description("文本")]
    Text,
    [Description("其他")]
    Other
}
public enum AIModelOptionEnum
{

    [Description("文本")]
    Text,
    [Description("数字")]
    Num,
    [Description("单选")]
    Radio,
    [Description("多选")]
    CheckBox,
    [Description("true，false")]
    Switch
}
/// <summary>
/// token添加类型
/// </summary>
[Description("累积类型")]
public enum TokenAddTypeEnum
{
    /// <summary>
    /// 签到
    /// </summary>
    [Description("签到")]
    QianDao,
    /// <summary>
    /// 充值
    /// </summary>
    [Description("充值")]
    Pay,
    /// <summary>
    /// 邀请
    /// </summary>
    [Description("邀请")]
    Invite,
    /// <summary>
    /// 绑定
    /// </summary>
    [Description("绑定")]
    Bind
}
/// <summary>
/// 使用token的请求类型
/// </summary>
[Description("使用类型")]
public enum TokenUsageType
{
    Chat,
    ChatImage,
    Midjourney
}
public enum DrawingImageType
{

    [Description("文生图")]
    text_image,
    [Description("艺术字")]
    yishuzi_image,
    [Description("二维码")]
    qr_code_image,
    [Description("图生图")]
    image_image,
}
