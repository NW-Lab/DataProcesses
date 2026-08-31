using System.Globalization;
using System.Text.Json;

namespace DataProcesses.Nodes.BuiltIn.Blocks.BleInputSt;

public sealed record BleInputStSettings(
    string DeviceId = "",
    string DeviceName = "",
    bool AutoConnect = true,
    string ServiceUuid = BleInputStSettings.NordicUartServiceUuid,
    string NotifyCharacteristicUuid = BleInputStSettings.NordicUartTxCharacteristicUuid,
    int ChannelCount = 2,
    int TimeoutMilliseconds = 5000)
{
    public const string NordicUartServiceUuid = "6e400001-b5a3-f393-e0a9-e50e24dcca9e";
    public const string NordicUartTxCharacteristicUuid = "6e400003-b5a3-f393-e0a9-e50e24dcca9e";
    public const int MinimumChannelCount = 1;
    public const int MaximumChannelCount = 16;
    public const int MinimumTimeoutMilliseconds = 1;
    public const int MaximumTimeoutMilliseconds = 600_000;

    public static BleInputStSettings Default { get; } = new();

    public string DeviceNameOrId => string.IsNullOrWhiteSpace(DeviceName) ? DeviceId : DeviceName;

    public static BleInputStSettings FromJson(string settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            return Default;
        }

        using var document = JsonDocument.Parse(settingsJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("BLE Input ST settings must be a JSON object.", nameof(settingsJson));
        }

        var settings = Default;
        if (document.RootElement.TryGetProperty("deviceId", out var deviceId))
        {
            settings = settings with { DeviceId = ReadString(deviceId, "deviceId") };
        }

        if (document.RootElement.TryGetProperty("deviceName", out var deviceName))
        {
            settings = settings with { DeviceName = ReadString(deviceName, "deviceName") };
        }

        if (document.RootElement.TryGetProperty("autoConnect", out var autoConnect))
        {
            settings = settings with { AutoConnect = ReadBoolean(autoConnect, "autoConnect") };
        }

        if (document.RootElement.TryGetProperty("serviceUuid", out var serviceUuid))
        {
            settings = settings with { ServiceUuid = ReadString(serviceUuid, "serviceUuid") };
        }

        if (document.RootElement.TryGetProperty("notifyCharacteristicUuid", out var notifyCharacteristicUuid))
        {
            settings = settings with { NotifyCharacteristicUuid = ReadString(notifyCharacteristicUuid, "notifyCharacteristicUuid") };
        }

        if (document.RootElement.TryGetProperty("channelCount", out var channelCount))
        {
            settings = settings with { ChannelCount = ReadInt32(channelCount, "channelCount") };
        }

        if (document.RootElement.TryGetProperty("timeoutMilliseconds", out var timeoutMilliseconds))
        {
            settings = settings with { TimeoutMilliseconds = ReadInt32(timeoutMilliseconds, "timeoutMilliseconds") };
        }

        settings.Validate();
        return settings;
    }

    public void Validate()
    {
        if (!Guid.TryParse(ServiceUuid, out _))
        {
            throw new ArgumentException("ServiceUuid must be a valid UUID.", nameof(ServiceUuid));
        }

        if (!Guid.TryParse(NotifyCharacteristicUuid, out _))
        {
            throw new ArgumentException("NotifyCharacteristicUuid must be a valid UUID.", nameof(NotifyCharacteristicUuid));
        }

        if (ChannelCount < MinimumChannelCount || ChannelCount > MaximumChannelCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ChannelCount),
                $"ChannelCount must be between {MinimumChannelCount} and {MaximumChannelCount}.");
        }

        if (TimeoutMilliseconds < MinimumTimeoutMilliseconds || TimeoutMilliseconds > MaximumTimeoutMilliseconds)
        {
            throw new ArgumentOutOfRangeException(
                nameof(TimeoutMilliseconds),
                $"TimeoutMilliseconds must be between {MinimumTimeoutMilliseconds} and {MaximumTimeoutMilliseconds}.");
        }
    }

    private static string ReadString(JsonElement value, string propertyName)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException($"BLE Input ST settings field '{propertyName}' must be a string.", nameof(value));
        }

        return value.GetString() ?? string.Empty;
    }

    private static bool ReadBoolean(JsonElement value, string propertyName)
    {
        if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return value.GetBoolean();
        }

        throw new ArgumentException($"BLE Input ST settings field '{propertyName}' must be a boolean.", nameof(value));
    }

    private static int ReadInt32(JsonElement value, string propertyName)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var integer))
        {
            return integer;
        }

        if (value.ValueKind == JsonValueKind.String
            && int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out integer))
        {
            return integer;
        }

        throw new ArgumentException($"BLE Input ST settings field '{propertyName}' must be an integer.", nameof(value));
    }
}