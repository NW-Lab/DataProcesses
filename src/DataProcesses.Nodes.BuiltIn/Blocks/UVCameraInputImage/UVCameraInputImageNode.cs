using System.Collections.Concurrent;
using System.Text.Json;

using DataProcesses.Plugin.Abstractions;

using OpenCvSharp;

namespace DataProcesses.Nodes.BuiltIn.Blocks.UVCameraInputImage;

public sealed class UVCameraInputImageNode : INode
{
    private static readonly ConcurrentDictionary<string, RuntimeState> RuntimeStateByNodeId = new(StringComparer.Ordinal);

    private readonly string nodeId;
    private readonly UVCameraInputImageSettings settings;
    private readonly Func<int, int, int, bool, double, CancellationToken, ImageFrame> captureImage;
    private readonly Func<DateTimeOffset> getTimestamp;
    private INodeContext? context;

    public UVCameraInputImageNode(
        string nodeId,
        UVCameraInputImageSettings settings,
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

    public NodeDefinition Definition => UVCameraInputImageBlock.Definition;

    public ValueTask InitializeAsync(INodeContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        return ValueTask.CompletedTask;
    }

    public ValueTask OnPacketAsync(string inputPortId, IDataPacket packet, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPortId);
        ArgumentNullException.ThrowIfNull(packet);

        if (!string.Equals(inputPortId, UVCameraInputImageBlock.ControlInputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        if (packet is not JsonMessage message)
        {
            throw new ArgumentException("UVCameraInputImage control input accepts JsonMessage packets only.", nameof(packet));
        }

        if (message.Payload.ValueKind != JsonValueKind.Object
            || !message.Payload.TryGetProperty("isPlay", out var isPlay))
        {
            return ValueTask.CompletedTask;
        }

        if (isPlay.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            throw new ArgumentException("UVCameraInputImage payload field 'isPlay' must be a boolean.", nameof(packet));
        }

        var state = GetRuntimeState();
        lock (state)
        {
            state.IsPlay = isPlay.GetBoolean();
        }

        return ValueTask.CompletedTask;
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var initializedContext = context ?? throw new InvalidOperationException("The node must be initialized before it starts.");
        var state = GetRuntimeState();
        var timestamp = getTimestamp();
        long sequenceNumber;

        lock (state)
        {
            if (!state.IsPlay
                || (state.LastCaptureTimestamp is not null
                    && timestamp - state.LastCaptureTimestamp.Value < TimeSpan.FromSeconds(1.0 / settings.FramesPerSecond)))
            {
                return;
            }

            sequenceNumber = state.NextSequenceNumber;
        }

        var frame = await Task.Run(
            () => captureImage(
                settings.DeviceIndex,
                settings.RequestedWidth,
                settings.RequestedHeight,
                settings.IsWhiteBalanceAuto,
                settings.WhiteBalanceTemperature,
                cancellationToken),
            cancellationToken).ConfigureAwait(false);

        var output = new ImageFrame(
            frame.Name,
            frame.Width,
            frame.Height,
            frame.PixelFormat,
            frame.PixelsInterleaved,
            sequenceNumber,
            frame.Timestamp ?? timestamp);
        await initializedContext.EmitAsync(UVCameraInputImageBlock.ImageOutputPortId, output, cancellationToken).ConfigureAwait(false);

        lock (state)
        {
            state.NextSequenceNumber = sequenceNumber + 1;
            state.LastCaptureTimestamp = timestamp;
        }
    }

    public ValueTask StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    private RuntimeState GetRuntimeState()
    {
        var state = RuntimeStateByNodeId.GetOrAdd(nodeId, static _ => new RuntimeState());
        lock (state)
        {
            if (state.ExecutionSessionId != settings.ExecutionSessionId)
            {
                state.ExecutionSessionId = settings.ExecutionSessionId;
                state.IsPlay = settings.IsPlay;
                state.NextSequenceNumber = 0;
                state.LastCaptureTimestamp = null;
            }
        }

        return state;
    }

    private static ImageFrame CaptureImage(
        int deviceIndex,
        int requestedWidth,
        int requestedHeight,
        bool isWhiteBalanceAuto,
        double whiteBalanceTemperature,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var capture = new VideoCapture(deviceIndex);
        if (!capture.IsOpened())
        {
            throw new InvalidOperationException($"Unable to open UV camera device {deviceIndex}. The device may not expose a UVC video interface.");
        }

        capture.Set(VideoCaptureProperties.FrameWidth, requestedWidth);
        capture.Set(VideoCaptureProperties.FrameHeight, requestedHeight);
        capture.Set(VideoCaptureProperties.AutoWB, isWhiteBalanceAuto ? 1 : 0);
        if (!isWhiteBalanceAuto)
        {
            capture.Set(VideoCaptureProperties.WBTemperature, whiteBalanceTemperature);
        }

        using var bgrFrame = new Mat();
        if (!capture.Read(bgrFrame) || bgrFrame.Empty())
        {
            throw new InvalidOperationException($"UV camera device {deviceIndex} did not return an image.");
        }

        using var rgbFrame = new Mat();
        Cv2.CvtColor(bgrFrame, rgbFrame, ColorConversionCodes.BGR2RGB);
        var pixels = new byte[checked(rgbFrame.Rows * rgbFrame.Cols * rgbFrame.Channels())];
        System.Runtime.InteropServices.Marshal.Copy(rgbFrame.Data, pixels, 0, pixels.Length);
        return new ImageFrame("uv-camera", rgbFrame.Cols, rgbFrame.Rows, ImagePixelFormat.Rgb24, pixels, 0);
    }

    private sealed class RuntimeState
    {
        public long ExecutionSessionId { get; set; } = long.MinValue;

        public bool IsPlay { get; set; }

        public long NextSequenceNumber { get; set; }

        public DateTimeOffset? LastCaptureTimestamp { get; set; }
    }
}