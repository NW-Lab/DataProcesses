using System.Text.Json;

namespace DataProcesses.Nodes.BuiltIn.Blocks.BreathSt;

public enum BreathStDetectionMethod
{
    BreathBelt,
    LedOxygen,
}

/// <summary>
/// Immutable configuration for the BreathSt Block.
/// </summary>
public sealed record BreathStSettings(
    BreathStDetectionMethod Method = BreathStDetectionMethod.BreathBelt,
    bool EmitAnomalyEvents = true,
    double PeakThresholdFraction = 0.55,
    double CoughSpikeThresholdFraction = 0.75,
    double MinimumBreathIntervalMilliseconds = 1_500.0,
    double MaximumBreathIntervalMilliseconds = 10_000.0,
    double CoughRefractoryMilliseconds = 1_000.0)
{
    public static BreathStSettings Default { get; } = new();

    public static BreathStSettings FromJson(string settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            return Default;
        }

        using var document = JsonDocument.Parse(settingsJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("BreathSt settings must be a JSON object.", nameof(settingsJson));
        }

        return Default.ApplyPayload(document.RootElement);
    }

    public BreathStSettings ApplyPayload(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("BreathSt payload must be a JSON object.", nameof(payload));
        }

        var settings = this;

        if (payload.TryGetProperty("method", out var methodElement)
            || payload.TryGetProperty("detectionMethod", out methodElement))
        {
            settings = settings with { Method = ParseMethod(methodElement) };
        }

        if (payload.TryGetProperty("emitAnomalyEvents", out var emitElement))
        {
            settings = settings with { EmitAnomalyEvents = ReadBoolean(emitElement, "emitAnomalyEvents") };
        }

        if (payload.TryGetProperty("peakThresholdFraction", out var peakThresholdElement))
        {
            settings = settings with { PeakThresholdFraction = ReadDouble(peakThresholdElement, "peakThresholdFraction") };
        }

        if (payload.TryGetProperty("coughSpikeThresholdFraction", out var coughThresholdElement))
        {
            settings = settings with { CoughSpikeThresholdFraction = ReadDouble(coughThresholdElement, "coughSpikeThresholdFraction") };
        }

        if (payload.TryGetProperty("minimumBreathIntervalMilliseconds", out var minimumIntervalElement))
        {
            settings = settings with { MinimumBreathIntervalMilliseconds = ReadDouble(minimumIntervalElement, "minimumBreathIntervalMilliseconds") };
        }

        if (payload.TryGetProperty("maximumBreathIntervalMilliseconds", out var maximumIntervalElement))
        {
            settings = settings with { MaximumBreathIntervalMilliseconds = ReadDouble(maximumIntervalElement, "maximumBreathIntervalMilliseconds") };
        }

        if (payload.TryGetProperty("coughRefractoryMilliseconds", out var refractoryElement))
        {
            settings = settings with { CoughRefractoryMilliseconds = ReadDouble(refractoryElement, "coughRefractoryMilliseconds") };
        }

        settings.Validate();
        return settings;
    }

    public void Validate()
    {
        if (!double.IsFinite(PeakThresholdFraction) || PeakThresholdFraction <= 0.0 || PeakThresholdFraction >= 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(PeakThresholdFraction),
                "PeakThresholdFraction must be a finite number greater than 0 and less than 1.");
        }

        if (!double.IsFinite(CoughSpikeThresholdFraction) || CoughSpikeThresholdFraction <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(CoughSpikeThresholdFraction),
                "CoughSpikeThresholdFraction must be a finite positive number.");
        }

        if (!double.IsFinite(MinimumBreathIntervalMilliseconds) || MinimumBreathIntervalMilliseconds <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MinimumBreathIntervalMilliseconds),
                "MinimumBreathIntervalMilliseconds must be a finite positive number.");
        }

        if (!double.IsFinite(MaximumBreathIntervalMilliseconds)
            || MaximumBreathIntervalMilliseconds <= MinimumBreathIntervalMilliseconds)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaximumBreathIntervalMilliseconds),
                "MaximumBreathIntervalMilliseconds must be greater than MinimumBreathIntervalMilliseconds.");
        }

        if (!double.IsFinite(CoughRefractoryMilliseconds) || CoughRefractoryMilliseconds < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(CoughRefractoryMilliseconds),
                "CoughRefractoryMilliseconds must be a finite non-negative number.");
        }
    }

    private static BreathStDetectionMethod ParseMethod(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var numericValue))
        {
            return numericValue switch
            {
                1 => BreathStDetectionMethod.LedOxygen,
                _ => BreathStDetectionMethod.BreathBelt,
            };
        }

        var text = ReadString(element, "method");
        return text.ToLowerInvariant() switch
        {
            "led" or "ledoxygen" or "led_oxygen" or "oxygen" or "spo2" or "bloodoxygen" or "blood_oxygen" => BreathStDetectionMethod.LedOxygen,
            _ => BreathStDetectionMethod.BreathBelt,
        };
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException($"BreathSt setting '{propertyName}' must be a string.", propertyName);
        }

        return element.GetString() ?? string.Empty;
    }

    private static bool ReadBoolean(JsonElement element, string propertyName)
    {
        return element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw new ArgumentException($"BreathSt setting '{propertyName}' must be a boolean.", propertyName),
        };
    }

    private static double ReadDouble(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetDouble(out var value))
        {
            throw new ArgumentException($"BreathSt setting '{propertyName}' must be a number.", propertyName);
        }

        return value;
    }
}