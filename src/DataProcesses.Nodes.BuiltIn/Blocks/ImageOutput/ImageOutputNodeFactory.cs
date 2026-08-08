using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.ImageOutput;

/// <summary>
/// Creates independent runtime instances of the Image Output Block.
/// </summary>
public sealed class ImageOutputNodeFactory : INodeFactory
{
    public NodeDefinition Definition => ImageOutputBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new ImageOutputNode();
    }
}
