using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.BreathSt;

/// <summary>
/// Declares the stable identity and port contract for the BreathSt Block.
/// </summary>
public static class BreathStBlock
{
    public const string TypeId = "dataprocesses.analysis.breath-st";
    public const string InputPortId = "stream-in";
    public const string RateOutputPortId = "breath-rate";
    public const string EventOutputPortId = "events";
    public const string IconPath = "Blocks/BreathSt/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "BreathSt",
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
                RateOutputPortId,
                "Breath Rate",
                PortDirection.Output,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.TimeSeries1D),
            new PortDefinition(
                EventOutputPortId,
                "Events",
                PortDirection.Output,
                PortDataKind.JsonMessage,
                IsRequired: false,
                DataSchema: PortDataSchema.JsonEnvelope),
        ],
        NodeType: NodeType.BasicProcess,
        Title: "BreathSt",
        Subtitle: "Respiration rate",
        IconPath: IconPath);
}