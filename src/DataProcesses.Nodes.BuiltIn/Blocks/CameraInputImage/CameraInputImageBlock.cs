using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.CameraInputImage;

/// <summary>
/// Declares the stable identity and port contract for the CameraInputImage Block.
/// </summary>
public static class CameraInputImageBlock
{
    public const string TypeId = "dataprocesses.input.camera-image";
    public const string TriggerInputPortId = "trigger";
    public const string ImageOutputPortId = "image";
    public const string IconPath = "Blocks/CameraInputImage/icon.png";

    public static NodeDefinition Definition { get; } = new(
        TypeId: TypeId,
        DisplayName: "CameraInputImage",
        Category: "Sources",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(
                TriggerInputPortId,
                "Trigger",
                PortDirection.Input,
                PortDataKind.JsonMessage,
                IsRequired: false,
                DataSchema: PortDataSchema.JsonEnvelope),
            new PortDefinition(
                ImageOutputPortId,
                "Image",
                PortDirection.Output,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.Image2D),
        ],
        NodeType: NodeType.Input,
        Title: "CameraInputImage",
        Subtitle: "Camera capture",
        IconPath: IconPath,
        DashboardWidget: new DashboardWidgetDefinition(
            IsVisibleByDefault: true,
            GridWidth: 3,
            GridHeight: 2));
}