using System.Text.Json;

namespace DataProcesses.Nodes.BuiltIn.Blocks.MovingAverage;

public enum MovingAverageWindowMode
{
    Samples,
    Duration,
}

/// <summary>
/// Immutable configuration for the Moving Average Block.
/// </summary>
public sealed record MovingAverageSettings(
    MovingAverageWindowMode WindowMode = MovingAverageWindowMode.Samples,
    int WindowSize = 10,
    double WindowDurationMilliseconds = 100.0)
{
    public static MovingAverageSettings Default { get; } = new();

    public static MovingAverageSettings FromJson(string settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            return Default;
        }

        using var document = JsonDocument.Parse(settingsJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Moving Average settings must be a JSON object.", nameof(settingsJson));
        }

        var settings = Default;
        var root = document.RootElement;
        if (root.TryGetProperty("windowMode", out var modeElement))
        {
            settings = settings with { WindowMode = ParseWindowMode(modeElement) };
        }

        if (root.TryGetProperty("windowSize", out var sizeElement))
        {
            if (sizeElement.ValueKind != JsonValueKind.Number || !sizeElement.TryGetInt32(out var windowSize))
            {
                throw new ArgumentException("Moving Average setting 'windowSize' must be an integer.", nameof(settingsJson));
            }

            settings = settings with { WindowSize = windowSize };
        }

        if (root.TryGetProperty("windowDurationMilliseconds", out var durationElement))
        {
            if (durationElement.ValueKind != JsonValueKind.Number || !durationElement.TryGetDouble(out var durationMilliseconds))
            {
                throw new ArgumentException("Moving Average setting 'windowDurationMilliseconds' must be a number.", nameof(settingsJson));
            }

            settings = settings with { WindowDurationMilliseconds = durationMilliseconds };
        }

        settings.Validate();
        return settings;
    }

    public void Validate()
    {
        if (WindowSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(WindowSize), "WindowSize must be at least 1.");
        }

        if (!double.IsFinite(WindowDurationMilliseconds) || WindowDurationMilliseconds <= 0.0 || WindowDurationMilliseconds > long.MaxValue / 1_000_000.0)
        {
            throw new ArgumentOutOfRangeException(nameof(WindowDurationMilliseconds), "WindowDurationMilliseconds must be a finite positive duration representable in nanoseconds.");
        }
    }

    private static MovingAverageWindowMode ParseWindowMode(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException("Moving Average setting 'windowMode' must be a string.", nameof(element));
        }

        return element.GetString()?.Trim().ToLowerInvariant() switch
        {
            "samples" => MovingAverageWindowMode.Samples,
            "duration" or "time" => MovingAverageWindowMode.Duration,
            _ => throw new ArgumentException("Moving Average setting 'windowMode' must be 'samples' or 'duration'.", nameof(element)),
        };
    }
}