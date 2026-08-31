using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.MovingAverage;

/// <summary>
/// Creates independent runtime instances of the Moving Average Block.
/// </summary>
public sealed class MovingAverageNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => MovingAverageBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new MovingAverageNode(MovingAverageSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new MovingAverageNode(MovingAverageSettings.FromJson(settingsJson));
    }
}