using DataProcesses.Nodes.BuiltIn.Blocks.TestSignalVec;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.TestSignalVec;

public sealed class TestSignalVecBlockTests
{
    [Fact]
    public void BuiltInCatalog_RegistersTestSignalVecBlock()
    {
        var plugin = new BuiltInNodePlugin();

        var factory = Assert.Single(
            plugin.NodeFactories,
            factory => string.Equals(factory.Definition.TypeId, TestSignalVecBlock.TypeId, StringComparison.Ordinal));

        Assert.Equal(TestSignalVecBlock.TypeId, factory.Definition.TypeId);
        Assert.Equal("TestSignal(Vec)ブロック", factory.Definition.DisplayName);
        Assert.Equal("TestSignal(Vec)ブロック", factory.Definition.Title);
        Assert.Equal("Vec", factory.Definition.Subtitle);
        Assert.Equal(TestSignalVecBlock.IconPath, factory.Definition.IconPath);
    }

    [Fact]
    public void TestSignalVecBlock_DefinesExpectedPorts()
    {
        Assert.Collection(
            TestSignalVecBlock.Definition.Ports,
            payloadIn =>
            {
                Assert.Equal(TestSignalVecBlock.PayloadInputPortId, payloadIn.Id);
                Assert.Equal(PortDirection.Input, payloadIn.Direction);
                Assert.Equal(PortDataKind.JsonMessage, payloadIn.DataKind);
            },
            stream =>
            {
                Assert.Equal(TestSignalVecBlock.StreamOutputPortId, stream.Id);
                Assert.Equal(PortDirection.Output, stream.Direction);
                Assert.Equal(PortDataKind.FastStream, stream.DataKind);
                Assert.Equal(PortDataSchema.NumericVector1D, stream.DataSchema);
            },
            payloadOut =>
            {
                Assert.Equal(TestSignalVecBlock.PayloadOutputPortId, payloadOut.Id);
                Assert.Equal(PortDirection.Output, payloadOut.Direction);
                Assert.Equal(PortDataKind.JsonMessage, payloadOut.DataKind);
            });
    }

    [Fact]
    public async Task StartAsync_EmitsVectorFrame()
    {
        var context = new RecordingNodeContext();
        var node = new TestSignalVecNode(TestSignalVecSettings.Default, () => DateTimeOffset.UnixEpoch);
        await node.InitializeAsync(context, CancellationToken.None);

        await node.StartAsync(CancellationToken.None);

        var emitted = Assert.Single(context.EmittedPackets, packet => packet.OutputPortId == TestSignalVecBlock.StreamOutputPortId);
        var frame = Assert.IsType<NumericVectorFrame>(emitted.Packet);
        Assert.Equal("signal", frame.Name);
        Assert.Equal(TestSignalVecSettings.DefaultLength, frame.Length);
        Assert.Equal(16, frame.Values.Length);
        var values = frame.Values.ToArray();
        Assert.Equal(1.0, values[0]);
        Assert.Equal(1, values.Count(static sample => sample == 1.0));
        Assert.Equal(15, values.Count(static sample => sample == 0.0));
    }

    [Fact]
    public void DefaultSettings_UseOneShotWaveType()
    {
        Assert.Equal(TestSignalVecWaveType.OneShot, TestSignalVecSettings.Default.WaveType);
    }

    [Fact]
    public async Task StartAsync_OneHotIndexWrapsFrom15To0()
    {
        var settings = TestSignalVecSettings.Default with
        {
            FrequencyHertz = 10.0,
            Length = 16,
            ExecutionStep = 15,
        };

        var contextAtIndex15 = new RecordingNodeContext();
        var nodeAtIndex15 = new TestSignalVecNode(
            settings,
            () => DateTimeOffset.FromUnixTimeMilliseconds(0));
        await nodeAtIndex15.InitializeAsync(contextAtIndex15, CancellationToken.None);
        await nodeAtIndex15.StartAsync(CancellationToken.None);

        var frameAtIndex15 = Assert.IsType<NumericVectorFrame>(
            Assert.Single(contextAtIndex15.EmittedPackets, packet => packet.OutputPortId == TestSignalVecBlock.StreamOutputPortId).Packet);
        var valuesAtIndex15 = frameAtIndex15.Values.ToArray();
        Assert.Equal(1.0, valuesAtIndex15[15]);
        Assert.Equal(1, valuesAtIndex15.Count(static sample => sample == 1.0));

        var contextAtIndex0 = new RecordingNodeContext();
        var nodeAtIndex0 = new TestSignalVecNode(
            settings with { ExecutionStep = 16 },
            () => DateTimeOffset.FromUnixTimeMilliseconds(0));
        await nodeAtIndex0.InitializeAsync(contextAtIndex0, CancellationToken.None);
        await nodeAtIndex0.StartAsync(CancellationToken.None);

        var frameAtIndex0 = Assert.IsType<NumericVectorFrame>(
            Assert.Single(contextAtIndex0.EmittedPackets, packet => packet.OutputPortId == TestSignalVecBlock.StreamOutputPortId).Packet);
        var valuesAtIndex0 = frameAtIndex0.Values.ToArray();
        Assert.Equal(1.0, valuesAtIndex0[0]);
        Assert.Equal(1, valuesAtIndex0.Count(static sample => sample == 1.0));
    }

    [Fact]
    public async Task StartAsync_SineWave_UsesFrequencyBasedCycleStepsAndVectorPhaseOffsets()
    {
        var settings = TestSignalVecSettings.Default with
        {
            WaveType = TestSignalVecWaveType.Sine,
            FrequencyHertz = 10.0,
            Length = 4,
            Amplitude = 1.0,
            ExecutionStep = 1,
        };

        var context = new RecordingNodeContext();
        var node = new TestSignalVecNode(
            settings,
            () => DateTimeOffset.FromUnixTimeMilliseconds(100));
        await node.InitializeAsync(context, CancellationToken.None);
        await node.StartAsync(CancellationToken.None);

        var frame = Assert.IsType<NumericVectorFrame>(
            Assert.Single(context.EmittedPackets, packet => packet.OutputPortId == TestSignalVecBlock.StreamOutputPortId).Packet);
        var values = frame.Values.ToArray();

        Assert.Equal(4, values.Length);
        Assert.InRange(values[0], 0.12, 0.13);
        Assert.InRange(values[1], 0.99, 1.0);
        Assert.InRange(values[2], -0.13, -0.12);
        Assert.InRange(values[3], -1.0, -0.99);
    }

    [Fact]
    public void Factory_CreatesConfiguredNodeFromSettingsJson()
    {
        var factory = new TestSignalVecNodeFactory();
        var node = factory.CreateNode("test-signal-vec-1", "{\"length\":64}");

        Assert.IsType<TestSignalVecNode>(node);
    }
}
