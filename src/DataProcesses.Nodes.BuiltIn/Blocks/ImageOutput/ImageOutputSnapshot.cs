using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.ImageOutput;

/// <summary>
/// Stores a bounded preview copy of the latest image packet for debug visualization.
/// </summary>
public sealed record ImageOutputSnapshot(
    string Name,
    int Width,
    int Height,
    ImagePixelFormat PixelFormat,
    int SourceByteLength,
    ReadOnlyMemory<byte> PreviewPixels,
    long SequenceNumber,
    DateTimeOffset? Timestamp);
