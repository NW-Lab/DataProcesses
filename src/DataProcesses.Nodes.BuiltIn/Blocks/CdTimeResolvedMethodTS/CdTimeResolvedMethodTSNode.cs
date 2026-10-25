using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.CdTimeResolvedMethodTS;

/// <summary>
/// Calculates a time-resolved central-difference vector from the first Fast Stream channel.
/// </summary>
public sealed class CdTimeResolvedMethodTSNode : INode
{
    private INodeContext? context;

    public NodeDefinition Definition => CdTimeResolvedMethodTSBlock.Definition;

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

        if (!string.Equals(inputPortId, CdTimeResolvedMethodTSBlock.InputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        if (packet is not FastStreamFrame inputFrame)
        {
            throw new ArgumentException("CdTimeResolvedMethodTS accepts Fast Stream input only.", nameof(packet));
        }

        if (!inputFrame.HasValidTiming)
        {
            throw new ArgumentException("CdTimeResolvedMethodTS requires a positive sample period or sample rate.", nameof(packet));
        }

        if (inputFrame.ChannelCount == 0)
        {
            throw new ArgumentException("CdTimeResolvedMethodTS requires at least one Fast Stream channel.", nameof(packet));
        }

        var nodeContext = context
            ?? throw new InvalidOperationException("The node must be initialized before it receives packets.");
        var values = CalculateCentralDifferences(
            inputFrame.Samples[0].Span,
            1.0 / inputFrame.GetSampleRateHertz(),
            cancellationToken);
        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(inputFrame.StartTimeUnixNanoseconds / 1_000_000);
        var output = new NumericVectorFrame(
            Name: "cd-time-resolved",
            Values: values.AsMemory(),
            SequenceNumber: inputFrame.SequenceNumber,
            Timestamp: timestamp);

        await nodeContext.EmitAsync(CdTimeResolvedMethodTSBlock.OutputPortId, output, cancellationToken);
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

    private static double[] CalculateCentralDifferences(
        ReadOnlySpan<double> samples,
        double secondsPerSample,
        CancellationToken cancellationToken)
    {
        var values = new double[samples.Length];
        if (samples.IsEmpty)
        {
            return values;
        }

        if (samples.Length == 1)
        {
            values[0] = 0.0;
            return values;
        }

        values[0] = CalculateDifference(samples[1], samples[0], secondsPerSample);
        for (var index = 1; index < samples.Length - 1; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            values[index] = CalculateDifference(samples[index + 1], samples[index - 1], 2.0 * secondsPerSample);
        }

        values[^1] = CalculateDifference(samples[^1], samples[^2], secondsPerSample);
        return values;
    }

    private static double CalculateDifference(double later, double earlier, double elapsedSeconds)
    {
        return double.IsFinite(later) && double.IsFinite(earlier)
            ? (later - earlier) / elapsedSeconds
            : double.NaN;
    }
}