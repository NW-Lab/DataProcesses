using System.Globalization;
using System.Text.Json;

namespace DataProcesses.Nodes.BuiltIn.Blocks.CsvOutput;

public enum CsvOutputWriteMode
{
    Append,
    NewFile,
}

public sealed record CsvOutputInputBinding(
    string SourceNodeId,
    string SourcePortId,
    string Tag);

public sealed record CsvOutputSettings(
    string FilePath,
    CsvOutputWriteMode WriteMode,
    int SpanMilliseconds,
    long ExecutionSessionId,
    IReadOnlyList<CsvOutputInputBinding> InputBindings)
{
    public static CsvOutputSettings Default { get; } = new(
        FilePath: string.Empty,
        WriteMode: CsvOutputWriteMode.Append,
        SpanMilliseconds: 100,
        ExecutionSessionId: 0,
        InputBindings: []);

    public static CsvOutputSettings FromJson(string settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            return Default;
        }

        using var document = JsonDocument.Parse(settingsJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("CSV Output settings must be a JSON object.", nameof(settingsJson));
        }

        return Default.ApplyPayload(document.RootElement);
    }

    public CsvOutputSettings ApplyPayload(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("CSV Output payload must be a JSON object.", nameof(payload));
        }

        var settings = this;

        if (payload.TryGetProperty("filePath", out var filePath))
        {
            settings = settings with { FilePath = ReadString(filePath, "filePath") };
        }

        if (payload.TryGetProperty("writeMode", out var writeMode))
        {
            settings = settings with { WriteMode = ParseWriteMode(writeMode) };
        }

        if (payload.TryGetProperty("spanMilliseconds", out var spanMilliseconds))
        {
            settings = settings with { SpanMilliseconds = ReadInt32(spanMilliseconds, "spanMilliseconds") };
        }

        if (payload.TryGetProperty("executionSessionId", out var executionSessionId))
        {
            settings = settings with { ExecutionSessionId = ReadInt64(executionSessionId, "executionSessionId") };
        }

        if (payload.TryGetProperty("inputBindings", out var inputBindings))
        {
            settings = settings with { InputBindings = ParseInputBindings(inputBindings) };
        }

        settings.Validate();
        return settings;
    }

    public void Validate()
    {
        if (SpanMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(SpanMilliseconds), "SpanMilliseconds must be positive.");
        }
    }

    private static CsvOutputWriteMode ParseWriteMode(JsonElement value)
    {
        var text = ReadString(value, "writeMode");
        return text.ToLowerInvariant() switch
        {
            "append" => CsvOutputWriteMode.Append,
            "new" => CsvOutputWriteMode.NewFile,
            "overwrite" => CsvOutputWriteMode.NewFile,
            _ => throw new ArgumentException($"Unsupported CSV Output writeMode '{text}'.", nameof(value)),
        };
    }

    private static IReadOnlyList<CsvOutputInputBinding> ParseInputBindings(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException("CSV Output settings field 'inputBindings' must be an array.", nameof(value));
        }

        var bindings = new List<CsvOutputInputBinding>();
        foreach (var element in value.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var sourceNodeId = element.TryGetProperty("sourceNodeId", out var sourceNode)
                ? ReadString(sourceNode, "inputBindings.sourceNodeId")
                : string.Empty;
            var sourcePortId = element.TryGetProperty("sourcePortId", out var sourcePort)
                ? ReadString(sourcePort, "inputBindings.sourcePortId")
                : string.Empty;
            var tag = element.TryGetProperty("tag", out var tagElement)
                ? ReadString(tagElement, "inputBindings.tag")
                : string.Empty;

            if (string.IsNullOrWhiteSpace(sourceNodeId) || string.IsNullOrWhiteSpace(sourcePortId))
            {
                continue;
            }

            bindings.Add(new CsvOutputInputBinding(sourceNodeId.Trim(), sourcePortId.Trim(), tag.Trim()));
        }

        return bindings;
    }

    private static string ReadString(JsonElement value, string propertyName)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException($"CSV Output settings field '{propertyName}' must be a string.", nameof(value));
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

        throw new ArgumentException($"CSV Output settings field '{propertyName}' must be an integer.", nameof(value));
    }

    private static long ReadInt64(JsonElement value, string propertyName)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var integer))
        {
            return integer;
        }

        if (value.ValueKind == JsonValueKind.String
            && long.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out integer))
        {
            return integer;
        }

        throw new ArgumentException($"CSV Output settings field '{propertyName}' must be an integer.", nameof(value));
    }
}
