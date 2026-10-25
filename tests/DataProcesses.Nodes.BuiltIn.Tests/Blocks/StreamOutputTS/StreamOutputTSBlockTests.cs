using DataProcesses.Nodes.BuiltIn.Blocks.StreamOutputTS;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.StreamOutputTS;

public sealed class StreamOutputTSBlockTests
{
    [Fact]
    public void Definition_UsesOneFastStreamInput()
    {
        var port = Assert.Single(StreamOutputTSBlock.Definition.Ports);

        Assert.Equal(StreamOutputTSBlock.InputPortId, port.Id);
        Assert.Equal(PortDirection.Input, port.Direction);
        Assert.Equal(PortDataKind.FastStream, port.DataKind);
        Assert.Equal(PortDataSchema.TimeSeries1D, port.DataSchema);

        var dashboardWidget = Assert.IsType<DashboardWidgetDefinition>(StreamOutputTSBlock.Definition.DashboardWidget);
        Assert.True(dashboardWidget.IsVisibleByDefault);
        Assert.Equal(3, dashboardWidget.GridWidth);
        Assert.Equal(3, dashboardWidget.GridHeight);
    }

    [Fact]
    public async Task OnPacketAsync_DownsamplesAndStoresLatestSnapshot()
    {
        var node = new StreamOutputTSNode();
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        var sourceSamples = Enumerable.Range(0, 1_024).Select(static value => (double)value).ToArray();
        var input = new FastStreamFrame(
            StartTimeUnixNanoseconds: 0,
            SamplePeriodNanoseconds: 22_676,
            ChannelNames: ["signal"],
            Samples: [sourceSamples.AsMemory()],
            SequenceNumber: 3,
            SampleRateHertz: 44_100);

        await node.OnPacketAsync(StreamOutputTSBlock.InputPortId, input, CancellationToken.None);

        var snapshot = Assert.IsType<StreamOutputTSSnapshot>(node.LatestSnapshot);
        Assert.Equal(1_024, snapshot.SourceSampleCount);
        Assert.Equal(StreamOutputTSNode.MaximumSamplesPerChannel, snapshot.Samples[0].Length);
        Assert.Equal(0.0, snapshot.Samples[0].Span[0]);
        Assert.Equal(1_023.0, snapshot.Samples[0].Span[^1]);
        Assert.Equal(3, snapshot.SequenceNumber);
        Assert.Equal(44_100, snapshot.SampleRateHertz);
    }
}


