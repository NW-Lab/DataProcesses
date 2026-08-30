using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.MovieInputImage;

/// <summary>
/// Creates runtime instances of the MovieInputImage Block.
/// </summary>
public sealed class MovieInputImageNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => MovieInputImageBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new MovieInputImageNode(nodeId, MovieInputImageSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new MovieInputImageNode(nodeId, MovieInputImageSettings.FromJson(settingsJson));
    }
}