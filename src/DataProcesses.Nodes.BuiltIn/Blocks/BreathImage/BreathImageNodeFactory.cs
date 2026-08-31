using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.BreathImage;

/// <summary>
/// Creates independent runtime instances of the BreathImage Block.
/// </summary>
public sealed class BreathImageNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => BreathImageBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new BreathImageNode(BreathImageSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new BreathImageNode(BreathImageSettings.FromJson(settingsJson));
    }
}