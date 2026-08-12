using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.StremOutputTS;

/// <summary>
/// Declares the stable identity and Fast Stream input contract for the StremOutputTS Block.
/// </summary>
public static class StremOutputTSBlock
{
    public const string TypeId = "dataprocesses.output.stream";
    public const string InputPortId = "input";
    public const string IconPath = "Blocks/StremOutputTS/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "StremOutputTS",
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
        Title: "StremOutputTS",
        Subtitle: "Debug stream",
        IconPath: IconPath,
        DashboardWidget: new DashboardWidgetDefinition(
            IsVisibleByDefault: true,
            GridWidth: 3,
            GridHeight: 3));
}


