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
            type = activeSettings.Type.ToString().ToLowerInvariant(),
            kind = activeSettings.Kind.ToString().ToLowerInvariant(),
            frequency = activeSettings.FrequencyHertz,
            width = activeSettings.Width,
            height = activeSettings.Height,
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

        var timestamp = getTimestamp();
        var intensity = BuildPatternIntensity(settings, timestamp);

        if (settings.Kind == TestSignalImgKind.Color)
        {
            var pixels = new byte[settings.Width * settings.Height * 3];
            for (var y = 0; y < settings.Height; y++)
            {
                for (var x = 0; x < settings.Width; x++)
                {
                    var index = (y * settings.Width) + x;
                    var value = intensity[index];
                    var red = value;
                    var green = (byte)Math.Clamp(value / 2, 0, 255);
                    var blue = (byte)Math.Clamp(255 - value, 0, 255);
                    var pixelIndex = ((y * settings.Width) + x) * 3;
                    pixels[pixelIndex] = red;
                    pixels[pixelIndex + 1] = green;
                    pixels[pixelIndex + 2] = blue;
                }
            }

            var colorFrame = new ImageFrame(
                name: "signal",
                width: settings.Width,
                height: settings.Height,
                pixelFormat: ImagePixelFormat.Rgb24,
                pixelsInterleaved: pixels,
                sequenceNumber: 0,
                timestamp: timestamp);

            await nodeContext.EmitAsync(TestSignalImgBlock.StreamOutputPortId, colorFrame, cancellationToken);
            return;
        }

        var monoPixels = new byte[settings.Width * settings.Height];
        intensity.CopyTo(monoPixels, 0);

        var frame = new ImageFrame(
            name: "signal",
            width: settings.Width,
            height: settings.Height,
            pixelFormat: ImagePixelFormat.Gray8,
            pixelsInterleaved: monoPixels,
            sequenceNumber: 0,
            timestamp: timestamp);

        await nodeContext.EmitAsync(TestSignalImgBlock.StreamOutputPortId, frame, cancellationToken);
    }

    private static byte[] BuildPatternIntensity(TestSignalImgSettings activeSettings, DateTimeOffset timestamp)
    {
        return activeSettings.Type switch
        {
            TestSignalImgType.Circle => BuildCirclePattern(activeSettings, timestamp),
            _ => BuildNumberPattern(activeSettings, timestamp),
        };
    }

    private static byte[] BuildNumberPattern(TestSignalImgSettings activeSettings, DateTimeOffset timestamp)
    {
        var pixels = new byte[activeSettings.Width * activeSettings.Height];
        var scaledStep = (long)Math.Floor(timestamp.ToUnixTimeMilliseconds() / 100.0 * activeSettings.FrequencyHertz);
        var digit = (int)(((scaledStep % 10) + 10) % 10);

        DrawSevenSegmentDigit(
            pixels,
            activeSettings.Width,
            activeSettings.Height,
            digit,
            255);

        return pixels;
    }

    private static byte[] BuildCirclePattern(TestSignalImgSettings activeSettings, DateTimeOffset timestamp)
    {
        var pixels = new byte[activeSettings.Width * activeSettings.Height];
        var centerX = (activeSettings.Width - 1) / 2.0;
        var centerY = (activeSettings.Height - 1) / 2.0;
        var minDimension = Math.Max(1, Math.Min(activeSettings.Width, activeSettings.Height));
        var spacing = Math.Max(6.0, minDimension / 6.0);
        var thickness = Math.Max(1.0, spacing * 0.18);
        var phase = (timestamp.ToUnixTimeMilliseconds() / 100.0) * activeSettings.FrequencyHertz;
        var offset = phase % spacing;

        for (var y = 0; y < activeSettings.Height; y++)
        {
            for (var x = 0; x < activeSettings.Width; x++)
            {
                var dx = x - centerX;
                var dy = y - centerY;
                var distance = Math.Sqrt((dx * dx) + (dy * dy));
                var ringPosition = (distance + offset) % spacing;
                var distanceToRing = Math.Min(ringPosition, spacing - ringPosition);
                var intensity = distanceToRing <= thickness
                    ? 255
                    : 0;

                pixels[(y * activeSettings.Width) + x] = (byte)intensity;
            }
        }

        return pixels;
    }

    private static void DrawSevenSegmentDigit(byte[] pixels, int width, int height, int digit, byte value)
    {
        var segmentStates = digit switch
        {
            0 => new[] { true, true, true, false, true, true, true },
            1 => new[] { false, false, true, false, false, true, false },
            2 => new[] { true, false, true, true, true, false, true },
            3 => new[] { true, false, true, true, false, true, true },
            4 => new[] { false, true, true, true, false, true, false },
            5 => new[] { true, true, false, true, false, true, true },
            6 => new[] { true, true, false, true, true, true, true },
            7 => new[] { true, false, true, false, false, true, false },
            8 => new[] { true, true, true, true, true, true, true },
            _ => new[] { true, true, true, true, false, true, true },
        };

        var left = (int)Math.Round(width * 0.18, MidpointRounding.AwayFromZero);
        var right = (int)Math.Round(width * 0.82, MidpointRounding.AwayFromZero);
        var top = (int)Math.Round(height * 0.12, MidpointRounding.AwayFromZero);
        var middle = (int)Math.Round(height * 0.50, MidpointRounding.AwayFromZero);
        var bottom = (int)Math.Round(height * 0.88, MidpointRounding.AwayFromZero);
        var thickness = Math.Max(2, (int)Math.Round(Math.Min(width, height) * 0.10, MidpointRounding.AwayFromZero));

        if (segmentStates[0]) DrawHorizontal(pixels, width, height, left, right, top, thickness, value);      // A
        if (segmentStates[1]) DrawVertical(pixels, width, height, left, top, middle, thickness, value);        // B
        if (segmentStates[2]) DrawVertical(pixels, width, height, right, top, middle, thickness, value);       // C
        if (segmentStates[3]) DrawHorizontal(pixels, width, height, left, right, middle, thickness, value);    // D
        if (segmentStates[4]) DrawVertical(pixels, width, height, left, middle, bottom, thickness, value);     // E
        if (segmentStates[5]) DrawVertical(pixels, width, height, right, middle, bottom, thickness, value);    // F
        if (segmentStates[6]) DrawHorizontal(pixels, width, height, left, right, bottom, thickness, value);    // G
    }

    private static void DrawHorizontal(byte[] pixels, int width, int height, int xStart, int xEnd, int yCenter, int thickness, byte value)
    {
        var yMin = Math.Clamp(yCenter - (thickness / 2), 0, height - 1);
        var yMax = Math.Clamp(yCenter + (thickness / 2), 0, height - 1);
        var minX = Math.Clamp(Math.Min(xStart, xEnd), 0, width - 1);
        var maxX = Math.Clamp(Math.Max(xStart, xEnd), 0, width - 1);

        for (var y = yMin; y <= yMax; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                pixels[(y * width) + x] = value;
            }
        }
    }

    private static void DrawVertical(byte[] pixels, int width, int height, int xCenter, int yStart, int yEnd, int thickness, byte value)
    {
        var xMin = Math.Clamp(xCenter - (thickness / 2), 0, width - 1);
        var xMax = Math.Clamp(xCenter + (thickness / 2), 0, width - 1);
        var minY = Math.Clamp(Math.Min(yStart, yEnd), 0, height - 1);
        var maxY = Math.Clamp(Math.Max(yStart, yEnd), 0, height - 1);

        for (var y = minY; y <= maxY; y++)
        {
            for (var x = xMin; x <= xMax; x++)
            {
                pixels[(y * width) + x] = value;
            }
        }
    }
}
