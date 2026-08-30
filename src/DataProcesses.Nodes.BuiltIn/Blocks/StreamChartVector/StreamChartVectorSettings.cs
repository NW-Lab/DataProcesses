using System.Text.Json;

namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamChartVector;

/// <summary>
/// Immutable rendering settings for the StreamChartVector Block.
/// </summary>
/// <param name="ColorMap">Palette used to map intensity to color.</param>
/// <param name="AutoScale">Derive the intensity range from the visible window instead of the fixed range.</param>
/// <param name="MinimumValue">Fixed lower intensity bound used when <paramref name="AutoScale"/> is disabled.</param>
/// <param name="MaximumValue">Fixed upper intensity bound used when <paramref name="AutoScale"/> is disabled.</param>
/// <param name="Interpolate">Blend between adjacent samples instead of holding the previous value.</param>
/// <param name="TimeSpanMilliseconds">Width of the visible time window on the horizontal axis.</param>
public sealed record StreamChartVectorSettings(
    StreamChartVectorColorMap ColorMap = StreamChartVectorColorMap.Jet,
    bool AutoScale = true,
    double MinimumValue = 0.0,
    double MaximumValue = 1.0,
    bool Interpolate = true,
    double TimeSpanMilliseconds = 5_000)
{
    public const double DefaultTimeSpanMilliseconds = 5_000;
    public const double MinimumTimeSpanMilliseconds = 100;
    public const double MaximumTimeSpanMilliseconds = 600_000;

    public static StreamChartVectorSettings Default { get; } = new();

    public void Validate()
    {
        if (!double.IsFinite(TimeSpanMilliseconds)
            || TimeSpanMilliseconds < MinimumTimeSpanMilliseconds
            || TimeSpanMilliseconds > MaximumTimeSpanMilliseconds)
        {
            throw new ArgumentOutOfRangeException(
                nameof(TimeSpanMilliseconds),
                TimeSpanMilliseconds,
                $"Time span must be between {MinimumTimeSpanMilliseconds} and {MaximumTimeSpanMilliseconds} milliseconds.");
        }

        if (!double.IsFinite(MinimumValue))
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumValue), MinimumValue, "Minimum value must be finite.");
        }

        if (!double.IsFinite(MaximumValue))
        {
            throw new ArgumentOutOfRangeException(nameof(MaximumValue), MaximumValue, "Maximum value must be finite.");
        }

        if (!AutoScale && MaximumValue <= MinimumValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaximumValue),
                MaximumValue,
                "Maximum value must be greater than the minimum value when auto scale is disabled.");
        }
    }

    public static StreamChartVectorSettings FromJson(string? settingsJson)
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
                throw new ArgumentException("StreamChartVector settings must be a JSON object.", nameof(settingsJson));
            }

            var settings = new StreamChartVectorSettings(
                ColorMap: GetOptionalColorMap(document.RootElement, "colorMap", Default.ColorMap),
                AutoScale: GetOptionalBoolean(document.RootElement, "autoScale", Default.AutoScale),
                MinimumValue: GetOptionalDouble(document.RootElement, "minValue", Default.MinimumValue),
                MaximumValue: GetOptionalDouble(document.RootElement, "maxValue", Default.MaximumValue),
                Interpolate: GetOptionalBoolean(document.RootElement, "interpolate", Default.Interpolate),
                TimeSpanMilliseconds: GetOptionalDouble(document.RootElement, "timeSpanMillis", Default.TimeSpanMilliseconds));

            settings.Validate();
            return settings;
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("StreamChartVector settings JSON is invalid.", nameof(settingsJson), ex);
        }
    }

    public static string ToColorMapName(StreamChartVectorColorMap colorMap) => colorMap switch
    {
        StreamChartVectorColorMap.Grayscale => "grayscale",
        StreamChartVectorColorMap.Hot => "hot",
        StreamChartVectorColorMap.Viridis => "viridis",
        _ => "jet",
    };

    public static StreamChartVectorColorMap ParseColorMap(string? name) => name?.Trim().ToLowerInvariant() switch
    {
        "grayscale" or "gray" or "grey" => StreamChartVectorColorMap.Grayscale,
        "hot" => StreamChartVectorColorMap.Hot,
        "viridis" => StreamChartVectorColorMap.Viridis,
        "jet" or null or "" => StreamChartVectorColorMap.Jet,
        _ => throw new ArgumentException($"Unsupported StreamChartVector colorMap '{name}'.", nameof(name)),
    };

    private static bool GetOptionalBoolean(JsonElement element, string propertyName, bool defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        if (property.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            throw new ArgumentException($"StreamChartVector settings field '{propertyName}' must be a boolean.", nameof(propertyName));
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
            throw new ArgumentException($"StreamChartVector settings field '{propertyName}' must be a number.", nameof(propertyName));
        }

        return property.GetDouble();
    }

    private static StreamChartVectorColorMap GetOptionalColorMap(
        JsonElement element,
        string propertyName,
        StreamChartVectorColorMap defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException($"StreamChartVector settings field '{propertyName}' must be a string.", nameof(propertyName));
        }

        return ParseColorMap(property.GetString());
    }
}
