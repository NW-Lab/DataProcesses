using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.HeartRateTS;

/// <summary>
/// Creates independent runtime instances of the HeartRateTS Block.
/// </summary>
public sealed class HeartRateTSNodeFactory : INodeFactory
{
    public NodeDefinition Definition => HeartRateTSBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new HeartRateTSNode();
    }
}