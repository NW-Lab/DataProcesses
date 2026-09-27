namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamChartFS;

/// <summary>
/// Immutable, bounded view of the latest Fast Stream frame received by a StreamChartFS Block.
/// </summary>
public sealed record StreamChartFSSnapshot(
    long StartTimeUnixNanoseconds,
    long SamplePeriodNanoseconds,
    int? SampleRateHertz,
    IReadOnlyList<string> ChannelNames,
    IReadOnlyList<ReadOnlyMemory<double>> Samples,
    int SourceSampleCount,
    long SequenceNumber);