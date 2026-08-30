using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.CameraInputImage;

/// <summary>
/// Creates runtime instances of the CameraInputImage Block.
/// </summary>
public sealed class CameraInputImageNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => CameraInputImageBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new CameraInputImageNode(nodeId, CameraInputImageSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new CameraInputImageNode(nodeId, CameraInputImageSettings.FromJson(settingsJson));
    }
}