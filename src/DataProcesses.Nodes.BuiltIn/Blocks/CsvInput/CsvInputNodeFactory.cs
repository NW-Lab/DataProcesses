using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.CsvInput;

/// <summary>
/// Creates runtime instances of the CSV Input Block.
/// </summary>
public sealed class CsvInputNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => CsvInputBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new CsvInputNode(CsvInputSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new CsvInputNode(CsvInputSettings.FromJson(settingsJson));
    }
}
