using System.Text.Json;

using DataProcesses.Nodes.BuiltIn;
using DataProcesses.Nodes.BuiltIn.Blocks.TestSignal;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.TestSignal;

public sealed class TestSignalBlockTests
{
    [Fact]
    public void BuiltInCatalog_RegistersTestSignalBlock()
    {
        var plugin = new BuiltInNodePlugin();

        var factory = Assert.Single(
            plugin.NodeFactories,
            factory => string.Equals(factory.Definition.TypeId, TestSignalBlock.TypeId, StringComparison.Ordinal));

        Assert.Equal(TestSignalBlock.TypeId, factory.Definition.TypeId);
        Assert.Equal("Test Signal", factory.Definition.DisplayName);
        Assert.Equal("TestSignal", factory.Definition.Title);
        Assert.Equal("Sin&squeare", factory.Definition.Subtitle);
        Assert.Equal(TestSignalBlock.IconPath, factory.Definition.IconPath);
        var dashboardWidget = Assert.IsType<DashboardWidgetDefinition>(factory.Definition.DashboardWidget);
        Assert.True(dashboardWidget.IsVisibleByDefault);
        Assert.Equal(2, dashboardWidget.GridWidth);
        Assert.Equal(1, dashboardWidget.GridHeight);
    }

    [Fact]
    public void TestSignalBlock_DefinesOneFastStreamOutput()
    {
        Assert.Collection(
            TestSignalBlock.Definition.Ports,
            payloadIn =>
            {
                Assert.Equal(TestSignalBlock.PayloadInputPortId, payloadIn.Id);
                Assert.Equal(PortDirection.Input, payloadIn.Direction);
                Assert.Equal(PortDataKind.JsonMessage, payloadIn.DataKind);
                Assert.False(payloadIn.IsRequired);
            },
            stream =>
            {
                Assert.Equal(TestSignalBlock.StreamOutputPortId, stream.Id);
                Assert.Equal(PortDirection.Output, stream.Direction);
                Assert.Equal(PortDataKind.FastStream, stream.DataKind);
            },
            payloadOut =>
            {
                Assert.Equal(TestSignalBlock.PayloadOutputPortId, payloadOut.Id);
                Assert.Equal(PortDirection.Output, payloadOut.Direction);
                Assert.Equal(PortDataKind.JsonMessage, payloadOut.DataKind);
                Assert.False(payloadOut.IsRequired);
            });
    }

    [Fact]
    public async Task StartAsync_EmitsEnabledStatusAndDefaultSineFrame()
    {
        var context = new RecordingNodeContext();
        var node = new TestSignalNode(TestSignalSettings.Default, () => DateTimeOffset.UnixEpoch);
        await node.InitializeAsync(context, CancellationToken.None);

        await node.StartAsync(CancellationToken.None);

        Assert.Collection(
            context.EmittedPackets,
            statusPacket =>
            {
                Assert.Equal(TestSignalBlock.PayloadOutputPortId, statusPacket.OutputPortId);
                var status = Assert.IsType<JsonMessage>(statusPacket.Packet);
                Assert.True(status.Payload.GetProperty("enabled").GetBoolean());
            },
            streamPacket =>
            {
                Assert.Equal(TestSignalBlock.StreamOutputPortId, streamPacket.OutputPortId);
                var frame = Assert.IsType<FastStreamFrame>(streamPacket.Packet);
                Assert.Equal(1_000_000, frame.SamplePeriodNanoseconds);
                Assert.Equal(0, frame.SequenceNumber);
                Assert.Equal(["signal"], frame.ChannelNames);
                var samples = Assert.Single(frame.Samples).Span;
                Assert.Equal(256, samples.Length);
                Assert.Equal(0, samples[0], precision: 12);
                Assert.Equal(Math.Sin(2 * Math.PI * 10.0 / 1_000), samples[1], precision: 12);
            });
    }

