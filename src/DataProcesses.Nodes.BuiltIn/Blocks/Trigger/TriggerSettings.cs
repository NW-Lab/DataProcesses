using System.Globalization;
using System.Text.Json;

namespace DataProcesses.Nodes.BuiltIn.Blocks.Trigger;

public enum TriggerPayloadValueType
{
    DateTime,
    Boolean,
    String,
    NumberArray,
    Number,
}

public sealed record TriggerSettings(
    string Topic = "dataprocesses.trigger",
    string PayloadPath = "payload.value",
    TriggerPayloadValueType PayloadValueType = TriggerPayloadValueType.DateTime,
    bool BoolValue = true,
    string StringValue = "trigger",
    double NumberValue = 1.0,
    string NumberArrayText = "1,2,3",
    bool EmitOnExecutionStart = true,
    bool EmitPeriodically = false,
    double InitialDelayMilliseconds = 0,
    double RepeatIntervalMilliseconds = 1000,
    long ExecutionSessionId = 0,
    long ManualTriggerNonce = 0)
{
    public static TriggerSettings Default { get; } = new();

    public static TriggerSettings FromJson(string settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            return Default;
        }

        using var document = JsonDocument.Parse(settingsJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Trigger settings must be a JSON object.", nameof(settingsJson));
        }

        return Default.ApplyPayload(document.RootElement);
    }

    public TriggerSettings ApplyPayload(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Trigger settings payload must be a JSON object.", nameof(payload));
        }

        var settings = this;

        if (payload.TryGetProperty("topic", out var topic))
        {
            settings = settings with { Topic = ReadString(topic, "topic") };
        }

        if (payload.TryGetProperty("payloadPath", out var payloadPath))
        {
            settings = settings with { PayloadPath = ReadString(payloadPath, "payloadPath") };
        }

        if (payload.TryGetProperty("payloadValueType", out var payloadValueType))
        {
            settings = settings with { PayloadValueType = ParsePayloadValueType(payloadValueType) };
        }

        if (payload.TryGetProperty("boolValue", out var boolValue))
        {
            settings = settings with { BoolValue = ReadBoolean(boolValue, "boolValue") };
        }

        if (payload.TryGetProperty("stringValue", out var stringValue))
        {
            settings = settings with { StringValue = ReadString(stringValue, "stringValue") };
        }

        if (payload.TryGetProperty("numberValue", out var numberValue))
        {
            settings = settings with { NumberValue = ReadDouble(numberValue, "numberValue") };
        }

        if (payload.TryGetProperty("numberArrayText", out var numberArrayText))
        {
            settings = settings with { NumberArrayText = ReadString(numberArrayText, "numberArrayText") };
        }

        if (payload.TryGetProperty("emitOnExecutionStart", out var emitOnExecutionStart))
        {
            settings = settings with { EmitOnExecutionStart = ReadBoolean(emitOnExecutionStart, "emitOnExecutionStart") };
        }

        if (payload.TryGetProperty("emitPeriodically", out var emitPeriodically))
        {
            settings = settings with { EmitPeriodically = ReadBoolean(emitPeriodically, "emitPeriodically") };
        }

        if (payload.TryGetProperty("initialDelayMilliseconds", out var initialDelayMilliseconds))
        {
            settings = settings with { InitialDelayMilliseconds = ReadDouble(initialDelayMilliseconds, "initialDelayMilliseconds") };
        }

        if (payload.TryGetProperty("repeatIntervalMilliseconds", out var repeatIntervalMilliseconds))
        {
            settings = settings with { RepeatIntervalMilliseconds = ReadDouble(repeatIntervalMilliseconds, "repeatIntervalMilliseconds") };
        }

        if (payload.TryGetProperty("executionSessionId", out var executionSessionId))
        {
            settings = settings with { ExecutionSessionId = ReadInt64(executionSessionId, "executionSessionId") };
        }

        if (payload.TryGetProperty("manualTriggerNonce", out var manualTriggerNonce))
        {
            settings = settings with { ManualTriggerNonce = ReadInt64(manualTriggerNonce, "manualTriggerNonce") };
        }

        settings.Validate();
        return settings;
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Topic))
        {
            throw new ArgumentException("Topic must not be empty.", nameof(Topic));
        }

        if (string.IsNullOrWhiteSpace(PayloadPath))
        {
            throw new ArgumentException("PayloadPath must not be empty.", nameof(PayloadPath));
        }

        if (!double.IsFinite(NumberValue))
        {
            throw new ArgumentOutOfRangeException(nameof(NumberValue), "NumberValue must be finite.");
        }

        if (!double.IsFinite(InitialDelayMilliseconds) || InitialDelayMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(InitialDelayMilliseconds), "InitialDelayMilliseconds must be finite and non-negative.");
        }

        if (!double.IsFinite(RepeatIntervalMilliseconds) || RepeatIntervalMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(RepeatIntervalMilliseconds), "RepeatIntervalMilliseconds must be finite and positive.");
        }

        _ = ParseNumberArray();
    }

    public IReadOnlyList<double> ParseNumberArray()
    {
        if (string.IsNullOrWhiteSpace(NumberArrayText))
        {
            return [];
        }

        var tokens = NumberArrayText
            .Split([',', ' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var values = new List<double>(tokens.Length);
        foreach (var token in tokens)
        {
            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !double.IsFinite(value))
            {
                throw new ArgumentException($"Trigger numberArrayText contains an invalid number '{token}'.", nameof(NumberArrayText));
            }

            values.Add(value);
        }

        return values;
    }

    private static TriggerPayloadValueType ParsePayloadValueType(JsonElement value)
    {
        var text = ReadString(value, "payloadValueType");
        return text.ToLowerInvariant() switch
        {
            "datetime" => TriggerPayloadValueType.DateTime,
            "boolean" => TriggerPayloadValueType.Boolean,
            "string" => TriggerPayloadValueType.String,
            "numberarray" => TriggerPayloadValueType.NumberArray,
            "number" => TriggerPayloadValueType.Number,
            _ => throw new ArgumentException($"Unsupported Trigger payloadValueType '{text}'.", nameof(value)),
        };
    }

    private static string ReadString(JsonElement value, string propertyName)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException($"Trigger settings field '{propertyName}' must be a string.", nameof(value));
        }

        return value.GetString() ?? string.Empty;
    }

    private static bool ReadBoolean(JsonElement value, string propertyName)
    {
        if (value.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            throw new ArgumentException($"Trigger settings field '{propertyName}' must be a boolean.", nameof(value));
        }

        return value.GetBoolean();
    }

    private static double ReadDouble(JsonElement value, string propertyName)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out var parsed))
        {
            throw new ArgumentException($"Trigger settings field '{propertyName}' must be a number.", nameof(value));
        }

        return parsed;
    }

    private static long ReadInt64(JsonElement value, string propertyName)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var parsed))
        {
            throw new ArgumentException($"Trigger settings field '{propertyName}' must be an integer.", nameof(value));
        }

        return parsed;
    }
}
