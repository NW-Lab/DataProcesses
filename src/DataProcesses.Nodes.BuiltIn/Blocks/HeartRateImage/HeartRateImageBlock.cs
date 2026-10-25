using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.HeartRateImage;

/// <summary>
/// Declares the stable identity and image-to-heart-rate contract for the HeartRateImage Block.
/// </summary>
public static class HeartRateImageBlock
{
    public const string TypeId = "dataprocesses.analysis.heart-rate-image";

    // Misspelled ID persisted by older Flows; migrated to TypeId on load.
    public const string LegacyTypeId = "dataprocesses.analysis.hart-rate-image";
    public const string InputPortId = "image-in";
    public const string OutputPortId = "heart-rate";
    public const string IconPath = "Blocks/HeartRateImage/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "HeartRateImage",
        Category: "Signal Analysis",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(
                InputPortId,
                "Image In",
                PortDirection.Input,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.Image2D),
            new PortDefinition(
                OutputPortId,
                "Heart Rate",
                PortDirection.Output,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.TimeSeries1D),
        ],
        NodeType: NodeType.BasicProcess,
        Title: "HeartRateImage",
        Subtitle: "rPPG heart rate",
        IconPath: IconPath);
}