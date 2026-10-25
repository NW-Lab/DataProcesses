using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.BleInputTS;

/// <summary>
/// Declares the stable identity and Fast Stream port for the BLE Input TS Block.
/// </summary>
public static class BleInputTSBlock
{
    public const string TypeId = "dataprocesses.input.ble-st";
    public const string StreamPortId = "stream";
    public const string IconPath = "Blocks/BleInputTS/icon.png";

    public static NodeDefinition Definition { get; } = new(
        TypeId: TypeId,
        DisplayName: "BLE Input TS",
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
        Title: "BleInputTS",
        Subtitle: "BLE GATT",
        IconPath: IconPath,
        DashboardWidget: new DashboardWidgetDefinition(
            IsVisibleByDefault: false,
            GridWidth: 2,
            GridHeight: 1));
}