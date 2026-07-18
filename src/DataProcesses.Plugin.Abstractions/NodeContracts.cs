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
    NodeType NodeType = NodeType.BasicProcess,
    string? Title = null,
    string? Subtitle = null,
    string? IconPath = null);

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

/// <summary>
/// Optional factory contract for Blocks that need their persisted settings when creating a runtime node.
/// Implementations must validate the JSON they consume and keep unsupported fields compatible unless a
/// future contract version explicitly changes the settings schema.
/// </summary>
public interface IConfiguredNodeFactory : INodeFactory
{
    INode CreateNode(string nodeId, string settingsJson);
}

public interface INodePlugin
{
    string Id { get; }

    string Version { get; }

    IReadOnlyCollection<INodeFactory> NodeFactories { get; }
}