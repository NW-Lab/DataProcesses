using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.FilterSt;

/// <summary>
/// Creates independent runtime instances of the FilterSt Block.
/// </summary>
public sealed class FilterStNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => FilterStBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new FilterStNode(FilterStSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new FilterStNode(FilterStSettings.FromJson(settingsJson));
    }
}