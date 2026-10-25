using DataProcesses.Core;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Core.Tests;

public sealed class FlowDocumentMigrationTests
{
    private static readonly IReadOnlyDictionary<string, string> Aliases =
        new Dictionary<string, string>(StringComparer.Ordinal) { ["old-type"] = "new-type" };

    [Fact]
    public void MigrateNodeTypeIds_ReplacesAliasedTypeIdsAndPreservesEverythingElse()
    {
        var connection = new Connection("a", "out", "b", "in", PortDataKind.FastStream);
        var document = new FlowDocument(
            Guid.NewGuid(),
            "Flow",
            [
                new NodeInstance("a", "old-type", 1, 2, "{\"x\":1}", Name: "Renamed", IsEnabled: false),
                new NodeInstance("b", "other-type", 3, 4, "{}"),
            ],
            [connection]);

        var migrated = FlowDocumentMigration.MigrateNodeTypeIds(document, Aliases);

        Assert.Equal(document.Nodes[0] with { TypeId = "new-type" }, migrated.Nodes[0]);
        Assert.Same(document.Nodes[1], migrated.Nodes[1]);
        Assert.Equal(document.Connections, migrated.Connections);
        Assert.Equal("old-type", document.Nodes[0].TypeId);
    }

    [Fact]
    public void MigrateNodeTypeIds_ReturnsSameDocumentWhenNothingMatches()
    {
        var document = new FlowDocument(Guid.NewGuid(), "Flow", [new NodeInstance("a", "new-type", 0, 0, "{}")], []);

        Assert.Same(document, FlowDocumentMigration.MigrateNodeTypeIds(document, Aliases));
    }

    [Fact]
    public void MigrateNodeTypeIds_ValidatesArguments()
    {
        var document = new FlowDocument(Guid.NewGuid(), "Flow", [], []);

        Assert.Throws<ArgumentNullException>(() => FlowDocumentMigration.MigrateNodeTypeIds(null!, Aliases));
        Assert.Throws<ArgumentNullException>(() => FlowDocumentMigration.MigrateNodeTypeIds(document, null!));
    }
}
