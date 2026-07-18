using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.FastFourierTransform;

/// <summary>
/// Computes deterministic one-sided magnitude spectra for each channel in a Fast Stream frame.
/// The first MVP implementation intentionally favors correctness and explicit behavior over a
/// specialized FFT dependency; optimized algorithms can replace the private calculation later
/// without changing the Block contract.
/// </summary>
public sealed class FastFourierTransformNode : INode
{
    private INodeContext? _context;

    public NodeDefinition Definition => FastFourierTransformBlock.Definition;

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

        if (!string.Equals(inputPortId, FastFourierTransformBlock.InputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        if (packet is not FastStreamFrame inputFrame)
        {
            throw new ArgumentException("FFT accepts Fast Stream input only.", nameof(packet));
        }

        if (inputFrame.SamplePeriodNanoseconds <= 0)
        {
            throw new ArgumentException("FFT requires a positive sample period.", nameof(packet));
        }

        var context = _context
            ?? throw new InvalidOperationException("The node must be initialized before it receives packets.");
        var sampleCount = inputFrame.SampleCount;
        var magnitudes = new ReadOnlyMemory<double>[inputFrame.ChannelCount];

        for (var channelIndex = 0; channelIndex < inputFrame.ChannelCount; channelIndex++)
        {
            var samples = inputFrame.Samples[channelIndex];
            if (samples.Length != sampleCount)
            {
                throw new ArgumentException("All Fast Stream channels must have the same sample count.", nameof(packet));
            }

            magnitudes[channelIndex] = CalculateOneSidedMagnitudes(samples.Span);
        }

        var frequencyResolutionHertz = sampleCount == 0
            ? 0.0
            : 1_000_000_000.0 / inputFrame.SamplePeriodNanoseconds / sampleCount;
        var spectrum = new SpectrumFrame(
            inputFrame.StartTimeUnixNanoseconds,
            inputFrame.SamplePeriodNanoseconds,
            frequencyResolutionHertz,
            inputFrame.ChannelNames,
            magnitudes,
            inputFrame.SequenceNumber);

        await context.EmitAsync(FastFourierTransformBlock.OutputPortId, spectrum, cancellationToken);
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

    private static double[] CalculateOneSidedMagnitudes(ReadOnlySpan<double> samples)
    {
        if (samples.IsEmpty)
        {
            return [];
        }

        var binCount = samples.Length / 2 + 1;
        var magnitudes = new double[binCount];

        for (var bin = 0; bin < binCount; bin++)
        {
            var real = 0.0;
            var imaginary = 0.0;

            for (var sampleIndex = 0; sampleIndex < samples.Length; sampleIndex++)
            {
                var angle = 2.0 * Math.PI * bin * sampleIndex / samples.Length;
                real += samples[sampleIndex] * Math.Cos(angle);
                imaginary -= samples[sampleIndex] * Math.Sin(angle);
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
