using System.Text.Json;

using DataProcesses.Nodes.BuiltIn.Blocks.BleInputTS;

namespace DataProcesses.Nodes.BuiltIn.Blocks.BleInputFS;

public enum BleInputFSFormat { Csv, Binary }
public enum BleInputFSNumericType { Int16, Int32, Float32, Float64 }
public enum BleInputFSByteOrder { LittleEndian, BigEndian }

public sealed record BleInputFSSettings(
    string DeviceId = "",
    string DeviceName = "",
    bool AutoConnect = true,
    string ServiceUuid = BleInputTSSettings.NordicUartServiceUuid,
    string NotifyCharacteristicUuid = BleInputTSSettings.NordicUartTxCharacteristicUuid,
    int ChannelCount = 2,
    int TimeoutMilliseconds = 5000,
    int SampleRateHertz = 1000,
    BleInputFSFormat Format = BleInputFSFormat.Csv,
    BleInputFSNumericType NumericType = BleInputFSNumericType.Float32,
    BleInputFSByteOrder ByteOrder = BleInputFSByteOrder.LittleEndian) : IBleInputGattSettings
{
    public const int MinimumSampleRateHertz = 1;
    public const int MaximumSampleRateHertz = 1_000_000;
    public static BleInputFSSettings Default { get; } = new();
    public string DeviceNameOrId => string.IsNullOrWhiteSpace(DeviceName) ? DeviceId : DeviceName;

    public static BleInputFSSettings FromJson(string settingsJson)
    {
        var gatt = BleInputTSSettings.FromJson(settingsJson);
        var settings = new BleInputFSSettings(
            gatt.DeviceId, gatt.DeviceName, gatt.AutoConnect, gatt.ServiceUuid,
            gatt.NotifyCharacteristicUuid, gatt.ChannelCount, gatt.TimeoutMilliseconds);
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            return settings;
        }

        using var document = JsonDocument.Parse(settingsJson);
        var root = document.RootElement;
        if (root.TryGetProperty("sampleRateHertz", out var rate))
        {
            if (rate.ValueKind != JsonValueKind.Number || !rate.TryGetInt32(out var integer))
            {
                throw new ArgumentException("BleInputFS sampleRateHertz must be an integer.", nameof(settingsJson));
            }

            settings = settings with { SampleRateHertz = integer };
        }

        settings = settings with
        {
            Format = ReadEnum(root, "format", settings.Format),
            NumericType = ReadEnum(root, "numericType", settings.NumericType),
            ByteOrder = ReadEnum(root, "byteOrder", settings.ByteOrder),
        };
        settings.Validate();
        return settings;
    }

    public void Validate()
    {
        new BleInputTSSettings(DeviceId, DeviceName, AutoConnect, ServiceUuid,
            NotifyCharacteristicUuid, ChannelCount, TimeoutMilliseconds).Validate();
        ArgumentNullException.ThrowIfNull(DeviceId);
        ArgumentNullException.ThrowIfNull(DeviceName);
        if (SampleRateHertz is < MinimumSampleRateHertz or > MaximumSampleRateHertz)
        {
            throw new ArgumentOutOfRangeException(nameof(SampleRateHertz));
        }

        if (!Enum.IsDefined(Format) || !Enum.IsDefined(NumericType) || !Enum.IsDefined(ByteOrder))
        {
            throw new ArgumentException("BleInputFS format, numeric type, and byte order must be supported values.");
        }
    }

    private static T ReadEnum<T>(JsonElement root, string name, T defaultValue) where T : struct, Enum
    {
        if (!root.TryGetProperty(name, out var value))
        {
            return defaultValue;
        }

        if (value.ValueKind == JsonValueKind.String
            && Enum.TryParse<T>(value.GetString(), ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed)
            && Enum.GetNames<T>().Contains(value.GetString(), StringComparer.OrdinalIgnoreCase))
        {
            return parsed;
        }

        throw new ArgumentException($"BleInputFS '{name}' must be a supported enum name.", nameof(root));
    }
}
