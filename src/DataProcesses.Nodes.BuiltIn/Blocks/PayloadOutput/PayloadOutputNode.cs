using System.Text.Json;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.PayloadOutput;

/// <summary>
/// Captures the latest Payload packet for debug-oriented inspection.
/// </summary>
public sealed class PayloadOutputNode : INode
{
    private bool _isInitialized;

    public NodeDefinition Definition => PayloadOutputBlock.Definition;

    /// <summary>
    /// Gets the most recent formatted log line accepted by this node.
    /// </summary>
    public string? LatestLogEntry { get; private set; }

    public ValueTask InitializeAsync(
        INodeContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);
        _isInitialized = true;
        return ValueTask.CompletedTask;
    }

    public ValueTask OnPacketAsync(
        string inputPortId,
        IDataPacket packet,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPortId);
        ArgumentNullException.ThrowIfNull(packet);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_isInitialized)
        {
            throw new InvalidOperationException("The node must be initialized before it receives packets.");
        }

        if (!string.Equals(inputPortId, PayloadOutputBlock.InputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        if (packet is not JsonMessage message)
        {
            throw new ArgumentException("Payload Output accepts Payload input only.", nameof(packet));
        }

        var loggedAt = DateTimeOffset.UtcNow;
        var correlation = string.IsNullOrWhiteSpace(message.CorrelationId)
            ? "-"
            : message.CorrelationId;
        var payloadJson = JsonSerializer.Serialize(message.Payload);
        LatestLogEntry = FormattableString.Invariant(
            $"[{loggedAt:yyyy-MM-dd HH:mm:ss.fff zzz}] topic={message.Topic} correlationId={correlation} payload={payloadJson}");

        return ValueTask.CompletedTask;
    }

    public ValueTask StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }
}
