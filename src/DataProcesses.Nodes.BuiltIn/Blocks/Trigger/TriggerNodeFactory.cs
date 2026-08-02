using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.Trigger;

/// <summary>
/// Creates runtime instances of the Trigger Block.
/// </summary>
public sealed class TriggerNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => TriggerBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new TriggerNode(nodeId, TriggerSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new TriggerNode(nodeId, TriggerSettings.FromJson(settingsJson));
    }
}
