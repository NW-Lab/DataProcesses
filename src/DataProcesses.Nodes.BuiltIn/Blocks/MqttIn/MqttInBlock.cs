using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.MqttIn;

public static class MqttInBlock
{
    public const string TypeId = "dataprocesses.input.mqtt-in";
    public const string OutputPortId = "message";
    public const string IconPath = "Blocks/Trigger/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "MQTT_In",
        Category: "Sources",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(
                OutputPortId,
                "JSON Message",
                PortDirection.Output,
                PortDataKind.JsonMessage,
                DataSchema: PortDataSchema.JsonEnvelope),
        ],
        NodeType: NodeType.Input,
        Title: "MQTT_In",
        Subtitle: "MQTT JSON input",
        IconPath: IconPath);
}