namespace DataProcesses.Plugin.Abstractions;

public enum PortDirection
{
    Input,
    Output,
}

public enum NodeType
{
    Input = 0,
    BasicProcess = 1,
    Output = 2,
    Debug = 3,
}

/// <summary>
/// Refines the shape or semantic contract within a <see cref="PortDataKind"/> family.
/// </summary>
public enum PortDataSchema
{
    Unspecified = 0,
    TimeSeries1D = 1,
    Spectrum1D = 2,
    NumericVector1D = 3,
    NumericMatrix2D = 4,
    Image2D = 5,
    JsonEnvelope = 6,
}

public sealed record PortDefinition(
    string Id,
    string DisplayName,
    PortDirection Direction,
    PortDataKind DataKind,
    bool IsRequired = true,
    PortDataSchema DataSchema = PortDataSchema.Unspecified);

/// <summary>
/// Stable metadata and port contract for a Block type.
/// </summary>
public sealed record NodeDefinition(
    string TypeId,
    string DisplayName,
    string Category,
    string Version,
    IReadOnlyList<PortDefinition> Ports,
    NodeType NodeType = NodeType.BasicProcess,
    string? Title = null,
    string? Subtitle = null,
    string? IconPath = null,
    DashboardWidgetDefinition? DashboardWidget = null);

/// <summary>
/// Default dashboard widget settings for newly placed Block instances.
/// The desktop host may let a user override these values per placed Block.
/// </summary>
/// <param name="IsVisibleByDefault">Whether a dashboard widget is created by default.</param>
/// <param name="GridWidth">Default dashboard widget width in grid cells.</param>
/// <param name="GridHeight">Default dashboard widget height in grid cells.</param>
public sealed record DashboardWidgetDefinition(
    bool IsVisibleByDefault = false,
    int GridWidth = 2,
    int GridHeight = 2);

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

/// <summary>
/// Optional contract for nodes that need source-connection metadata when receiving a packet.
/// Hosts call this in preference to <see cref="INode.OnPacketAsync"/> when available.
/// </summary>
public interface IConnectionAwareNode : INode
{
    ValueTask OnPacketAsync(
        string inputPortId,
        IDataPacket packet,
        string sourceNodeId,
        string sourcePortId,
        string? connectionTag,
        CancellationToken cancellationToken);
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