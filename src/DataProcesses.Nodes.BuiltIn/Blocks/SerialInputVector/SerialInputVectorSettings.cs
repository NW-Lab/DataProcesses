using System.Globalization;
using System.Text.Json;

namespace DataProcesses.Nodes.BuiltIn.Blocks.SerialInputVector;

public sealed record SerialInputVectorSettings(
    string ComPortName = "COM3",
    int BaudRate = 115200)
{
    public static SerialInputVectorSettings Default { get; } = new();

    public static SerialInputVectorSettings FromJson(string settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            return Default;
        }

        using var document = JsonDocument.Parse(settingsJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Serial Input Vector settings must be a JSON object.", nameof(settingsJson));
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
    }

    private static string ReadString(JsonElement value, string propertyName)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException($"Serial Input Vector settings field '{propertyName}' must be a string.", nameof(value));
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

        throw new ArgumentException($"Serial Input Vector settings field '{propertyName}' must be an integer.", nameof(value));
    }
}