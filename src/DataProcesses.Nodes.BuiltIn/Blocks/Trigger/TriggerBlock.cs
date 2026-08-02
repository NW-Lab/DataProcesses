using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.Trigger;

/// <summary>
/// Declares the stable identity and Payload output contract for the Trigger Block.
/// </summary>
public static class TriggerBlock
{
    public const string TypeId = "dataprocesses.trigger";
    public const string PayloadOutputPortId = "payload-out";
    public const string IconPath = "Blocks/Trigger/icon.png";

    public static NodeDefinition Definition { get; } = new(
        TypeId: TypeId,
        DisplayName: "Trigger",
        Category: "Sources",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(
                PayloadOutputPortId,
                "Payload Out",
                PortDirection.Output,
                PortDataKind.JsonMessage),
        ],
        NodeType: NodeType.Input,
        Title: "Trigger",
        Subtitle: "Manual/start/periodic",
        IconPath: IconPath,
        DashboardWidget: new DashboardWidgetDefinition(
            IsVisibleByDefault: true,
            GridWidth: 1,
            GridHeight: 1));
}
