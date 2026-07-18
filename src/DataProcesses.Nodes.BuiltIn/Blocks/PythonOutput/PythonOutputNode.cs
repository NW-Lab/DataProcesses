using System.Text.Json;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.PythonOutput;

/// <summary>
/// Establishes a typed output boundary for future out-of-process Python workers.
/// The MVP deliberately does not launch Python; it validates the selected input transport,
/// records a receipt, and publishes a JSON status message for diagnostics.
/// </summary>
public sealed class PythonOutputNode : INode
{
    private INodeContext? _context;

    public NodeDefinition Definition => PythonOutputBlock.Definition;

    /// <summary>
    /// Gets the latest packet receipt accepted by this boundary.
    /// </summary>
    public PythonOutputReceipt? LastReceipt { get; private set; }

    public ValueTask InitializeAsync(
        INodeContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _context = context ?? throw new ArgumentNullException(nameof(context));
        return ValueTask.CompletedTask;
    }

    public async ValueTask OnPacketAsync(
        string inputPortId,
        IDataPacket packet,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPortId);
        ArgumentNullException.ThrowIfNull(packet);
        cancellationToken.ThrowIfCancellationRequested();

        var context = _context
            ?? throw new InvalidOperationException("The node must be initialized before it receives packets.");
        ValidateInputPortAndPacketKind(inputPortId, packet);

        var receivedAt = DateTimeOffset.UtcNow;
        LastReceipt = new PythonOutputReceipt(
            inputPortId,
            packet.Kind,
            packet.GetType().Name,
            receivedAt);

        var payload = JsonSerializer.SerializeToElement(new
        {
            delivery = "deferred",
            inputPort = inputPortId,
            packet = DescribePacket(packet),
            receivedAt,
        });
        var status = new JsonMessage(
            Topic: "dataprocesses.python-output.received",
            Payload: payload,
            Timestamp: receivedAt);

        await context.EmitAsync(PythonOutputBlock.StatusOutputPortId, status, cancellationToken);
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

    private static void ValidateInputPortAndPacketKind(string inputPortId, IDataPacket packet)
    {
        if (string.Equals(inputPortId, PythonOutputBlock.FastStreamInputPortId, StringComparison.Ordinal))
        {
            if (packet.Kind != PortDataKind.FastStream)
            {
                throw new ArgumentException("The Fast Stream input accepts Fast Stream packets only.", nameof(packet));
            }

            return;
        }

        if (string.Equals(inputPortId, PythonOutputBlock.JsonMessageInputPortId, StringComparison.Ordinal))
        {
            if (packet is not JsonMessage)
            {
                throw new ArgumentException("The JSON Message input accepts JSON Message packets only.", nameof(packet));
            }

            return;
        }

        throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
    }

    private static object DescribePacket(IDataPacket packet) => packet switch
    {
        FastStreamFrame frame => new
        {
            kind = "fastStream",
            frame.SequenceNumber,
            frame.ChannelCount,
            frame.SampleCount,
        },
        SpectrumFrame spectrum => new
        {
            kind = "spectrum",
            spectrum.SequenceNumber,
            spectrum.ChannelCount,
            spectrum.BinCount,
            spectrum.FrequencyResolutionHertz,
        },
        JsonMessage message => new
        {
            kind = "jsonMessage",
            message.Topic,
            message.CorrelationId,
        },
        _ => new
        {
            kind = packet.Kind.ToString(),
            packetType = packet.GetType().Name,
        },
    };
}
