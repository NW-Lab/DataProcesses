using DataProcesses.Nodes.BuiltIn;
using DataProcesses.Nodes.BuiltIn.Blocks.Trigger;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.Trigger;

public sealed class TriggerBlockTests
{
    [Fact]
    public void BuiltInCatalog_RegistersTriggerBlock()
    {
        var plugin = new BuiltInNodePlugin();

        var factory = Assert.Single(
            plugin.NodeFactories,
            candidate => string.Equals(candidate.Definition.TypeId, TriggerBlock.TypeId, StringComparison.Ordinal));

        Assert.Equal("Trigger", factory.Definition.DisplayName);
        Assert.Equal("Trigger", factory.Definition.Title);
        Assert.Equal("Manual/start/periodic", factory.Definition.Subtitle);
        var dashboardWidget = Assert.IsType<DashboardWidgetDefinition>(factory.Definition.DashboardWidget);
        Assert.True(dashboardWidget.IsVisibleByDefault);
        Assert.Equal(1, dashboardWidget.GridWidth);
        Assert.Equal(1, dashboardWidget.GridHeight);
    }

    [Fact]
    public void TriggerBlock_DefinesPayloadOutputOnly()
    {
        var output = Assert.Single(TriggerBlock.Definition.Ports);
        Assert.Equal(TriggerBlock.PayloadOutputPortId, output.Id);
        Assert.Equal(PortDirection.Output, output.Direction);
        Assert.Equal(PortDataKind.JsonMessage, output.DataKind);
    }

    [Fact]
    public async Task StartAsync_EmitsPayloadWhenExecutionStarts()
    {
        var context = new RecordingNodeContext
        {
            NodeId = $"trigger-node-{Guid.NewGuid():N}",
        };
        var settings = TriggerSettings.Default with
        {
            Topic = "app.trigger",
            PayloadPath = "payload.meta.startedAt",
            PayloadValueType = TriggerPayloadValueType.String,
            StringValue = "started",
            EmitPeriodically = false,
            ManualTriggerNonce = 0,
            ExecutionSessionId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };
        var node = new TriggerNode(context.NodeId, settings, () => DateTimeOffset.UnixEpoch);
        await node.InitializeAsync(context, CancellationToken.None);

        await node.StartAsync(CancellationToken.None);

        var emitted = Assert.Single(context.EmittedPackets);
        var message = Assert.IsType<JsonMessage>(emitted.Packet);
        Assert.Equal("app.trigger", message.Topic);
        Assert.Equal("started", message.Payload.GetProperty("meta").GetProperty("startedAt").GetString());
    }

    [Fact]
    public async Task StartAsync_EmitsPeriodicPayloadFromInitialAndRepeatInterval()
    {
        var nodeId = $"trigger-node-{Guid.NewGuid():N}";
        var sessionId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var settings = TriggerSettings.Default with
        {
            EmitOnExecutionStart = false,
            EmitPeriodically = true,
            InitialDelayMilliseconds = 100,
            RepeatIntervalMilliseconds = 100,
            PayloadPath = "payload.value",
            PayloadValueType = TriggerPayloadValueType.Number,
            NumberValue = 42,
            ExecutionSessionId = sessionId,
        };

        var firstContext = new RecordingNodeContext { NodeId = nodeId };
        var firstNode = new TriggerNode(nodeId, settings, () => DateTimeOffset.UnixEpoch.AddMilliseconds(250));
        await firstNode.InitializeAsync(firstContext, CancellationToken.None);

        await firstNode.StartAsync(CancellationToken.None);

        Assert.Empty(firstContext.EmittedPackets);

        var secondContext = new RecordingNodeContext { NodeId = nodeId };
        var secondNode = new TriggerNode(nodeId, settings, () => DateTimeOffset.UnixEpoch.AddMilliseconds(450));
        await secondNode.InitializeAsync(secondContext, CancellationToken.None);

        await secondNode.StartAsync(CancellationToken.None);

        Assert.Equal(2, secondContext.EmittedPackets.Count);
    }

    [Fact]
    public async Task StartAsync_EmitsManualTriggerOnlyWhenNonceIncreases()
    {
        var nodeId = $"trigger-node-{Guid.NewGuid():N}";
        var sessionId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var baseSettings = TriggerSettings.Default with
        {
            EmitOnExecutionStart = false,
            EmitPeriodically = false,
            PayloadValueType = TriggerPayloadValueType.Boolean,
            BoolValue = true,
            ExecutionSessionId = sessionId,
            ManualTriggerNonce = 1,
        };

        var firstContext = new RecordingNodeContext { NodeId = nodeId };
        var firstNode = new TriggerNode(nodeId, baseSettings);
        await firstNode.InitializeAsync(firstContext, CancellationToken.None);

        await firstNode.StartAsync(CancellationToken.None);

        Assert.Single(firstContext.EmittedPackets);

        var secondContext = new RecordingNodeContext { NodeId = nodeId };
        var secondNode = new TriggerNode(nodeId, baseSettings);
        await secondNode.InitializeAsync(secondContext, CancellationToken.None);

        await secondNode.StartAsync(CancellationToken.None);

        Assert.Empty(secondContext.EmittedPackets);

        var thirdContext = new RecordingNodeContext { NodeId = nodeId };
        var thirdNode = new TriggerNode(nodeId, baseSettings with { ManualTriggerNonce = 2 });
        await thirdNode.InitializeAsync(thirdContext, CancellationToken.None);

        await thirdNode.StartAsync(CancellationToken.None);

        Assert.Single(thirdContext.EmittedPackets);
    }

    [Fact]
    public void Factory_CreatesConfiguredNodeFromSettingsJson()
    {
        var factory = new TriggerNodeFactory();

        var node = factory.CreateNode(
            "trigger-1",
            "{\"topic\":\"app.trigger\",\"payloadPath\":\"payload.meta.value\",\"payloadValueType\":\"number\",\"numberValue\":2.5}");

        Assert.IsType<TriggerNode>(node);
    }
}
