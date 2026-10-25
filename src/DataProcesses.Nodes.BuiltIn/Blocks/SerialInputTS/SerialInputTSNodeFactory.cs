using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.SerialInputTS;

/// <summary>
/// Creates runtime instances of the Serial Input TS Block.
/// </summary>
public sealed class SerialInputTSNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => SerialInputTSBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new SerialInputTSNode(SerialInputTSSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new SerialInputTSNode(SerialInputTSSettings.FromJson(settingsJson));
    }
}