using DataProcesses.Nodes.BuiltIn.Blocks.FastFourierTransform;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.FftFS;

/// <summary>
/// Applies the existing one-sided Fast Stream FFT and preserves exact source-rate metadata.
/// </summary>
public sealed class FftFSNode : INode
{
    private readonly FastFourierTransformNode fft = new();

    public NodeDefinition Definition => FftFSBlock.Definition;

    public ValueTask InitializeAsync(INodeContext context, CancellationToken cancellationToken)
    {
        return fft.InitializeAsync(context, cancellationToken);
    }

    public ValueTask OnPacketAsync(string inputPortId, IDataPacket packet, CancellationToken cancellationToken)
    {
        if (!string.Equals(inputPortId, FftFSBlock.InputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        ArgumentNullException.ThrowIfNull(packet);
        if (packet is not FastStreamFrame { SampleRateHertz: > 0 })
        {
            throw new ArgumentException("FftFS requires FS frames with a positive SampleRateHertz.", nameof(packet));
        }

        return fft.OnPacketAsync(FastFourierTransformBlock.InputPortId, packet, cancellationToken);
    }

    public ValueTask StartAsync(CancellationToken cancellationToken)
    {
        return fft.StartAsync(cancellationToken);
    }

    public ValueTask StopAsync(CancellationToken cancellationToken)
    {
        return fft.StopAsync(cancellationToken);
    }
}