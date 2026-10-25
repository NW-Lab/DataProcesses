using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.TestSignalFS;

/// <summary>
/// Declares the Fast Stream output contract for the TestSignal(FS) Block.
/// </summary>
public static class TestSignalFSBlock
{
    public const string TypeId = "dataprocesses.test-signal-fs";
    public const string StreamOutputPortId = "stream";
    public const string IconPath = "Blocks/TestSignalTS/icon.png";

    public static NodeDefinition Definition { get; } = new(
        TypeId: TypeId,
        DisplayName: "TestSignal(FS)ブロック",
        Category: "Sources",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(
                StreamOutputPortId,
                "Signal",
                PortDirection.Output,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.FixedRateTimeSeries1D),
        ],
        NodeType: NodeType.Input,
        Title: "TestSignal(FS)ブロック",
        Subtitle: "FS",
        IconPath: IconPath);
}