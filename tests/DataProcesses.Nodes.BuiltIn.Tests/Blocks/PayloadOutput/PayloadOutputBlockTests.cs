using System.Text.Json;
using DataProcesses.Nodes.BuiltIn.Blocks.PayloadOutput;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.PayloadOutput;

public sealed class PayloadOutputBlockTests
{
    [Fact]
    public void Definition_UsesOnePayloadInputAndDefaultDashboardSize()
    {
        var port = Assert.Single(PayloadOutputBlock.Definition.Ports);

        Assert.Equal(PayloadOutputBlock.InputPortId, port.Id);
        Assert.Equal(PortDirection.Input, port.Direction);
        Assert.Equal(PortDataKind.JsonMessage, port.DataKind);

        var dashboard = Assert.IsType<DashboardWidgetDefinition>(PayloadOutputBlock.Definition.DashboardWidget);
        Assert.True(dashboard.IsVisibleByDefault);
        Assert.Equal(3, dashboard.GridWidth);
        Assert.Equal(3, dashboard.GridHeight);
    }

    [Fact]
    public async Task OnPacketAsync_StoresTimestampedLogEntry()
    {
        var node = new PayloadOutputNode();
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        var payload = JsonSerializer.SerializeToElement(new { level = "info", value = 42 });
        var input = new JsonMessage("debug.event", payload, DateTimeOffset.UtcNow, "corr-1");

        await node.OnPacketAsync(PayloadOutputBlock.InputPortId, input, CancellationToken.None);

        var entry = Assert.IsType<string>(node.LatestLogEntry);
        Assert.Contains("topic=debug.event", entry, StringComparison.Ordinal);
        Assert.Contains("correlationId=corr-1", entry, StringComparison.Ordinal);
        Assert.Contains("\"value\":42", entry, StringComparison.Ordinal);
    }
}
