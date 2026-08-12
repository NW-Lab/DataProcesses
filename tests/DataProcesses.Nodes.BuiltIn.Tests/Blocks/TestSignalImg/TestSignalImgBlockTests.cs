using DataProcesses.Nodes.BuiltIn.Blocks.TestSignalImg;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.TestSignalImg;

public sealed class TestSignalImgBlockTests
{
    [Fact]
    public void BuiltInCatalog_RegistersTestSignalImgBlock()
    {
        var plugin = new BuiltInNodePlugin();

        var factory = Assert.Single(
            plugin.NodeFactories,
            factory => string.Equals(factory.Definition.TypeId, TestSignalImgBlock.TypeId, StringComparison.Ordinal));

        Assert.Equal(TestSignalImgBlock.TypeId, factory.Definition.TypeId);
        Assert.Equal("TestSignal(Img)ブロック", factory.Definition.DisplayName);
        Assert.Equal("TestSignal(Img)ブロック", factory.Definition.Title);
        Assert.Equal("Img", factory.Definition.Subtitle);
    }

    [Fact]
    public void TestSignalImgBlock_DefinesExpectedPorts()
    {
        Assert.Collection(
            TestSignalImgBlock.Definition.Ports,
            payloadIn =>
            {
                Assert.Equal(TestSignalImgBlock.PayloadInputPortId, payloadIn.Id);
                Assert.Equal(PortDirection.Input, payloadIn.Direction);
                Assert.Equal(PortDataKind.JsonMessage, payloadIn.DataKind);
            },
            stream =>
            {
                Assert.Equal(TestSignalImgBlock.StreamOutputPortId, stream.Id);
                Assert.Equal(PortDirection.Output, stream.Direction);
                Assert.Equal(PortDataKind.FastStream, stream.DataKind);
                Assert.Equal(PortDataSchema.Image2D, stream.DataSchema);
            },
            payloadOut =>
            {
                Assert.Equal(TestSignalImgBlock.PayloadOutputPortId, payloadOut.Id);
                Assert.Equal(PortDirection.Output, payloadOut.Direction);
                Assert.Equal(PortDataKind.JsonMessage, payloadOut.DataKind);
            });
    }

    [Fact]
    public async Task StartAsync_EmitsImageFrame()
    {
        var context = new RecordingNodeContext();
        var node = new TestSignalImgNode(TestSignalImgSettings.Default, () => DateTimeOffset.UnixEpoch);
        await node.InitializeAsync(context, CancellationToken.None);

        await node.StartAsync(CancellationToken.None);

        var emitted = Assert.Single(context.EmittedPackets, packet => packet.OutputPortId == TestSignalImgBlock.StreamOutputPortId);
        var frame = Assert.IsType<ImageFrame>(emitted.Packet);
        Assert.Equal(100, frame.Width);
        Assert.Equal(100, frame.Height);
        Assert.Equal(ImagePixelFormat.Gray8, frame.PixelFormat);
        Assert.Equal(10_000, frame.PixelsInterleaved.Length);
    }

    [Fact]
    public void DefaultSettings_UseMonoAndOneHertz()
    {
        Assert.Equal(TestSignalImgType.Number, TestSignalImgSettings.Default.Type);
        Assert.Equal(TestSignalImgKind.Mono, TestSignalImgSettings.Default.Kind);
        Assert.Equal(1.0, TestSignalImgSettings.Default.FrequencyHertz);
        Assert.Equal(100, TestSignalImgSettings.Default.Width);
        Assert.Equal(100, TestSignalImgSettings.Default.Height);
    }

