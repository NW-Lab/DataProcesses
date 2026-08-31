using System.Text.Json;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.BreathSt;

/// <summary>
/// Detects respiratory peaks in the first Fast Stream channel and emits breaths per minute.
/// </summary>
public sealed class BreathStNode : INode
{
    private readonly BreathStSettings settings;

    private INodeContext? _context;
    private bool _hasPreviousSample;
    private bool _hasPreviousPreviousSample;
    private double _previousSample;
    private double _previousPreviousSample;
    private double _previousRawSample;
    private long _previousSampleTimestampNanoseconds;
    private long? _lastBreathTimestampNanoseconds;
    private long? _lastCoughTimestampNanoseconds;
    private double _lastBreathRate = double.NaN;

    public BreathStNode(BreathStSettings? settings = null)
    {
        this.settings = settings ?? BreathStSettings.Default;
        this.settings.Validate();
    }

    public NodeDefinition Definition => BreathStBlock.Definition;

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

        if (!string.Equals(inputPortId, BreathStBlock.InputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        if (packet is not FastStreamFrame inputFrame)
        {
            throw new ArgumentException("BreathSt accepts Fast Stream input only.", nameof(packet));
        }

        ValidateFrame(inputFrame, packet);

        var context = _context
            ?? throw new InvalidOperationException("The node must be initialized before it receives packets.");
        var sourceSamples = inputFrame.Samples[0].Span;
        var breathRateSamples = new double[sourceSamples.Length];
        var peakThreshold = CalculatePeakThreshold(sourceSamples, settings.Method, settings.PeakThresholdFraction);
        var coughThreshold = CalculateCoughThreshold(sourceSamples, settings.CoughSpikeThresholdFraction);
        var anomalyEvents = settings.EmitAnomalyEvents ? new List<JsonMessage>() : null;

        for (var sampleIndex = 0; sampleIndex < sourceSamples.Length; sampleIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rawSample = sourceSamples[sampleIndex];
            var sampleTimestamp = inputFrame.StartTimeUnixNanoseconds + (sampleIndex * inputFrame.SamplePeriodNanoseconds);

            if (double.IsFinite(rawSample))
            {
                var detectionSample = TransformSample(rawSample, settings.Method);
                if (IsBreathPeak(detectionSample, peakThreshold) && TryAcceptBreath(_previousSampleTimestampNanoseconds, out var breathRate))
                {
                    _lastBreathRate = breathRate;
                }

                if (anomalyEvents is not null
                    && IsCoughLikeSpike(rawSample, coughThreshold)
                    && TryAcceptCough(sampleTimestamp))
                {
                    anomalyEvents.Add(CreateCoughEvent(inputFrame, sampleIndex, sampleTimestamp, rawSample - _previousRawSample));
                }

                PushSample(detectionSample, rawSample, sampleTimestamp);
            }
            else
            {
                ResetWindow();
            }

            breathRateSamples[sampleIndex] = _lastBreathRate;
        }

        var outputFrame = new FastStreamFrame(
            inputFrame.StartTimeUnixNanoseconds,
            inputFrame.SamplePeriodNanoseconds,
            ["breath-rate-brpm"],
            [breathRateSamples.AsMemory()],
            inputFrame.SequenceNumber);

        await context.EmitAsync(BreathStBlock.RateOutputPortId, outputFrame, cancellationToken);

        if (anomalyEvents is not null)
        {
            for (var index = 0; index < anomalyEvents.Count; index++)
            {
                await context.EmitAsync(BreathStBlock.EventOutputPortId, anomalyEvents[index], cancellationToken);
            }
        }
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

    private bool IsBreathPeak(double currentSample, double threshold)
    {
        return _hasPreviousPreviousSample
            && _previousSample >= threshold
            && _previousSample > _previousPreviousSample
            && _previousSample >= currentSample;
    }

    private bool TryAcceptBreath(long peakTimestampNanoseconds, out double breathRate)
    {
        breathRate = double.NaN;

        if (_lastBreathTimestampNanoseconds is null)
        {
            _lastBreathTimestampNanoseconds = peakTimestampNanoseconds;
            return false;
        }

        var intervalNanoseconds = peakTimestampNanoseconds - _lastBreathTimestampNanoseconds.Value;
        if (intervalNanoseconds < MillisecondsToNanoseconds(settings.MinimumBreathIntervalMilliseconds))
        {
            return false;
        }

        _lastBreathTimestampNanoseconds = peakTimestampNanoseconds;
        if (intervalNanoseconds > MillisecondsToNanoseconds(settings.MaximumBreathIntervalMilliseconds))
        {
            return false;
        }

        breathRate = 60_000_000_000.0 / intervalNanoseconds;
        return true;
    }

    private bool IsCoughLikeSpike(double rawSample, double threshold)
    {
        return _hasPreviousSample && Math.Abs(rawSample - _previousRawSample) >= threshold;
    }

    private bool TryAcceptCough(long timestampNanoseconds)
    {
        if (_lastCoughTimestampNanoseconds is not null
            && timestampNanoseconds - _lastCoughTimestampNanoseconds.Value < MillisecondsToNanoseconds(settings.CoughRefractoryMilliseconds))
        {
            return false;
        }

        _lastCoughTimestampNanoseconds = timestampNanoseconds;
        return true;
    }

    private JsonMessage CreateCoughEvent(
        FastStreamFrame inputFrame,
        int sampleIndex,
        long timestampNanoseconds,
        double delta)
    {
        var payload = JsonSerializer.SerializeToElement(new
        {
            eventType = "cough-like-spike",
            method = FormatMethod(settings.Method),
            channel = inputFrame.ChannelNames[0],
            sequenceNumber = inputFrame.SequenceNumber,
            sampleIndex,
            timestampUnixNanoseconds = timestampNanoseconds,
            delta,
        });

        return new JsonMessage(
            Topic: "dataprocesses.breath-st.anomaly",
            Payload: payload,
            Timestamp: DateTimeOffset.FromUnixTimeMilliseconds(timestampNanoseconds / 1_000_000));
    }

    private void PushSample(double sample, double rawSample, long timestampNanoseconds)
    {
        if (_hasPreviousSample)
        {
            _previousPreviousSample = _previousSample;
            _hasPreviousPreviousSample = true;
        }

        _previousSample = sample;
        _previousRawSample = rawSample;
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
        _lastBreathTimestampNanoseconds = null;
        _lastCoughTimestampNanoseconds = null;
        _lastBreathRate = double.NaN;
    }

    private static void ValidateFrame(FastStreamFrame inputFrame, IDataPacket packet)
    {
        if (inputFrame.SamplePeriodNanoseconds <= 0)
        {
            throw new ArgumentException("BreathSt requires a positive sample period.", nameof(packet));
        }

        if (inputFrame.ChannelCount == 0)
        {
            throw new ArgumentException("BreathSt requires at least one Fast Stream channel.", nameof(packet));
        }

        if (inputFrame.ChannelNames.Count != inputFrame.ChannelCount)
        {
            throw new ArgumentException("Fast Stream channel names must match the channel count.", nameof(packet));
        }

        var sampleCount = inputFrame.SampleCount;
        for (var channelIndex = 0; channelIndex < inputFrame.ChannelCount; channelIndex++)
        {
            if (inputFrame.Samples[channelIndex].Length != sampleCount)
            {
                throw new ArgumentException("All Fast Stream channels must have the same sample count.", nameof(packet));
            }
        }
    }

    private static double CalculatePeakThreshold(
        ReadOnlySpan<double> samples,
        BreathStDetectionMethod method,
        double thresholdFraction)
    {
        var hasFiniteSample = false;
        var minimum = 0.0;
        var maximum = 0.0;

        for (var index = 0; index < samples.Length; index++)
        {
            var rawSample = samples[index];
            if (!double.IsFinite(rawSample))
            {
                continue;
            }

            var sample = TransformSample(rawSample, method);
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

        return minimum + ((maximum - minimum) * thresholdFraction);
    }

    private static double CalculateCoughThreshold(ReadOnlySpan<double> samples, double thresholdFraction)
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

        return (maximum - minimum) * thresholdFraction;
    }

    private static double TransformSample(double sample, BreathStDetectionMethod method)
    {
        return method == BreathStDetectionMethod.LedOxygen ? -sample : sample;
    }

    private static string FormatMethod(BreathStDetectionMethod method)
    {
        return method == BreathStDetectionMethod.LedOxygen ? "ledOxygen" : "breathBelt";
    }

    private static long MillisecondsToNanoseconds(double milliseconds)
    {
        return (long)(milliseconds * 1_000_000.0);
    }
}