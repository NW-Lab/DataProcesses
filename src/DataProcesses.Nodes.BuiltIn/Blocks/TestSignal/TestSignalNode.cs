using System.Text.Json;

using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.TestSignal;

public sealed class TestSignalNode(TestSignalSettings settings) : INode
{
    private INodeContext? _context;
    private TestSignalSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    public NodeDefinition Definition => TestSignalBlock.Definition;

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

        if (!string.Equals(inputPortId, TestSignalBlock.PayloadInputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        if (packet is not JsonMessage message)
        {
            throw new ArgumentException("The Test Signal Payload input accepts JSON Message packets only.", nameof(packet));
        }

        var previousIsEnabled = _settings.IsEnabled;
        _settings = _settings.ApplyPayload(message.Payload);

        if (_settings.PayloadThrough)
        {
            var context = _context
                ?? throw new InvalidOperationException("The node must be initialized before it receives packets.");
            await context.EmitAsync(TestSignalBlock.PayloadOutputPortId, message, cancellationToken);
        }

        if (previousIsEnabled == _settings.IsEnabled)
        {
            return;
        }

        var initializedContext = _context
            ?? throw new InvalidOperationException("The node must be initialized before it receives packets.");

        await EmitEnabledStatusAsync(initializedContext, cancellationToken);
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken)
    {
        var context = _context
            ?? throw new InvalidOperationException("The node must be initialized before it starts.");

        await EmitEnabledStatusAsync(context, cancellationToken);

        if (!_settings.IsEnabled)
        {
            return;
        }

        var samples = new double[TestSignalSettings.SampleCount];

        for (var index = 0; index < samples.Length; index++)
        {
            samples[index] = CreateSample(index, _settings);
        }

        var frame = new FastStreamFrame(
            StartTimeUnixNanoseconds: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000,
            SamplePeriodNanoseconds: 1_000_000_000L / TestSignalSettings.SampleRateHertz,
            ChannelNames: ["signal"],
            Samples: [samples.AsMemory()],
            SequenceNumber: 0);

        await context.EmitAsync(TestSignalBlock.StreamOutputPortId, frame, cancellationToken);
    }

    public ValueTask StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    private static double CreateSample(int index, TestSignalSettings settings)
    {
        var radians = 2 * Math.PI * settings.FrequencyHertz * index / TestSignalSettings.SampleRateHertz;
        return settings.WaveType switch
        {
            TestSignalWaveType.Sine => settings.Amplitude * Math.Sin(radians),
            TestSignalWaveType.Square => settings.Amplitude * (Math.Sin(radians) >= 0 ? 1 : -1),
            _ => throw new InvalidOperationException($"Unsupported wave type '{settings.WaveType}'."),
        };
    }

    private ValueTask EmitEnabledStatusAsync(INodeContext context, CancellationToken cancellationToken)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var payload = JsonSerializer.SerializeToElement(new
        {
            enabled = _settings.IsEnabled,
        });
        var message = new JsonMessage(
            Topic: "dataprocesses.test-signal.status",
            Payload: payload,
            Timestamp: timestamp);

        return context.EmitAsync(TestSignalBlock.PayloadOutputPortId, message, cancellationToken);
    }
}