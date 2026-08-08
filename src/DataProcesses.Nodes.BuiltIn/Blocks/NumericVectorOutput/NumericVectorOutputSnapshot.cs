namespace DataProcesses.Nodes.BuiltIn.Blocks.NumericVectorOutput;

/// <summary>
/// Stores a bounded copy of the latest numeric vector for debug visualization.
/// </summary>
public sealed record NumericVectorOutputSnapshot(
    string Name,
    ReadOnlyMemory<double> Values,
    int SourceLength,
    long SequenceNumber,
    DateTimeOffset? Timestamp);
