namespace DataProcesses.Engine;

public sealed record FlowExecutionLogEntry(
    DateTimeOffset Timestamp,
    FlowExecutionLogLevel Level,
    string Message,
    string? NodeId = null);

public enum FlowExecutionLogLevel
{
    Information,
    Warning,
    Error,
}