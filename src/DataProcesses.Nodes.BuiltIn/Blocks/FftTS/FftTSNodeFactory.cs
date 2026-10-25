using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.FftTS;

/// <summary>
/// Creates independent runtime instances of the FFT_TS Block.
/// </summary>
public sealed class FftTSNodeFactory : INodeFactory
{
    public NodeDefinition Definition => FftTSBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new FftTSNode();
    }
}