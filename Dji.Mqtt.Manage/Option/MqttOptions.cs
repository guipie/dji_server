

namespace Dji.Mqtt.Manage.Option;
public sealed class MqttOptions : IConfigurableOptions
{

    public string ClientId { get; set; } = Guid.NewGuid().ToString();

    [Required(ErrorMessage = "mqtt server不能为空")]
    public string Server { get; set; }
    public int Port { get; set; } = 1883;
    public bool UseTls { get; set; } = false;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int KeepAliveSeconds { get; set; } = 60;
    public List<string> SubscribedTopics { get; set; } = new();
    public bool CleanSession { get; set; } = true;

}
