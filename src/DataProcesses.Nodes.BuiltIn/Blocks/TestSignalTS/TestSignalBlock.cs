using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.TestSignal;

/// <summary>
/// Declares the stable identity and port contract for the Test Signal Block.
/// </summary>
public static class TestSignalBlock
{
    public const string TypeId = "dataprocesses.test-signal";
    public const string PayloadInputPortId = "payload-in";
    public const string StreamOutputPortId = "stream";
    public const string PayloadOutputPortId = "payload-out";
    public const string IconPath = "Blocks/TestSignalTS/icon.png";

    public static NodeDefinition Definition { get; } = new(
        TypeId: TypeId,
        DisplayName: "TestSignal(TS)ブロック",
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
                DataSchema: PortDataSchema.TimeSeries1D),
            new PortDefinition(
                PayloadOutputPortId,
                "Payload Out",
                PortDirection.Output,
                PortDataKind.JsonMessage,
                IsRequired: false,
                DataSchema: PortDataSchema.JsonEnvelope),
        ],
            NodeType: NodeType.Input,
            Title: "TestSignal(TS)ブロック",
            Subtitle: "TS",
            IconPath: IconPath,
            DashboardWidget: new DashboardWidgetDefinition(
                IsVisibleByDefault: true,
                GridWidth: 2,
                GridHeight: 1));
}