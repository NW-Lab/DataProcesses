using System.Text.Json;

namespace DataProcesses.Nodes.BuiltIn.Blocks.TestSignalFS;

public enum TestSignalFSWaveType
{
    Sine,
    Square,
}

public sealed record TestSignalFSSettings(
    TestSignalFSWaveType WaveType = TestSignalFSWaveType.Sine,
    double FrequencyHertz = 440.0,
    double PeakIntensity = 1.0,
    int SampleRateHertz = 44_100)
{
    public const int SampleCount = 256;
    public const int MaximumSampleRateHertz = 1_000_000;

    public static TestSignalFSSettings Default { get; } = new();

    public static TestSignalFSSettings FromJson(string settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            return Default;
        }

        using var document = JsonDocument.Parse(settingsJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("TestSignal(FS) settings must be a JSON object.", nameof(settingsJson));
        }

        var settings = Default;
        var root = document.RootElement;
        if (root.TryGetProperty("waveType", out var waveTypeElement))
        {
            settings = settings with { WaveType = ParseWaveType(waveTypeElement) };
        }

        if (root.TryGetProperty("frequency", out var frequencyElement))
        {
            settings = settings with { FrequencyHertz = ReadDouble(frequencyElement, "frequency") };
        }

        if (root.TryGetProperty("peakIntensity", out var peakIntensityElement))
        {
            settings = settings with { PeakIntensity = ReadDouble(peakIntensityElement, "peakIntensity") };
        }

        if (root.TryGetProperty("sampleRateHertz", out var sampleRateElement))
        {
            if (sampleRateElement.ValueKind != JsonValueKind.Number || !sampleRateElement.TryGetInt32(out var sampleRateHertz))
            {
                throw new ArgumentException("TestSignal(FS) setting 'sampleRateHertz' must be an integer.", nameof(settingsJson));
            }

            settings = settings with { SampleRateHertz = sampleRateHertz };
        }

        settings.Validate();
        return settings;
    }

    public void Validate()
    {
        if (WaveType is not TestSignalFSWaveType.Sine and not TestSignalFSWaveType.Square)
        {
            throw new ArgumentOutOfRangeException(nameof(WaveType), "Wave type must be sine or square.");
        }

        if (!double.IsFinite(FrequencyHertz) || FrequencyHertz <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(FrequencyHertz), "Frequency must be a positive finite value.");
        }

        if (SampleRateHertz < 1 || SampleRateHertz > MaximumSampleRateHertz)
        {
            throw new ArgumentOutOfRangeException(nameof(SampleRateHertz), SampleRateHertz, $"Sample rate must be between 1 and {MaximumSampleRateHertz} Hz.");
        }

        if (FrequencyHertz > SampleRateHertz / 2.0)
        {
            throw new ArgumentOutOfRangeException(nameof(FrequencyHertz), "Signal frequency must not exceed the sample rate's Nyquist frequency.");
        }

        if (!double.IsFinite(PeakIntensity) || PeakIntensity < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(PeakIntensity), "Peak intensity must be a non-negative finite value.");
        }
    }

    private static TestSignalFSWaveType ParseWaveType(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException("TestSignal(FS) setting 'waveType' must be a string.", nameof(value));
        }

        return value.GetString()?.Trim().ToLowerInvariant() switch
        {
            "sine" or "sin" => TestSignalFSWaveType.Sine,
            "square" => TestSignalFSWaveType.Square,
            var unsupported => throw new ArgumentException($"Unsupported TestSignal(FS) wave type '{unsupported}'.", nameof(value)),
        };
    }

    private static double ReadDouble(JsonElement value, string propertyName)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out var result))
        {
            throw new ArgumentException($"TestSignal(FS) setting '{propertyName}' must be a number.", nameof(value));
        }

        return result;
    }
}