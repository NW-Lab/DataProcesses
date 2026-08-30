using System.Collections.Concurrent;
using System.Text.Json;

using DataProcesses.Plugin.Abstractions;

using OpenCvSharp;

namespace DataProcesses.Nodes.BuiltIn.Blocks.MovieInputImage;

public sealed class MovieInputImageNode : INode
{
    private static readonly ConcurrentDictionary<string, MovieRuntimeState> RuntimeStateByNodeId = new(StringComparer.Ordinal);

    private readonly string nodeId;
    private readonly MovieInputImageSettings settings;
    private readonly Func<string, long, int, int, CancellationToken, ImageFrame?> readFrame;
    private readonly Func<DateTimeOffset> getTimestamp;
    private INodeContext? context;

    public MovieInputImageNode(
        string nodeId,
        MovieInputImageSettings settings,
        Func<string, long, int, int, CancellationToken, ImageFrame?>? readFrame = null,
        Func<DateTimeOffset>? getTimestamp = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        this.nodeId = nodeId;
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.settings.Validate();
        this.readFrame = readFrame ?? ReadFrame;
        this.getTimestamp = getTimestamp ?? (() => DateTimeOffset.UtcNow);
    }

    public NodeDefinition Definition => MovieInputImageBlock.Definition;

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

        if (!string.Equals(inputPortId, MovieInputImageBlock.ControlInputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        if (packet is not JsonMessage message)
        {
            throw new ArgumentException("MovieInputImage control input accepts JsonMessage packets only.", nameof(packet));
        }

        if (message.Payload.ValueKind != JsonValueKind.Object
            || !message.Payload.TryGetProperty("isPlay", out var isPlay))
        {
            return ValueTask.CompletedTask;
        }

        if (isPlay.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            throw new ArgumentException("MovieInputImage payload field 'isPlay' must be a boolean.", nameof(packet));
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
        var now = getTimestamp();
        long frameIndex;

        lock (state)
        {
            if (!state.IsPlay || (state.LastFrameTimestamp is not null && now - state.LastFrameTimestamp.Value < TimeSpan.FromSeconds(1.0 / settings.FramesPerSecond)))
            {
                return;
            }

            frameIndex = state.NextFrameIndex;
        }

        var frame = await Task.Run(
            () => readFrame(settings.MoviePath, frameIndex, settings.OutputWidth, settings.OutputHeight, cancellationToken),
            cancellationToken).ConfigureAwait(false);
        if (frame is null)
        {
            lock (state)
            {
                state.NextFrameIndex = 0;
            }

            return;
        }

        var timestampedFrame = new ImageFrame(
            frame.Name,
            frame.Width,
            frame.Height,
            frame.PixelFormat,
            frame.PixelsInterleaved,
            frameIndex,
            frame.Timestamp ?? now);
        await initializedContext.EmitAsync(MovieInputImageBlock.ImageOutputPortId, timestampedFrame, cancellationToken).ConfigureAwait(false);

        lock (state)
        {
            state.NextFrameIndex = frameIndex + 1;
            state.LastFrameTimestamp = now;
        }
    }

    public ValueTask StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    private MovieRuntimeState GetRuntimeState()
    {
        var state = RuntimeStateByNodeId.GetOrAdd(nodeId, static _ => new MovieRuntimeState());
        lock (state)
        {
            if (state.ExecutionSessionId != settings.ExecutionSessionId)
            {
                state.ExecutionSessionId = settings.ExecutionSessionId;
                state.IsPlay = settings.IsPlay;
                state.NextFrameIndex = 0;
                state.LastFrameTimestamp = null;
            }
        }

        return state;
    }

    private static ImageFrame? ReadFrame(string moviePath, long frameIndex, int outputWidth, int outputHeight, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(moviePath))
        {
            throw new InvalidOperationException("MovieInputImage requires a movie file path.");
        }

        using var capture = new VideoCapture(moviePath);
        if (!capture.IsOpened())
        {
            throw new InvalidOperationException($"Unable to open movie file '{moviePath}'.");
        }

        capture.Set(VideoCaptureProperties.PosFrames, frameIndex);
        using var bgrFrame = new Mat();
        if (!capture.Read(bgrFrame) || bgrFrame.Empty())
        {
            return null;
        }

        using var rgbFrame = new Mat();
        Cv2.CvtColor(bgrFrame, rgbFrame, ColorConversionCodes.BGR2RGB);
        using var resizedFrame = new Mat();
        Cv2.Resize(rgbFrame, resizedFrame, new Size(outputWidth, outputHeight), interpolation: InterpolationFlags.Area);
        var pixels = new byte[checked(resizedFrame.Rows * resizedFrame.Cols * resizedFrame.Channels())];
        System.Runtime.InteropServices.Marshal.Copy(resizedFrame.Data, pixels, 0, pixels.Length);

        return new ImageFrame("movie", resizedFrame.Cols, resizedFrame.Rows, ImagePixelFormat.Rgb24, pixels, frameIndex);
    }

    private sealed class MovieRuntimeState
    {
        public long ExecutionSessionId { get; set; } = long.MinValue;

        public bool IsPlay { get; set; }

        public long NextFrameIndex { get; set; }

        public DateTimeOffset? LastFrameTimestamp { get; set; }
    }
}