using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.TestSignalImg;

/// <summary>
/// Declares the stable identity and port contract for the TestSignalImg Block.
/// </summary>
public static class TestSignalImgBlock
{
    public const string TypeId = "dataprocesses.test-signal-img";
    public const string StreamOutputPortId = "stream";
    public const string PayloadInputPortId = "payload-in";
    public const string PayloadOutputPortId = "payload-out";
    public const string IconPath = "Blocks/TestSignalImg/icon.png";

    public static NodeDefinition Definition { get; } = new(
        TypeId: TypeId,
        DisplayName: "TestSignal(Img)ブロック",
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
                "Image",
                PortDirection.Output,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.Image2D),
            new PortDefinition(
                PayloadOutputPortId,
                "Payload Out",
                PortDirection.Output,
                PortDataKind.JsonMessage,
                IsRequired: false,
                DataSchema: PortDataSchema.JsonEnvelope),
        ],
        NodeType: NodeType.Input,
        Title: "TestSignal(Img)ブロック",
        Subtitle: "Img",
        IconPath: IconPath,
        DashboardWidget: new DashboardWidgetDefinition(
            IsVisibleByDefault: true,
            GridWidth: 2,
            GridHeight: 1));
}
