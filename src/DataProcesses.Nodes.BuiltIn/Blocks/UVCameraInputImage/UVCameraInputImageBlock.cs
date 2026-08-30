using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.UVCameraInputImage;

/// <summary>
/// Declares the stable identity and port contract for the UVCameraInputImage Block.
/// </summary>
public static class UVCameraInputImageBlock
{
    public const string TypeId = "dataprocesses.input.uv-camera-image";
    public const string ControlInputPortId = "control";
    public const string ImageOutputPortId = "image";
    public const string IconPath = "Blocks/UVCameraInputImage/icon.png";

    public static NodeDefinition Definition { get; } = new(
        TypeId,
        "UVCameraInputImage",
        "Sources",
        "0.1.0",
        [
            new PortDefinition(ControlInputPortId, "Control", PortDirection.Input, PortDataKind.JsonMessage, IsRequired: false, DataSchema: PortDataSchema.JsonEnvelope),
            new PortDefinition(ImageOutputPortId, "Image", PortDirection.Output, PortDataKind.FastStream, DataSchema: PortDataSchema.Image2D),
        ],
        NodeType.Input,
        "UVCameraInputImage",
        "UV camera stream",
        IconPath,
        new DashboardWidgetDefinition(IsVisibleByDefault: true, GridWidth: 3, GridHeight: 2));
}