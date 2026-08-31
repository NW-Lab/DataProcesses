using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.CdTimeResolvedMethodSt;

/// <summary>
/// Declares the stable identity and Stream-to-Vector port contract for the CdTimeResolvedMethodSt Block.
/// </summary>
public static class CdTimeResolvedMethodStBlock
{
    public const string TypeId = "dataprocesses.analysis.cd-time-resolved-method-st";
    public const string InputPortId = "stream-in";
    public const string OutputPortId = "cd-time-resolved";
    public const string IconPath = "Blocks/CdTimeResolvedMethodSt/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "CdTimeResolvedMethodSt",
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
        Title: "CdTimeResolvedMethodSt",
        Subtitle: "CD time-resolved vector",
        IconPath: IconPath);
}