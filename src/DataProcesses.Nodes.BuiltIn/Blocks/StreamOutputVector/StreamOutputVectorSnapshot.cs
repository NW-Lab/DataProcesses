namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamOutputVector;

/// <summary>
/// Stores a bounded copy of the latest numeric vector for debug visualization.
/// </summary>
public sealed record StreamOutputVectorSnapshot(
    string Name,
    ReadOnlyMemory<double> Values,
    int SourceLength,
    long SequenceNumber,
    DateTimeOffset? Timestamp);

