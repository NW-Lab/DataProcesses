using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.SerialInputVector;

/// <summary>
/// Creates runtime instances of the Serial Input Vector Block.
/// </summary>
public sealed class SerialInputVectorNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => SerialInputVectorBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new SerialInputVectorNode(SerialInputVectorSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new SerialInputVectorNode(SerialInputVectorSettings.FromJson(settingsJson));
    }
}