using System.Text.Json;

namespace DataProcesses.Nodes.BuiltIn.Blocks.FilterSt;

public enum FilterStKind
{
    LowPass,
    HighPass,
    BandPass,
    BandStop,
}

/// <summary>
/// Immutable configuration for the FilterSt Block.
/// </summary>
public sealed record FilterStSettings(
    FilterStKind FilterType = FilterStKind.LowPass,
    double CutoffFrequencyHertz = 5.0,
    double LowerCutoffFrequencyHertz = 1.0,
    double UpperCutoffFrequencyHertz = 10.0,
    int Order = 2)
{
    public const int MinimumOrder = 2;
    public const int MaximumOrder = 10;

    public static FilterStSettings Default { get; } = new();

    public static FilterStSettings FromJson(string settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            return Default;
        }

        using var document = JsonDocument.Parse(settingsJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("FilterSt settings must be a JSON object.", nameof(settingsJson));
        }

        var settings = Default;
        var root = document.RootElement;

        if (root.TryGetProperty("filterType", out var filterTypeElement)
            || root.TryGetProperty("type", out filterTypeElement)
            || root.TryGetProperty("kind", out filterTypeElement))
        {
            settings = settings with { FilterType = ParseFilterType(filterTypeElement) };
        }

        if (root.TryGetProperty("cutoffFrequencyHertz", out var cutoffElement)
            || root.TryGetProperty("cutoffFrequency", out cutoffElement)
            || root.TryGetProperty("cutoff", out cutoffElement))
        {
            settings = settings with { CutoffFrequencyHertz = ReadDouble(cutoffElement, "cutoffFrequencyHertz") };
        }

        if (root.TryGetProperty("lowerCutoffFrequencyHertz", out var lowerElement)
            || root.TryGetProperty("lowerCutoffFrequency", out lowerElement)
            || root.TryGetProperty("lowerCutoff", out lowerElement))
        {
            settings = settings with { LowerCutoffFrequencyHertz = ReadDouble(lowerElement, "lowerCutoffFrequencyHertz") };
        }

        if (root.TryGetProperty("upperCutoffFrequencyHertz", out var upperElement)
            || root.TryGetProperty("upperCutoffFrequency", out upperElement)
            || root.TryGetProperty("upperCutoff", out upperElement))
        {
            settings = settings with { UpperCutoffFrequencyHertz = ReadDouble(upperElement, "upperCutoffFrequencyHertz") };
        }

        if (root.TryGetProperty("order", out var orderElement))
        {
            if (orderElement.ValueKind != JsonValueKind.Number || !orderElement.TryGetInt32(out var order))
            {
                throw new ArgumentException("FilterSt setting 'order' must be an integer.", nameof(settingsJson));
            }

            settings = settings with { Order = order };
        }

        settings.Validate();
        return settings;
    }

    public void Validate()
    {
        if (Order is < MinimumOrder or > MaximumOrder)
        {
            throw new ArgumentOutOfRangeException(nameof(Order), $"Order must be between {MinimumOrder} and {MaximumOrder}.");
        }

        if (!double.IsFinite(CutoffFrequencyHertz) || CutoffFrequencyHertz <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(CutoffFrequencyHertz), "CutoffFrequencyHertz must be a finite positive frequency.");
        }

        if (!double.IsFinite(LowerCutoffFrequencyHertz) || LowerCutoffFrequencyHertz <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(LowerCutoffFrequencyHertz), "LowerCutoffFrequencyHertz must be a finite positive frequency.");
        }

        if (!double.IsFinite(UpperCutoffFrequencyHertz) || UpperCutoffFrequencyHertz <= LowerCutoffFrequencyHertz)
        {
            throw new ArgumentOutOfRangeException(nameof(UpperCutoffFrequencyHertz), "UpperCutoffFrequencyHertz must be greater than LowerCutoffFrequencyHertz.");
        }
    }

    private static FilterStKind ParseFilterType(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException("FilterSt setting 'filterType' must be a string.", nameof(element));
        }

        return element.GetString()?.Trim().ToLowerInvariant() switch
        {
            "lowpass" or "low-pass" or "low_pass" => FilterStKind.LowPass,
            "highpass" or "high-pass" or "high_pass" => FilterStKind.HighPass,
            "bandpass" or "band-pass" or "band_pass" => FilterStKind.BandPass,
            "bandstop" or "band-stop" or "band_stop" or "notch" => FilterStKind.BandStop,
            _ => throw new ArgumentException("FilterSt setting 'filterType' must be lowPass, highPass, bandPass, or bandStop.", nameof(element)),
        };
    }

    private static double ReadDouble(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetDouble(out var value))
        {
            throw new ArgumentException($"FilterSt setting '{propertyName}' must be a number.", propertyName);
        }

        return value;
    }
}