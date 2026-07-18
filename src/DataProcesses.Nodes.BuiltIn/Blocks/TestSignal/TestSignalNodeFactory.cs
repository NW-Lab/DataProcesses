using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.TestSignal;

/// <summary>
/// Creates runtime instances of the Test Signal Block.
/// </summary>
public sealed class TestSignalNodeFactory : INodeFactory
{
    public NodeDefinition Definition => TestSignalBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new TestSignalNode();
    }
}