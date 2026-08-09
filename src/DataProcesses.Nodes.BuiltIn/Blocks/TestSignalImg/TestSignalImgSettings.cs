using System.Text.Json;

namespace DataProcesses.Nodes.BuiltIn.Blocks.TestSignalImg;

public enum TestSignalImgKind
{
    Text,
}

public sealed record TestSignalImgSettings(
    bool IsEnabled = true,
    TestSignalImgKind Kind = TestSignalImgKind.Text,
    int Width = 100,
    int Height = 100,
    int FrameRateMilliseconds = 1_000,
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

        if (FrameRateMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(FrameRateMilliseconds), FrameRateMilliseconds, "Frame rate must be positive.");
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
                Kind: GetOptionalKind(document.RootElement, "kind", Default.Kind),
                Width: GetOptionalInt(document.RootElement, "width", Default.Width),
                Height: GetOptionalInt(document.RootElement, "height", Default.Height),
                FrameRateMilliseconds: GetOptionalInt(document.RootElement, "frameRateMillis", Default.FrameRateMilliseconds),
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
            Kind: GetOptionalKind(payload, "kind", Kind),
            Width: GetOptionalInt(payload, "width", Width),
            Height: GetOptionalInt(payload, "height", Height),
            FrameRateMilliseconds: GetOptionalInt(payload, "frameRateMillis", FrameRateMilliseconds),
            PayloadThrough: GetOptionalBoolean(payload, "payloadThrough", PayloadThrough));
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
            "text" => TestSignalImgKind.Text,
            _ => throw new ArgumentException($"Unsupported TestSignalImg kind '{property.GetString()}'.", nameof(property)),
        };
    }
}
