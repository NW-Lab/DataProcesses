using DataProcesses.Nodes.BuiltIn.Blocks.StremOutputTS;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.StremOutputTS;

public sealed class StremOutputTSBlockTests
{
    [Fact]
    public void Definition_UsesOneFastStreamInput()
    {
        var port = Assert.Single(StremOutputTSBlock.Definition.Ports);

        Assert.Equal(StremOutputTSBlock.InputPortId, port.Id);
        Assert.Equal(PortDirection.Input, port.Direction);
        Assert.Equal(PortDataKind.FastStream, port.DataKind);
        Assert.Equal(PortDataSchema.TimeSeries1D, port.DataSchema);
    }

    [Fact]
    public async Task OnPacketAsync_DownsamplesAndStoresLatestSnapshot()
    {
        var node = new StremOutputTSNode();
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        var sourceSamples = Enumerable.Range(0, 1_024).Select(static value => (double)value).ToArray();
        var input = new FastStreamFrame(
            StartTimeUnixNanoseconds: 0,
            SamplePeriodNanoseconds: 1_000_000,
            ChannelNames: ["signal"],
            Samples: [sourceSamples.AsMemory()],
            SequenceNumber: 3);

        await node.OnPacketAsync(StremOutputTSBlock.InputPortId, input, CancellationToken.None);

        var snapshot = Assert.IsType<StremOutputTSSnapshot>(node.LatestSnapshot);
        Assert.Equal(1_024, snapshot.SourceSampleCount);
        Assert.Equal(StremOutputTSNode.MaximumSamplesPerChannel, snapshot.Samples[0].Length);
        Assert.Equal(0.0, snapshot.Samples[0].Span[0]);
        Assert.Equal(1_023.0, snapshot.Samples[0].Span[^1]);
        Assert.Equal(3, snapshot.SequenceNumber);
    }
}


