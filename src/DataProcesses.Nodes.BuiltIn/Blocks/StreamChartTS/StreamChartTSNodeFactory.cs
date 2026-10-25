using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamChartTS;

/// <summary>
/// Creates runtime instances of the StreamChartTS Block.
/// </summary>
public sealed class StreamChartTSNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => StreamChartTSBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new StreamChartTSNode(StreamChartTSSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new StreamChartTSNode(StreamChartTSSettings.FromJson(settingsJson));
    }
}
