using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.CdTimeResolvedMethodSt;

/// <summary>
/// Creates independent runtime instances of the CdTimeResolvedMethodSt Block.
/// </summary>
public sealed class CdTimeResolvedMethodStNodeFactory : INodeFactory
{
    public NodeDefinition Definition => CdTimeResolvedMethodStBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new CdTimeResolvedMethodStNode();
    }
}