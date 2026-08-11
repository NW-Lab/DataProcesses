using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamOutputVector;

/// <summary>
/// Declares the stable identity and Numeric Vector sink contract for the StreamOutputVector Block.
/// </summary>
public static class StreamOutputVectorBlock
{
    public const string TypeId = "dataprocesses.output.numeric-vector";
    public const string InputPortId = "input";
    public const string IconPath = "Blocks/StreamOutputVector/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "StreamOutputVector",
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
        Title: "StreamOutputVector",
        Subtitle: "Debug vector",
        IconPath: IconPath,
        DashboardWidget: new DashboardWidgetDefinition(
            IsVisibleByDefault: true,
            GridWidth: 3,
            GridHeight: 2));
}


