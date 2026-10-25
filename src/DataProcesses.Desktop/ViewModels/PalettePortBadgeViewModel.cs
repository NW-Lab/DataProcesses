using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Desktop.ViewModels;

/// <summary>
/// Compact port summary shown in the Node Library. Fast Stream and JSON Message badges differ by shape and letter, not only color.
/// </summary>
public sealed class PalettePortBadgeViewModel
{
    public PalettePortBadgeViewModel(PortDataKind dataKind, PortDataSchema dataSchema)
    {
        DataKind = dataKind;
        DataSchema = dataSchema;
    }

    public PortDataKind DataKind { get; }

    public PortDataSchema DataSchema { get; }

    public bool IsFastStream => DataKind == PortDataKind.FastStream;

    public bool IsJsonMessage => DataKind == PortDataKind.JsonMessage;

    public string KindLetter => IsFastStream ? "S" : "J";

    public string KindColor => IsFastStream ? PortKindColors.FastStream : PortKindColors.JsonMessage;

    public string SchemaLabel => DataSchema switch
    {
        PortDataSchema.TimeSeries1D => "TS",
        PortDataSchema.FixedRateTimeSeries1D => "FS",
        PortDataSchema.Spectrum1D => "VEC",
        PortDataSchema.NumericVector1D => "VEC",
        PortDataSchema.NumericMatrix2D => "VEC",
        PortDataSchema.Image2D => "IMG",
        _ => string.Empty,
    };

    public bool HasSchemaLabel => SchemaLabel.Length > 0;

    public string Description
    {
        get
        {
            var kind = IsFastStream ? "Fast Stream" : "JSON Message";
            return HasSchemaLabel ? $"{kind} {SchemaLabel}" : kind;
        }
    }

    internal static IReadOnlyList<PalettePortBadgeViewModel> Create(IReadOnlyList<PortDefinition> ports, PortDirection direction)
    {
        var badges = new List<PalettePortBadgeViewModel>();
        foreach (var port in ports)
        {
            if (port.Direction != direction)
            {
                continue;
            }

            var candidate = new PalettePortBadgeViewModel(port.DataKind, port.DataSchema);
            var exists = false;
            foreach (var badge in badges)
            {
                if (badge.DataKind == candidate.DataKind
                    && string.Equals(badge.SchemaLabel, candidate.SchemaLabel, StringComparison.Ordinal))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                badges.Add(candidate);
            }
        }

        return badges;
    }
}
