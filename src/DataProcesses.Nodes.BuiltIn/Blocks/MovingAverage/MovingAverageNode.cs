using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.MovingAverage;

/// <summary>
/// Smooths each Fast Stream channel using a sample-count or elapsed-time moving window.
/// </summary>
public sealed class MovingAverageNode : INode
{
    private readonly MovingAverageSettings settings;
    private INodeContext? context;
    private ChannelWindow[]? channelWindows;

    public MovingAverageNode(MovingAverageSettings settings)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.settings.Validate();
    }

    public NodeDefinition Definition => MovingAverageBlock.Definition;

    public ValueTask InitializeAsync(INodeContext nodeContext, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        context = nodeContext ?? throw new ArgumentNullException(nameof(nodeContext));
        return ValueTask.CompletedTask;
    }

    public async ValueTask OnPacketAsync(string inputPortId, IDataPacket packet, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPortId);
        ArgumentNullException.ThrowIfNull(packet);
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(inputPortId, MovingAverageBlock.InputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        if (packet is not FastStreamFrame inputFrame)
        {
            throw new ArgumentException("Moving Average accepts Fast Stream input only.", nameof(packet));
        }

        if (settings.WindowMode == MovingAverageWindowMode.Duration && inputFrame.SamplePeriodNanoseconds <= 0)
        {
            throw new ArgumentException("Time-window moving averages require a positive sample period.", nameof(packet));
        }

        var nodeContext = context ?? throw new InvalidOperationException("The node must be initialized before it receives packets.");
        var windows = EnsureChannelWindows(inputFrame.ChannelCount);
        var averagedChannels = new ReadOnlyMemory<double>[inputFrame.ChannelCount];
        var durationNanoseconds = settings.WindowMode == MovingAverageWindowMode.Duration
            ? (long)Math.Round(settings.WindowDurationMilliseconds * 1_000_000.0, MidpointRounding.AwayFromZero)
            : 0;

        for (var channelIndex = 0; channelIndex < inputFrame.ChannelCount; channelIndex++)
        {
            var inputSamples = inputFrame.Samples[channelIndex].Span;
            var averagedSamples = new double[inputSamples.Length];
            var window = windows[channelIndex];

            for (var sampleIndex = 0; sampleIndex < inputSamples.Length; sampleIndex++)
            {
                var timestampNanoseconds = checked(inputFrame.StartTimeUnixNanoseconds + (sampleIndex * inputFrame.SamplePeriodNanoseconds));
                averagedSamples[sampleIndex] = window.Add(inputSamples[sampleIndex], timestampNanoseconds, settings.WindowMode, settings.WindowSize, durationNanoseconds);
            }

            averagedChannels[channelIndex] = averagedSamples;
        }

        var averagedFrame = new FastStreamFrame(inputFrame.StartTimeUnixNanoseconds, inputFrame.SamplePeriodNanoseconds, inputFrame.ChannelNames, averagedChannels, inputFrame.SequenceNumber);
        await nodeContext.EmitAsync(MovingAverageBlock.OutputPortId, averagedFrame, cancellationToken);
    }

    public ValueTask StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        channelWindows = null;
        return ValueTask.CompletedTask;
    }

    private ChannelWindow[] EnsureChannelWindows(int channelCount)
    {
        if (channelWindows is null || channelWindows.Length != channelCount)
        {
            channelWindows = new ChannelWindow[channelCount];
            for (var index = 0; index < channelWindows.Length; index++)
            {
                channelWindows[index] = new ChannelWindow();
            }
        }

        return channelWindows;
    }

    private sealed class ChannelWindow
    {
        private readonly Queue<Sample> samples = new();
        private double sum;
        private long? lastTimestampNanoseconds;

        public double Add(double value, long timestampNanoseconds, MovingAverageWindowMode windowMode, int windowSize, long durationNanoseconds)
        {
            if (lastTimestampNanoseconds is { } lastTimestamp && timestampNanoseconds < lastTimestamp)
            {
                samples.Clear();
                sum = 0.0;
            }

            lastTimestampNanoseconds = timestampNanoseconds;
            samples.Enqueue(new Sample(value, timestampNanoseconds));
            sum += value;

            if (windowMode == MovingAverageWindowMode.Samples)
            {
                while (samples.Count > windowSize)
                {
                    sum -= samples.Dequeue().Value;
                }
            }
            else
            {
                var earliestTimestamp = timestampNanoseconds - durationNanoseconds;
                while (samples.TryPeek(out var oldest) && oldest.TimestampNanoseconds < earliestTimestamp)
                {
                    sum -= samples.Dequeue().Value;
                }
            }

            return sum / samples.Count;
        }

        private readonly record struct Sample(double Value, long TimestampNanoseconds);
    }
}