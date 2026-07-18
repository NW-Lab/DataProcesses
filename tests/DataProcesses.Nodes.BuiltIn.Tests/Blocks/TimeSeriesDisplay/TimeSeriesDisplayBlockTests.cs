using DataProcesses.Nodes.BuiltIn.Blocks.TimeSeriesDisplay;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.TimeSeriesDisplay;

public sealed class TimeSeriesDisplayBlockTests
{
    [Fact]
    public void Definition_UsesOneFastStreamInput()
    {
        var port = Assert.Single(TimeSeriesDisplayBlock.Definition.Ports);

        Assert.Equal(TimeSeriesDisplayBlock.InputPortId, port.Id);
        Assert.Equal(PortDirection.Input, port.Direction);
        Assert.Equal(PortDataKind.FastStream, port.DataKind);
    }

    [Fact]
    public async Task OnPacketAsync_DownsamplesAndStoresLatestSnapshot()
    {
        var node = new TimeSeriesDisplayNode();
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        var sourceSamples = Enumerable.Range(0, 1_024).Select(static value => (double)value).ToArray();
        var input = new FastStreamFrame(
            StartTimeUnixNanoseconds: 0,
            SamplePeriodNanoseconds: 1_000_000,
            ChannelNames: ["signal"],
            Samples: [sourceSamples.AsMemory()],
            SequenceNumber: 3);

        await node.OnPacketAsync(TimeSeriesDisplayBlock.InputPortId, input, CancellationToken.None);

        var snapshot = Assert.IsType<TimeSeriesSnapshot>(node.LatestSnapshot);
        Assert.Equal(1_024, snapshot.SourceSampleCount);
        Assert.Equal(TimeSeriesDisplayNode.MaximumSamplesPerChannel, snapshot.Samples[0].Length);
        Assert.Equal(0.0, snapshot.Samples[0].Span[0]);
        Assert.Equal(1_023.0, snapshot.Samples[0].Span[^1]);
        Assert.Equal(3, snapshot.SequenceNumber);
    }
}
