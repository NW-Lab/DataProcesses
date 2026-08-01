using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamOutput;

/// <summary>
/// Creates independent runtime instances of the Stream Output Block.
/// </summary>
public sealed class StreamOutputNodeFactory : INodeFactory
{
    public NodeDefinition Definition => StreamOutputBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new StreamOutputNode();
    }
}
