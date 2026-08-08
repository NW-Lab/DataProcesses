using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.ImageOutput;

/// <summary>
/// Declares the stable identity and Image sink contract for the Image Output Block.
/// </summary>
public static class ImageOutputBlock
{
    public const string TypeId = "dataprocesses.output.image";
    public const string InputPortId = "input";
    public const string IconPath = "Blocks/ImageOutput/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "Image Output",
        Category: "Debug",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(
                InputPortId,
                "Input",
                PortDirection.Input,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.Image2D),
        ],
        NodeType: NodeType.Debug,
        Title: "Image Output",
        Subtitle: "Debug image",
        IconPath: IconPath,
        DashboardWidget: new DashboardWidgetDefinition(
            IsVisibleByDefault: true,
            GridWidth: 3,
            GridHeight: 2));
}
