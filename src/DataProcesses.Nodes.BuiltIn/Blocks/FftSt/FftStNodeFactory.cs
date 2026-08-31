using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.FftSt;

/// <summary>
/// Creates independent runtime instances of the FFTst Block.
/// </summary>
public sealed class FftStNodeFactory : INodeFactory
{
    public NodeDefinition Definition => FftStBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new FftStNode();
    }
}