using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.BleInputVector;

/// <summary>
/// Declares the stable identity and vector port for the BLE Input Vector Block.
/// </summary>
public static class BleInputVectorBlock
{
    public const string TypeId = "dataprocesses.input.ble-vector";
    public const string VectorPortId = "vector";
    public const string IconPath = "Blocks/BleInputSt/icon.png";

    public static NodeDefinition Definition { get; } = new(
        TypeId: TypeId,
        DisplayName: "BLE Input Vector",
        Category: "Sources",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(
                Id: VectorPortId,
                DisplayName: "Vector",
                Direction: PortDirection.Output,
                DataKind: PortDataKind.FastStream,
                IsRequired: false,
                DataSchema: PortDataSchema.NumericVector1D),
        ],
        NodeType: NodeType.Input,
        Title: "BleInputVector",
        Subtitle: "IMU XYZ BLE",
        IconPath: IconPath,
        DashboardWidget: new DashboardWidgetDefinition(
            IsVisibleByDefault: false,
            GridWidth: 2,
            GridHeight: 1));
}