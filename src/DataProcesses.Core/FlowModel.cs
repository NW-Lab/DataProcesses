using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Core;

public sealed record FlowDocument(
    Guid Id,
    string Name,
    IReadOnlyList<NodeInstance> Nodes,
    IReadOnlyList<Connection> Connections,
    int SchemaVersion = 1);

public sealed record NodeInstance(
    string Id,
    string TypeId,
    double X,
    double Y,
    string SettingsJson);

public sealed record Connection(
    string SourceNodeId,
    string SourcePortId,
    string TargetNodeId,
    string TargetPortId,
    PortDataKind DataKind);

public static class ConnectionValidator
{
    public static bool CanConnect(PortDefinition source, PortDefinition target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        return source.Direction == PortDirection.Output
            && target.Direction == PortDirection.Input
            && source.DataKind == target.DataKind;
    }
}