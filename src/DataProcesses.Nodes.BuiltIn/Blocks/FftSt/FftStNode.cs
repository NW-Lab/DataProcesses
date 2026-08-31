using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.FftSt;

/// <summary>
/// Computes a one-sided FFT magnitude vector from the first channel of a Fast Stream frame.
/// </summary>
public sealed class FftStNode : INode
{
    private INodeContext? context;

    public NodeDefinition Definition => FftStBlock.Definition;

    public ValueTask InitializeAsync(
        INodeContext nodeContext,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        context = nodeContext ?? throw new ArgumentNullException(nameof(nodeContext));
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

        if (!string.Equals(inputPortId, FftStBlock.InputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        if (packet is not FastStreamFrame inputFrame)
        {
            throw new ArgumentException("FFTst accepts Fast Stream input only.", nameof(packet));
        }

        if (inputFrame.SamplePeriodNanoseconds <= 0)
        {
            throw new ArgumentException("FFTst requires a positive sample period.", nameof(packet));
        }

        if (inputFrame.ChannelCount == 0)
        {
            throw new ArgumentException("FFTst requires at least one Fast Stream channel.", nameof(packet));
        }

        var sampleCount = inputFrame.SampleCount;
        for (var channelIndex = 0; channelIndex < inputFrame.ChannelCount; channelIndex++)
        {
            if (inputFrame.Samples[channelIndex].Length != sampleCount)
            {
                throw new ArgumentException("All Fast Stream channels must have the same sample count.", nameof(packet));
            }
        }

        var nodeContext = context
            ?? throw new InvalidOperationException("The node must be initialized before it receives packets.");
        var magnitudes = CalculateOneSidedMagnitudes(inputFrame.Samples[0].Span, cancellationToken);
        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(inputFrame.StartTimeUnixNanoseconds / 1_000_000);
        var output = new NumericVectorFrame(
            Name: "fft-magnitude",
            Values: magnitudes.AsMemory(),
            SequenceNumber: inputFrame.SequenceNumber,
            Timestamp: timestamp);

        await nodeContext.EmitAsync(FftStBlock.OutputPortId, output, cancellationToken);
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

    private static double[] CalculateOneSidedMagnitudes(
        ReadOnlySpan<double> samples,
        CancellationToken cancellationToken)
    {
        if (samples.IsEmpty)
        {
            return [];
        }

        var binCount = samples.Length / 2 + 1;
        var magnitudes = new double[binCount];

        for (var bin = 0; bin < binCount; bin++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var real = 0.0;
            var imaginary = 0.0;
            var angleStep = 2.0 * Math.PI * bin / samples.Length;
            var cosineStep = Math.Cos(angleStep);
            var sineStep = Math.Sin(angleStep);
            var cosine = 1.0;
            var sine = 0.0;

            for (var sampleIndex = 0; sampleIndex < samples.Length; sampleIndex++)
            {
                var sample = samples[sampleIndex];
                real += sample * cosine;
                imaginary -= sample * sine;

                var nextCosine = cosine * cosineStep - sine * sineStep;
                sine = sine * cosineStep + cosine * sineStep;
                cosine = nextCosine;
            }

            var magnitude = Math.Sqrt(real * real + imaginary * imaginary) / samples.Length;
            if (bin > 0 && bin < samples.Length - bin)
            {
                magnitude *= 2.0;
            }

            magnitudes[bin] = magnitude;
        }

        return magnitudes;
    }
}