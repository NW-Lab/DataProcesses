using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamChartFS;

/// <summary>
/// Declares the single Fast Stream input contract for the StreamChartFS Block.
/// </summary>
public static class StreamChartFSBlock
{
    public const string TypeId = "dataprocesses.output.stream-chart-fs";
    public const string InputPortId = "input";
    public const int MaximumChannels = 4;
    public const string IconPath = "Blocks/StreamChartTS/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "StreamChartFS",
        Category: "Output",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(
                InputPortId,
                "Input",
                PortDirection.Input,
                PortDataKind.FastStream,
                IsRequired: false,
                DataSchema: PortDataSchema.FixedRateTimeSeries1D),
        ],
        NodeType: NodeType.Output,
        Title: "StreamChartFS",
        Subtitle: "Fast Stream Chart",
        IconPath: IconPath,
        DashboardWidget: new DashboardWidgetDefinition(
            IsVisibleByDefault: true,
            GridWidth: 4,
            GridHeight: 3));
}