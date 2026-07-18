using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.TimeSeriesDisplay;

/// <summary>
/// Maintains a bounded, downsampled view of the latest Fast Stream frame for dashboard rendering.
/// </summary>
public sealed class TimeSeriesDisplayNode : INode
{
    public const int MaximumSamplesPerChannel = 512;

    private bool _isInitialized;

    public NodeDefinition Definition => TimeSeriesDisplayBlock.Definition;

    /// <summary>
    /// Gets the most recent display-oriented snapshot. A dashboard renderer can bind to this state
    /// without retaining the source frame's full sample buffers.
    /// </summary>
    public TimeSeriesSnapshot? LatestSnapshot { get; private set; }

    public ValueTask InitializeAsync(
        INodeContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);
        _isInitialized = true;
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

        if (!_isInitialized)
        {
            throw new InvalidOperationException("The node must be initialized before it receives packets.");
        }

        if (!string.Equals(inputPortId, TimeSeriesDisplayBlock.InputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        if (packet is not FastStreamFrame inputFrame)
        {
            throw new ArgumentException("Time Series accepts Fast Stream input only.", nameof(packet));
        }

        var displayChannels = new ReadOnlyMemory<double>[inputFrame.ChannelCount];
        for (var channelIndex = 0; channelIndex < inputFrame.ChannelCount; channelIndex++)
        {
            displayChannels[channelIndex] = Downsample(inputFrame.Samples[channelIndex].Span);
        }

        LatestSnapshot = new TimeSeriesSnapshot(
            inputFrame.StartTimeUnixNanoseconds,
            inputFrame.SamplePeriodNanoseconds,
            inputFrame.ChannelNames.ToArray(),
            displayChannels,
            inputFrame.SampleCount,
            inputFrame.SequenceNumber);

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

    private static double[] Downsample(ReadOnlySpan<double> samples)
    {
        if (samples.Length <= MaximumSamplesPerChannel)
        {
            return samples.ToArray();
        }

        var downsampled = new double[MaximumSamplesPerChannel];
        for (var displayIndex = 0; displayIndex < downsampled.Length; displayIndex++)
        {
            var sourceIndex = displayIndex * (samples.Length - 1) / (downsampled.Length - 1);
            downsampled[displayIndex] = samples[sourceIndex];
        }

        return downsampled;
    }
}