    [Fact]
    public async Task StartAsync_ColorMode_EmitsRgb24Frame()
    {
        var context = new RecordingNodeContext();
        var node = new TestSignalImgNode(
            TestSignalImgSettings.Default with { Kind = TestSignalImgKind.Color, Width = 10, Height = 8 },
            () => DateTimeOffset.UnixEpoch);
        await node.InitializeAsync(context, CancellationToken.None);

        await node.StartAsync(CancellationToken.None);

        var emitted = Assert.Single(context.EmittedPackets, packet => packet.OutputPortId == TestSignalImgBlock.StreamOutputPortId);
        var frame = Assert.IsType<ImageFrame>(emitted.Packet);
        Assert.Equal(ImagePixelFormat.Rgb24, frame.PixelFormat);
        Assert.Equal(10 * 8 * 3, frame.PixelsInterleaved.Length);
    }

    [Fact]
    public void Factory_CreatesConfiguredNodeFromSettingsJson()
    {
        var factory = new TestSignalImgNodeFactory();
        var node = factory.CreateNode("test-signal-img-1", "{\"width\":80,\"height\":60}");

        Assert.IsType<TestSignalImgNode>(node);
    }

    [Fact]
    public async Task StartAsync_NumberType_ChangesFrameByTimestamp()
    {
        var contextA = new RecordingNodeContext();
        var contextB = new RecordingNodeContext();

        var settings = TestSignalImgSettings.Default with
        {
            Type = TestSignalImgType.Number,
            Width = 64,
            Height = 64,
            FrequencyHertz = 1.0,
            Kind = TestSignalImgKind.Mono,
        };

        var nodeA = new TestSignalImgNode(settings, () => DateTimeOffset.UnixEpoch + TimeSpan.FromMilliseconds(50));
        var nodeB = new TestSignalImgNode(settings, () => DateTimeOffset.UnixEpoch + TimeSpan.FromMilliseconds(150));

        await nodeA.InitializeAsync(contextA, CancellationToken.None);
        await nodeB.InitializeAsync(contextB, CancellationToken.None);

        await nodeA.StartAsync(CancellationToken.None);
        await nodeB.StartAsync(CancellationToken.None);

        var frameA = Assert.IsType<ImageFrame>(Assert.Single(contextA.EmittedPackets, packet => packet.OutputPortId == TestSignalImgBlock.StreamOutputPortId).Packet);
        var frameB = Assert.IsType<ImageFrame>(Assert.Single(contextB.EmittedPackets, packet => packet.OutputPortId == TestSignalImgBlock.StreamOutputPortId).Packet);

        Assert.False(frameA.PixelsInterleaved.Span.SequenceEqual(frameB.PixelsInterleaved.Span));
    }

    [Fact]
    public async Task StartAsync_CircleType_ChangesFrameByTimestamp()
    {
        var contextA = new RecordingNodeContext();
        var contextB = new RecordingNodeContext();

        var settings = TestSignalImgSettings.Default with
        {
            Type = TestSignalImgType.Circle,
            Width = 64,
            Height = 64,
            FrequencyHertz = 1.0,
            Kind = TestSignalImgKind.Mono,
        };

        var nodeA = new TestSignalImgNode(settings, () => DateTimeOffset.UnixEpoch + TimeSpan.FromMilliseconds(10));
        var nodeB = new TestSignalImgNode(settings, () => DateTimeOffset.UnixEpoch + TimeSpan.FromMilliseconds(60));

        await nodeA.InitializeAsync(contextA, CancellationToken.None);
        await nodeB.InitializeAsync(contextB, CancellationToken.None);

        await nodeA.StartAsync(CancellationToken.None);
        await nodeB.StartAsync(CancellationToken.None);

        var frameA = Assert.IsType<ImageFrame>(Assert.Single(contextA.EmittedPackets, packet => packet.OutputPortId == TestSignalImgBlock.StreamOutputPortId).Packet);
        var frameB = Assert.IsType<ImageFrame>(Assert.Single(contextB.EmittedPackets, packet => packet.OutputPortId == TestSignalImgBlock.StreamOutputPortId).Packet);

        Assert.False(frameA.PixelsInterleaved.Span.SequenceEqual(frameB.PixelsInterleaved.Span));
    }
}
