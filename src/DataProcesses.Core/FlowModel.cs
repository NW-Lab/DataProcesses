using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Core;

public sealed record FlowDocument(
    Guid Id,
    string Name,
    IReadOnlyList<NodeInstance> Nodes,
    IReadOnlyList<Connection> Connections,
    int SchemaVersion = 1);

public sealed record NodeInstance(
    string Id,
    string TypeId,
    double X,
    double Y,
    string SettingsJson,
    string? Name = null,
    string? Description = null,
    bool IsEnabled = true,
    bool? ShowOnDashboard = null,
    int? DashboardGridWidth = null,
    int? DashboardGridHeight = null);

public sealed record Connection(
    string SourceNodeId,
    string SourcePortId,
    string TargetNodeId,
    string TargetPortId,
    PortDataKind DataKind);

public sealed record FlowValidationResult(IReadOnlyList<FlowValidationIssue> Issues)
{
    public bool IsValid => Issues.All(static issue => issue.Severity != FlowValidationSeverity.Error);
}

public sealed record FlowValidationIssue(
    FlowValidationSeverity Severity,
    FlowValidationIssueCode Code,
    string Message,
    string? NodeId = null,
    Connection? Connection = null);

public enum FlowValidationSeverity
{
    Warning,
    Error,
}

public enum FlowValidationIssueCode
{
    DuplicateNodeId,
    UnknownNodeType,
    UnknownSourceNode,
    UnknownTargetNode,
    UnknownSourcePort,
    UnknownTargetPort,
    IncompatiblePorts,
    MissingRequiredInput,
    CycleDetected,
}

public static class FlowValidator
{
    public static FlowValidationResult Validate(
        FlowDocument document,
        IEnumerable<NodeDefinition> nodeDefinitions)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(nodeDefinitions);

