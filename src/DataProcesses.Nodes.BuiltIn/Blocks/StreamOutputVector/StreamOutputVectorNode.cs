using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamOutputVector;

/// <summary>
/// Captures the latest numeric vector packet for debug-oriented inspection.
/// </summary>
public sealed class StreamOutputVectorNode : INode
{
    public const int MaximumValues = 1_024;

    private bool isInitialized;

    public NodeDefinition Definition => StreamOutputVectorBlock.Definition;

    public StreamOutputVectorSnapshot? LatestSnapshot { get; private set; }

    public ValueTask InitializeAsync(
        INodeContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);
        isInitialized = true;
        return ValueTask.CompletedTask;
    }

    public ValueTask OnPacketAsync(
        string inputPortId,
        IDataPacket packet,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPortId);
        ArgumentNullException.ThrowIfNull(packet);
        cancellationToken.ThrowIfCancellationRequested();

        if (!isInitialized)
        {
            throw new InvalidOperationException("The node must be initialized before it receives packets.");
        }

        if (!string.Equals(inputPortId, StreamOutputVectorBlock.InputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        if (packet is not NumericVectorFrame vector)
        {
            throw new ArgumentException("StreamOutputVector accepts Numeric Vector input only.", nameof(packet));
        }

        var values = Downsample(vector.Values);
        LatestSnapshot = new StreamOutputVectorSnapshot(
            vector.Name,
            values,
            vector.Length,
            vector.SequenceNumber,
            vector.Timestamp);

        return ValueTask.CompletedTask;
    }

    public ValueTask StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    private static ReadOnlyMemory<double> Downsample(ReadOnlyMemory<double> source)
    {
        if (source.Length <= MaximumValues)
        {
            return source;
        }

        return DownsampleCore(source.Span);
    }

    private static ReadOnlyMemory<double> DownsampleCore(ReadOnlySpan<double> source)
    {
        if (source.Length <= MaximumValues)
        {
            return source.Length == 0 ? [] : source.ToArray();
        }

        var sampled = new double[MaximumValues];
        for (var index = 0; index < sampled.Length; index++)
        {
            var sourceIndex = index * (source.Length - 1) / (sampled.Length - 1);
            sampled[index] = source[sourceIndex];
        }

        return sampled;
    }
}


