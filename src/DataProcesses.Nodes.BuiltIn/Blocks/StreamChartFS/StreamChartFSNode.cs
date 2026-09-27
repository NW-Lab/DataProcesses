using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamChartFS;

/// <summary>
/// Validates Fast Stream time-series frames and retains a bounded snapshot for charting.
/// </summary>
public sealed class StreamChartFSNode : INode
{
    public const int MaximumSamplesPerChannel = 512;

    private bool isInitialized;

    public NodeDefinition Definition => StreamChartFSBlock.Definition;

    public StreamChartFSSnapshot? LatestSnapshot { get; private set; }

    public ValueTask InitializeAsync(INodeContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);
        isInitialized = true;
        LatestSnapshot = null;
        return ValueTask.CompletedTask;
    }

    public ValueTask OnPacketAsync(string inputPortId, IDataPacket packet, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPortId);
        ArgumentNullException.ThrowIfNull(packet);
        cancellationToken.ThrowIfCancellationRequested();

        if (!isInitialized)
        {
            throw new InvalidOperationException("The node must be initialized before it receives packets.");
        }

        if (!string.Equals(inputPortId, StreamChartFSBlock.InputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        if (packet is not FastStreamFrame frame)
        {
            throw new ArgumentException("StreamChartFS accepts Fast Stream input only.", nameof(packet));
        }

        if (!frame.HasValidTiming)
        {
            throw new ArgumentException("StreamChartFS requires a positive sample period or sample rate.", nameof(packet));
        }

        if (frame.ChannelCount == 0 || frame.SampleCount == 0)
        {
            return ValueTask.CompletedTask;
        }

        if (frame.ChannelCount > StreamChartFSBlock.MaximumChannels)
        {
            throw new ArgumentException($"StreamChartFS supports at most {StreamChartFSBlock.MaximumChannels} channels.", nameof(packet));
        }

        if (frame.ChannelNames.Count != frame.ChannelCount)
        {
            throw new ArgumentException("Fast Stream channel names must match the channel count.", nameof(packet));
        }

        var displayChannels = new ReadOnlyMemory<double>[frame.ChannelCount];
        for (var channelIndex = 0; channelIndex < frame.ChannelCount; channelIndex++)
        {
            var channelSamples = frame.Samples[channelIndex].Span;
            if (channelSamples.Length != frame.SampleCount)
            {
                throw new ArgumentException("All Fast Stream channels must have the same sample count.", nameof(packet));
            }

            displayChannels[channelIndex] = Downsample(channelSamples);
        }

        LatestSnapshot = new StreamChartFSSnapshot(
            frame.StartTimeUnixNanoseconds,
            frame.SamplePeriodNanoseconds,
            frame.SampleRateHertz,
            frame.ChannelNames.ToArray(),
            displayChannels,
            frame.SampleCount,
            frame.SequenceNumber);

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
        var displayCount = Math.Min(samples.Length, MaximumSamplesPerChannel);
        var displaySamples = new double[displayCount];
        for (var displayIndex = 0; displayIndex < displayCount; displayIndex++)
        {
            var sourceIndex = displayCount == 1
                ? 0
                : (int)((long)displayIndex * (samples.Length - 1) / (displayCount - 1));
            displaySamples[displayIndex] = samples[sourceIndex];
        }

        return displaySamples;
    }
}