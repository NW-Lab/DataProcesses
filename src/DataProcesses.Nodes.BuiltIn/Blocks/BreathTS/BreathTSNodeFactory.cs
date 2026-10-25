using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.BreathTS;

/// <summary>
/// Creates independent runtime instances of the BreathTS Block.
/// </summary>
public sealed class BreathTSNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => BreathTSBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new BreathTSNode(BreathTSSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new BreathTSNode(BreathTSSettings.FromJson(settingsJson));
    }
}