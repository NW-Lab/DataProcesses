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