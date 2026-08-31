using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.HartRateSt;

/// <summary>
/// Detects local peaks in the first Fast Stream channel and emits a BPM time series.
/// </summary>
public sealed class HartRateStNode : INode
{
    private const long MinPeakIntervalNanoseconds = 300_000_000;
    private const long MaxPeakIntervalNanoseconds = 2_000_000_000;
    private const double PeakThresholdFraction = 0.6;

    private INodeContext? _context;
    private bool _hasPreviousSample;
    private bool _hasPreviousPreviousSample;
    private double _previousSample;
    private double _previousPreviousSample;
    private long _previousSampleTimestampNanoseconds;
    private long? _lastPeakTimestampNanoseconds;
    private double _lastHeartRateBpm = double.NaN;

    public NodeDefinition Definition => HartRateStBlock.Definition;

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

        if (!string.Equals(inputPortId, HartRateStBlock.InputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        if (packet is not FastStreamFrame inputFrame)
        {
            throw new ArgumentException("HartRateSt accepts Fast Stream input only.", nameof(packet));
        }

        if (inputFrame.SamplePeriodNanoseconds <= 0)
        {
            throw new ArgumentException("HartRateSt requires a positive sample period.", nameof(packet));
        }

        if (inputFrame.ChannelCount == 0)
        {
            throw new ArgumentException("HartRateSt requires at least one Fast Stream channel.", nameof(packet));
        }

        if (inputFrame.ChannelNames.Count != inputFrame.ChannelCount)
        {
            throw new ArgumentException("Fast Stream channel names must match the channel count.", nameof(packet));
        }

        var context = _context
            ?? throw new InvalidOperationException("The node must be initialized before it receives packets.");
        var sampleCount = inputFrame.SampleCount;

        for (var channelIndex = 0; channelIndex < inputFrame.ChannelCount; channelIndex++)
        {
            if (inputFrame.Samples[channelIndex].Length != sampleCount)
            {
                throw new ArgumentException("All Fast Stream channels must have the same sample count.", nameof(packet));
            }
        }

        var sourceSamples = inputFrame.Samples[0].Span;
        var heartRateSamples = new double[sourceSamples.Length];
        var peakThreshold = CalculatePeakThreshold(sourceSamples);

        for (var sampleIndex = 0; sampleIndex < sourceSamples.Length; sampleIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sample = sourceSamples[sampleIndex];

            if (double.IsFinite(sample))
            {
                if (IsPeak(sample, peakThreshold) && TryAcceptPeak(_previousSampleTimestampNanoseconds, out var heartRateBpm))
                {
                    _lastHeartRateBpm = heartRateBpm;
                }

                PushSample(sample, inputFrame.StartTimeUnixNanoseconds + (sampleIndex * inputFrame.SamplePeriodNanoseconds));
            }
            else
            {
                ResetWindow();
            }

            heartRateSamples[sampleIndex] = _lastHeartRateBpm;
        }

        var outputFrame = new FastStreamFrame(
            inputFrame.StartTimeUnixNanoseconds,
            inputFrame.SamplePeriodNanoseconds,
            ["heart-rate-bpm"],
            [heartRateSamples.AsMemory()],
            inputFrame.SequenceNumber);

        await context.EmitAsync(HartRateStBlock.OutputPortId, outputFrame, cancellationToken);
    }

    public ValueTask StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ResetDetector();
        return ValueTask.CompletedTask;
    }

    private bool IsPeak(double currentSample, double threshold)
    {
        return _hasPreviousPreviousSample
            && _previousSample >= threshold
            && _previousSample > _previousPreviousSample
            && _previousSample >= currentSample;
    }

    private bool TryAcceptPeak(long peakTimestampNanoseconds, out double heartRateBpm)
    {
        heartRateBpm = double.NaN;

        if (_lastPeakTimestampNanoseconds is null)
        {
            _lastPeakTimestampNanoseconds = peakTimestampNanoseconds;
            return false;
        }

        var intervalNanoseconds = peakTimestampNanoseconds - _lastPeakTimestampNanoseconds.Value;
        if (intervalNanoseconds < MinPeakIntervalNanoseconds)
        {
            return false;
        }

        _lastPeakTimestampNanoseconds = peakTimestampNanoseconds;
        if (intervalNanoseconds > MaxPeakIntervalNanoseconds)
        {
            return false;
        }

        heartRateBpm = 60_000_000_000.0 / intervalNanoseconds;
        return true;
    }

    private void PushSample(double sample, long timestampNanoseconds)
    {
        if (_hasPreviousSample)
        {
            _previousPreviousSample = _previousSample;
            _hasPreviousPreviousSample = true;
        }

        _previousSample = sample;
        _previousSampleTimestampNanoseconds = timestampNanoseconds;
        _hasPreviousSample = true;
    }

    private void ResetWindow()
    {
        _hasPreviousSample = false;
        _hasPreviousPreviousSample = false;
    }

    private void ResetDetector()
    {
        ResetWindow();
        _lastPeakTimestampNanoseconds = null;
        _lastHeartRateBpm = double.NaN;
    }

    private static double CalculatePeakThreshold(ReadOnlySpan<double> samples)
    {
        var hasFiniteSample = false;
        var minimum = 0.0;
        var maximum = 0.0;

        for (var index = 0; index < samples.Length; index++)
        {
            var sample = samples[index];
            if (!double.IsFinite(sample))
            {
                continue;
            }

            if (!hasFiniteSample)
            {
                minimum = sample;
                maximum = sample;
                hasFiniteSample = true;
                continue;
            }

            minimum = Math.Min(minimum, sample);
            maximum = Math.Max(maximum, sample);
        }

        if (!hasFiniteSample || minimum == maximum)
        {
            return double.PositiveInfinity;
        }

        return minimum + ((maximum - minimum) * PeakThresholdFraction);
    }
}