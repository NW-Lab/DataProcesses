using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.AudioOutFS;

/// <summary>
/// Declares the Fast Stream input and Dashboard controls for audio playback and recording.
/// </summary>
public static class AudioOutFSBlock
{
    public const string TypeId = "dataprocesses.output.audio-fs";
    public const string InputPortId = "input";
    public const string IconPath = "Blocks/AudioOutFS/icon.png";

    public static NodeDefinition Definition { get; } = new(
        TypeId: TypeId,
        DisplayName: "AudioOutFS",
        Category: "Output",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(
                InputPortId,
                "Input",
                PortDirection.Input,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.FixedRateTimeSeries1D),
        ],
        NodeType: NodeType.Output,
        Title: "AudioOutFS",
        Subtitle: "Fast Stream Audio Output",
        IconPath: IconPath,
        DashboardWidget: new DashboardWidgetDefinition(
            IsVisibleByDefault: true,
            GridWidth: 3,
            GridHeight: 2));
}