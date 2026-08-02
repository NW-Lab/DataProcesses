using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;

using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.Trigger;

public sealed class TriggerNode : INode
{
    private static readonly ConcurrentDictionary<string, TriggerRuntimeState> RuntimeStateByNodeId = new(StringComparer.Ordinal);

    private readonly string nodeId;
    private readonly TriggerSettings settings;
    private readonly Func<DateTimeOffset> getTimestamp;
    private INodeContext? context;

    public TriggerNode(string nodeId, TriggerSettings settings, Func<DateTimeOffset>? getTimestamp = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        this.nodeId = nodeId;
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.settings.Validate();
        this.getTimestamp = getTimestamp ?? (() => DateTimeOffset.UtcNow);
    }

    public NodeDefinition Definition => TriggerBlock.Definition;

    public ValueTask InitializeAsync(INodeContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        return ValueTask.CompletedTask;
    }

    public ValueTask OnPacketAsync(string inputPortId, IDataPacket packet, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var initializedContext = context
            ?? throw new InvalidOperationException("The node must be initialized before it starts.");

        var state = RuntimeStateByNodeId.GetOrAdd(nodeId, static _ => new TriggerRuntimeState());
        var now = getTimestamp();

        var emitExecutionStart = false;
        var emitManual = false;
        var periodicTicksToEmit = 0;

        lock (state)
        {
            if (state.ExecutionSessionId != settings.ExecutionSessionId)
            {
                state.ResetForSession(settings.ExecutionSessionId, now);
            }

            state.RunStartedAt ??= now;

            if (settings.EmitOnExecutionStart && !state.HasEmittedExecutionStart)
            {
                state.HasEmittedExecutionStart = true;
                emitExecutionStart = true;
            }

            if (settings.ManualTriggerNonce > state.LastManualTriggerNonce)
            {
                state.LastManualTriggerNonce = settings.ManualTriggerNonce;
                emitManual = true;
            }

            if (settings.EmitPeriodically)
            {
                periodicTicksToEmit = state.ConsumePendingPeriodicTicks(now, settings);
            }
        }

        if (emitExecutionStart)
        {
            await EmitPayloadAsync(initializedContext, cancellationToken).ConfigureAwait(false);
        }

        if (emitManual)
        {
            await EmitPayloadAsync(initializedContext, cancellationToken).ConfigureAwait(false);
        }

        for (var index = 0; index < periodicTicksToEmit; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await EmitPayloadAsync(initializedContext, cancellationToken).ConfigureAwait(false);
        }
    }

    public ValueTask StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    private ValueTask EmitPayloadAsync(INodeContext nodeContext, CancellationToken cancellationToken)
    {
        var timestamp = getTimestamp();
        var payloadValue = CreatePayloadValue(timestamp);
        var payloadObject = BuildPayloadObject(settings.PayloadPath, payloadValue);
        var payloadElement = JsonSerializer.SerializeToElement(payloadObject);

        var message = new JsonMessage(
            Topic: settings.Topic,
            Payload: payloadElement,
            Timestamp: timestamp);

        return nodeContext.EmitAsync(TriggerBlock.PayloadOutputPortId, message, cancellationToken);
    }

    private object CreatePayloadValue(DateTimeOffset timestamp)
    {
        return settings.PayloadValueType switch
        {
            TriggerPayloadValueType.DateTime => timestamp.ToString("O"),
            TriggerPayloadValueType.Boolean => settings.BoolValue,
            TriggerPayloadValueType.String => settings.StringValue,
            TriggerPayloadValueType.Number => settings.NumberValue,
            TriggerPayloadValueType.NumberArray => settings.ParseNumberArray().ToArray(),
            _ => throw new InvalidOperationException($"Unsupported Trigger payload value type '{settings.PayloadValueType}'."),
        };
    }

    private static JsonObject BuildPayloadObject(string payloadPath, object value)
    {
        var objectRoot = new JsonObject();
        var tokens = payloadPath
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (tokens.Count == 0)
        {
            objectRoot["value"] = JsonSerializer.SerializeToNode(value);
            return objectRoot;
        }

        if (string.Equals(tokens[0], "payload", StringComparison.OrdinalIgnoreCase))
        {
            tokens.RemoveAt(0);
        }

        if (tokens.Count == 0)
        {
            objectRoot["value"] = JsonSerializer.SerializeToNode(value);
            return objectRoot;
        }

        JsonObject current = objectRoot;
        for (var index = 0; index < tokens.Count - 1; index++)
        {
            var key = tokens[index];
            var child = new JsonObject();
            current[key] = child;
            current = child;
        }

        current[tokens[^1]] = JsonSerializer.SerializeToNode(value);
        return objectRoot;
    }

    private sealed class TriggerRuntimeState
    {
        public long ExecutionSessionId { get; private set; } = long.MinValue;

        public DateTimeOffset? RunStartedAt { get; set; }

        public bool HasEmittedExecutionStart { get; set; }

        public long LastManualTriggerNonce { get; set; }

        public long LastPeriodicTickIndex { get; set; } = -1;

        public void ResetForSession(long executionSessionId, DateTimeOffset now)
        {
            ExecutionSessionId = executionSessionId;
            RunStartedAt = now;
            HasEmittedExecutionStart = false;
            LastManualTriggerNonce = 0;
            LastPeriodicTickIndex = -1;
        }

        public int ConsumePendingPeriodicTicks(DateTimeOffset now, TriggerSettings settings)
        {
            if (RunStartedAt is null)
            {
                RunStartedAt = now;
            }

            var elapsedMilliseconds = (now - RunStartedAt.Value).TotalMilliseconds;
            if (elapsedMilliseconds < settings.InitialDelayMilliseconds)
            {
                return 0;
            }

            var dueTickIndex = (long)Math.Floor((elapsedMilliseconds - settings.InitialDelayMilliseconds) / settings.RepeatIntervalMilliseconds);
            if (dueTickIndex <= LastPeriodicTickIndex)
            {
                return 0;
            }

            var emitCount = dueTickIndex - LastPeriodicTickIndex;
            LastPeriodicTickIndex = dueTickIndex;
            return (int)Math.Min(emitCount, int.MaxValue);
        }
    }
}
