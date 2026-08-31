using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.SerialInputSt;

/// <summary>
/// Creates runtime instances of the Serial Input ST Block.
/// </summary>
public sealed class SerialInputStNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => SerialInputStBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new SerialInputStNode(SerialInputStSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new SerialInputStNode(SerialInputStSettings.FromJson(settingsJson));
    }
}