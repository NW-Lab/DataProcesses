using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.FilterTS;

/// <summary>
/// Declares the stable identity and Fast Stream port contract for the FilterTS Block.
/// </summary>
public static class FilterTSBlock
{
    public const string TypeId = "dataprocesses.filter.filter-st";
    public const string InputPortId = "input";
    public const string OutputPortId = "output";
    public const string IconPath = "Blocks/FilterTS/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "FilterTS",
        Category: "Signal Processing",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(
                InputPortId,
                "Input",
                PortDirection.Input,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.TimeSeries1D),
            new PortDefinition(
                OutputPortId,
                "Filtered",
                PortDirection.Output,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.TimeSeries1D),
        ],
        NodeType: NodeType.BasicProcess,
        Title: "FilterTS",
        Subtitle: "Stream filter",
        IconPath: IconPath);
}