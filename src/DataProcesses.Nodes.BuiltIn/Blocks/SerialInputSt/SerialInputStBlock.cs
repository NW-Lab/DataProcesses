using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.SerialInputSt;

/// <summary>
/// Declares the stable identity and Fast Stream port for the Serial Input ST Block.
/// </summary>
public static class SerialInputStBlock
{
    public const string TypeId = "dataprocesses.input.serial-st";
    public const string StreamPortId = "stream";
    public const string IconPath = "Blocks/SerialInputSt/icon.png";

    public static NodeDefinition Definition { get; } = new(
        TypeId: TypeId,
        DisplayName: "Serial Input ST",
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
        Title: "SerialInputSt",
        Subtitle: "Arduino CSV",
        IconPath: IconPath,
        DashboardWidget: new DashboardWidgetDefinition(
            IsVisibleByDefault: false,
            GridWidth: 2,
            GridHeight: 1));
}