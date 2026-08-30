using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamChartVector;

/// <summary>
/// Creates independent runtime instances of the StreamChartVector Block.
/// </summary>
public sealed class StreamChartVectorNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => StreamChartVectorBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new StreamChartVectorNode(StreamChartVectorSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new StreamChartVectorNode(StreamChartVectorSettings.FromJson(settingsJson));
    }
}
