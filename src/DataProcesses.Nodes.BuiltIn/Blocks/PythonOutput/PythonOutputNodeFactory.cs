using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.PythonOutput;

/// <summary>
/// Creates independent runtime instances of the Python Output Block.
/// </summary>
public sealed class PythonOutputNodeFactory : INodeFactory
{
    public NodeDefinition Definition => PythonOutputBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new PythonOutputNode();
    }
}
