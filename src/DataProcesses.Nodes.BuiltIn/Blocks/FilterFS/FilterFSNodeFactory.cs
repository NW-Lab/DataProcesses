using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.FilterFS;

/// <summary>
/// Creates configured runtime instances of the FilterFS Block.
/// </summary>
public sealed class FilterFSNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => FilterFSBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new FilterFSNode(FilterFSSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new FilterFSNode(FilterFSSettings.FromJson(settingsJson));
    }
}