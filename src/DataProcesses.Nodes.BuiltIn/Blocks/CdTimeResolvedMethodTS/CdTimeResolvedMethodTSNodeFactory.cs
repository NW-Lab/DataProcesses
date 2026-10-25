using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.CdTimeResolvedMethodTS;

/// <summary>
/// Creates independent runtime instances of the CdTimeResolvedMethodTS Block.
/// </summary>
public sealed class CdTimeResolvedMethodTSNodeFactory : INodeFactory
{
    public NodeDefinition Definition => CdTimeResolvedMethodTSBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new CdTimeResolvedMethodTSNode();
    }
}