namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamChartVector;

/// <summary>
/// Describes the currently visible StreamChartVector window.
/// </summary>
/// <param name="Name">Logical name of the most recent vector.</param>
/// <param name="ColumnCount">Number of retained time slices.</param>
/// <param name="RowCount">Number of vector indices in the newest slice.</param>
/// <param name="LatestMilliseconds">Millisecond position of the newest slice.</param>
/// <param name="TimeSpanMilliseconds">Width of the visible time window.</param>
/// <param name="SequenceNumber">Sequence number of the newest vector.</param>
public sealed record StreamChartVectorSnapshot(
    string Name,
    int ColumnCount,
    int RowCount,
    double LatestMilliseconds,
    double TimeSpanMilliseconds,
    long SequenceNumber);
