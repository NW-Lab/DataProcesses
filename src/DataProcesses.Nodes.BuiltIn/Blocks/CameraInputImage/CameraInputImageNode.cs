using System.Collections.Concurrent;
using System.Text.Json;

using DataProcesses.Plugin.Abstractions;

using OpenCvSharp;

namespace DataProcesses.Nodes.BuiltIn.Blocks.CameraInputImage;

public sealed class CameraInputImageNode : INode
{
    private static readonly ConcurrentDictionary<string, CameraRuntimeState> RuntimeStateByNodeId = new(StringComparer.Ordinal);

    private readonly string nodeId;
    private readonly CameraInputImageSettings settings;
    private readonly Func<DateTimeOffset> getTimestamp;
    private readonly Func<int, int, int, bool, double, CancellationToken, ImageFrame> captureImage;
    private INodeContext? context;

    public CameraInputImageNode(
        string nodeId,
        CameraInputImageSettings settings,
        Func<int, int, int, bool, double, CancellationToken, ImageFrame>? captureImage = null,
        Func<DateTimeOffset>? getTimestamp = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        this.nodeId = nodeId;
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.settings.Validate();
        this.captureImage = captureImage ?? CaptureImage;
        this.getTimestamp = getTimestamp ?? (() => DateTimeOffset.UtcNow);
    }

    public NodeDefinition Definition => CameraInputImageBlock.Definition;

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

        if (!string.Equals(inputPortId, CameraInputImageBlock.TriggerInputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        if (packet is not JsonMessage message)
        {
            throw new ArgumentException("CameraInputImage trigger input accepts JsonMessage packets only.", nameof(packet));
        }

        if (!IsTriggerRequested(message.Payload))
        {
            return;
        }

        await EmitImageAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = context ?? throw new InvalidOperationException("The node must be initialized before it starts.");

        var state = RuntimeStateByNodeId.GetOrAdd(nodeId, static _ => new CameraRuntimeState());
        var now = getTimestamp();
        var shouldCapture = false;
        lock (state)
        {
            if (state.ExecutionSessionId != settings.ExecutionSessionId)
            {
                state.ExecutionSessionId = settings.ExecutionSessionId;
                state.LastManualTriggerNonce = 0;
            }

            if (settings.ManualTriggerNonce > state.LastManualTriggerNonce)
            {
                state.LastManualTriggerNonce = settings.ManualTriggerNonce;
                shouldCapture = true;
            }

            if (settings.ContinuousCapture
                && (state.LastCaptureTimestamp is null
                    || now - state.LastCaptureTimestamp.Value >= TimeSpan.FromSeconds(1.0 / settings.FramesPerSecond)))
            {
                shouldCapture = true;
            }
        }

        if (shouldCapture)
        {
            await EmitImageAsync(now, cancellationToken).ConfigureAwait(false);
        }
    }

    public ValueTask StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    private async ValueTask EmitImageAsync(CancellationToken cancellationToken)
    {
        await EmitImageAsync(getTimestamp(), cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask EmitImageAsync(DateTimeOffset timestamp, CancellationToken cancellationToken)
    {
        var initializedContext = context ?? throw new InvalidOperationException("The node must be initialized before it captures an image.");
        var image = await Task.Run(
            () => captureImage(settings.DeviceIndex, settings.RequestedWidth, settings.RequestedHeight, settings.IsWhiteBalanceAuto, settings.WhiteBalanceTemperature, cancellationToken),
            cancellationToken).ConfigureAwait(false);
        var timestampedImage = new ImageFrame(
            image.Name,
            image.Width,
            image.Height,
            image.PixelFormat,
            image.PixelsInterleaved,
            image.SequenceNumber,
            image.Timestamp ?? timestamp);
        await initializedContext.EmitAsync(CameraInputImageBlock.ImageOutputPortId, timestampedImage, cancellationToken).ConfigureAwait(false);

        var state = RuntimeStateByNodeId.GetOrAdd(nodeId, static _ => new CameraRuntimeState());
        lock (state)
        {
            state.LastCaptureTimestamp = timestamp;
        }
    }

    private static bool IsTriggerRequested(JsonElement payload)
    {
        return payload.ValueKind == JsonValueKind.Object
            && payload.TryGetProperty("Trigger", out var trigger)
            && trigger.ValueKind == JsonValueKind.True;
    }

    private static ImageFrame CaptureImage(int deviceIndex, int requestedWidth, int requestedHeight, bool isWhiteBalanceAuto, double whiteBalanceTemperature, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var capture = new VideoCapture(deviceIndex);
        if (!capture.IsOpened())
        {
            throw new InvalidOperationException($"Unable to open camera device {deviceIndex}.");
        }

        capture.Set(VideoCaptureProperties.FrameWidth, requestedWidth);
        capture.Set(VideoCaptureProperties.FrameHeight, requestedHeight);
        capture.Set(VideoCaptureProperties.AutoWB, isWhiteBalanceAuto ? 1 : 0);
        if (!isWhiteBalanceAuto)
        {
            capture.Set(VideoCaptureProperties.WBTemperature, whiteBalanceTemperature);
        }

        using var frame = new Mat();
        if (!capture.Read(frame) || frame.Empty())
        {
            throw new InvalidOperationException($"Camera device {deviceIndex} did not return an image.");
        }

        using var rgbFrame = new Mat();
        Cv2.CvtColor(frame, rgbFrame, ColorConversionCodes.BGR2RGB);
        var pixelCount = checked(rgbFrame.Rows * rgbFrame.Cols * rgbFrame.Channels());
        var pixels = new byte[pixelCount];
        System.Runtime.InteropServices.Marshal.Copy(rgbFrame.Data, pixels, 0, pixels.Length);

        return new ImageFrame(
            name: "camera",
            width: rgbFrame.Cols,
            height: rgbFrame.Rows,
            pixelFormat: ImagePixelFormat.Rgb24,
            pixelsInterleaved: pixels,
            sequenceNumber: 0);
    }

    private sealed class CameraRuntimeState
    {
        public long ExecutionSessionId { get; set; } = long.MinValue;

        public long LastManualTriggerNonce { get; set; }

        public DateTimeOffset? LastCaptureTimestamp { get; set; }
    }
}