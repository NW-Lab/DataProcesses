using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.BleInputVector;

/// <summary>
/// Creates runtime instances of the BLE Input Vector Block.
/// </summary>
public sealed class BleInputVectorNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => BleInputVectorBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new BleInputVectorNode(BleInputVectorSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new BleInputVectorNode(BleInputVectorSettings.FromJson(settingsJson));
    }
}