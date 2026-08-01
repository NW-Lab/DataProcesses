namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamOutput;

/// <summary>
/// Immutable display-oriented view of the latest Fast Stream frame received by a Stream Output Block.
/// </summary>
public sealed record StreamOutputSnapshot(
    long StartTimeUnixNanoseconds,
    long SamplePeriodNanoseconds,
    IReadOnlyList<string> ChannelNames,
    IReadOnlyList<ReadOnlyMemory<double>> Samples,
    int SourceSampleCount,
    long SequenceNumber);
