using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.FilterFS;

/// <summary>
/// Declares the Fast Stream input and output contract for the FilterFS Block.
/// </summary>
public static class FilterFSBlock
{
    public const string TypeId = "dataprocesses.filter.filter-fs";
    public const string InputPortId = "input";
    public const string OutputPortId = "output";
    public const string IconPath = "Blocks/FilterTS/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "FilterFS",
        Category: "Signal Processing",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(
                InputPortId,
                "Input",
                PortDirection.Input,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.FixedRateTimeSeries1D),
            new PortDefinition(
                OutputPortId,
                "Filtered",
                PortDirection.Output,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.FixedRateTimeSeries1D),
        ],
        NodeType: NodeType.BasicProcess,
        Title: "FilterFS",
        Subtitle: "Fast Stream Filter",
        IconPath: IconPath);
}