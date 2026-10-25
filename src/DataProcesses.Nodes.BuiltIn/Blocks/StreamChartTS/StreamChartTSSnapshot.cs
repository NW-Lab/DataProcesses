namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamChartTS;

/// <summary>
/// Contains sampled display data for one channel in the chart.
/// </summary>
/// <param name="ChannelIndex">1-based channel index.</param>
/// <param name="ChannelName">Configured or derived channel name.</param>
/// <param name="Millis">Relative time in milliseconds from display base.</param>
/// <param name="Values">Sampled numeric values.</param>
/// <param name="TotalSampleCount">Total raw sample count received in the frame.</param>
public sealed record StreamChartChannelSnapshot(
    int ChannelIndex,
    string ChannelName,
    ReadOnlyMemory<double> Millis,
    ReadOnlyMemory<double> Values,
    int TotalSampleCount);

/// <summary>
/// Display-oriented snapshot of the StreamChartTS Block holding current channels data.
/// </summary>
public sealed record StreamChartTSSnapshot(
    StreamChartTimeAlignment TimeAlignmentMode,
    double TimeSpanMilliseconds,
    IReadOnlyList<StreamChartChannelSnapshot?> Channels,
    long SequenceNumber,
    long? BaseStartTimeUnixNanoseconds = null);
