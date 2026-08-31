using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.HumansImage;

/// <summary>
/// Creates independent runtime instances of the HumansImage Block.
/// </summary>
public sealed class HumansImageNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => HumansImageBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new HumansImageNode(HumansImageSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new HumansImageNode(HumansImageSettings.FromJson(settingsJson));
    }
}