using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.FftFS;

/// <summary>
/// Creates independent runtime instances of the FFT_FS Block.
/// </summary>
public sealed class FftFSNodeFactory : INodeFactory
{
    public NodeDefinition Definition => FftFSBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new FftFSNode();
    }
}