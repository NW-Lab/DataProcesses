using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.PayloadOutput;

/// <summary>
/// Creates independent runtime instances of the Payload Output Block.
/// </summary>
public sealed class PayloadOutputNodeFactory : INodeFactory
{
    public NodeDefinition Definition => PayloadOutputBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new PayloadOutputNode();
    }
}
