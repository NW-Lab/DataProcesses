using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.MqttOut;

public sealed class MqttOutNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => MqttOutBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        return CreateNode(nodeId, "{}");
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        ArgumentNullException.ThrowIfNull(settingsJson);
        return new MqttOutNode(nodeId, MqttOutSettings.FromJson(settingsJson));
    }
}