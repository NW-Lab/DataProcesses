using System.Text.Json;

namespace DataProcesses.Plugin.Abstractions;

/// <summary>
/// Identifies the transport semantics of a node port.
/// </summary>
public enum PortDataKind
{
    FastStream,
    JsonMessage,
}

/// <summary>
/// Describes how image pixel bytes are laid out in memory.
/// </summary>
public enum ImagePixelFormat
{
    Gray8,
    Rgb24,
    Rgba32,
}

/// <summary>
/// Base contract for data exchanged between nodes.
/// </summary>
public interface IDataPacket
{
    PortDataKind Kind { get; }
}

/// <summary>
/// High-throughput, regularly sampled numeric data.
/// CSV is intentionally not used for internal node-to-node transport.
/// </summary>
/// <param name="StartTimeUnixNanoseconds">Timestamp of the first sample.</param>
/// <param name="SamplePeriodNanoseconds">Nominal interval between samples.</param>
/// <param name="ChannelNames">Channel names in sample storage order.</param>
/// <param name="Samples">Channel-major sample arrays.</param>
/// <param name="SequenceNumber">Monotonic sequence number within a stream.</param>
public sealed record FastStreamFrame(
    long StartTimeUnixNanoseconds,
    long SamplePeriodNanoseconds,
    IReadOnlyList<string> ChannelNames,
    IReadOnlyList<ReadOnlyMemory<double>> Samples,
    long SequenceNumber) : IDataPacket
{
    public PortDataKind Kind => PortDataKind.FastStream;

    public int ChannelCount => Samples.Count;

    public int SampleCount => Samples.Count == 0 ? 0 : Samples[0].Length;
}

/// <summary>
/// Frequency-domain magnitudes derived from a regularly sampled Fast Stream frame.
/// Frequency bin zero is DC and each subsequent bin advances by
/// <see cref="FrequencyResolutionHertz"/>.
/// </summary>
/// <param name="SourceStartTimeUnixNanoseconds">Timestamp of the first source sample.</param>
/// <param name="SourceSamplePeriodNanoseconds">Sampling interval of the source frame.</param>
/// <param name="FrequencyResolutionHertz">Distance between adjacent frequency bins.</param>
/// <param name="ChannelNames">Channel names in magnitude storage order.</param>
/// <param name="Magnitudes">Channel-major one-sided magnitude spectra.</param>
/// <param name="SequenceNumber">Monotonic sequence number inherited from the source stream.</param>
public sealed record SpectrumFrame(
    long SourceStartTimeUnixNanoseconds,
    long SourceSamplePeriodNanoseconds,
    double FrequencyResolutionHertz,
    IReadOnlyList<string> ChannelNames,
    IReadOnlyList<ReadOnlyMemory<double>> Magnitudes,
    long SequenceNumber) : IDataPacket
{
    public PortDataKind Kind => PortDataKind.FastStream;

    public int ChannelCount => Magnitudes.Count;

    public int BinCount => Magnitudes.Count == 0 ? 0 : Magnitudes[0].Length;
}

/// <summary>
/// One-dimensional dense numeric data for non-time-series processing.
/// </summary>
/// <param name="Name">Logical data name (for example, "fft-magnitude").</param>
/// <param name="Values">Dense numeric values in index order.</param>
/// <param name="SequenceNumber">Monotonic sequence number within a stream.</param>
/// <param name="Timestamp">Optional logical timestamp for the vector.</param>
public sealed record NumericVectorFrame(
    string Name,
    ReadOnlyMemory<double> Values,
    long SequenceNumber,
    DateTimeOffset? Timestamp = null) : IDataPacket
{
    public PortDataKind Kind => PortDataKind.FastStream;

    public int Length => Values.Length;
}

