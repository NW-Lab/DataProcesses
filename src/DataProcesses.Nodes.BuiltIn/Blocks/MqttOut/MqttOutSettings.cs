using System.Text.Json;

namespace DataProcesses.Nodes.BuiltIn.Blocks.MqttOut;

public sealed record MqttOutSettings
{
    public string BrokerHost { get; init; } = "localhost";
    public int Port { get; init; } = 1883;
    public string ClientId { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public bool UseTls { get; init; }
    public string Topic { get; init; } = string.Empty;
    public int QualityOfService { get; init; }
    public bool Retain { get; init; }

    public static MqttOutSettings Default { get; } = new();

    public static MqttOutSettings FromJson(string settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            return Default;
        }

        using var document = JsonDocument.Parse(settingsJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("MQTT_Out settings must be a JSON object.", nameof(settingsJson));
        }

        var settings = JsonSerializer.Deserialize<MqttOutSettings>(
            settingsJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? Default;
        settings.Validate();
        return settings;
    }

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(BrokerHost);
        if (Port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(Port), "MQTT port must be between 1 and 65535.");
        }

        if (QualityOfService is < 0 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(QualityOfService), "MQTT QoS must be 0, 1, or 2.");
        }
    }
}