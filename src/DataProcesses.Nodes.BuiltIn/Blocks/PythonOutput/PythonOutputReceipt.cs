using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.PythonOutput;

/// <summary>
/// Describes the latest packet accepted by the Python Output boundary.
/// </summary>
public sealed record PythonOutputReceipt(
    string InputPortId,
    PortDataKind DataKind,
    string PacketType,
    DateTimeOffset ReceivedAt);
