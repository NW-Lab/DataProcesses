using DataProcesses.Core;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Engine;

public sealed record FlowRunResult(
    FlowExecutionState State,
    IReadOnlyList<FlowValidationIssue> ValidationIssues,
    IReadOnlyList<FlowExecutionLogEntry> Logs)
{
    public IReadOnlyList<FlowOutputPacket> OutputPackets { get; init; } = [];
}

public sealed record FlowOutputPacket(
    string NodeId,
    string OutputPortId,
    IDataPacket Packet);