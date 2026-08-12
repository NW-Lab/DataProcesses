using System.Text.Json;

namespace DataProcesses.Nodes.BuiltIn.Blocks.TestSignalVec;

public enum TestSignalVecWaveType
{
    OneShot,
    Sine,
    Square,
}

public sealed record TestSignalVecSettings(
    bool IsEnabled = true,
    TestSignalVecWaveType WaveType = TestSignalVecWaveType.OneShot,
    double FrequencyHertz = 10.0,
    double Amplitude = 1.0,
    int Length = 16,
    double SamplePeriodMilliseconds = 1.0,
    bool PayloadThrough = true,
    long ExecutionStep = 0)
{
    public const int MinimumLength = 1;
    public const int MaximumLength = 1_024;
    public const int DefaultLength = 16;

    public static TestSignalVecSettings Default { get; } = new();

    public void Validate()
    {
        if (Length < MinimumLength || Length > MaximumLength)
        {
            throw new ArgumentOutOfRangeException(nameof(Length), Length, $"Length must be between {MinimumLength} and {MaximumLength}.");
        }

        if (FrequencyHertz <= 0 || double.IsNaN(FrequencyHertz) || double.IsInfinity(FrequencyHertz))
        {
            throw new ArgumentOutOfRangeException(nameof(FrequencyHertz), FrequencyHertz, "Frequency must be a positive finite number.");
        }

        if (Amplitude < 0 || double.IsNaN(Amplitude) || double.IsInfinity(Amplitude))
        {
            throw new ArgumentOutOfRangeException(nameof(Amplitude), Amplitude, "Amplitude must be zero or greater and finite.");
        }

        if (SamplePeriodMilliseconds <= 0 || double.IsNaN(SamplePeriodMilliseconds) || double.IsInfinity(SamplePeriodMilliseconds))
        {
            throw new ArgumentOutOfRangeException(nameof(SamplePeriodMilliseconds), SamplePeriodMilliseconds, "Sample period must be positive and finite.");
        }
    }

    public static TestSignalVecSettings FromJson(string settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            return Default;
        }

        try
        {
            using var document = JsonDocument.Parse(settingsJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("TestSignalVec settings must be a JSON object.", nameof(settingsJson));
            }

            return new TestSignalVecSettings(
                IsEnabled: GetOptionalBoolean(document.RootElement, "isEnabled", Default.IsEnabled),
                WaveType: GetOptionalWaveType(document.RootElement, "waveType", Default.WaveType),
                FrequencyHertz: GetOptionalDouble(document.RootElement, "frequency", Default.FrequencyHertz),
                Amplitude: GetOptionalDouble(document.RootElement, "amplitude", Default.Amplitude),
                Length: GetOptionalInt(document.RootElement, "length", Default.Length),
                SamplePeriodMilliseconds: GetOptionalDouble(document.RootElement, "samplePeriodMillis", Default.SamplePeriodMilliseconds),
                PayloadThrough: GetOptionalBoolean(document.RootElement, "payloadThrough", Default.PayloadThrough),
                ExecutionStep: GetOptionalLong(document.RootElement, "executionStep", Default.ExecutionStep));
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("TestSignalVec settings JSON is invalid.", nameof(settingsJson), ex);
        }
    }

    public TestSignalVecSettings ApplyPayload(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("TestSignalVec payload must be a JSON object.", nameof(payload));
        }

        return new TestSignalVecSettings(
            IsEnabled: GetOptionalBoolean(payload, "isEnabled", IsEnabled),
            WaveType: GetOptionalWaveType(payload, "waveType", WaveType),
            FrequencyHertz: GetOptionalDouble(payload, "frequency", FrequencyHertz),
            Amplitude: GetOptionalDouble(payload, "amplitude", Amplitude),
            Length: GetOptionalInt(payload, "length", Length),
            SamplePeriodMilliseconds: GetOptionalDouble(payload, "samplePeriodMillis", SamplePeriodMilliseconds),
            PayloadThrough: GetOptionalBoolean(payload, "payloadThrough", PayloadThrough),
            ExecutionStep: GetOptionalLong(payload, "executionStep", ExecutionStep));
    }

    private static bool GetOptionalBoolean(JsonElement element, string propertyName, bool defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        if (property.ValueKind != JsonValueKind.True && property.ValueKind != JsonValueKind.False)
        {
            throw new ArgumentException($"TestSignalVec payload field '{propertyName}' must be a boolean.", nameof(property));
        }

        return property.GetBoolean();
    }

    private static double GetOptionalDouble(JsonElement element, string propertyName, double defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        if (property.ValueKind != JsonValueKind.Number)
        {
            throw new ArgumentException($"TestSignalVec payload field '{propertyName}' must be a number.", nameof(property));
        }

        return property.GetDouble();
    }

    private static int GetOptionalInt(JsonElement element, string propertyName, int defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        if (property.ValueKind != JsonValueKind.Number)
        {
            throw new ArgumentException($"TestSignalVec payload field '{propertyName}' must be a number.", nameof(property));
        }

        return property.GetInt32();
    }

    private static long GetOptionalLong(JsonElement element, string propertyName, long defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        if (property.ValueKind != JsonValueKind.Number || !property.TryGetInt64(out var value))
        {
            throw new ArgumentException($"TestSignalVec payload field '{propertyName}' must be an integer number.", nameof(property));
        }

        return value;
    }

    private static TestSignalVecWaveType GetOptionalWaveType(JsonElement element, string propertyName, TestSignalVecWaveType defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException($"TestSignalVec payload field '{propertyName}' must be a string.", nameof(property));
        }

        return property.GetString() switch
        {
            "oneShot" => TestSignalVecWaveType.OneShot,
            "oneshot" => TestSignalVecWaveType.OneShot,
            "sine" => TestSignalVecWaveType.Sine,
            "square" => TestSignalVecWaveType.Square,
            _ => throw new ArgumentException($"Unsupported TestSignalVec waveType '{property.GetString()}'.", nameof(property)),
        };
    }
}
