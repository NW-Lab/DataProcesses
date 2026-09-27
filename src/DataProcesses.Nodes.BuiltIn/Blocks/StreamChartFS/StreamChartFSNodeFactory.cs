using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamChartFS;

/// <summary>
/// Creates independent runtime instances of the StreamChartFS Block.
/// </summary>
public sealed class StreamChartFSNodeFactory : INodeFactory
{
    public NodeDefinition Definition => StreamChartFSBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new StreamChartFSNode();
    }
}