using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamChartSt;

/// <summary>
/// Maintains multi-channel time-series data for time-series chart visualization.
/// </summary>
public sealed class StreamChartStNode : INode
{
    public const int MaximumSamplesPerChannel = 512;

    private readonly StreamChartStSettings settings;
    private readonly StreamChartStHistory history = new();
    private readonly StreamChartChannelSnapshot?[] channelSnapshots = new StreamChartChannelSnapshot?[StreamChartStBlock.MaxStreamInputs];
    private readonly long?[] channelBaseStartTimes = new long?[StreamChartStBlock.MaxStreamInputs];

    private bool isInitialized;
    private long? firstStreamBaseTimeNanoseconds;
    private long latestSequenceNumber;

    public StreamChartStNode(StreamChartStSettings? settings = null)
    {
        this.settings = settings ?? StreamChartStSettings.Default;
        this.settings.Validate();
    }

    public NodeDefinition Definition => StreamChartStBlock.Definition;

    public StreamChartStSettings Settings => settings;

    public StreamChartStHistory History => history;

    public StreamChartStSnapshot? LatestSnapshot { get; private set; }

    public ValueTask InitializeAsync(
        INodeContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);
        isInitialized = true;
        firstStreamBaseTimeNanoseconds = null;
        history.Clear();
        for (var i = 0; i < channelSnapshots.Length; i++)
        {
            channelSnapshots[i] = null;
            channelBaseStartTimes[i] = null;
        }

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

        if (!StreamChartStBlock.TryGetChannelIndex(inputPortId, out var channelIndex))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        if (packet is not FastStreamFrame frame)
        {
            throw new ArgumentException("StreamChartSt accepts Fast Stream input only.", nameof(packet));
        }

        if (frame.ChannelCount == 0 || frame.SampleCount == 0)
        {
            return ValueTask.CompletedTask;
        }

        var channelZeroIndex = channelIndex - 1;
        latestSequenceNumber = Math.Max(latestSequenceNumber, frame.SequenceNumber);

        if (channelIndex == 1 && firstStreamBaseTimeNanoseconds is null)
        {
            firstStreamBaseTimeNanoseconds = frame.StartTimeUnixNanoseconds;
        }

        if (channelBaseStartTimes[channelZeroIndex] is null)
        {
            channelBaseStartTimes[channelZeroIndex] = frame.StartTimeUnixNanoseconds;
        }

        var baseTimeNanoseconds = settings.TimeAlignmentMode == StreamChartTimeAlignment.AlignToFirstStream
            ? (firstStreamBaseTimeNanoseconds ?? frame.StartTimeUnixNanoseconds)
            : channelBaseStartTimes[channelZeroIndex]!.Value;

        var sampleSpan = frame.Samples[0].Span;
        var sampleCount = sampleSpan.Length;
        var downsampledCount = Math.Min(sampleCount, MaximumSamplesPerChannel);

        var millis = new double[downsampledCount];
        var values = new double[downsampledCount];

        for (var i = 0; i < downsampledCount; i++)
        {
            var sourceIndex = downsampledCount == 1
                ? 0
                : i * (sampleCount - 1) / (downsampledCount - 1);

            var sampleTimeNano = frame.StartTimeUnixNanoseconds + (sourceIndex * frame.SamplePeriodNanoseconds);
            var elapsedMillis = (sampleTimeNano - baseTimeNanoseconds) / 1_000_000.0;

            millis[i] = elapsedMillis;
            values[i] = sampleSpan[sourceIndex];
        }

        var channelName = settings.GetChannelName(channelIndex);
        channelSnapshots[channelZeroIndex] = new StreamChartChannelSnapshot(
            ChannelIndex: channelIndex,
            ChannelName: channelName,
            Millis: millis,
            Values: values,
            TotalSampleCount: sampleCount);

        history.Append(
            channelIndex,
            frame.StartTimeUnixNanoseconds,
            frame.SamplePeriodNanoseconds,
            sampleSpan,
            settings);

        LatestSnapshot = new StreamChartStSnapshot(
            TimeAlignmentMode: settings.TimeAlignmentMode,
            TimeSpanMilliseconds: settings.TimeSpanMilliseconds,
            Channels: [.. channelSnapshots],
            SequenceNumber: latestSequenceNumber,
            BaseStartTimeUnixNanoseconds: firstStreamBaseTimeNanoseconds);

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
}
