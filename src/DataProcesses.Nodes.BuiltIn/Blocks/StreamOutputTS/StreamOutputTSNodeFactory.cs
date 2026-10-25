using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamOutputTS;

/// <summary>
/// Creates independent runtime instances of the StreamOutputTS Block.
/// </summary>
public sealed class StreamOutputTSNodeFactory : INodeFactory
{
    public NodeDefinition Definition => StreamOutputTSBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new StreamOutputTSNode();
    }
}

