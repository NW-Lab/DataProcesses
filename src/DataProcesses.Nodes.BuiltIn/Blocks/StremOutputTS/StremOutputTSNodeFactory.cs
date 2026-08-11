using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.StremOutputTS;

/// <summary>
/// Creates independent runtime instances of the StremOutputTS Block.
/// </summary>
public sealed class StremOutputTSNodeFactory : INodeFactory
{
    public NodeDefinition Definition => StremOutputTSBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new StremOutputTSNode();
    }
}

