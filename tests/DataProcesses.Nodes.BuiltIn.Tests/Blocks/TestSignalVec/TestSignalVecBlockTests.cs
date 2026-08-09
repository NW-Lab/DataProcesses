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
        Assert.Equal(128, frame.Values.Length);
    }

    [Fact]
    public void Factory_CreatesConfiguredNodeFromSettingsJson()
    {
        var factory = new TestSignalVecNodeFactory();
        var node = factory.CreateNode("test-signal-vec-1", "{\"length\":64}");

        Assert.IsType<TestSignalVecNode>(node);
    }
}
