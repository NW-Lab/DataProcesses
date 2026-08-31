using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.SerialInputVector;

/// <summary>
/// Declares the stable identity and vector port for the Serial Input Vector Block.
/// </summary>
public static class SerialInputVectorBlock
{
    public const string TypeId = "dataprocesses.input.serial-vector";
    public const string VectorPortId = "vector";
    public const string IconPath = "Blocks/SerialInputSt/icon.png";

    public static NodeDefinition Definition { get; } = new(
        TypeId: TypeId,
        DisplayName: "Serial Input Vector",
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
        Title: "SerialInputVector",
        Subtitle: "IMU XYZ CSV",
        IconPath: IconPath,
        DashboardWidget: new DashboardWidgetDefinition(
            IsVisibleByDefault: false,
            GridWidth: 2,
            GridHeight: 1));
}