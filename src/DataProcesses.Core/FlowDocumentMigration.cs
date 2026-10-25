namespace DataProcesses.Core;

/// <summary>
/// Upgrades persisted Flow documents to current identifiers.
/// </summary>
public static class FlowDocumentMigration
{
    /// <summary>
    /// Replaces node type IDs that appear as keys in <paramref name="typeIdAliases"/> with their mapped values.
    /// Returns the original document when no node uses a retired type ID.
    /// </summary>
    public static FlowDocument MigrateNodeTypeIds(
        FlowDocument document,
        IReadOnlyDictionary<string, string> typeIdAliases)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(typeIdAliases);

        if (typeIdAliases.Count == 0)
        {
            return document;
        }

        NodeInstance[]? migratedNodes = null;
        for (var index = 0; index < document.Nodes.Count; index++)
        {
            var node = document.Nodes[index];
            if (!typeIdAliases.TryGetValue(node.TypeId, out var currentTypeId))
            {
                continue;
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(currentTypeId, nameof(typeIdAliases));
            migratedNodes ??= [.. document.Nodes];
            migratedNodes[index] = node with { TypeId = currentTypeId };
        }

        return migratedNodes is null ? document : document with { Nodes = migratedNodes };
    }
}
