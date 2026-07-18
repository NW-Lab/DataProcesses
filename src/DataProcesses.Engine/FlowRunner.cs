using DataProcesses.Core;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Engine;

public sealed class FlowRunner
{
    private readonly IReadOnlyDictionary<string, INodeFactory> factoriesByTypeId;

    public FlowRunner(IEnumerable<INodeFactory> factories)
    {
        ArgumentNullException.ThrowIfNull(factories);

        factoriesByTypeId = factories.ToDictionary(static factory => factory.Definition.TypeId, StringComparer.Ordinal);
    }

    public FlowExecutionState State { get; private set; } = FlowExecutionState.Stopped;

    public async ValueTask<FlowRunResult> RunAsync(
        FlowDocument document,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        var logs = new List<FlowExecutionLogEntry>();
        State = FlowExecutionState.Validating;
        logs.Add(CreateLog(FlowExecutionLogLevel.Information, "Validating flow."));

        var validationResult = FlowValidator.Validate(
            document,
            factoriesByTypeId.Values.Select(static factory => factory.Definition));

        if (!validationResult.IsValid)
        {
            State = FlowExecutionState.Faulted;
            logs.Add(CreateLog(FlowExecutionLogLevel.Error, "Flow validation failed."));
            return new FlowRunResult(State, validationResult.Issues, logs);
        }

        var orderedNodes = GetTopologicalOrder(document);
        var runtimeNodes = new Dictionary<string, INode>(StringComparer.Ordinal);

        try
        {
            State = FlowExecutionState.Starting;
            logs.Add(CreateLog(FlowExecutionLogLevel.Information, "Starting flow."));

            foreach (var node in orderedNodes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var factory = factoriesByTypeId[node.TypeId];
                var runtimeNode = factory.CreateNode(node.Id);
                runtimeNodes.Add(node.Id, runtimeNode);
            }

            var contexts = runtimeNodes.ToDictionary(
                static pair => pair.Key,
                pair => new FlowNodeContext(pair.Key, document.Connections, runtimeNodes, logs),
                StringComparer.Ordinal);

            foreach (var node in orderedNodes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await runtimeNodes[node.Id].InitializeAsync(contexts[node.Id], cancellationToken).ConfigureAwait(false);
                logs.Add(CreateLog(FlowExecutionLogLevel.Information, "Initialized node.", node.Id));
            }

            State = FlowExecutionState.Running;
            logs.Add(CreateLog(FlowExecutionLogLevel.Information, "Flow is running."));

            foreach (var node in orderedNodes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await runtimeNodes[node.Id].StartAsync(cancellationToken).ConfigureAwait(false);
                logs.Add(CreateLog(FlowExecutionLogLevel.Information, "Started node.", node.Id));
            }

            State = FlowExecutionState.Stopping;
            logs.Add(CreateLog(FlowExecutionLogLevel.Information, "Stopping flow."));

            for (var index = orderedNodes.Count - 1; index >= 0; index--)
            {
                var node = orderedNodes[index];
                await runtimeNodes[node.Id].StopAsync(cancellationToken).ConfigureAwait(false);
                logs.Add(CreateLog(FlowExecutionLogLevel.Information, "Stopped node.", node.Id));
            }

            State = FlowExecutionState.Stopped;
            logs.Add(CreateLog(FlowExecutionLogLevel.Information, "Flow stopped."));
            return new FlowRunResult(State, validationResult.Issues, logs);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            State = FlowExecutionState.Stopping;
            await StopInitializedNodesAsync(orderedNodes, runtimeNodes, logs, CancellationToken.None).ConfigureAwait(false);
            State = FlowExecutionState.Stopped;
            logs.Add(CreateLog(FlowExecutionLogLevel.Warning, "Flow run was canceled."));
            return new FlowRunResult(State, validationResult.Issues, logs);
        }
        catch (Exception exception)
        {
            State = FlowExecutionState.Faulted;
            logs.Add(CreateLog(FlowExecutionLogLevel.Error, exception.Message));
            await StopInitializedNodesAsync(orderedNodes, runtimeNodes, logs, CancellationToken.None).ConfigureAwait(false);
            return new FlowRunResult(State, validationResult.Issues, logs);
        }
    }

