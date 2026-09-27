using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.TestSignalFS;

/// <summary>
/// Generates one fixed-size Fast Stream frame using an exact integer sample rate.
/// </summary>
public sealed class TestSignalFSNode : INode
{
    private readonly TestSignalFSSettings settings;
    private readonly Func<DateTimeOffset> getTimestamp;
    private INodeContext? context;
    private long sequenceNumber;

    public TestSignalFSNode(TestSignalFSSettings settings, Func<DateTimeOffset>? getTimestamp = null)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.settings.Validate();
        this.getTimestamp = getTimestamp ?? (() => DateTimeOffset.UtcNow);
    }

    public NodeDefinition Definition => TestSignalFSBlock.Definition;

    public ValueTask InitializeAsync(INodeContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        return ValueTask.CompletedTask;
    }

    public ValueTask OnPacketAsync(string inputPortId, IDataPacket packet, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPortId);
        ArgumentNullException.ThrowIfNull(packet);
        cancellationToken.ThrowIfCancellationRequested();
        throw new ArgumentException("TestSignal(FS) does not define input ports.", nameof(inputPortId));
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var initializedContext = context
            ?? throw new InvalidOperationException("The node must be initialized before it starts.");

        var timestamp = getTimestamp();
        var startTimeUnixNanoseconds = timestamp.ToUnixTimeMilliseconds() * 1_000_000L;
        var frameStartSeconds = startTimeUnixNanoseconds / 1_000_000_000.0;
        var samples = new double[TestSignalFSSettings.SampleCount];

        for (var sampleIndex = 0; sampleIndex < samples.Length; sampleIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var elapsedSeconds = frameStartSeconds + (sampleIndex / (double)settings.SampleRateHertz);
            var radians = 2.0 * Math.PI * settings.FrequencyHertz * elapsedSeconds;
            var sine = Math.Sin(radians);
            samples[sampleIndex] = settings.WaveType switch
            {
                TestSignalFSWaveType.Sine => settings.PeakIntensity * sine,
                TestSignalFSWaveType.Square => settings.PeakIntensity * (sine >= 0.0 ? 1.0 : -1.0),
                _ => throw new InvalidOperationException($"Unsupported TestSignal(FS) wave type '{settings.WaveType}'."),
            };
        }

        var frame = new FastStreamFrame(
            StartTimeUnixNanoseconds: startTimeUnixNanoseconds,
            SamplePeriodNanoseconds: Math.Max(1L, (long)Math.Round(1_000_000_000.0 / settings.SampleRateHertz, MidpointRounding.AwayFromZero)),
            ChannelNames: ["signal"],
            Samples: [samples.AsMemory()],
            SequenceNumber: sequenceNumber++,
            SampleRateHertz: settings.SampleRateHertz);

        await initializedContext.EmitAsync(TestSignalFSBlock.StreamOutputPortId, frame, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }
}