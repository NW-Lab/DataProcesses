using System.Globalization;
using System.Text.Json;

namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamChartSt;

/// <summary>
/// Specifies how time axes (millis) are aligned across channels.
/// </summary>
public enum StreamChartTimeAlignment
{
    Independent,
    AlignToFirstStream,
}

/// <summary>
/// Immutable configuration for the StreamChartSt Block.
/// </summary>
public sealed record StreamChartStSettings(
    StreamChartTimeAlignment TimeAlignmentMode = StreamChartTimeAlignment.Independent,
    double TimeSpanMilliseconds = 5000.0,
    IReadOnlyList<string>? ChannelNames = null)
{
    public const double DefaultTimeSpanMilliseconds = 5000.0;
    public const double MinimumTimeSpanMilliseconds = 1.0;
    public const int MaxChannels = 4;

    private static readonly string[] DefaultNames = ["CH1", "CH2", "CH3", "CH4"];

    public IReadOnlyList<string> ChannelNames { get; init; } = NormalizeChannelNames(ChannelNames);

    public string Channel1Name => GetChannelName(1);
    public string Channel2Name => GetChannelName(2);
    public string Channel3Name => GetChannelName(3);
    public string Channel4Name => GetChannelName(4);

    public static StreamChartStSettings Default { get; } = new();

    public string GetChannelName(int channelIndex)
    {
        if (channelIndex < 1 || channelIndex > MaxChannels)
        {
            throw new ArgumentOutOfRangeException(nameof(channelIndex));
        }

        var name = ChannelNames[channelIndex - 1];
        return string.IsNullOrWhiteSpace(name) ? DefaultNames[channelIndex - 1] : name;
    }

    public static StreamChartStSettings FromJson(string settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            return Default;
        }

        using var document = JsonDocument.Parse(settingsJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("StreamChartSt settings must be a JSON object.", nameof(settingsJson));
        }

        return Default.ApplyPayload(document.RootElement);
    }

    public StreamChartStSettings ApplyPayload(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("StreamChartSt payload must be a JSON object.", nameof(payload));
        }

        var settings = this;

        if (payload.TryGetProperty("timeAlignmentMode", out var alignmentElement))
        {
            settings = settings with { TimeAlignmentMode = ParseTimeAlignment(alignmentElement) };
        }

        if (payload.TryGetProperty("timeSpanMillis", out var timeSpanElement)
            || payload.TryGetProperty("timeSpanMilliseconds", out timeSpanElement))
        {
            settings = settings with { TimeSpanMilliseconds = ReadDouble(timeSpanElement, "timeSpanMillis") };
        }

        var names = new List<string>(settings.ChannelNames);
        var namesUpdated = false;

        if (payload.TryGetProperty("channelNames", out var channelNamesElement)
            && channelNamesElement.ValueKind == JsonValueKind.Array)
        {
            var arrayNames = channelNamesElement.EnumerateArray()
                .Select(static element => element.ValueKind == JsonValueKind.String ? element.GetString() ?? string.Empty : string.Empty)
                .Take(MaxChannels)
                .ToArray();

            names = [.. NormalizeChannelNames(arrayNames)];
            namesUpdated = true;
        }

        for (var index = 1; index <= MaxChannels; index++)
        {
            var propertyName = $"channel{index}Name";
            if (payload.TryGetProperty(propertyName, out var chNameElement)
                && chNameElement.ValueKind == JsonValueKind.String)
            {
                while (names.Count < MaxChannels)
                {
                    names.Add(DefaultNames[names.Count]);
                }

                names[index - 1] = chNameElement.GetString() ?? DefaultNames[index - 1];
                namesUpdated = true;
            }
        }

        if (namesUpdated)
        {
            settings = settings with { ChannelNames = NormalizeChannelNames(names) };
        }

        settings.Validate();
        return settings;
    }

    public void Validate()
    {
        if (!double.IsFinite(TimeSpanMilliseconds) || TimeSpanMilliseconds < MinimumTimeSpanMilliseconds)
        {
            throw new ArgumentOutOfRangeException(
                nameof(TimeSpanMilliseconds),
                $"TimeSpanMilliseconds must be a finite number greater than or equal to {MinimumTimeSpanMilliseconds}.");
        }
    }

    private static IReadOnlyList<string> NormalizeChannelNames(IReadOnlyList<string>? names)
    {
        var result = new string[MaxChannels];
        for (var i = 0; i < MaxChannels; i++)
        {
            if (names is not null && i < names.Count && !string.IsNullOrWhiteSpace(names[i]))
            {
                result[i] = names[i];
            }
            else
            {
                result[i] = DefaultNames[i];
            }
        }

        return result;
    }

    private static StreamChartTimeAlignment ParseTimeAlignment(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var numericValue))
        {
            return numericValue switch
            {
                1 => StreamChartTimeAlignment.AlignToFirstStream,
                _ => StreamChartTimeAlignment.Independent,
            };
        }

        var text = ReadString(element, "timeAlignmentMode");
        return text.ToLowerInvariant() switch
        {
            "aligntofirst" or "aligntofirststream" or "align_to_first" => StreamChartTimeAlignment.AlignToFirstStream,
            _ => StreamChartTimeAlignment.Independent,
        };
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException($"StreamChartSt payload field '{propertyName}' must be a string.", propertyName);
        }

        return element.GetString() ?? string.Empty;
    }

    private static double ReadDouble(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var numericValue))
        {
            return numericValue;
        }

        if (element.ValueKind == JsonValueKind.String
            && double.TryParse(element.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
        {
            return parsedValue;
        }

        throw new ArgumentException($"StreamChartSt payload field '{propertyName}' must be a number.", propertyName);
    }
}
