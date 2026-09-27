namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamOutputFS;

/// <summary>
/// Immutable display-oriented view of the latest Fast Stream frame.
/// </summary>
public sealed record StreamOutputFSSnapshot(
    long StartTimeUnixNanoseconds,
    double SampleRateHertz,
    IReadOnlyList<string> ChannelNames,
    IReadOnlyList<ReadOnlyMemory<double>> Samples,
    int SourceSampleCount,
    long SequenceNumber);