using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.TestSignalImg;

/// <summary>
/// Creates runtime instances of the TestSignalImg Block.
/// </summary>
public sealed class TestSignalImgNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => TestSignalImgBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new TestSignalImgNode(TestSignalImgSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new TestSignalImgNode(TestSignalImgSettings.FromJson(settingsJson));
    }
}
