using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamOutputImage;

/// <summary>
/// Captures the latest image packet for debug-oriented inspection.
/// </summary>
public sealed class StreamOutputImageNode : INode
{
    public const int MaximumPreviewBytes = 262_144;

    private bool isInitialized;

    public NodeDefinition Definition => StreamOutputImageBlock.Definition;

    public StreamOutputImageSnapshot? LatestSnapshot { get; private set; }

    public ValueTask InitializeAsync(
        INodeContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);
        isInitialized = true;
        return ValueTask.CompletedTask;
    }

    public ValueTask OnPacketAsync(
        string inputPortId,
        IDataPacket packet,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPortId);
        ArgumentNullException.ThrowIfNull(packet);
        cancellationToken.ThrowIfCancellationRequested();

        if (!isInitialized)
        {
            throw new InvalidOperationException("The node must be initialized before it receives packets.");
        }

        if (!string.Equals(inputPortId, StreamOutputImageBlock.InputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        if (packet is not ImageFrame image)
        {
            throw new ArgumentException("StreamOutputImage accepts Image input only.", nameof(packet));
        }

        var preview = BuildPreview(image.PixelsInterleaved);
        LatestSnapshot = new StreamOutputImageSnapshot(
            image.Name,
            image.Width,
            image.Height,
            image.PixelFormat,
            image.PixelsInterleaved.Length,
            preview,
            image.SequenceNumber,
            image.Timestamp);

        return ValueTask.CompletedTask;
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

    private static ReadOnlyMemory<byte> BuildPreview(ReadOnlyMemory<byte> source)
    {
        if (source.Length <= MaximumPreviewBytes)
        {
            return source;
        }

        return source.Span[..MaximumPreviewBytes].ToArray();
    }
}


