using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.NumericVectorOutput;

/// <summary>
/// Creates independent runtime instances of the Numeric Vector Output Block.
/// </summary>
public sealed class NumericVectorOutputNodeFactory : INodeFactory
{
    public NodeDefinition Definition => NumericVectorOutputBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new NumericVectorOutputNode();
    }
}
