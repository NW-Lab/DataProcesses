using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamChartVector;

/// <summary>
/// Declares the stable identity and single Numeric Vector sink contract for the StreamChartVector Block.
/// </summary>
public static class StreamChartVectorBlock
{
    public const string TypeId = "dataprocesses.output.vector-chart";
    public const string InputPortId = "input";
    public const string IconPath = "Blocks/StreamChartVector/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "StreamChartVector",
        Category: "Debug",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(
                InputPortId,
                "Input",
                PortDirection.Input,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.NumericVector1D),
        ],
        NodeType: NodeType.Debug,
        Title: "StreamChartVector",
        Subtitle: "Vector waterfall chart",
        IconPath: IconPath,
        DashboardWidget: new DashboardWidgetDefinition(
            IsVisibleByDefault: true,
            GridWidth: 6,
            GridHeight: 4));
}
