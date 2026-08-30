using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.MovieInputImage;

/// <summary>
/// Declares the stable identity and port contract for the MovieInputImage Block.
/// </summary>
public static class MovieInputImageBlock
{
    public const string TypeId = "dataprocesses.input.movie-image";
    public const string ControlInputPortId = "control";
    public const string ImageOutputPortId = "image";
    public const string IconPath = "Blocks/MovieInputImage/icon.png";

    public static NodeDefinition Definition { get; } = new(
        TypeId: TypeId,
        DisplayName: "MovieInputImage",
        Category: "Sources",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(
                ControlInputPortId,
                "Control",
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
        Title: "MovieInputImage",
        Subtitle: "Movie playback",
        IconPath: IconPath,
        DashboardWidget: new DashboardWidgetDefinition(
            IsVisibleByDefault: true,
            GridWidth: 3,
            GridHeight: 2));
}