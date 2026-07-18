using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.LowPassFilter;

/// <summary>
/// Declares the stable identity and Fast Stream port contract for the Low-pass Filter Block.
/// </summary>
public static class LowPassFilterBlock
{
    public const string TypeId = "dataprocesses.filter.low-pass";
    public const string InputPortId = "input";
    public const string OutputPortId = "output";
    public const string IconPath = "Blocks/LowPassFilter/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "Low-pass Filter",
        Category: "Signal Processing",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(InputPortId, "Input", PortDirection.Input, PortDataKind.FastStream),
            new PortDefinition(OutputPortId, "Filtered", PortDirection.Output, PortDataKind.FastStream),
        ],
        NodeType: NodeType.BasicProcess,
        Title: "Low-pass Filter",
        Subtitle: "Smooth stream",
        IconPath: IconPath);
}
