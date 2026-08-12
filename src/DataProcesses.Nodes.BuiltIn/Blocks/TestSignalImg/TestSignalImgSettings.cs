using System.Text.Json;

namespace DataProcesses.Nodes.BuiltIn.Blocks.TestSignalImg;

public enum TestSignalImgKind
{
    Mono,
    Color,
}

public enum TestSignalImgType
{
    Number,
    Circle,
}

public sealed record TestSignalImgSettings(
    bool IsEnabled = true,
    TestSignalImgType Type = TestSignalImgType.Number,
    TestSignalImgKind Kind = TestSignalImgKind.Mono,
    double FrequencyHertz = 1.0,
    int Width = 100,
    int Height = 100,
    bool PayloadThrough = true)
{
    public const int MinimumDimension = 1;
    public const int MaximumDimension = 1_024;

    public static TestSignalImgSettings Default { get; } = new();

    public void Validate()
    {
        if (Width < MinimumDimension || Width > MaximumDimension)
        {
            throw new ArgumentOutOfRangeException(nameof(Width), Width, $"Width must be between {MinimumDimension} and {MaximumDimension}.");
        }

        if (Height < MinimumDimension || Height > MaximumDimension)
        {
            throw new ArgumentOutOfRangeException(nameof(Height), Height, $"Height must be between {MinimumDimension} and {MaximumDimension}.");
        }

        if (FrequencyHertz <= 0 || double.IsNaN(FrequencyHertz) || double.IsInfinity(FrequencyHertz))
        {
            throw new ArgumentOutOfRangeException(nameof(FrequencyHertz), FrequencyHertz, "Frequency must be a positive finite number.");
        }
    }

    public static TestSignalImgSettings FromJson(string settingsJson)
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
                throw new ArgumentException("TestSignalImg settings must be a JSON object.", nameof(settingsJson));
            }

            return new TestSignalImgSettings(
                IsEnabled: GetOptionalBoolean(document.RootElement, "isEnabled", Default.IsEnabled),
                Type: GetOptionalType(document.RootElement, "type", Default.Type),
                Kind: GetOptionalKind(document.RootElement, "kind", Default.Kind),
                FrequencyHertz: GetOptionalFrequencyHertz(document.RootElement, Default.FrequencyHertz),
                Width: GetOptionalInt(document.RootElement, "width", Default.Width),
                Height: GetOptionalInt(document.RootElement, "height", Default.Height),
                PayloadThrough: GetOptionalBoolean(document.RootElement, "payloadThrough", Default.PayloadThrough));
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("TestSignalImg settings JSON is invalid.", nameof(settingsJson), ex);
        }
    }

    public TestSignalImgSettings ApplyPayload(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("TestSignalImg payload must be a JSON object.", nameof(payload));
        }

        return new TestSignalImgSettings(
            IsEnabled: GetOptionalBoolean(payload, "isEnabled", IsEnabled),
            Type: GetOptionalType(payload, "type", Type),
            Kind: GetOptionalKind(payload, "kind", Kind),
            FrequencyHertz: GetOptionalFrequencyHertz(payload, FrequencyHertz),
            Width: GetOptionalInt(payload, "width", Width),
            Height: GetOptionalInt(payload, "height", Height),
            PayloadThrough: GetOptionalBoolean(payload, "payloadThrough", PayloadThrough));
    }

    private static double GetOptionalFrequencyHertz(JsonElement element, double defaultValue)
    {
        if (element.TryGetProperty("frequency", out var frequencyProperty))
        {
            if (frequencyProperty.ValueKind != JsonValueKind.Number)
            {
                throw new ArgumentException("TestSignalImg payload field 'frequency' must be a number.", nameof(frequencyProperty));
            }

            return frequencyProperty.GetDouble();
        }

        if (element.TryGetProperty("frameRateMillis", out var frameRateProperty))
        {
            if (frameRateProperty.ValueKind != JsonValueKind.Number)
            {
                throw new ArgumentException("TestSignalImg payload field 'frameRateMillis' must be a number.", nameof(frameRateProperty));
            }

            var frameRateMillis = frameRateProperty.GetDouble();
            if (frameRateMillis <= 0 || double.IsNaN(frameRateMillis) || double.IsInfinity(frameRateMillis))
            {
                throw new ArgumentException("TestSignalImg payload field 'frameRateMillis' must be a positive finite number.", nameof(frameRateProperty));
            }

            return 1000.0 / frameRateMillis;
        }

        return defaultValue;
    }

    private static bool GetOptionalBoolean(JsonElement element, string propertyName, bool defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        if (property.ValueKind != JsonValueKind.True && property.ValueKind != JsonValueKind.False)
        {
            throw new ArgumentException($"TestSignalImg payload field '{propertyName}' must be a boolean.", nameof(property));
        }

        return property.GetBoolean();
    }

    private static int GetOptionalInt(JsonElement element, string propertyName, int defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        if (property.ValueKind != JsonValueKind.Number)
        {
            throw new ArgumentException($"TestSignalImg payload field '{propertyName}' must be a number.", nameof(property));
        }

        return property.GetInt32();
    }

    private static TestSignalImgKind GetOptionalKind(JsonElement element, string propertyName, TestSignalImgKind defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException($"TestSignalImg payload field '{propertyName}' must be a string.", nameof(property));
        }

        return property.GetString() switch
        {
            "mono" => TestSignalImgKind.Mono,
            "color" => TestSignalImgKind.Color,
            "text" => TestSignalImgKind.Mono,
            _ => throw new ArgumentException($"Unsupported TestSignalImg kind '{property.GetString()}'.", nameof(property)),
        };
    }

    private static TestSignalImgType GetOptionalType(JsonElement element, string propertyName, TestSignalImgType defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException($"TestSignalImg payload field '{propertyName}' must be a string.", nameof(property));
        }

        return property.GetString() switch
        {
            "number" => TestSignalImgType.Number,
            "circle" => TestSignalImgType.Circle,
            _ => throw new ArgumentException($"Unsupported TestSignalImg type '{property.GetString()}'.", nameof(property)),
        };
    }
}
