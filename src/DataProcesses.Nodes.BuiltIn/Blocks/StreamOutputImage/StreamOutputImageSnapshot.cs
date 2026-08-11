using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamOutputImage;

/// <summary>
/// Stores a bounded preview copy of the latest image packet for debug visualization.
/// </summary>
public sealed record StreamOutputImageSnapshot(
    string Name,
    int Width,
    int Height,
    ImagePixelFormat PixelFormat,
    int SourceByteLength,
    ReadOnlyMemory<byte> PreviewPixels,
    long SequenceNumber,
    DateTimeOffset? Timestamp);

