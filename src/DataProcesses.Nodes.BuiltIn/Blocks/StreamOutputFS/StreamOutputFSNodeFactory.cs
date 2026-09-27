using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamOutputFS;

/// <summary>
/// Creates independent runtime instances of the StreamOutputFS Block.
/// </summary>
public sealed class StreamOutputFSNodeFactory : INodeFactory
{
    public NodeDefinition Definition => StreamOutputFSBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new StreamOutputFSNode();
    }
}