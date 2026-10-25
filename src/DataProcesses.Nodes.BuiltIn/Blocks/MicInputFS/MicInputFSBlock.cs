using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.MicInputFS;

/// <summary>
/// Declares the Fast Stream output contract for microphone capture.
/// </summary>
public static class MicInputFSBlock
{
    public const string TypeId = "dataprocesses.input.mic-fs";
    public const string StreamOutputPortId = "stream";
    public const string IconPath = "Blocks/MicInputFS/icon.png";

    public static NodeDefinition Definition { get; } = new(
        TypeId: TypeId,
        DisplayName: "MicInputFS",
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
        Title: "MicInputFS",
        Subtitle: "Microphone Fast Stream",
        IconPath: IconPath);
}