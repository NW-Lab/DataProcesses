using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamChartSt;

/// <summary>
/// Creates runtime instances of the StreamChartSt Block.
/// </summary>
public sealed class StreamChartStNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => StreamChartStBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new StreamChartStNode(StreamChartStSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new StreamChartStNode(StreamChartStSettings.FromJson(settingsJson));
    }
}
