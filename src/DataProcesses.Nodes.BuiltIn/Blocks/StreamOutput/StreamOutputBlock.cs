using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamOutput;

/// <summary>
/// Declares the stable identity and Fast Stream input contract for the Stream Output Block.
/// </summary>
public static class StreamOutputBlock
{
    public const string TypeId = "dataprocesses.output.stream";
    public const string InputPortId = "input";
    public const string IconPath = "Blocks/StreamOutput/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "Stream Output",
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
        Title: "Stream Output",
        Subtitle: "Debug stream",
        IconPath: IconPath);
}
