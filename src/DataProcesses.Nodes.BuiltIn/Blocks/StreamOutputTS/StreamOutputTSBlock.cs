using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamOutputTS;

/// <summary>
/// Declares the stable identity and Fast Stream input contract for the StreamOutputTS Block.
/// </summary>
public static class StreamOutputTSBlock
{
    public const string TypeId = "dataprocesses.output.stream";
    public const string InputPortId = "input";
    public const string IconPath = "Blocks/StreamOutputTS/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "StreamOutputTS",
        Category: "Debug",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(
                InputPortId,
                "Input",
                PortDirection.Input,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.TimeSeries1D),
        ],
        NodeType: NodeType.Debug,
        Title: "StreamOutputTS",
        Subtitle: "Debug stream",
        IconPath: IconPath,
        DashboardWidget: new DashboardWidgetDefinition(
            IsVisibleByDefault: true,
            GridWidth: 3,
            GridHeight: 3));
}


