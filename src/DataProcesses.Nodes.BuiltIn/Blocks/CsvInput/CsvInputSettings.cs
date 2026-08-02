using System.Globalization;
using System.Text.Json;

namespace DataProcesses.Nodes.BuiltIn.Blocks.CsvInput;

public enum CsvInputSourceType
{
    File,
    ComPort,
}

public enum CsvFilePlaybackMode
{
    Immediate,
    Millis,
}

public sealed record CsvInputSettings(
    int OutputCount = 2,
    CsvInputSourceType SourceType = CsvInputSourceType.File,
    string FilePath = "",
    CsvFilePlaybackMode FilePlaybackMode = CsvFilePlaybackMode.Immediate,
    string ComPortName = "COM3",
    int BaudRate = 115200,
    bool HasHeaderRow = true)
{
    public static CsvInputSettings Default { get; } = new();

    public static CsvInputSettings FromJson(string settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            return Default;
        }

        using var document = JsonDocument.Parse(settingsJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("CSV Input settings must be a JSON object.", nameof(settingsJson));
        }

        return Default.ApplyPayload(document.RootElement);
    }

    public CsvInputSettings ApplyPayload(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("CSV Input payload must be a JSON object.", nameof(payload));
        }

        var settings = this;

        if (payload.TryGetProperty("outputCount", out var outputCount))
        {
            settings = settings with { OutputCount = ReadInt32(outputCount, "outputCount") };
        }

        if (payload.TryGetProperty("sourceType", out var sourceType))
        {
            settings = settings with { SourceType = ParseSourceType(sourceType) };
        }

        if (payload.TryGetProperty("filePath", out var filePath))
        {
            settings = settings with { FilePath = ReadString(filePath, "filePath") };
        }

        if (payload.TryGetProperty("filePlaybackMode", out var filePlaybackMode))
        {
            settings = settings with { FilePlaybackMode = ParsePlaybackMode(filePlaybackMode) };
        }

        if (payload.TryGetProperty("comPortName", out var comPortName))
        {
            settings = settings with { ComPortName = ReadString(comPortName, "comPortName") };
        }

        if (payload.TryGetProperty("baudRate", out var baudRate))
        {
            settings = settings with { BaudRate = ReadInt32(baudRate, "baudRate") };
        }

        if (payload.TryGetProperty("hasHeaderRow", out var hasHeaderRow))
        {
            settings = settings with { HasHeaderRow = ReadBoolean(hasHeaderRow, "hasHeaderRow") };
        }

        settings.Validate();
        return settings;
    }

    public void Validate()
    {
        if (OutputCount < 1 || OutputCount > CsvInputBlock.MaxStreamOutputs)
        {
            throw new ArgumentOutOfRangeException(nameof(OutputCount),
                $"OutputCount must be between 1 and {CsvInputBlock.MaxStreamOutputs}.");
        }

        if (SourceType == CsvInputSourceType.ComPort && string.IsNullOrWhiteSpace(ComPortName))
        {
            throw new ArgumentException("ComPortName must not be empty when sourceType is com.", nameof(ComPortName));
        }

        if (BaudRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(BaudRate), "BaudRate must be positive.");
        }
    }

    private static CsvInputSourceType ParseSourceType(JsonElement value)
    {
        var text = ReadString(value, "sourceType");
        return text.ToLowerInvariant() switch
        {
            "file" => CsvInputSourceType.File,
            "com" => CsvInputSourceType.ComPort,
            "comport" => CsvInputSourceType.ComPort,
            _ => throw new ArgumentException($"Unsupported CSV Input sourceType '{text}'.", nameof(value)),
        };
    }

    private static CsvFilePlaybackMode ParsePlaybackMode(JsonElement value)
    {
        var text = ReadString(value, "filePlaybackMode");
        return text.ToLowerInvariant() switch
        {
            "immediate" => CsvFilePlaybackMode.Immediate,
            "millis" => CsvFilePlaybackMode.Millis,
            _ => throw new ArgumentException($"Unsupported CSV Input filePlaybackMode '{text}'.", nameof(value)),
        };
    }

    private static string ReadString(JsonElement value, string propertyName)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException($"CSV Input settings field '{propertyName}' must be a string.", nameof(value));
        }

        return value.GetString() ?? string.Empty;
    }

    private static bool ReadBoolean(JsonElement value, string propertyName)
    {
        if (value.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            throw new ArgumentException($"CSV Input settings field '{propertyName}' must be a boolean.", nameof(value));
        }

        return value.GetBoolean();
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

        throw new ArgumentException($"CSV Input settings field '{propertyName}' must be an integer.", nameof(value));
    }
}
