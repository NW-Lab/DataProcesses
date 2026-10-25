using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.BleInputFS;

public static class BleInputFSBlock
{
    public const string TypeId = "dataprocesses.input.ble-fs";
    public const string StreamPortId = "stream";

    public static NodeDefinition Definition { get; } = new(
        TypeId,
        "BLE Input FS",
        "Sources",
        "0.1.0",
        [new PortDefinition(StreamPortId, "Stream", PortDirection.Output, PortDataKind.FastStream,
            IsRequired: false, DataSchema: PortDataSchema.FixedRateTimeSeries1D)],
        NodeType: NodeType.Input,
        Title: "BleInputFS",
        Subtitle: "BLE GATT",
        IconPath: "Blocks/BleInputTS/icon.png",
        DashboardWidget: new DashboardWidgetDefinition(IsVisibleByDefault: false));
}
