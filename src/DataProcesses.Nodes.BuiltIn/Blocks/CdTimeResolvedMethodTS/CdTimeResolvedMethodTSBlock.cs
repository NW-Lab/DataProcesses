using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.CdTimeResolvedMethodTS;

/// <summary>
/// Declares the stable identity and Stream-to-Vector port contract for the CdTimeResolvedMethodTS Block.
/// </summary>
public static class CdTimeResolvedMethodTSBlock
{
    public const string TypeId = "dataprocesses.analysis.cd-time-resolved-method-st";
    public const string InputPortId = "stream-in";
    public const string OutputPortId = "cd-time-resolved";
    public const string IconPath = "Blocks/CdTimeResolvedMethodTS/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "CdTimeResolvedMethodTS",
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
                "CD Vector",
                PortDirection.Output,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.NumericVector1D),
        ],
        NodeType: NodeType.BasicProcess,
        Title: "CdTimeResolvedMethodTS",
        Subtitle: "CD time-resolved vector",
        IconPath: IconPath);
}