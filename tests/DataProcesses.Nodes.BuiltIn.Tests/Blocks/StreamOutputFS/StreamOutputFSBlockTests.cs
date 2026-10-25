using DataProcesses.Nodes.BuiltIn.Blocks.StreamOutputFS;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.StreamOutputFS;

public sealed class StreamOutputFSBlockTests
{
    [Fact]
    public void Definition_UsesOneFastStreamTimeSeriesInput()
    {
        var port = Assert.Single(StreamOutputFSBlock.Definition.Ports);

        Assert.Equal(StreamOutputFSBlock.InputPortId, port.Id);
        Assert.Equal(PortDirection.Input, port.Direction);
        Assert.Equal(PortDataKind.FastStream, port.DataKind);
        Assert.Equal(PortDataSchema.FixedRateTimeSeries1D, port.DataSchema);

        var dashboardWidget = Assert.IsType<DashboardWidgetDefinition>(StreamOutputFSBlock.Definition.DashboardWidget);
        Assert.True(dashboardWidget.IsVisibleByDefault);
        Assert.Equal(3, dashboardWidget.GridWidth);
        Assert.Equal(3, dashboardWidget.GridHeight);
    }

    [Fact]
    public async Task OnPacketAsync_DownsamplesAndStoresRateAndLatestSnapshot()
    {
        var node = new StreamOutputFSNode();
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        var sourceSamples = Enumerable.Range(0, 1_024).Select(static value => (double)value).ToArray();
        var input = new FastStreamFrame(
            StartTimeUnixNanoseconds: 0,
            SamplePeriodNanoseconds: 22_676,
            ChannelNames: ["signal"],
            Samples: [sourceSamples.AsMemory()],
            SequenceNumber: 3,
            SampleRateHertz: 44_100);

        await node.OnPacketAsync(StreamOutputFSBlock.InputPortId, input, CancellationToken.None);

        var snapshot = Assert.IsType<StreamOutputFSSnapshot>(node.LatestSnapshot);
        Assert.Equal(44_100.0, snapshot.SampleRateHertz);
        Assert.Equal(1_024, snapshot.SourceSampleCount);
        Assert.Equal(StreamOutputFSNode.MaximumSamplesPerChannel, snapshot.Samples[0].Length);
        Assert.Equal(0.0, snapshot.Samples[0].Span[0]);
        Assert.Equal(1_023.0, snapshot.Samples[0].Span[^1]);
        Assert.Equal(3, snapshot.SequenceNumber);
    }

    [Fact]
    public async Task OnPacketAsync_RejectsFrameWithoutSampleRate()
    {
        var node = new StreamOutputFSNode();
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        var periodOnlyFrame = new FastStreamFrame(0, 1_000_000, ["signal"], [new double[] { 0.0, 1.0 }.AsMemory()], 0);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await node.OnPacketAsync(StreamOutputFSBlock.InputPortId, periodOnlyFrame, CancellationToken.None));
        Assert.Null(node.LatestSnapshot);
    }
}