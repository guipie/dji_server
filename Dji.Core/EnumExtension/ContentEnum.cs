
namespace Admin.NET.Core;

/// <summary>
/// 内容状态枚举
/// </summary>
[Description("内容状态枚举")]
public enum ContentStatusEnum
{
    [Description("待审核")]
    Pending = 0,

    [Description("审核通过")]
    Passed = 1,

    [Description("推荐")]
    Recommend = 2,

    [Description("置顶")]
    Top = 3,
}
