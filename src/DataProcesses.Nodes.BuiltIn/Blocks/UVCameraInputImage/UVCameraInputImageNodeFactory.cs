using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.UVCameraInputImage;

/// <summary>
/// Creates runtime instances of the UVCameraInputImage Block.
/// </summary>
public sealed class UVCameraInputImageNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => UVCameraInputImageBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new UVCameraInputImageNode(nodeId, UVCameraInputImageSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new UVCameraInputImageNode(nodeId, UVCameraInputImageSettings.FromJson(settingsJson));
    }
}