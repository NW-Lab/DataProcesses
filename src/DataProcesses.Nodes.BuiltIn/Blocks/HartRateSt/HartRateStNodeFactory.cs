using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.HartRateSt;

/// <summary>
/// Creates independent runtime instances of the HartRateSt Block.
/// </summary>
public sealed class HartRateStNodeFactory : INodeFactory
{
    public NodeDefinition Definition => HartRateStBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new HartRateStNode();
    }
}