    private static async ValueTask StopInitializedNodesAsync(
        IReadOnlyList<NodeInstance> orderedNodes,
        IReadOnlyDictionary<string, INode> runtimeNodes,
        ICollection<FlowExecutionLogEntry> logs,
        CancellationToken cancellationToken)
    {
        for (var index = orderedNodes.Count - 1; index >= 0; index--)
        {
            var node = orderedNodes[index];
            if (!runtimeNodes.TryGetValue(node.Id, out var runtimeNode))
            {
                continue;
            }

            await runtimeNode.StopAsync(cancellationToken).ConfigureAwait(false);
            logs.Add(CreateLog(FlowExecutionLogLevel.Information, "Stopped node.", node.Id));
        }
    }

    private static IReadOnlyList<NodeInstance> GetTopologicalOrder(FlowDocument document)
    {
        var nodesById = document.Nodes.ToDictionary(static node => node.Id, StringComparer.Ordinal);
        var incomingCounts = document.Nodes.ToDictionary(static node => node.Id, static _ => 0, StringComparer.Ordinal);
        var outgoing = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var connection in document.Connections)
        {
            incomingCounts[connection.TargetNodeId]++;

            if (!outgoing.TryGetValue(connection.SourceNodeId, out var targets))
            {
                targets = [];
                outgoing.Add(connection.SourceNodeId, targets);
            }

            targets.Add(connection.TargetNodeId);
        }

        var ready = new Queue<string>(incomingCounts.Where(static pair => pair.Value == 0).Select(static pair => pair.Key));
        var orderedNodes = new List<NodeInstance>(document.Nodes.Count);

        while (ready.Count > 0)
        {
            var nodeId = ready.Dequeue();
            orderedNodes.Add(nodesById[nodeId]);

            if (!outgoing.TryGetValue(nodeId, out var targets))
            {
                continue;
            }

            foreach (var targetNodeId in targets)
            {
                incomingCounts[targetNodeId]--;
                if (incomingCounts[targetNodeId] == 0)
                {
                    ready.Enqueue(targetNodeId);
                }
            }
        }

        return orderedNodes;
    }

    private static FlowExecutionLogEntry CreateLog(
        FlowExecutionLogLevel level,
        string message,
        string? nodeId = null)
    {
        return new FlowExecutionLogEntry(DateTimeOffset.UtcNow, level, message, nodeId);
    }

    private sealed class FlowNodeContext(
        string nodeId,
        IReadOnlyList<Connection> connections,
        IReadOnlyDictionary<string, INode> runtimeNodes,
        ICollection<FlowExecutionLogEntry> logs) : INodeContext
    {
        public string NodeId { get; } = nodeId;

        public async ValueTask EmitAsync(
            string outputPortId,
            IDataPacket packet,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(outputPortId);
            ArgumentNullException.ThrowIfNull(packet);

            logs.Add(CreateLog(FlowExecutionLogLevel.Information, $"Emitted packet on '{outputPortId}'.", NodeId));

            foreach (var connection in connections)
            {
                if (!string.Equals(connection.SourceNodeId, NodeId, StringComparison.Ordinal)
                    || !string.Equals(connection.SourcePortId, outputPortId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (runtimeNodes.TryGetValue(connection.TargetNodeId, out var targetNode))
                {
                    await targetNode.OnPacketAsync(connection.TargetPortId, packet, cancellationToken).ConfigureAwait(false);
                    logs.Add(CreateLog(FlowExecutionLogLevel.Information, $"Delivered packet to '{connection.TargetPortId}'.", connection.TargetNodeId));
                }
            }
        }
    }
}