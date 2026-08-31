using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.BreathImage;

/// <summary>
/// Declares the stable identity and image-to-respiration contract for the BreathImage Block.
/// </summary>
public static class BreathImageBlock
{
    public const string TypeId = "dataprocesses.analysis.breath-image";
    public const string InputPortId = "image-in";
    public const string OutputPortId = "breath-rate";
    public const string IconPath = "Blocks/BreathImage/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "BreathImage",
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
                "Breath Rate",
                PortDirection.Output,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.TimeSeries1D),
        ],
        NodeType: NodeType.BasicProcess,
        Title: "BreathImage",
        Subtitle: "Image respiration",
        IconPath: IconPath);
}