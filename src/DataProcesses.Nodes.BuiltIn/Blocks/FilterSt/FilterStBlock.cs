using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.FilterSt;

/// <summary>
/// Declares the stable identity and Fast Stream port contract for the FilterSt Block.
/// </summary>
public static class FilterStBlock
{
    public const string TypeId = "dataprocesses.filter.filter-st";
    public const string InputPortId = "input";
    public const string OutputPortId = "output";
    public const string IconPath = "Blocks/FilterSt/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "FilterSt",
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
        Title: "FilterSt",
        Subtitle: "Stream filter",
        IconPath: IconPath);
}