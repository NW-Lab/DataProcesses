using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.CsvOutput;

/// <summary>
/// Creates runtime instances of the CSV Output Block.
/// </summary>
public sealed class CsvOutputNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => CsvOutputBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new CsvOutputNode(nodeId, CsvOutputSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new CsvOutputNode(nodeId, CsvOutputSettings.FromJson(settingsJson));
    }
}
