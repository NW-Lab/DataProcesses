namespace DataProcesses.Nodes.BuiltIn.Blocks.TimeSeriesDisplay;

/// <summary>
/// Immutable display-oriented view of the latest Fast Stream frame received by a Time Series Block.
/// </summary>
public sealed record TimeSeriesSnapshot(
    long StartTimeUnixNanoseconds,
    long SamplePeriodNanoseconds,
    IReadOnlyList<string> ChannelNames,
    IReadOnlyList<ReadOnlyMemory<double>> Samples,
    int SourceSampleCount,
    long SequenceNumber);
