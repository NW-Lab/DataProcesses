using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.HartRateImage;

/// <summary>
/// Creates independent runtime instances of the HartRateImage Block.
/// </summary>
public sealed class HartRateImageNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => HartRateImageBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new HartRateImageNode(HartRateImageSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new HartRateImageNode(HartRateImageSettings.FromJson(settingsJson));
    }
}