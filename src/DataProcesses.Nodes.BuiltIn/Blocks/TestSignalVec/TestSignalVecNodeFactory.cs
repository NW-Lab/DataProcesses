using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.TestSignalVec;

/// <summary>
/// Creates runtime instances of the TestSignalVec Block.
/// </summary>
public sealed class TestSignalVecNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => TestSignalVecBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new TestSignalVecNode(TestSignalVecSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new TestSignalVecNode(TestSignalVecSettings.FromJson(settingsJson));
    }
}
