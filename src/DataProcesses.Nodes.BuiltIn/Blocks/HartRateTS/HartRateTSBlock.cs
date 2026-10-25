using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.HartRateTS;

/// <summary>
/// Declares the stable identity and Fast Stream port contract for the HartRateTS Block.
/// </summary>
public static class HartRateTSBlock
{
    public const string TypeId = "dataprocesses.analysis.hart-rate-st";
    public const string InputPortId = "stream-in";
    public const string OutputPortId = "heart-rate";
    public const string IconPath = "Blocks/HartRateTS/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "HartRateTS",
        Category: "Signal Analysis",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(
                InputPortId,
                "Stream In",
                PortDirection.Input,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.TimeSeries1D),
            new PortDefinition(
                OutputPortId,
                "Heart Rate",
                PortDirection.Output,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.TimeSeries1D),
        ],
        NodeType: NodeType.BasicProcess,
        Title: "HartRateTS",
        Subtitle: "Heart rate",
        IconPath: IconPath);
}