/// <summary>
/// Two-dimensional dense numeric data using a row-major buffer.
/// </summary>
/// <param name="Name">Logical data name (for example, "spectrogram").</param>
/// <param name="RowCount">Number of rows.</param>
/// <param name="ColumnCount">Number of columns.</param>
/// <param name="ValuesRowMajor">Row-major values with length RowCount * ColumnCount.</param>
/// <param name="SequenceNumber">Monotonic sequence number within a stream.</param>
/// <param name="Timestamp">Optional logical timestamp for the matrix.</param>
public sealed record NumericMatrixFrame : IDataPacket
{
    public NumericMatrixFrame(
        string name,
        int rowCount,
        int columnCount,
        ReadOnlyMemory<double> valuesRowMajor,
        long sequenceNumber,
        DateTimeOffset? timestamp = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (rowCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rowCount));
        }

        if (columnCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(columnCount));
        }

        var expectedLength = rowCount * columnCount;
        if (valuesRowMajor.Length != expectedLength)
        {
            throw new ArgumentException(
                $"Values length must be {expectedLength} for a {rowCount}x{columnCount} matrix.",
                nameof(valuesRowMajor));
        }

        Name = name;
        RowCount = rowCount;
        ColumnCount = columnCount;
        ValuesRowMajor = valuesRowMajor;
        SequenceNumber = sequenceNumber;
        Timestamp = timestamp;
    }

    public string Name { get; }

    public int RowCount { get; }

    public int ColumnCount { get; }

    public ReadOnlyMemory<double> ValuesRowMajor { get; }

    public long SequenceNumber { get; }

    public DateTimeOffset? Timestamp { get; }

    public PortDataKind Kind => PortDataKind.FastStream;
}

/// <summary>
/// 2D image data using an interleaved HxWxC byte layout.
/// </summary>
/// <param name="Name">Logical image name.</param>
/// <param name="Width">Image width in pixels.</param>
/// <param name="Height">Image height in pixels.</param>
/// <param name="PixelFormat">Pixel format and channel count.</param>
/// <param name="PixelsInterleaved">Interleaved pixel bytes in HxWxC order.</param>
/// <param name="SequenceNumber">Monotonic sequence number within a stream.</param>
/// <param name="Timestamp">Optional logical timestamp for the frame.</param>
public sealed record ImageFrame : IDataPacket
{
    public ImageFrame(
        string name,
        int width,
        int height,
        ImagePixelFormat pixelFormat,
        ReadOnlyMemory<byte> pixelsInterleaved,
        long sequenceNumber,
        DateTimeOffset? timestamp = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (width < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        var channelCount = GetChannelCount(pixelFormat);
        var expectedLength = width * height * channelCount;
        if (pixelsInterleaved.Length != expectedLength)
        {
            throw new ArgumentException(
                $"Pixels length must be {expectedLength} for {width}x{height} {pixelFormat}.",
                nameof(pixelsInterleaved));
        }

        Name = name;
        Width = width;
        Height = height;
        PixelFormat = pixelFormat;
        PixelsInterleaved = pixelsInterleaved;
        SequenceNumber = sequenceNumber;
        Timestamp = timestamp;
    }

    public string Name { get; }

    public int Width { get; }

    public int Height { get; }

    public ImagePixelFormat PixelFormat { get; }

    public ReadOnlyMemory<byte> PixelsInterleaved { get; }

    public long SequenceNumber { get; }

    public DateTimeOffset? Timestamp { get; }

    public PortDataKind Kind => PortDataKind.FastStream;

    public int ChannelCount => GetChannelCount(PixelFormat);

    private static int GetChannelCount(ImagePixelFormat pixelFormat)
    {
        return pixelFormat switch
        {
            ImagePixelFormat.Gray8 => 1,
            ImagePixelFormat.Rgb24 => 3,
            ImagePixelFormat.Rgba32 => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(pixelFormat)),
        };
    }
}

/// <summary>
/// Event, command, state, or extensible structured data using a payload envelope.
/// </summary>
public sealed record JsonMessage(
    string Topic,
    JsonElement Payload,
    DateTimeOffset Timestamp,
    string? CorrelationId = null) : IDataPacket
{
    public PortDataKind Kind => PortDataKind.JsonMessage;
}