using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.LowPassFilter;

/// <summary>
/// Creates independent runtime instances of the Low-pass Filter Block.
/// </summary>
public sealed class LowPassFilterNodeFactory : INodeFactory
{
    public NodeDefinition Definition => LowPassFilterBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new LowPassFilterNode();
    }
}
