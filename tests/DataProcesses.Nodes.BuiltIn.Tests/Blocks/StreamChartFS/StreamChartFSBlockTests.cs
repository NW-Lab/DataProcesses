using DataProcesses.Nodes.BuiltIn.Blocks.StreamChartFS;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.StreamChartFS;

public sealed class StreamChartFSBlockTests
{
    [Fact]
    public void Definition_DeclaresOneOptionalFastStreamTimeSeriesInput()
    {
        var port = Assert.Single(StreamChartFSBlock.Definition.Ports);

        Assert.Equal(StreamChartFSBlock.InputPortId, port.Id);
        Assert.Equal(PortDirection.Input, port.Direction);
        Assert.Equal(PortDataKind.FastStream, port.DataKind);
        Assert.Equal(PortDataSchema.FixedRateTimeSeries1D, port.DataSchema);
        Assert.False(port.IsRequired);
        Assert.Equal(NodeType.Output, StreamChartFSBlock.Definition.NodeType);
    }

    [Fact]
    public async Task OnPacketAsync_KeepsBoundedSamplesAndExactSampleRate()
    {
        var node = new StreamChartFSNode();
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        var sourceSamples = Enumerable.Range(0, 1_024).Select(static index => (double)index).ToArray();
        var frame = new FastStreamFrame(
            StartTimeUnixNanoseconds: 2_000_000_000,
            SamplePeriodNanoseconds: 22_676,
            ChannelNames: ["audio"],
            Samples: [sourceSamples.AsMemory()],
            SequenceNumber: 12,
            SampleRateHertz: 44_100);

        await node.OnPacketAsync(StreamChartFSBlock.InputPortId, frame, CancellationToken.None);

        var snapshot = Assert.IsType<StreamChartFSSnapshot>(node.LatestSnapshot);
        Assert.Equal(2_000_000_000, snapshot.StartTimeUnixNanoseconds);
        Assert.Equal(44_100, snapshot.SampleRateHertz);
        Assert.Equal(22_676, snapshot.SamplePeriodNanoseconds);
        Assert.Equal(1_024, snapshot.SourceSampleCount);
        Assert.Equal(512, snapshot.Samples[0].Length);
        Assert.Equal(0.0, snapshot.Samples[0].Span[0]);
        Assert.Equal(1_023.0, snapshot.Samples[0].Span[^1]);
        Assert.Equal(12, snapshot.SequenceNumber);
    }

    [Fact]
    public async Task OnPacketAsync_RetainsMultipleChannels()
    {
        var node = new StreamChartFSNode();
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        var frame = CreateFrame(
            ["left", "right"],
            [new double[] { 1.0, 2.0, 3.0 }.AsMemory(), new double[] { -1.0, -2.0, -3.0 }.AsMemory()]);

        await node.OnPacketAsync(StreamChartFSBlock.InputPortId, frame, CancellationToken.None);

        var snapshot = Assert.IsType<StreamChartFSSnapshot>(node.LatestSnapshot);
        Assert.Equal(["left", "right"], snapshot.ChannelNames);
        Assert.Equal(2, snapshot.Samples.Count);
        Assert.Equal(new[] { -1.0, -2.0, -3.0 }, snapshot.Samples[1].ToArray());
    }

    [Fact]
    public async Task OnPacketAsync_RejectsMoreThanFourChannels()
    {
        var node = new StreamChartFSNode();
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        var names = Enumerable.Range(1, 5).Select(static index => $"CH{index}").ToArray();
        var samples = Enumerable.Range(0, 5)
            .Select(static _ => (ReadOnlyMemory<double>)new double[] { 0.0, 1.0 })
            .ToArray();
        var frame = CreateFrame(names, samples);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await node.OnPacketAsync(StreamChartFSBlock.InputPortId, frame, CancellationToken.None));
    }

    [Fact]
    public async Task OnPacketAsync_RejectsJsonMessage()
    {
        var node = new StreamChartFSNode();
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        using var document = System.Text.Json.JsonDocument.Parse("{}");
        var message = new JsonMessage("test", document.RootElement, DateTimeOffset.UnixEpoch);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await node.OnPacketAsync(StreamChartFSBlock.InputPortId, message, CancellationToken.None));
    }

    [Fact]
    public async Task OnPacketAsync_RejectsFrameWithoutSampleRate()
    {
        var node = new StreamChartFSNode();
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        var periodOnlyFrame = new FastStreamFrame(0, 1_000_000, ["signal"], [new double[] { 0.0, 1.0 }.AsMemory()], 0);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await node.OnPacketAsync(StreamChartFSBlock.InputPortId, periodOnlyFrame, CancellationToken.None));
        Assert.Null(node.LatestSnapshot);
    }

    [Fact]
    public void Factory_CreatesIndependentInstances()
    {
        var factory = new StreamChartFSNodeFactory();

        Assert.NotSame(factory.CreateNode("first"), factory.CreateNode("second"));
    }

    private static FastStreamFrame CreateFrame(
        IReadOnlyList<string> channelNames,
        IReadOnlyList<ReadOnlyMemory<double>> samples)
    {
        return new FastStreamFrame(
            StartTimeUnixNanoseconds: 0,
            SamplePeriodNanoseconds: 1_000_000,
            ChannelNames: channelNames,
            Samples: samples,
            SequenceNumber: 0,
            SampleRateHertz: 1_000);
    }
}