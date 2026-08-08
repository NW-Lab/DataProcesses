using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.NumericVectorOutput;

/// <summary>
/// Declares the stable identity and Numeric Vector sink contract for the Numeric Vector Output Block.
/// </summary>
public static class NumericVectorOutputBlock
{
    public const string TypeId = "dataprocesses.output.numeric-vector";
    public const string InputPortId = "input";
    public const string IconPath = "Blocks/NumericVectorOutput/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "Numeric Vector Output",
        Category: "Debug",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(
                InputPortId,
                "Input",
                PortDirection.Input,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.NumericVector1D),
        ],
        NodeType: NodeType.Debug,
        Title: "Numeric Vector Output",
        Subtitle: "Debug vector",
        IconPath: IconPath,
        DashboardWidget: new DashboardWidgetDefinition(
            IsVisibleByDefault: true,
            GridWidth: 3,
            GridHeight: 2));
}
