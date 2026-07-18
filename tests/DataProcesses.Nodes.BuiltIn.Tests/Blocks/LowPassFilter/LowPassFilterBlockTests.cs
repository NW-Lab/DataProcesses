using DataProcesses.Nodes.BuiltIn.Blocks.LowPassFilter;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.LowPassFilter;

public sealed class LowPassFilterBlockTests
{
    [Fact]
    public void Definition_UsesFastStreamInputAndOutput()
    {
        var ports = LowPassFilterBlock.Definition.Ports;

        Assert.Collection(
            ports,
            input =>
            {
                Assert.Equal(LowPassFilterBlock.InputPortId, input.Id);
                Assert.Equal(PortDirection.Input, input.Direction);
                Assert.Equal(PortDataKind.FastStream, input.DataKind);
            },
            output =>
            {
                Assert.Equal(LowPassFilterBlock.OutputPortId, output.Id);
                Assert.Equal(PortDirection.Output, output.Direction);
                Assert.Equal(PortDataKind.FastStream, output.DataKind);
            });
    }

    [Fact]
    public async Task OnPacketAsync_AppliesDeterministicLowPassSmoothing()
    {
        var context = new RecordingNodeContext();
        var node = new LowPassFilterNode();
        await node.InitializeAsync(context, CancellationToken.None);
        var input = new FastStreamFrame(
            StartTimeUnixNanoseconds: 0,
            SamplePeriodNanoseconds: 1_000_000,
            ChannelNames: ["signal"],
            Samples: [new double[] { 0.0, 1.0, 1.0 }.AsMemory()],
            SequenceNumber: 0);

        await node.OnPacketAsync(LowPassFilterBlock.InputPortId, input, CancellationToken.None);

        var emitted = Assert.Single(context.EmittedPackets);
        Assert.Equal(LowPassFilterBlock.OutputPortId, emitted.OutputPortId);
        var filtered = Assert.IsType<FastStreamFrame>(emitted.Packet);
        Assert.Equal(new[] { 0.0, 0.25, 0.4375 }, filtered.Samples[0].ToArray(), new DoubleComparer(6));
    }

    private sealed class DoubleComparer(int precision) : IEqualityComparer<double>
    {
        public bool Equals(double x, double y) => Math.Round(x, precision) == Math.Round(y, precision);

        public int GetHashCode(double value) => Math.Round(value, precision).GetHashCode();
    }
}
