using System.Text.Json;

namespace DataProcesses.Nodes.BuiltIn.Blocks.MicInputFS;

public enum MicInputFSChannelMode
{
    Stereo,
    Left,
    Right,
}

public sealed record MicInputFSSettings(
    int SampleRateHertz = 44_100,
    MicInputFSChannelMode ChannelMode = MicInputFSChannelMode.Stereo)
{
    public const int MinimumSampleRateHertz = 8_000;
    public const int MaximumSampleRateHertz = 192_000;
    public const int SamplesPerFrame = 4_096;

    public static MicInputFSSettings Default { get; } = new();

    public static IReadOnlyList<string> ChannelModeOptions { get; } = ["Stereo", "Left", "Right"];

    public static IReadOnlyList<int> SampleRateOptions { get; } = [16_000, 22_050, 32_000, 44_100, 48_000, 96_000, 192_000];

    public static MicInputFSSettings FromJson(string settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            return Default;
        }

        using var document = JsonDocument.Parse(settingsJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("MicInputFS settings must be a JSON object.", nameof(settingsJson));
        }

        var settings = Default;
        var root = document.RootElement;
        if (root.TryGetProperty("sampleRateHertz", out var sampleRateElement))
        {
            if (sampleRateElement.ValueKind != JsonValueKind.Number || !sampleRateElement.TryGetInt32(out var sampleRateHertz))
            {
                throw new ArgumentException("MicInputFS setting 'sampleRateHertz' must be an integer.", nameof(settingsJson));
            }

            settings = settings with { SampleRateHertz = sampleRateHertz };
        }

        if (root.TryGetProperty("channelMode", out var channelModeElement))
        {
            if (channelModeElement.ValueKind != JsonValueKind.String)
            {
                throw new ArgumentException("MicInputFS setting 'channelMode' must be a string.", nameof(settingsJson));
            }

            settings = settings with { ChannelMode = ParseChannelMode(channelModeElement.GetString()) };
        }

        settings.Validate();
        return settings;
    }

    public void Validate()
    {
        if (SampleRateHertz < MinimumSampleRateHertz || SampleRateHertz > MaximumSampleRateHertz)
        {
            throw new ArgumentOutOfRangeException(
                nameof(SampleRateHertz),
                SampleRateHertz,
                $"Sample rate must be between {MinimumSampleRateHertz} and {MaximumSampleRateHertz} Hz.");
        }

        if (ChannelMode is not MicInputFSChannelMode.Stereo and not MicInputFSChannelMode.Left and not MicInputFSChannelMode.Right)
        {
            throw new ArgumentOutOfRangeException(nameof(ChannelMode), "Channel mode must be Stereo, Left, or Right.");
        }
    }

    private static MicInputFSChannelMode ParseChannelMode(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "stereo" => MicInputFSChannelMode.Stereo,
            "left" => MicInputFSChannelMode.Left,
            "right" => MicInputFSChannelMode.Right,
            _ => throw new ArgumentException($"Unsupported MicInputFS channel mode '{value}'.", nameof(value)),
        };
    }
}