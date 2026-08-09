using System.Text.Json;

using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.TestSignalVec;

public sealed class TestSignalVecNode : INode
{
    private readonly TestSignalVecSettings settings;
    private readonly Func<DateTimeOffset> getTimestamp;
    private INodeContext? context;

    public TestSignalVecNode(TestSignalVecSettings settings, Func<DateTimeOffset>? getTimestamp = null)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.settings.Validate();
        this.getTimestamp = getTimestamp ?? (() => DateTimeOffset.UtcNow);
    }

    public NodeDefinition Definition => TestSignalVecBlock.Definition;

    public ValueTask InitializeAsync(INodeContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        return ValueTask.CompletedTask;
    }

    public async ValueTask OnPacketAsync(string inputPortId, IDataPacket packet, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPortId);
        ArgumentNullException.ThrowIfNull(packet);

        if (!string.Equals(inputPortId, TestSignalVecBlock.PayloadInputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        if (packet is not JsonMessage message)
        {
            throw new ArgumentException("TestSignalVec payload input accepts JsonMessage packets only.", nameof(packet));
        }

        var nextSettings = settings.ApplyPayload(message.Payload);
        nextSettings.Validate();

        if (!nextSettings.PayloadThrough)
        {
            await EmitStatusAsync(nextSettings, cancellationToken);
            return;
        }

        await context!.EmitAsync(TestSignalVecBlock.PayloadOutputPortId, message, cancellationToken);
        await EmitStatusAsync(nextSettings, cancellationToken);
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var initializedContext = context ?? throw new InvalidOperationException("The node must be initialized before it starts.");
        await EmitStatusAsync(settings, cancellationToken);
        await EmitVectorAsync(initializedContext, cancellationToken);
    }

    public ValueTask StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    private async Task EmitStatusAsync(TestSignalVecSettings activeSettings, CancellationToken cancellationToken)
    {
        if (context is null)
        {
            return;
        }

        var statusObject = JsonSerializer.SerializeToElement(new
        {
            enabled = activeSettings.IsEnabled,
            waveType = activeSettings.WaveType.ToString().ToLowerInvariant(),
            frequency = activeSettings.FrequencyHertz,
            amplitude = activeSettings.Amplitude,
            length = activeSettings.Length,
            samplePeriodMillis = activeSettings.SamplePeriodMilliseconds,
            payloadThrough = activeSettings.PayloadThrough,
        });

        var message = new JsonMessage(
            Topic: "dataprocesses.test-signal-vec.status",
            Payload: statusObject,
            Timestamp: getTimestamp());

        await context.EmitAsync(TestSignalVecBlock.PayloadOutputPortId, message, cancellationToken);
    }

    private async Task EmitVectorAsync(INodeContext nodeContext, CancellationToken cancellationToken)
    {
        if (!settings.IsEnabled)
        {
            return;
        }

        var values = new double[settings.Length];
        var phaseStep = 2 * Math.PI * settings.FrequencyHertz * settings.SamplePeriodMilliseconds / 1000.0;
        for (var index = 0; index < values.Length; index++)
        {
            var radians = phaseStep * index;
            values[index] = settings.WaveType switch
            {
                TestSignalVecWaveType.Square => settings.Amplitude * (Math.Sin(radians) >= 0 ? 1 : -1),
                _ => settings.Amplitude * Math.Sin(radians),
            };
        }

        var vector = new NumericVectorFrame(
            Name: "signal",
            Values: values.AsMemory(),
            SequenceNumber: 0,
            Timestamp: getTimestamp());

        await nodeContext.EmitAsync(TestSignalVecBlock.StreamOutputPortId, vector, cancellationToken);
    }
}
