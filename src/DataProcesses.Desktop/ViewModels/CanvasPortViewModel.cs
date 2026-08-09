using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Desktop.ViewModels;

public sealed class CanvasPortViewModel(CanvasNodeViewModel node, PortDefinition definition) : ViewModelBase
{
    public CanvasNodeViewModel Node { get; } = node;

    public PortDefinition Definition { get; } = definition;

    public string Id => Definition.Id;

    public string DisplayName => Definition.DisplayName;

    public PortDirection Direction => Definition.Direction;

    public PortDataKind DataKind => Definition.DataKind;

    public PortDataSchema DataSchema => Definition.DataSchema;

    public bool IsInput => Direction == PortDirection.Input;

    public bool IsOutput => Direction == PortDirection.Output;

    public bool IsFastStream => DataKind == PortDataKind.FastStream;

    public bool IsJsonMessage => DataKind == PortDataKind.JsonMessage;

    public string KindLabel => DataKind == PortDataKind.FastStream ? "S" : "P";

    public string DataFamilyLabel => DataKind == PortDataKind.FastStream ? "Fast Stream" : "Payload";

    public bool HasDetailedSchema => DataSchema != PortDataSchema.Unspecified;

    public string SchemaLabel => DataSchema switch
    {
        PortDataSchema.Unspecified => "Unspecified",
        PortDataSchema.TimeSeries1D => "Time Series (1D)",
        PortDataSchema.Spectrum1D => "Spectrum (1D)",
        PortDataSchema.NumericVector1D => "Numeric Vector (1D)",
        PortDataSchema.NumericMatrix2D => "Numeric Matrix (2D)",
        PortDataSchema.Image2D => "Image (2D)",
        PortDataSchema.JsonEnvelope => "JSON Envelope",
        _ => "Unknown Schema",
    };

    public string SchemaBadge => DataSchema switch
    {
        PortDataSchema.Unspecified => string.Empty,
        PortDataSchema.TimeSeries1D => "TS",
        PortDataSchema.Spectrum1D => "VEC",
        PortDataSchema.NumericVector1D => "VEC",
        PortDataSchema.NumericMatrix2D => "VEC",
        PortDataSchema.Image2D => "IMG",
        PortDataSchema.JsonEnvelope => "JSON",
        _ => "?",
    };

    public string KindBadgeBackground => IsFastStream ? "#1D70B8" : "#D92D20";

    public string SchemaBadgeBackground => IsFastStream ? "#DBEAFE" : "#FEE2E2";

    public string SchemaBadgeBorderBrush => IsFastStream ? "#60A5FA" : "#F87171";

    public string SchemaBadgeForeground => IsFastStream ? "#1E3A8A" : "#7F1D1D";

    public string ShapeClass => DataKind == PortDataKind.FastStream ? "fastStream" : "payload";

    public string DirectionLabel => IsInput ? "Input" : "Output";

    public string AccessibleName => HasDetailedSchema
        ? $"{DirectionLabel}: {DisplayName} ({DataFamilyLabel}, {SchemaLabel})"
        : $"{DirectionLabel}: {DisplayName} ({DataFamilyLabel})";

    public string ToolTipText => HasDetailedSchema
        ? $"{AccessibleName} [{SchemaBadge}]"
        : AccessibleName;
}