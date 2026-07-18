namespace DataProcesses.Plugin.Abstractions;

public enum PortDirection
{
    Input,
    Output,
}

public enum NodeType
{
    Input,
    BasicProcess,
    Output,
}

public sealed record PortDefinition(
    string Id,
    string DisplayName,
    PortDirection Direction,
    PortDataKind DataKind,
    bool IsRequired = true);

public sealed record NodeDefinition(
    string TypeId,
    string DisplayName,
    string Category,
    string Version,
    IReadOnlyList<PortDefinition> Ports,
    NodeType NodeType = NodeType.BasicProcess);

public interface INode
{
    NodeDefinition Definition { get; }

    ValueTask InitializeAsync(INodeContext context, CancellationToken cancellationToken);

    ValueTask OnPacketAsync(
        string inputPortId,
        IDataPacket packet,
        CancellationToken cancellationToken);

    ValueTask StartAsync(CancellationToken cancellationToken);

    ValueTask StopAsync(CancellationToken cancellationToken);
}

public interface INodeContext
{
    string NodeId { get; }

    ValueTask EmitAsync(
        string outputPortId,
        IDataPacket packet,
        CancellationToken cancellationToken);
}

public interface INodeFactory
{
    NodeDefinition Definition { get; }

    INode CreateNode(string nodeId);
}

public interface INodePlugin
{
    string Id { get; }

    string Version { get; }

    IReadOnlyCollection<INodeFactory> NodeFactories { get; }
}