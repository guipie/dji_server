

namespace Dji.Application;
public class TencentSmsOptions : IConfigurableOptions
{
    public string SecretId { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string SmsSdkAppId { get; set; } = string.Empty;
    public string SignName { get; set; } = string.Empty;
    public string TemplateId { get; set; } = string.Empty;
}
