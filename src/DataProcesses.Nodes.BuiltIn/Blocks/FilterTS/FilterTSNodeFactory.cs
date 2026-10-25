using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.FilterTS;

/// <summary>
/// Creates independent runtime instances of the FilterTS Block.
/// </summary>
public sealed class FilterTSNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => FilterTSBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new FilterTSNode(FilterTSSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new FilterTSNode(FilterTSSettings.FromJson(settingsJson));
    }
}