namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamOutputTS;

/// <summary>
/// Immutable display-oriented view of the latest Fast Stream frame received by a StreamOutputTS Block.
/// </summary>
public sealed record StreamOutputTSSnapshot(
    long StartTimeUnixNanoseconds,
    long SamplePeriodNanoseconds,
    IReadOnlyList<string> ChannelNames,
    IReadOnlyList<ReadOnlyMemory<double>> Samples,
    int SourceSampleCount,
    long SequenceNumber,
    int? SampleRateHertz = null);

