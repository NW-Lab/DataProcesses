using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.BleInputFS;

public sealed class BleInputFSNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => BleInputFSBlock.Definition;

    public INode CreateNode(string nodeId) => CreateNode(nodeId, "{}");

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new BleInputFSNode(BleInputFSSettings.FromJson(settingsJson));
    }
}
