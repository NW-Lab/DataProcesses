using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamOutputImage;

/// <summary>
/// Creates independent runtime instances of the StreamOutputImage Block.
/// </summary>
public sealed class StreamOutputImageNodeFactory : INodeFactory
{
    public NodeDefinition Definition => StreamOutputImageBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new StreamOutputImageNode();
    }
}

