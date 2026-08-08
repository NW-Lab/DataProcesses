using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.PayloadOutput;

/// <summary>
/// Declares the stable identity and Payload input contract for the Payload Output Block.
/// </summary>
public static class PayloadOutputBlock
{
    public const string TypeId = "dataprocesses.output.payload";
    public const string InputPortId = "payload";
    public const string IconPath = "Blocks/PayloadOutput/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "Payload Output",
        Category: "Debug",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(
                InputPortId,
                "Payload",
                PortDirection.Input,
                PortDataKind.JsonMessage,
                DataSchema: PortDataSchema.JsonEnvelope),
        ],
        NodeType: NodeType.Debug,
        Title: "Payload Output",
        Subtitle: "Debug payload",
        IconPath: IconPath,
        DashboardWidget: new DashboardWidgetDefinition(
            IsVisibleByDefault: true,
            GridWidth: 3,
            GridHeight: 3));
}
