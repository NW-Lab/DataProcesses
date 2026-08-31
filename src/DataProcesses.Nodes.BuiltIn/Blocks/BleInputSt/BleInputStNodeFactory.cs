using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.BleInputSt;

/// <summary>
/// Creates runtime instances of the BLE Input ST Block.
/// </summary>
public sealed class BleInputStNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => BleInputStBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new BleInputStNode(BleInputStSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new BleInputStNode(BleInputStSettings.FromJson(settingsJson));
    }
}