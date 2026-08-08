using DataProcesses.Nodes.BuiltIn.Blocks.StreamOutput;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.StreamOutput;

public sealed class StreamOutputBlockTests
{
    [Fact]
    public void Definition_UsesOneFastStreamInput()
    {
        var port = Assert.Single(StreamOutputBlock.Definition.Ports);

        Assert.Equal(StreamOutputBlock.InputPortId, port.Id);
        Assert.Equal(PortDirection.Input, port.Direction);
        Assert.Equal(PortDataKind.FastStream, port.DataKind);
        Assert.Equal(PortDataSchema.TimeSeries1D, port.DataSchema);
    }

    [Fact]
    public async Task OnPacketAsync_DownsamplesAndStoresLatestSnapshot()
    {
        var node = new StreamOutputNode();
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        var sourceSamples = Enumerable.Range(0, 1_024).Select(static value => (double)value).ToArray();
        var input = new FastStreamFrame(
            StartTimeUnixNanoseconds: 0,
            SamplePeriodNanoseconds: 1_000_000,
            ChannelNames: ["signal"],
            Samples: [sourceSamples.AsMemory()],
            SequenceNumber: 3);

        await node.OnPacketAsync(StreamOutputBlock.InputPortId, input, CancellationToken.None);

        var snapshot = Assert.IsType<StreamOutputSnapshot>(node.LatestSnapshot);
        Assert.Equal(1_024, snapshot.SourceSampleCount);
        Assert.Equal(StreamOutputNode.MaximumSamplesPerChannel, snapshot.Samples[0].Length);
        Assert.Equal(0.0, snapshot.Samples[0].Span[0]);
        Assert.Equal(1_023.0, snapshot.Samples[0].Span[^1]);
        Assert.Equal(3, snapshot.SequenceNumber);
    }
}
