using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.HumansImage;

/// <summary>
/// Declares the stable identity and image-to-person-count contract for the HumansImage Block.
/// </summary>
public static class HumansImageBlock
{
    public const string TypeId = "dataprocesses.analysis.humans-image";
    public const string InputPortId = "image-in";
    public const string OutputPortId = "humans-count";
    public const string IconPath = "Blocks/HumansImage/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "HumansImage",
        Category: "Image Analysis",
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
                "Humans Count",
                PortDirection.Output,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.TimeSeries1D),
        ],
        NodeType: NodeType.BasicProcess,
        Title: "HumansImage",
        Subtitle: "Face count",
        IconPath: IconPath);
}