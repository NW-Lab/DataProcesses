using System.Text.Json;

using DataProcesses.Nodes.BuiltIn.Blocks.FilterTS;

namespace DataProcesses.Nodes.BuiltIn.Blocks.FilterFS;

public enum FilterFSKind
{
    LowPass,
    HighPass,
    BandPass,
    BandStop,
    Notch,
}

public sealed record FilterFSSettings(
    FilterFSKind FilterType = FilterFSKind.LowPass,
    double CutoffFrequencyHertz = 5.0,
    double LowerCutoffFrequencyHertz = 1.0,
    double UpperCutoffFrequencyHertz = 10.0,
    double NotchBandwidthHertz = 2.0,
    int Order = FilterTSSettings.MinimumOrder)
{
    public const int MinimumOrder = FilterTSSettings.MinimumOrder;
    public const int MaximumOrder = FilterTSSettings.MaximumOrder;

    public static FilterFSSettings Default { get; } = new();

    public static IReadOnlyList<string> FilterTypeOptions { get; } = ["lowPass", "highPass", "bandPass", "bandStop", "notch"];

    public static FilterFSSettings FromJson(string settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            return Default;
        }

        using var document = JsonDocument.Parse(settingsJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("FilterFS settings must be a JSON object.", nameof(settingsJson));
        }

        var settings = Default;
        var root = document.RootElement;
        if (root.TryGetProperty("filterType", out var filterTypeElement))
        {
            settings = settings with { FilterType = ParseFilterType(filterTypeElement) };
        }

        if (root.TryGetProperty("cutoffFrequencyHertz", out var cutoffElement))
        {
            settings = settings with { CutoffFrequencyHertz = ReadDouble(cutoffElement, "cutoffFrequencyHertz") };
        }

        if (root.TryGetProperty("lowerCutoffFrequencyHertz", out var lowerElement))
        {
            settings = settings with { LowerCutoffFrequencyHertz = ReadDouble(lowerElement, "lowerCutoffFrequencyHertz") };
        }

        if (root.TryGetProperty("upperCutoffFrequencyHertz", out var upperElement))
        {
            settings = settings with { UpperCutoffFrequencyHertz = ReadDouble(upperElement, "upperCutoffFrequencyHertz") };
        }

        if (root.TryGetProperty("notchBandwidthHertz", out var bandwidthElement))
        {
            settings = settings with { NotchBandwidthHertz = ReadDouble(bandwidthElement, "notchBandwidthHertz") };
        }

        if (root.TryGetProperty("order", out var orderElement))
        {
            if (orderElement.ValueKind != JsonValueKind.Number || !orderElement.TryGetInt32(out var order))
            {
                throw new ArgumentException("FilterFS setting 'order' must be an integer.", nameof(settingsJson));
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
            throw new ArgumentOutOfRangeException(nameof(CutoffFrequencyHertz), "Cutoff frequency must be a finite positive value.");
        }

        if (!double.IsFinite(LowerCutoffFrequencyHertz) || LowerCutoffFrequencyHertz <= 0.0
            || !double.IsFinite(UpperCutoffFrequencyHertz) || UpperCutoffFrequencyHertz <= LowerCutoffFrequencyHertz)
        {
            throw new ArgumentOutOfRangeException(nameof(UpperCutoffFrequencyHertz), "Band cutoff frequencies must be positive and upper must exceed lower.");
        }

        if (FilterType == FilterFSKind.Notch
            && (!double.IsFinite(NotchBandwidthHertz) || NotchBandwidthHertz <= 0.0
                || CutoffFrequencyHertz <= NotchBandwidthHertz / 2.0))
        {
            throw new ArgumentOutOfRangeException(nameof(NotchBandwidthHertz), "Notch bandwidth must be positive and narrower than twice the center frequency.");
        }

        if (FilterType is not FilterFSKind.LowPass
            and not FilterFSKind.HighPass
            and not FilterFSKind.BandPass
            and not FilterFSKind.BandStop
            and not FilterFSKind.Notch)
        {
            throw new ArgumentOutOfRangeException(nameof(FilterType), "Unsupported FilterFS type.");
        }
    }

    public FilterTSSettings ToFilterTSSettings()
    {
        Validate();
        return FilterType switch
        {
            FilterFSKind.LowPass => new FilterTSSettings(
                FilterTSKind.LowPass,
                CutoffFrequencyHertz,
                LowerCutoffFrequencyHertz,
                UpperCutoffFrequencyHertz,
                Order),
            FilterFSKind.HighPass => new FilterTSSettings(
                FilterTSKind.HighPass,
                CutoffFrequencyHertz,
                LowerCutoffFrequencyHertz,
                UpperCutoffFrequencyHertz,
                Order),
            FilterFSKind.BandPass => new FilterTSSettings(
                FilterTSKind.BandPass,
                CutoffFrequencyHertz,
                LowerCutoffFrequencyHertz,
                UpperCutoffFrequencyHertz,
                Order),
            FilterFSKind.BandStop => new FilterTSSettings(
                FilterTSKind.BandStop,
                CutoffFrequencyHertz,
                LowerCutoffFrequencyHertz,
                UpperCutoffFrequencyHertz,
                Order),
            FilterFSKind.Notch => new FilterTSSettings(
                FilterTSKind.BandStop,
                CutoffFrequencyHertz,
                CutoffFrequencyHertz - (NotchBandwidthHertz / 2.0),
                CutoffFrequencyHertz + (NotchBandwidthHertz / 2.0),
                Order),
            _ => throw new InvalidOperationException($"Unsupported FilterFS type '{FilterType}'."),
        };
    }

    private static FilterFSKind ParseFilterType(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException("FilterFS setting 'filterType' must be a string.", nameof(element));
        }

        return element.GetString()?.Trim().ToLowerInvariant() switch
        {
            "lowpass" or "low-pass" or "low_pass" => FilterFSKind.LowPass,
            "highpass" or "high-pass" or "high_pass" => FilterFSKind.HighPass,
            "bandpass" or "band-pass" or "band_pass" => FilterFSKind.BandPass,
            "bandstop" or "band-stop" or "band_stop" => FilterFSKind.BandStop,
            "notch" => FilterFSKind.Notch,
            var value => throw new ArgumentException($"Unsupported FilterFS type '{value}'.", nameof(element)),
        };
    }

    private static double ReadDouble(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetDouble(out var value))
        {
            throw new ArgumentException($"FilterFS setting '{propertyName}' must be a number.", nameof(element));
        }

        return value;
    }
}