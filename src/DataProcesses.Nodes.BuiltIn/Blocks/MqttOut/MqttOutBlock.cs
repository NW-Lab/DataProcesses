using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.MqttOut;

public static class MqttOutBlock
{
    public const string TypeId = "dataprocesses.output.mqtt-out";
    public const string InputPortId = "message";
    public const string IconPath = "Blocks/PayloadOutput/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "MQTT_Out",
        Category: "Output",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(
                InputPortId,
                "JSON Message",
                PortDirection.Input,
                PortDataKind.JsonMessage,
                DataSchema: PortDataSchema.JsonEnvelope),
        ],
        NodeType: NodeType.Output,
        Title: "MQTT_Out",
        Subtitle: "MQTT JSON output",
        IconPath: IconPath);
}