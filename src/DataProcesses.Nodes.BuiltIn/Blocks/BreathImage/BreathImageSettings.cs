using System.Text.Json;

namespace DataProcesses.Nodes.BuiltIn.Blocks.BreathImage;

/// <summary>
/// Immutable configuration for the BreathImage Block.
/// </summary>
public sealed record BreathImageSettings(
    double RegionScale = 0.55,
    int MinimumSampleCount = 90,
    double WindowSeconds = 20.0,
    double MinimumBreathRateBpm = 6.0,
    double MaximumBreathRateBpm = 30.0,
    double DefaultFrameRateHertz = 30.0)
{
    public static BreathImageSettings Default { get; } = new();

    public static BreathImageSettings FromJson(string settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            return Default;
        }

        using var document = JsonDocument.Parse(settingsJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("BreathImage settings must be a JSON object.", nameof(settingsJson));
        }

        return Default.ApplyPayload(document.RootElement);
    }

    public BreathImageSettings ApplyPayload(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("BreathImage payload must be a JSON object.", nameof(payload));
        }

        var settings = this;

        if (payload.TryGetProperty("regionScale", out var regionScaleElement))
        {
            settings = settings with { RegionScale = ReadDouble(regionScaleElement, "regionScale") };
        }

        if (payload.TryGetProperty("minimumSampleCount", out var minimumSampleCountElement))
        {
            settings = settings with { MinimumSampleCount = ReadInt32(minimumSampleCountElement, "minimumSampleCount") };
        }

        if (payload.TryGetProperty("windowSeconds", out var windowSecondsElement))
        {
            settings = settings with { WindowSeconds = ReadDouble(windowSecondsElement, "windowSeconds") };
        }

        if (payload.TryGetProperty("minimumBreathRateBpm", out var minimumBreathRateElement))
        {
            settings = settings with { MinimumBreathRateBpm = ReadDouble(minimumBreathRateElement, "minimumBreathRateBpm") };
        }

        if (payload.TryGetProperty("maximumBreathRateBpm", out var maximumBreathRateElement))
        {
            settings = settings with { MaximumBreathRateBpm = ReadDouble(maximumBreathRateElement, "maximumBreathRateBpm") };
        }

        if (payload.TryGetProperty("defaultFrameRateHertz", out var defaultFrameRateElement))
        {
            settings = settings with { DefaultFrameRateHertz = ReadDouble(defaultFrameRateElement, "defaultFrameRateHertz") };
        }

        settings.Validate();
        return settings;
    }

    public void Validate()
    {
        if (!double.IsFinite(RegionScale) || RegionScale <= 0.0 || RegionScale > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(RegionScale),
                "RegionScale must be a finite number greater than 0 and less than or equal to 1.");
        }

        if (MinimumSampleCount < 8)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MinimumSampleCount),
                "MinimumSampleCount must be 8 or greater.");
        }

        if (!double.IsFinite(WindowSeconds) || WindowSeconds <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(WindowSeconds),
                "WindowSeconds must be a finite positive number.");
        }

        if (!double.IsFinite(MinimumBreathRateBpm) || MinimumBreathRateBpm <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MinimumBreathRateBpm),
                "MinimumBreathRateBpm must be a finite positive number.");
        }

        if (!double.IsFinite(MaximumBreathRateBpm) || MaximumBreathRateBpm <= MinimumBreathRateBpm)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaximumBreathRateBpm),
                "MaximumBreathRateBpm must be greater than MinimumBreathRateBpm.");
        }

        if (!double.IsFinite(DefaultFrameRateHertz) || DefaultFrameRateHertz <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(DefaultFrameRateHertz),
                "DefaultFrameRateHertz must be a finite positive number.");
        }
    }

    private static int ReadInt32(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out var value))
        {
            throw new ArgumentException($"BreathImage setting '{propertyName}' must be an integer.", propertyName);
        }

        return value;
    }

    private static double ReadDouble(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetDouble(out var value))
        {
            throw new ArgumentException($"BreathImage setting '{propertyName}' must be a number.", propertyName);
        }

        return value;
    }
}