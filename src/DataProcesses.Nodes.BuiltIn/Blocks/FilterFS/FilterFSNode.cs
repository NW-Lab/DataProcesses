using DataProcesses.Nodes.BuiltIn.Blocks.FilterTS;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.FilterFS;

/// <summary>
/// Applies one of five Fast Stream filters while preserving the frame's exact sample-rate metadata.
/// </summary>
public sealed class FilterFSNode : INode
{
    private readonly FilterTSNode implementation;

    public FilterFSNode(FilterFSSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        implementation = new FilterTSNode(settings.ToFilterTSSettings());
    }

    public NodeDefinition Definition => FilterFSBlock.Definition;

    public ValueTask InitializeAsync(INodeContext context, CancellationToken cancellationToken)
    {
        return implementation.InitializeAsync(context, cancellationToken);
    }

    public ValueTask OnPacketAsync(string inputPortId, IDataPacket packet, CancellationToken cancellationToken)
    {
        if (!string.Equals(inputPortId, FilterFSBlock.InputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        ArgumentNullException.ThrowIfNull(packet);
        if (packet is not FastStreamFrame { SampleRateHertz: > 0 })
        {
            throw new ArgumentException("FilterFS requires FS frames with a positive SampleRateHertz.", nameof(packet));
        }

        return implementation.OnPacketAsync(FilterTSBlock.InputPortId, packet, cancellationToken);
    }

    public ValueTask StartAsync(CancellationToken cancellationToken)
    {
        return implementation.StartAsync(cancellationToken);
    }

    public ValueTask StopAsync(CancellationToken cancellationToken)
    {
        return implementation.StopAsync(cancellationToken);
    }
}