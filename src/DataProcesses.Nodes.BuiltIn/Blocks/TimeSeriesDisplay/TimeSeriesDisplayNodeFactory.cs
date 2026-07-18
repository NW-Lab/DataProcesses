using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.TimeSeriesDisplay;

/// <summary>
/// Creates independent runtime instances of the Time Series Block.
/// </summary>
public sealed class TimeSeriesDisplayNodeFactory : INodeFactory
{
    public NodeDefinition Definition => TimeSeriesDisplayBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new TimeSeriesDisplayNode();
    }
}
