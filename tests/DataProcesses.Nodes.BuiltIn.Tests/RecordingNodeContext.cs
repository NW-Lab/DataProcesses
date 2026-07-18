using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests;

internal sealed class RecordingNodeContext : INodeContext
{
    private readonly List<EmittedPacket> _emittedPackets = [];

    public string NodeId { get; init; } = "test-node";

    public IReadOnlyList<EmittedPacket> EmittedPackets => _emittedPackets;

    public ValueTask EmitAsync(
        string outputPortId,
        IDataPacket packet,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPortId);
        ArgumentNullException.ThrowIfNull(packet);
        cancellationToken.ThrowIfCancellationRequested();
        _emittedPackets.Add(new EmittedPacket(outputPortId, packet));
        return ValueTask.CompletedTask;
    }
}

internal sealed record EmittedPacket(string OutputPortId, IDataPacket Packet);
