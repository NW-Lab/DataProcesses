using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.FastFourierTransform;

/// <summary>
/// Creates independent runtime instances of the FFT Block.
/// </summary>
public sealed class FastFourierTransformNodeFactory : INodeFactory
{
    public NodeDefinition Definition => FastFourierTransformBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new FastFourierTransformNode();
    }
}
