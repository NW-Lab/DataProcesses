using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.TestSignalFS;

/// <summary>
/// Creates configured runtime instances of the TestSignal(FS) Block.
/// </summary>
public sealed class TestSignalFSNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => TestSignalFSBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new TestSignalFSNode(TestSignalFSSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new TestSignalFSNode(TestSignalFSSettings.FromJson(settingsJson));
    }
}