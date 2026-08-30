using System.Text.Json;

namespace DataProcesses.Nodes.BuiltIn.Blocks.MovieInputImage;

public sealed record MovieInputImageSettings(
    string MoviePath = "",
    double FramesPerSecond = 10.0,
    int OutputWidth = 640,
    int OutputHeight = 480,
    bool IsPlay = true,
    long ExecutionSessionId = 0)
{
    public const double MinimumFramesPerSecond = 0.1;
    public const double MaximumFramesPerSecond = 60.0;
    public const int MinimumDimension = 1;
    public const int MaximumDimension = 3_840;

    public static MovieInputImageSettings Default { get; } = new();

    public void Validate()
    {
        if (FramesPerSecond < MinimumFramesPerSecond
            || FramesPerSecond > MaximumFramesPerSecond
            || !double.IsFinite(FramesPerSecond))
        {
            throw new ArgumentOutOfRangeException(nameof(FramesPerSecond), FramesPerSecond, $"FPS must be between {MinimumFramesPerSecond} and {MaximumFramesPerSecond}.");
        }

        if (OutputWidth < MinimumDimension || OutputWidth > MaximumDimension)
        {
            throw new ArgumentOutOfRangeException(nameof(OutputWidth), OutputWidth, $"Output width must be between {MinimumDimension} and {MaximumDimension}.");
        }

        if (OutputHeight < MinimumDimension || OutputHeight > MaximumDimension)
        {
            throw new ArgumentOutOfRangeException(nameof(OutputHeight), OutputHeight, $"Output height must be between {MinimumDimension} and {MaximumDimension}.");
        }
    }

    public static MovieInputImageSettings FromJson(string settingsJson)
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
                throw new ArgumentException("MovieInputImage settings must be a JSON object.", nameof(settingsJson));
            }

            var settings = new MovieInputImageSettings(
                MoviePath: GetOptionalString(document.RootElement, "moviePath", Default.MoviePath),
                FramesPerSecond: GetOptionalDouble(document.RootElement, "fps", Default.FramesPerSecond),
                OutputWidth: GetOptionalInt(document.RootElement, "width", Default.OutputWidth),
                OutputHeight: GetOptionalInt(document.RootElement, "height", Default.OutputHeight),
                IsPlay: GetOptionalBoolean(document.RootElement, "isPlay", Default.IsPlay),
                ExecutionSessionId: GetOptionalLong(document.RootElement, "executionSessionId", Default.ExecutionSessionId));
            settings.Validate();
            return settings;
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("MovieInputImage settings JSON is invalid.", nameof(settingsJson), ex);
        }
    }

    private static string GetOptionalString(JsonElement element, string propertyName, string defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException($"MovieInputImage settings field '{propertyName}' must be a string.", nameof(property));
        }

        return property.GetString() ?? string.Empty;
    }

    private static double GetOptionalDouble(JsonElement element, string propertyName, double defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        if (property.ValueKind != JsonValueKind.Number || !property.TryGetDouble(out var value))
        {
            throw new ArgumentException($"MovieInputImage settings field '{propertyName}' must be a number.", nameof(property));
        }

        return value;
    }

    private static bool GetOptionalBoolean(JsonElement element, string propertyName, bool defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        if (property.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            throw new ArgumentException($"MovieInputImage settings field '{propertyName}' must be a boolean.", nameof(property));
        }

        return property.GetBoolean();
    }

    private static int GetOptionalInt(JsonElement element, string propertyName, int defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        if (property.ValueKind != JsonValueKind.Number || !property.TryGetInt32(out var value))
        {
            throw new ArgumentException($"MovieInputImage settings field '{propertyName}' must be an integer.", nameof(property));
        }

        return value;
    }

    private static long GetOptionalLong(JsonElement element, string propertyName, long defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        if (property.ValueKind != JsonValueKind.Number || !property.TryGetInt64(out var value))
        {
            throw new ArgumentException($"MovieInputImage settings field '{propertyName}' must be an integer.", nameof(property));
        }

        return value;
    }
}