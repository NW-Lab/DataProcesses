using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.BreathTS;

/// <summary>
/// Declares the stable identity and port contract for the BreathTS Block.
/// </summary>
public static class BreathTSBlock
{
    public const string TypeId = "dataprocesses.analysis.breath-st";
    public const string InputPortId = "stream-in";
    public const string RateOutputPortId = "breath-rate";
    public const string EventOutputPortId = "events";
    public const string IconPath = "Blocks/BreathTS/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "BreathTS",
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
        Title: "BreathTS",
        Subtitle: "Respiration rate",
        IconPath: IconPath);
}