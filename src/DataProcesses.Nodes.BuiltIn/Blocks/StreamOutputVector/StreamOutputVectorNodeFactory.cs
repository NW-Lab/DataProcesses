using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamOutputVector;

/// <summary>
/// Creates independent runtime instances of the StreamOutputVector Block.
/// </summary>
public sealed class StreamOutputVectorNodeFactory : INodeFactory
{
    public NodeDefinition Definition => StreamOutputVectorBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new StreamOutputVectorNode();
    }
}

