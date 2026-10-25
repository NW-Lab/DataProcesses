using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamOutputFS;

/// <summary>
/// Declares the Fast Stream input contract for the StreamOutputFS Block.
/// </summary>
public static class StreamOutputFSBlock
{
    public const string TypeId = "dataprocesses.output.stream-fs";
    public const string InputPortId = "input";
    public const string IconPath = "Blocks/StremOutputTS/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "StreamOutputFS",
        Category: "Debug",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(
                InputPortId,
                "Input",
                PortDirection.Input,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.FixedRateTimeSeries1D),
        ],
        NodeType: NodeType.Debug,
        Title: "StreamOutputFS",
        Subtitle: "Fast Stream output",
        IconPath: IconPath,
        DashboardWidget: new DashboardWidgetDefinition(
            IsVisibleByDefault: true,
            GridWidth: 3,
            GridHeight: 3));
}