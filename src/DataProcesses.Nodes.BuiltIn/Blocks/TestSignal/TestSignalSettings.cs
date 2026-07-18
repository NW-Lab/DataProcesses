using System.Text.Json;

namespace DataProcesses.Nodes.BuiltIn.Blocks.TestSignal;

public enum TestSignalWaveType
{
    Sine,
    Square,
}

public sealed record TestSignalSettings(
    TestSignalWaveType WaveType = TestSignalWaveType.Sine,
    double FrequencyHertz = 10.0,
    double Amplitude = 1.0,
    bool IsEnabled = true,
    bool PayloadThrough = true)
{
    public const int SampleRateHertz = 1_000;
    public const int SampleCount = 256;

    public static TestSignalSettings Default { get; } = new();

    public static TestSignalSettings FromJson(string settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            return Default;
        }

        using var document = JsonDocument.Parse(settingsJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Test Signal settings must be a JSON object.", nameof(settingsJson));
        }

        return Default.ApplyPayload(document.RootElement);
    }

    public TestSignalSettings ApplyPayload(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Test Signal payload must be a JSON object.", nameof(payload));
        }

        var settings = this;

        if (payload.TryGetProperty("waveType", out var waveType))
        {
            settings = settings with { WaveType = ParseWaveType(waveType) };
        }

        if (payload.TryGetProperty("frequency", out var frequency))
        {
            settings = settings with { FrequencyHertz = ReadDouble(frequency, "frequency") };
        }

        if (payload.TryGetProperty("amplitude", out var amplitude))
        {
            settings = settings with { Amplitude = ReadDouble(amplitude, "amplitude") };
        }

        if (payload.TryGetProperty("isEnabled", out var isEnabled))
        {
            if (isEnabled.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
            {
                throw new ArgumentException("Test Signal payload field 'isEnabled' must be a boolean.", nameof(payload));
            }

            settings = settings with { IsEnabled = isEnabled.GetBoolean() };
        }

        if (payload.TryGetProperty("payloadThrough", out var payloadThrough))
        {
            if (payloadThrough.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
            {
                throw new ArgumentException("Test Signal payload field 'payloadThrough' must be a boolean.", nameof(payload));
            }

            settings = settings with { PayloadThrough = payloadThrough.GetBoolean() };
        }

        settings.Validate();
        return settings;
    }

    public void Validate()
    {
        if (!double.IsFinite(FrequencyHertz) || FrequencyHertz <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(FrequencyHertz), "Frequency must be a positive finite value.");
        }

        if (!double.IsFinite(Amplitude) || Amplitude < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Amplitude), "Amplitude must be a non-negative finite value.");
        }
    }

    private static TestSignalWaveType ParseWaveType(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException("Test Signal payload field 'waveType' must be a string.", nameof(value));
        }

        var text = value.GetString();
        if (string.Equals(text, "sine", StringComparison.OrdinalIgnoreCase)
            || string.Equals(text, "sin", StringComparison.OrdinalIgnoreCase))
        {
            return TestSignalWaveType.Sine;
        }

        if (string.Equals(text, "square", StringComparison.OrdinalIgnoreCase))
        {
            return TestSignalWaveType.Square;
        }

        throw new ArgumentException($"Unsupported Test Signal waveType '{text}'.", nameof(value));
    }

    private static double ReadDouble(JsonElement value, string propertyName)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out var result))
        {
            throw new ArgumentException($"Test Signal payload field '{propertyName}' must be a number.", nameof(value));
        }

        return result;
    }
}