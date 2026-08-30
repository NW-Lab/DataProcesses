using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamChartVector;

/// <summary>
/// Accumulates Numeric Vector frames into a time-bounded intensity chart window.
/// </summary>
public sealed class StreamChartVectorNode : INode
{
    private readonly StreamChartVectorHistory history = new();
    private DateTimeOffset? firstTimestamp;
    private bool isInitialized;

    public StreamChartVectorNode(StreamChartVectorSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();
        Settings = settings;
    }

    public NodeDefinition Definition => StreamChartVectorBlock.Definition;

    public StreamChartVectorSettings Settings { get; }

    public StreamChartVectorSnapshot? LatestSnapshot { get; private set; }

    public ValueTask InitializeAsync(INodeContext context, CancellationToken cancellationToken)
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

        if (!string.Equals(inputPortId, StreamChartVectorBlock.InputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        if (packet is not NumericVectorFrame vector)
        {
            throw new ArgumentException("StreamChartVector accepts Numeric Vector input only.", nameof(packet));
        }

        var milliseconds = ResolveMilliseconds(vector);
        history.Append(milliseconds, vector.Values.Span, Settings.TimeSpanMilliseconds);

        LatestSnapshot = new StreamChartVectorSnapshot(
            vector.Name,
            history.ColumnCount,
            history.RowCount,
            milliseconds,
            Settings.TimeSpanMilliseconds,
            vector.SequenceNumber);

        return ValueTask.CompletedTask;
    }

    public StreamChartVectorImage Render(int pixelWidth = StreamChartVectorHistory.DefaultPixelWidth)
    {
        return history.Render(Settings, pixelWidth);
    }

    public ValueTask StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        history.Clear();
        firstTimestamp = null;
        LatestSnapshot = null;
        return ValueTask.CompletedTask;
    }

    // Frames without a timestamp fall back to the sequence number as the millisecond position.
    private double ResolveMilliseconds(NumericVectorFrame vector)
    {
        if (vector.Timestamp is not { } timestamp)
        {
            return vector.SequenceNumber;
        }

        firstTimestamp ??= timestamp;
        return Math.Max(0, (timestamp - firstTimestamp.Value).TotalMilliseconds);
    }
}
