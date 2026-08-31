using System.Text.Json;

namespace DataProcesses.Nodes.BuiltIn.Blocks.HumansImage;

/// <summary>
/// Immutable configuration for the HumansImage Block.
/// </summary>
public sealed record HumansImageSettings(
    int MinimumFacePixelCount = 16,
    int MinimumFaceWidthPixels = 3,
    int MinimumFaceHeightPixels = 3,
    double MinimumSkinRatio = 0.60)
{
    public static HumansImageSettings Default { get; } = new();

    public static HumansImageSettings FromJson(string settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            return Default;
        }

        using var document = JsonDocument.Parse(settingsJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("HumansImage settings must be a JSON object.", nameof(settingsJson));
        }

        return Default.ApplyPayload(document.RootElement);
    }

    public HumansImageSettings ApplyPayload(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("HumansImage payload must be a JSON object.", nameof(payload));
        }

        var settings = this;

        if (payload.TryGetProperty("minimumFacePixelCount", out var minimumFacePixelCountElement))
        {
            settings = settings with { MinimumFacePixelCount = ReadInt32(minimumFacePixelCountElement, "minimumFacePixelCount") };
        }

        if (payload.TryGetProperty("minimumFaceWidthPixels", out var minimumFaceWidthPixelsElement))
        {
            settings = settings with { MinimumFaceWidthPixels = ReadInt32(minimumFaceWidthPixelsElement, "minimumFaceWidthPixels") };
        }

        if (payload.TryGetProperty("minimumFaceHeightPixels", out var minimumFaceHeightPixelsElement))
        {
            settings = settings with { MinimumFaceHeightPixels = ReadInt32(minimumFaceHeightPixelsElement, "minimumFaceHeightPixels") };
        }

        if (payload.TryGetProperty("minimumSkinRatio", out var minimumSkinRatioElement))
        {
            settings = settings with { MinimumSkinRatio = ReadDouble(minimumSkinRatioElement, "minimumSkinRatio") };
        }

        settings.Validate();
        return settings;
    }

    public void Validate()
    {
        if (MinimumFacePixelCount < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MinimumFacePixelCount),
                "MinimumFacePixelCount must be 1 or greater.");
        }

        if (MinimumFaceWidthPixels < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MinimumFaceWidthPixels),
                "MinimumFaceWidthPixels must be 1 or greater.");
        }

        if (MinimumFaceHeightPixels < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MinimumFaceHeightPixels),
                "MinimumFaceHeightPixels must be 1 or greater.");
        }

        if (!double.IsFinite(MinimumSkinRatio) || MinimumSkinRatio <= 0.0 || MinimumSkinRatio > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MinimumSkinRatio),
                "MinimumSkinRatio must be a finite number greater than 0 and less than or equal to 1.");
        }
    }

    private static int ReadInt32(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out var value))
        {
            throw new ArgumentException($"HumansImage setting '{propertyName}' must be an integer.", propertyName);
        }

        return value;
    }

    private static double ReadDouble(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetDouble(out var value))
        {
            throw new ArgumentException($"HumansImage setting '{propertyName}' must be a number.", propertyName);
        }

        return value;
    }
}