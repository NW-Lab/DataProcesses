using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.MqttIn;

public sealed class MqttInNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => MqttInBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        return CreateNode(nodeId, "{}");
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        ArgumentNullException.ThrowIfNull(settingsJson);
        return new MqttInNode(nodeId, MqttInSettings.FromJson(settingsJson));
    }
}