using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.MovingAverage;

/// <summary>
/// Declares the stable identity and Fast Stream port contract for the Moving Average Block.
/// </summary>
public static class MovingAverageBlock
{
    public const string TypeId = "dataprocesses.filter.moving-average";
    public const string InputPortId = "input";
    public const string OutputPortId = "output";
    public const string IconPath = "Blocks/MovingAverage/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "Moving Average",
        Category: "Signal Processing",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(InputPortId, "Input", PortDirection.Input, PortDataKind.FastStream, DataSchema: PortDataSchema.TimeSeries1D),
            new PortDefinition(OutputPortId, "Average", PortDirection.Output, PortDataKind.FastStream, DataSchema: PortDataSchema.TimeSeries1D),
        ],
        NodeType: NodeType.BasicProcess,
        Title: "Moving Average",
        Subtitle: "Smooth stream",
        IconPath: IconPath);
}