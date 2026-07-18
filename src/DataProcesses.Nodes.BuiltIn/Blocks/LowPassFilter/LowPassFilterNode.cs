using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.LowPassFilter;

/// <summary>
/// Applies a deterministic first-order low-pass filter independently to each Fast Stream channel.
/// </summary>
public sealed class LowPassFilterNode : INode
{
    private const double SmoothingFactor = 0.25;
    private INodeContext? _context;
    private double[]? _previousSamples;

    public NodeDefinition Definition => LowPassFilterBlock.Definition;

    public ValueTask InitializeAsync(
        INodeContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _context = context ?? throw new ArgumentNullException(nameof(context));
        return ValueTask.CompletedTask;
    }

    public async ValueTask OnPacketAsync(
        string inputPortId,
        IDataPacket packet,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPortId);
        ArgumentNullException.ThrowIfNull(packet);
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(inputPortId, LowPassFilterBlock.InputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        if (packet is not FastStreamFrame inputFrame)
        {
            throw new ArgumentException("Low-pass Filter accepts Fast Stream input only.", nameof(packet));
        }

        var context = _context
            ?? throw new InvalidOperationException("The node must be initialized before it receives packets.");
        var filteredChannels = new ReadOnlyMemory<double>[inputFrame.ChannelCount];
        var previousSamples = EnsurePreviousSampleCapacity(inputFrame.ChannelCount);

        for (var channelIndex = 0; channelIndex < inputFrame.ChannelCount; channelIndex++)
        {
            var inputSamples = inputFrame.Samples[channelIndex];
            var filteredSamples = new double[inputSamples.Length];
            var previous = previousSamples[channelIndex];

            for (var sampleIndex = 0; sampleIndex < inputSamples.Length; sampleIndex++)
            {
                var sample = inputSamples.Span[sampleIndex];
                previous = sampleIndex == 0 && inputFrame.SequenceNumber == 0
                    ? sample
                    : previous + SmoothingFactor * (sample - previous);
                filteredSamples[sampleIndex] = previous;
            }

            previousSamples[channelIndex] = previous;
            filteredChannels[channelIndex] = filteredSamples;
        }

        var filteredFrame = new FastStreamFrame(
            inputFrame.StartTimeUnixNanoseconds,
            inputFrame.SamplePeriodNanoseconds,
            inputFrame.ChannelNames,
            filteredChannels,
            inputFrame.SequenceNumber);

        await context.EmitAsync(LowPassFilterBlock.OutputPortId, filteredFrame, cancellationToken);
    }

    public ValueTask StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _previousSamples = null;
        return ValueTask.CompletedTask;
    }

    private double[] EnsurePreviousSampleCapacity(int channelCount)
    {
        if (_previousSamples is null || _previousSamples.Length != channelCount)
        {
            _previousSamples = new double[channelCount];
        }

        return _previousSamples;
    }
}
