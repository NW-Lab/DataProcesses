using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.HeartRateImage;

/// <summary>
/// Creates independent runtime instances of the HeartRateImage Block.
/// </summary>
public sealed class HeartRateImageNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => HeartRateImageBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new HeartRateImageNode(HeartRateImageSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new HeartRateImageNode(HeartRateImageSettings.FromJson(settingsJson));
    }
}