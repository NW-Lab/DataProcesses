using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.TestSignalVec;

/// <summary>
/// Declares the stable identity and port contract for the TestSignalVec Block.
/// </summary>
public static class TestSignalVecBlock
{
    public const string TypeId = "dataprocesses.test-signal-vec";
    public const string StreamOutputPortId = "stream";
    public const string PayloadInputPortId = "payload-in";
    public const string PayloadOutputPortId = "payload-out";
    public const string IconPath = "Blocks/TestSignalVec/icon.png";

    public static NodeDefinition Definition { get; } = new(
        TypeId: TypeId,
        DisplayName: "TestSignal(Vec)ブロック",
        Category: "Sources",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(
                PayloadInputPortId,
                "Payload In",
                PortDirection.Input,
                PortDataKind.JsonMessage,
                IsRequired: false,
                DataSchema: PortDataSchema.JsonEnvelope),
            new PortDefinition(
                StreamOutputPortId,
                "Signal",
                PortDirection.Output,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.NumericVector1D),
            new PortDefinition(
                PayloadOutputPortId,
                "Payload Out",
                PortDirection.Output,
                PortDataKind.JsonMessage,
                IsRequired: false,
                DataSchema: PortDataSchema.JsonEnvelope),
        ],
        NodeType: NodeType.Input,
        Title: "TestSignal(Vec)ブロック",
        Subtitle: "Vec",
        IconPath: IconPath,
        DashboardWidget: new DashboardWidgetDefinition(
            IsVisibleByDefault: true,
            GridWidth: 2,
            GridHeight: 1));
}
