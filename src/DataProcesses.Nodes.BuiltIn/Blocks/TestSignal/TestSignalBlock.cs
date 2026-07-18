using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.TestSignal;

/// <summary>
/// Declares the stable identity and port contract for the Test Signal Block.
/// </summary>
public static class TestSignalBlock
{
    public const string TypeId = "dataprocesses.test-signal";
    public const string OutputPortId = "stream";

    public static NodeDefinition Definition { get; } = new(
        TypeId: TypeId,
        DisplayName: "Test Signal",
        Category: "Sources",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(
                OutputPortId,
                "Signal",
                PortDirection.Output,
                PortDataKind.FastStream),
        ],
        NodeType: NodeType.Input);
}