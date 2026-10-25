using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.BleInputTS;

/// <summary>
/// Creates runtime instances of the BLE Input TS Block.
/// </summary>
public sealed class BleInputTSNodeFactory : IConfiguredNodeFactory
{
    public NodeDefinition Definition => BleInputTSBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new BleInputTSNode(BleInputTSSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return new BleInputTSNode(BleInputTSSettings.FromJson(settingsJson));
    }
}