        var issues = new List<FlowValidationIssue>();
        var definitionsByTypeId = nodeDefinitions.ToDictionary(static definition => definition.TypeId, StringComparer.Ordinal);
        var nodesById = new Dictionary<string, NodeInstance>(StringComparer.Ordinal);
        var duplicateNodeIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var node in document.Nodes)
        {
            if (!nodesById.TryAdd(node.Id, node))
            {
                duplicateNodeIds.Add(node.Id);
            }

            if (!definitionsByTypeId.ContainsKey(node.TypeId))
            {
                issues.Add(new FlowValidationIssue(
                    FlowValidationSeverity.Error,
                    FlowValidationIssueCode.UnknownNodeType,
                    $"Node '{node.Id}' references unknown type '{node.TypeId}'.",
                    node.Id));
            }
        }

        foreach (var duplicateNodeId in duplicateNodeIds)
        {
            issues.Add(new FlowValidationIssue(
                FlowValidationSeverity.Error,
                FlowValidationIssueCode.DuplicateNodeId,
                $"Node id '{duplicateNodeId}' is used more than once.",
                duplicateNodeId));
        }

        var validConnectionEdges = new List<(string SourceNodeId, string TargetNodeId)>();

        foreach (var connection in document.Connections)
        {
            if (!nodesById.TryGetValue(connection.SourceNodeId, out var sourceNode))
            {
                issues.Add(new FlowValidationIssue(
                    FlowValidationSeverity.Error,
                    FlowValidationIssueCode.UnknownSourceNode,
                    $"Connection references unknown source node '{connection.SourceNodeId}'.",
                    connection.SourceNodeId,
                    connection));
                continue;
            }

            if (!nodesById.TryGetValue(connection.TargetNodeId, out var targetNode))
            {
                issues.Add(new FlowValidationIssue(
                    FlowValidationSeverity.Error,
                    FlowValidationIssueCode.UnknownTargetNode,
                    $"Connection references unknown target node '{connection.TargetNodeId}'.",
                    connection.TargetNodeId,
                    connection));
                continue;
            }

            if (!definitionsByTypeId.TryGetValue(sourceNode.TypeId, out var sourceDefinition)
                || !definitionsByTypeId.TryGetValue(targetNode.TypeId, out var targetDefinition))
            {
                continue;
            }

            var sourcePort = sourceDefinition.Ports.FirstOrDefault(port => string.Equals(port.Id, connection.SourcePortId, StringComparison.Ordinal));
            if (sourcePort is null)
            {
                issues.Add(new FlowValidationIssue(
                    FlowValidationSeverity.Error,
                    FlowValidationIssueCode.UnknownSourcePort,
                    $"Connection references unknown source port '{connection.SourcePortId}' on node '{connection.SourceNodeId}'.",
                    connection.SourceNodeId,
                    connection));
                continue;
            }

            var targetPort = targetDefinition.Ports.FirstOrDefault(port => string.Equals(port.Id, connection.TargetPortId, StringComparison.Ordinal));
            if (targetPort is null)
            {
                issues.Add(new FlowValidationIssue(
                    FlowValidationSeverity.Error,
                    FlowValidationIssueCode.UnknownTargetPort,
                    $"Connection references unknown target port '{connection.TargetPortId}' on node '{connection.TargetNodeId}'.",
                    connection.TargetNodeId,
                    connection));
                continue;
            }

            if (!ConnectionValidator.CanConnect(sourcePort, targetPort) || connection.DataKind != sourcePort.DataKind)
            {
                issues.Add(new FlowValidationIssue(
                    FlowValidationSeverity.Error,
                    FlowValidationIssueCode.IncompatiblePorts,
                    $"Connection from '{connection.SourceNodeId}.{connection.SourcePortId}' to '{connection.TargetNodeId}.{connection.TargetPortId}' is not compatible.",
                    connection.TargetNodeId,
                    connection));
                continue;
            }

            validConnectionEdges.Add((connection.SourceNodeId, connection.TargetNodeId));
        }

        AddMissingRequiredInputIssues(document, definitionsByTypeId, issues);
        AddCycleIssues(validConnectionEdges, issues);

        return new FlowValidationResult(issues);
    }

    private static void AddMissingRequiredInputIssues(
        FlowDocument document,
        IReadOnlyDictionary<string, NodeDefinition> definitionsByTypeId,
        ICollection<FlowValidationIssue> issues)
    {
        foreach (var node in document.Nodes)
        {
            if (!node.IsEnabled)
            {
                continue;
            }

            if (!definitionsByTypeId.TryGetValue(node.TypeId, out var definition))
            {
                continue;
            }

            foreach (var requiredInput in definition.Ports.Where(static port => port.Direction == PortDirection.Input && port.IsRequired))
            {
                var hasConnection = document.Connections.Any(connection =>
                    string.Equals(connection.TargetNodeId, node.Id, StringComparison.Ordinal)
                    && string.Equals(connection.TargetPortId, requiredInput.Id, StringComparison.Ordinal));

                if (!hasConnection)
                {
                    issues.Add(new FlowValidationIssue(
                        FlowValidationSeverity.Error,
                        FlowValidationIssueCode.MissingRequiredInput,
                        $"Node '{node.Id}' has unconnected required input '{requiredInput.Id}'.",
                        node.Id));
                }
            }
        }
    }

    private static void AddCycleIssues(
        IReadOnlyList<(string SourceNodeId, string TargetNodeId)> edges,
        ICollection<FlowValidationIssue> issues)
    {
        var outgoing = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var edge in edges)
        {
            if (!outgoing.TryGetValue(edge.SourceNodeId, out var targets))
            {
                targets = [];
                outgoing.Add(edge.SourceNodeId, targets);
            }

            targets.Add(edge.TargetNodeId);
        }

        var visited = new HashSet<string>(StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);

        foreach (var nodeId in outgoing.Keys)
        {
            if (HasCycle(nodeId, outgoing, visited, visiting))
            {
                issues.Add(new FlowValidationIssue(
                    FlowValidationSeverity.Error,
                    FlowValidationIssueCode.CycleDetected,
                    "Flow contains a cycle."));
                return;
            }
        }
    }

    private static bool HasCycle(
        string nodeId,
        IReadOnlyDictionary<string, List<string>> outgoing,
        ISet<string> visited,
        ISet<string> visiting)
    {
        if (visited.Contains(nodeId))
        {
            return false;
        }

        if (!visiting.Add(nodeId))
        {
            return true;
        }

        if (outgoing.TryGetValue(nodeId, out var targets))
        {
            foreach (var target in targets)
            {
                if (HasCycle(target, outgoing, visited, visiting))
                {
                    return true;
                }
            }
        }

        visiting.Remove(nodeId);
        visited.Add(nodeId);
        return false;
    }
}

public static class ConnectionValidator
{
    public static bool CanConnect(PortDefinition source, PortDefinition target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        return source.Direction == PortDirection.Output
            && target.Direction == PortDirection.Input
            && source.DataKind == target.DataKind;
    }
}