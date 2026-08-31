using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.BreathSt;

/// <summary>
/// Creates independent runtime instances of the BreathSt Block.
/// </summary>
public sealed class BreathStNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => BreathStBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new BreathStNode(BreathStSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new BreathStNode(BreathStSettings.FromJson(settingsJson));
    }
}