using System.Text.Json;

using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.TestSignalImg;

public sealed class TestSignalImgNode : INode
{
    private readonly TestSignalImgSettings settings;
    private readonly Func<DateTimeOffset> getTimestamp;
    private INodeContext? context;

    public TestSignalImgNode(TestSignalImgSettings settings, Func<DateTimeOffset>? getTimestamp = null)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.settings.Validate();
        this.getTimestamp = getTimestamp ?? (() => DateTimeOffset.UtcNow);
    }

    public NodeDefinition Definition => TestSignalImgBlock.Definition;

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

        if (!string.Equals(inputPortId, TestSignalImgBlock.PayloadInputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        if (packet is not JsonMessage message)
        {
            throw new ArgumentException("TestSignalImg payload input accepts JsonMessage packets only.", nameof(packet));
        }

        var nextSettings = settings.ApplyPayload(message.Payload);
        nextSettings.Validate();

        if (!nextSettings.PayloadThrough)
        {
            await EmitStatusAsync(nextSettings, cancellationToken);
            return;
        }

        await context!.EmitAsync(TestSignalImgBlock.PayloadOutputPortId, message, cancellationToken);
        await EmitStatusAsync(nextSettings, cancellationToken);
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var initializedContext = context ?? throw new InvalidOperationException("The node must be initialized before it starts.");
        await EmitStatusAsync(settings, cancellationToken);
        await EmitImageAsync(initializedContext, cancellationToken);
    }

    public ValueTask StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    private async Task EmitStatusAsync(TestSignalImgSettings activeSettings, CancellationToken cancellationToken)
    {
        if (context is null)
        {
            return;
        }

        var statusObject = JsonSerializer.SerializeToElement(new
        {
            enabled = activeSettings.IsEnabled,
            kind = activeSettings.Kind.ToString().ToLowerInvariant(),
            width = activeSettings.Width,
            height = activeSettings.Height,
            frameRateMillis = activeSettings.FrameRateMilliseconds,
            payloadThrough = activeSettings.PayloadThrough,
        });

        var message = new JsonMessage(
            Topic: "dataprocesses.test-signal-img.status",
            Payload: statusObject,
            Timestamp: getTimestamp());

        await context.EmitAsync(TestSignalImgBlock.PayloadOutputPortId, message, cancellationToken);
    }

    private async Task EmitImageAsync(INodeContext nodeContext, CancellationToken cancellationToken)
    {
        if (!settings.IsEnabled)
        {
            return;
        }

        var pixels = new byte[settings.Width * settings.Height];
        var value = 1 + (DateTimeOffset.UtcNow.Second % 100);
        for (var index = 0; index < pixels.Length; index++)
        {
            pixels[index] = (byte)Math.Clamp(value, 0, 255);
        }

        var frame = new ImageFrame(
            name: "signal",
            width: settings.Width,
            height: settings.Height,
            pixelFormat: ImagePixelFormat.Gray8,
            pixelsInterleaved: pixels,
            sequenceNumber: 0,
            timestamp: getTimestamp());

        await nodeContext.EmitAsync(TestSignalImgBlock.StreamOutputPortId, frame, cancellationToken);
    }
}
