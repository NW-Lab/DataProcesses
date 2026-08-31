using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.HumansImage;

/// <summary>
/// Counts face-like skin-color regions in a single image frame.
/// </summary>
public sealed class HumansImageNode : INode
{
    private readonly HumansImageSettings settings;

    private INodeContext? _context;

    public HumansImageNode(HumansImageSettings? settings = null)
    {
        this.settings = settings ?? HumansImageSettings.Default;
        this.settings.Validate();
    }

    public NodeDefinition Definition => HumansImageBlock.Definition;

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

        if (!string.Equals(inputPortId, HumansImageBlock.InputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        if (packet is not ImageFrame image)
        {
            throw new ArgumentException("HumansImage accepts Image input only.", nameof(packet));
        }

        var context = _context
            ?? throw new InvalidOperationException("The node must be initialized before it receives packets.");

        var humansCount = CountFaceCandidates(image);
        var outputFrame = new FastStreamFrame(
            ResolveTimestampNanoseconds(image),
            0,
            ["humans-count"],
            [new double[] { humansCount }.AsMemory()],
            image.SequenceNumber);

        await context.EmitAsync(HumansImageBlock.OutputPortId, outputFrame, cancellationToken);
    }

    public ValueTask StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    private int CountFaceCandidates(ImageFrame image)
    {
        if (image.Width == 0 || image.Height == 0 || image.PixelFormat == ImagePixelFormat.Gray8)
        {
            return 0;
        }

        var mask = BuildSkinMask(image);
        var visited = new bool[mask.Length];
        var queue = new int[mask.Length];
        var count = 0;

        for (var index = 0; index < mask.Length; index++)
        {
            if (!mask[index] || visited[index])
            {
                continue;
            }

            var component = FloodFill(index, image.Width, image.Height, mask, visited, queue);
            if (IsFaceCandidate(component))
            {
                count++;
            }
        }

        return count;
    }

    private bool[] BuildSkinMask(ImageFrame image)
    {
        var pixels = image.PixelsInterleaved.Span;
        var bytesPerPixel = GetBytesPerPixel(image.PixelFormat);
        var mask = new bool[image.Width * image.Height];

        for (var y = 0; y < image.Height; y++)
        {
            var rowOffset = y * image.Width * bytesPerPixel;
            for (var x = 0; x < image.Width; x++)
            {
                var pixelOffset = rowOffset + (x * bytesPerPixel);
                mask[(y * image.Width) + x] = IsSkinColoredPixel(
                    pixels[pixelOffset],
                    pixels[pixelOffset + 1],
                    pixels[pixelOffset + 2]);
            }
        }

        return mask;
    }

    private static ConnectedComponent FloodFill(
        int startIndex,
        int width,
        int height,
        bool[] mask,
        bool[] visited,
        int[] queue)
    {
        var head = 0;
        var tail = 0;
        queue[tail++] = startIndex;
        visited[startIndex] = true;

        var minX = width;
        var minY = height;
        var maxX = 0;
        var maxY = 0;
        var pixelCount = 0;

        while (head < tail)
        {
            var index = queue[head++];
            var x = index % width;
            var y = index / width;

            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
            pixelCount++;

            TryEnqueue(index - 1, x > 0);
            TryEnqueue(index + 1, x + 1 < width);
            TryEnqueue(index - width, y > 0);
            TryEnqueue(index + width, y + 1 < height);
        }

        return new ConnectedComponent(pixelCount, minX, minY, maxX, maxY);

        void TryEnqueue(int neighborIndex, bool isInsideImage)
        {
            if (!isInsideImage || visited[neighborIndex] || !mask[neighborIndex])
            {
                return;
            }

            visited[neighborIndex] = true;
            queue[tail++] = neighborIndex;
        }
    }

    private bool IsFaceCandidate(ConnectedComponent component)
    {
        var width = component.MaxX - component.MinX + 1;
        var height = component.MaxY - component.MinY + 1;
        if (component.PixelCount < settings.MinimumFacePixelCount
            || width < settings.MinimumFaceWidthPixels
            || height < settings.MinimumFaceHeightPixels)
        {
            return false;
        }

        var boundingArea = width * height;
        var skinRatio = component.PixelCount / (double)boundingArea;
        var aspectRatio = width / (double)height;
        return skinRatio >= settings.MinimumSkinRatio && aspectRatio is >= 0.45 and <= 2.20;
    }

    private static bool IsSkinColoredPixel(byte red, byte green, byte blue)
    {
        var max = Math.Max(red, Math.Max(green, blue));
        var min = Math.Min(red, Math.Min(green, blue));
        var sum = red + green + blue;
        if (sum == 0)
        {
            return false;
        }

        var redRatio = red / (double)sum;
        var greenRatio = green / (double)sum;
        var blueRatio = blue / (double)sum;

        return red > 95
            && green > 40
            && blue > 20
            && max - min > 15
            && Math.Abs(red - green) > 15
            && red > green
            && red > blue
            && redRatio is >= 0.36 and <= 0.60
            && greenRatio is >= 0.25 and <= 0.40
            && blueRatio is >= 0.15 and <= 0.32;
    }

    private static long ResolveTimestampNanoseconds(ImageFrame image)
    {
        return image.Timestamp is { } timestamp
            ? (timestamp - DateTimeOffset.UnixEpoch).Ticks * 100L
            : image.SequenceNumber;
    }

    private static int GetBytesPerPixel(ImagePixelFormat pixelFormat)
    {
        return pixelFormat switch
        {
            ImagePixelFormat.Rgb24 => 3,
            ImagePixelFormat.Rgba32 => 4,
            ImagePixelFormat.Gray8 => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(pixelFormat)),
        };
    }

    private readonly record struct ConnectedComponent(int PixelCount, int MinX, int MinY, int MaxX, int MaxY);
}