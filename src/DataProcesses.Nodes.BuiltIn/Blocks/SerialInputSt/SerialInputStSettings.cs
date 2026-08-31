using System.Globalization;
using System.Text.Json;

namespace DataProcesses.Nodes.BuiltIn.Blocks.SerialInputSt;

public sealed record SerialInputStSettings(
    string ComPortName = "COM3",
    int BaudRate = 115200,
    int ChannelCount = 2)
{
    public const int MinimumChannelCount = 1;
    public const int MaximumChannelCount = 16;

    public static SerialInputStSettings Default { get; } = new();

    public static SerialInputStSettings FromJson(string settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            return Default;
        }

        using var document = JsonDocument.Parse(settingsJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Serial Input ST settings must be a JSON object.", nameof(settingsJson));
        }

        var settings = Default;
        if (document.RootElement.TryGetProperty("comPortName", out var comPortName))
        {
            settings = settings with { ComPortName = ReadString(comPortName, "comPortName") };
        }

        if (document.RootElement.TryGetProperty("baudRate", out var baudRate))
        {
            settings = settings with { BaudRate = ReadInt32(baudRate, "baudRate") };
        }

        if (document.RootElement.TryGetProperty("channelCount", out var channelCount))
        {
            settings = settings with { ChannelCount = ReadInt32(channelCount, "channelCount") };
        }

        settings.Validate();
        return settings;
    }

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ComPortName);

        if (BaudRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(BaudRate), "BaudRate must be positive.");
        }

        if (ChannelCount < MinimumChannelCount || ChannelCount > MaximumChannelCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ChannelCount),
                $"ChannelCount must be between {MinimumChannelCount} and {MaximumChannelCount}.");
        }
    }

    private static string ReadString(JsonElement value, string propertyName)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException($"Serial Input ST settings field '{propertyName}' must be a string.", nameof(value));
        }

        return value.GetString() ?? string.Empty;
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

        throw new ArgumentException($"Serial Input ST settings field '{propertyName}' must be an integer.", nameof(value));
    }
}