using DataProcesses.Core;

namespace DataProcesses.Engine;

public sealed record FlowRunResult(
    FlowExecutionState State,
    IReadOnlyList<FlowValidationIssue> ValidationIssues,
    IReadOnlyList<FlowExecutionLogEntry> Logs);