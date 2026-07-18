using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.TestSignal;

public sealed class TestSignalNode : INode
{
    private INodeContext? _context;

    public NodeDefinition Definition => TestSignalBlock.Definition;

    public ValueTask InitializeAsync(
        INodeContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _context = context ?? throw new ArgumentNullException(nameof(context));
        return ValueTask.CompletedTask;
    }

    public ValueTask OnPacketAsync(
        string inputPortId,
        IDataPacket packet,
        CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Test Signal is a source node and has no input ports.");

    public async ValueTask StartAsync(CancellationToken cancellationToken)
    {
        var context = _context
            ?? throw new InvalidOperationException("The node must be initialized before it starts.");

        const int sampleRate = 1_000;
        const int sampleCount = 256;
        const double frequency = 10.0;
        var samples = new double[sampleCount];

        for (var index = 0; index < samples.Length; index++)
        {
            samples[index] = Math.Sin(2 * Math.PI * frequency * index / sampleRate);
        }

        var frame = new FastStreamFrame(
            StartTimeUnixNanoseconds: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000,
            SamplePeriodNanoseconds: 1_000_000_000L / sampleRate,
            ChannelNames: ["signal"],
            Samples: [samples.AsMemory()],
            SequenceNumber: 0);

        await context.EmitAsync(TestSignalBlock.OutputPortId, frame, cancellationToken);
    }

    public ValueTask StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }
}