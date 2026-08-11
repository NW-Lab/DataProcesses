namespace DataProcesses.Nodes.BuiltIn.Blocks.StremOutputTS;

/// <summary>
/// Immutable display-oriented view of the latest Fast Stream frame received by a StremOutputTS Block.
/// </summary>
public sealed record StremOutputTSSnapshot(
    long StartTimeUnixNanoseconds,
    long SamplePeriodNanoseconds,
    IReadOnlyList<string> ChannelNames,
    IReadOnlyList<ReadOnlyMemory<double>> Samples,
    int SourceSampleCount,
    long SequenceNumber);