    [Fact]
    public async Task StartAsync_UsesConfiguredSquareWave()
    {
        var context = new RecordingNodeContext();
        var node = new TestSignalNode(new TestSignalSettings(TestSignalWaveType.Square, FrequencyHertz: 5.0, Amplitude: 2.0, SamplePeriodMilliseconds: 2.0), () => DateTimeOffset.UnixEpoch);
        await node.InitializeAsync(context, CancellationToken.None);

        await node.StartAsync(CancellationToken.None);

        var streamPacket = Assert.Single(context.EmittedPackets, packet => packet.OutputPortId == TestSignalBlock.StreamOutputPortId);
        var frame = Assert.IsType<FastStreamFrame>(streamPacket.Packet);
        Assert.Equal(2_000_000, frame.SamplePeriodNanoseconds);
        var samples = Assert.Single(frame.Samples).Span;
        Assert.Equal(2.0, samples[0]);
        Assert.Equal(2.0, samples[1]);
    }

    [Fact]
    public async Task StartAsync_EmitsStatusOnlyWhenSignalIsDisabled()
    {
        var context = new RecordingNodeContext();
        var node = new TestSignalNode(TestSignalSettings.Default with { IsEnabled = false });
        await node.InitializeAsync(context, CancellationToken.None);

        await node.StartAsync(CancellationToken.None);

        var emitted = Assert.Single(context.EmittedPackets);
        Assert.Equal(TestSignalBlock.PayloadOutputPortId, emitted.OutputPortId);
        var status = Assert.IsType<JsonMessage>(emitted.Packet);
        Assert.False(status.Payload.GetProperty("enabled").GetBoolean());
    }

    [Fact]
    public async Task OnPacketAsync_CanDisablePayloadThroughFromSettings()
    {
        var context = new RecordingNodeContext();
        var node = new TestSignalNode(TestSignalSettings.Default with { PayloadThrough = false });
        await node.InitializeAsync(context, CancellationToken.None);
        var payload = JsonSerializer.SerializeToElement(new
        {
            frequency = 5.0,
        });
        var message = new JsonMessage("dataprocesses.test-signal.configure", payload, DateTimeOffset.UtcNow);

        await node.OnPacketAsync(TestSignalBlock.PayloadInputPortId, message, CancellationToken.None);

        Assert.Empty(context.EmittedPackets);
    }

    [Fact]
    public async Task OnPacketAsync_UpdatesSettingsAndEmitsStatusWhenEnabledChanges()
    {
        var context = new RecordingNodeContext();
        var node = new TestSignalNode(TestSignalSettings.Default);
        await node.InitializeAsync(context, CancellationToken.None);
        var payload = JsonSerializer.SerializeToElement(new
        {
            isEnabled = false,
            waveType = "square",
            frequency = 5.0,
            samplePeriodMillis = 2.0,
            amplitude = 2.0,
        });
        var message = new JsonMessage("dataprocesses.test-signal.configure", payload, DateTimeOffset.UtcNow);

        await node.OnPacketAsync(TestSignalBlock.PayloadInputPortId, message, CancellationToken.None);

        Assert.Collection(
            context.EmittedPackets,
            throughPacket =>
            {
                Assert.Equal(TestSignalBlock.PayloadOutputPortId, throughPacket.OutputPortId);
                Assert.Same(message, throughPacket.Packet);
            },
            statusPacket =>
            {
                Assert.Equal(TestSignalBlock.PayloadOutputPortId, statusPacket.OutputPortId);
                var status = Assert.IsType<JsonMessage>(statusPacket.Packet);
                Assert.False(status.Payload.GetProperty("enabled").GetBoolean());
            });
    }

    [Fact]
    public void Factory_CreatesAnIndependentNodeInstance()
    {
        var factory = new TestSignalNodeFactory();

        var first = factory.CreateNode("test-signal-1");
        var second = factory.CreateNode("test-signal-2");

        Assert.IsType<TestSignalNode>(first);
        Assert.IsType<TestSignalNode>(second);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void Factory_CreatesConfiguredNodeFromSettingsJson()
    {
        var factory = new TestSignalNodeFactory();

        var node = factory.CreateNode(
            "test-signal-1",
            "{\"waveType\":\"square\",\"frequency\":5.0,\"samplePeriodMillis\":2.0,\"amplitude\":2.0}");

        Assert.IsType<TestSignalNode>(node);
    }
}