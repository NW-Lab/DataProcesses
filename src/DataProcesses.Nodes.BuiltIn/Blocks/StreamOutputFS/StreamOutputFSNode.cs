using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamOutputFS;

/// <summary>
/// Keeps a bounded latest snapshot of Fast Stream channels and their sample rate.
/// </summary>
public sealed class StreamOutputFSNode : INode
{
    public const int MaximumSamplesPerChannel = 512;

    private bool isInitialized;

    public NodeDefinition Definition => StreamOutputFSBlock.Definition;

    public StreamOutputFSSnapshot? LatestSnapshot { get; private set; }

    public ValueTask InitializeAsync(INodeContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);
        isInitialized = true;
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

        if (!string.Equals(inputPortId, StreamOutputFSBlock.InputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        if (packet is not FastStreamFrame frame)
        {
            throw new ArgumentException("StreamOutputFS accepts Fast Stream input only.", nameof(packet));
        }

        if (!frame.HasValidTiming)
        {
            throw new ArgumentException("StreamOutputFS requires a positive sample period or sample rate.", nameof(packet));
        }

        var displayChannels = new ReadOnlyMemory<double>[frame.ChannelCount];
        for (var channelIndex = 0; channelIndex < frame.ChannelCount; channelIndex++)
        {
            displayChannels[channelIndex] = Downsample(frame.Samples[channelIndex].Span);
        }

        LatestSnapshot = new StreamOutputFSSnapshot(
            frame.StartTimeUnixNanoseconds,
            frame.GetSampleRateHertz(),
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