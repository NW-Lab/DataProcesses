using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.TimeSeriesDisplay;

/// <summary>
/// Declares the stable identity and Fast Stream input contract for the Time Series Display Block.
/// </summary>
public static class TimeSeriesDisplayBlock
{
    public const string TypeId = "dataprocesses.dashboard.time-series";
    public const string InputPortId = "input";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "Time Series",
        Category: "Dashboard",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(InputPortId, "Input", PortDirection.Input, PortDataKind.FastStream),
        ],
        NodeType: NodeType.Output);
}
