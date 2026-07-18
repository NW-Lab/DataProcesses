using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.TestSignal;

/// <summary>
/// Creates runtime instances of the Test Signal Block.
/// </summary>
public sealed class TestSignalNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => TestSignalBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new TestSignalNode(TestSignalSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new TestSignalNode(TestSignalSettings.FromJson(settingsJson));
    }
}