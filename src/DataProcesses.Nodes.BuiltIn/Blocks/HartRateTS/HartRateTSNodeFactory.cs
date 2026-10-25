using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.HartRateTS;

/// <summary>
/// Creates independent runtime instances of the HartRateTS Block.
/// </summary>
public sealed class HartRateTSNodeFactory : INodeFactory
{
    public NodeDefinition Definition => HartRateTSBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new HartRateTSNode();
    }
}