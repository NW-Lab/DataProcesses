using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.SerialInputTS;

/// <summary>
/// Declares the stable identity and Fast Stream port for the Serial Input TS Block.
/// </summary>
public static class SerialInputTSBlock
{
    public const string TypeId = "dataprocesses.input.serial-st";
    public const string StreamPortId = "stream";
    public const string IconPath = "Blocks/SerialInputTS/icon.png";

    public static NodeDefinition Definition { get; } = new(
        TypeId: TypeId,
        DisplayName: "Serial Input TS",
        Category: "Sources",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(
                Id: StreamPortId,
                DisplayName: "Stream",
                Direction: PortDirection.Output,
                DataKind: PortDataKind.FastStream,
                IsRequired: false,
                DataSchema: PortDataSchema.TimeSeries1D),
        ],
        NodeType: NodeType.Input,
        Title: "SerialInputTS",
        Subtitle: "Arduino CSV",
        IconPath: IconPath,
        DashboardWidget: new DashboardWidgetDefinition(
            IsVisibleByDefault: false,
            GridWidth: 2,
            GridHeight: 1));
}