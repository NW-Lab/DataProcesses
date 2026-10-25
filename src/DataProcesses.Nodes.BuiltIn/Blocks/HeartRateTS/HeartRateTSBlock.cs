using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.HeartRateTS;

/// <summary>
/// Declares the stable identity and Fast Stream port contract for the HeartRateTS Block.
/// </summary>
public static class HeartRateTSBlock
{
    public const string TypeId = "dataprocesses.analysis.heart-rate-st";

    // Misspelled ID persisted by older Flows; migrated to TypeId on load.
    public const string LegacyTypeId = "dataprocesses.analysis.hart-rate-st";
    public const string InputPortId = "stream-in";
    public const string OutputPortId = "heart-rate";
    public const string IconPath = "Blocks/HeartRateTS/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "HeartRateTS",
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
        Title: "HeartRateTS",
        Subtitle: "Heart rate",
        IconPath: IconPath);
}