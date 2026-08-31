using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.HartRateImage;

/// <summary>
/// Declares the stable identity and image-to-heart-rate contract for the HartRateImage Block.
/// </summary>
public static class HartRateImageBlock
{
    public const string TypeId = "dataprocesses.analysis.hart-rate-image";
    public const string InputPortId = "image-in";
    public const string OutputPortId = "heart-rate";
    public const string IconPath = "Blocks/HartRateImage/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "HartRateImage",
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
        Title: "HartRateImage",
        Subtitle: "rPPG heart rate",
        IconPath: IconPath);
}