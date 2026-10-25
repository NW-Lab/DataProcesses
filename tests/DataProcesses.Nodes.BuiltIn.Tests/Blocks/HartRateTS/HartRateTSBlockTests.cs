using DataProcesses.Nodes.BuiltIn.Blocks.HartRateTS;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.HartRateTS;

public sealed class HartRateTSBlockTests
{
    [Fact]
    public void Definition_UsesFastStreamInputAndOutput()
    {
        var ports = HartRateTSBlock.Definition.Ports;

        Assert.Collection(
            ports,
            input =>
            {
                Assert.Equal(HartRateTSBlock.InputPortId, input.Id);
                Assert.Equal(PortDirection.Input, input.Direction);
                Assert.Equal(PortDataKind.FastStream, input.DataKind);
                Assert.Equal(PortDataSchema.TimeSeries1D, input.DataSchema);
            },
            output =>
            {
                Assert.Equal(HartRateTSBlock.OutputPortId, output.Id);
                Assert.Equal(PortDirection.Output, output.Direction);
                Assert.Equal(PortDataKind.FastStream, output.DataKind);
                Assert.Equal(PortDataSchema.TimeSeries1D, output.DataSchema);
            });
    }

    [Fact]
    public async Task OnPacketAsync_EmitsBpmAfterTwoDetectedPeaks()
    {
        var context = new RecordingNodeContext();
        var node = new HartRateTSNode();
        await node.InitializeAsync(context, CancellationToken.None);
        var input = new FastStreamFrame(
            StartTimeUnixNanoseconds: 0,
            SamplePeriodNanoseconds: 100_000_000,
            ChannelNames: ["ecg"],
            Samples: [new double[] { 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0 }.AsMemory()],
            SequenceNumber: 3);

        await node.OnPacketAsync(HartRateTSBlock.InputPortId, input, CancellationToken.None);

        var emitted = Assert.Single(context.EmittedPackets);
        Assert.Equal(HartRateTSBlock.OutputPortId, emitted.OutputPortId);
        var output = Assert.IsType<FastStreamFrame>(emitted.Packet);
        Assert.Equal(0, output.StartTimeUnixNanoseconds);
        Assert.Equal(100_000_000, output.SamplePeriodNanoseconds);
        Assert.Equal(3, output.SequenceNumber);
        Assert.Equal(new[] { "heart-rate-bpm" }, output.ChannelNames);

        var heartRates = output.Samples[0].ToArray();
        Assert.All(heartRates[..12], AssertIsNaN);
        Assert.Equal(new[] { 60.0, 60.0 }, heartRates[12..], new DoubleComparer(6));
    }

    [Fact]
    public async Task OnPacketAsync_RejectsNonPositiveSamplePeriod()
    {
        var context = new RecordingNodeContext();
        var node = new HartRateTSNode();
        await node.InitializeAsync(context, CancellationToken.None);
        var input = new FastStreamFrame(
            StartTimeUnixNanoseconds: 0,
            SamplePeriodNanoseconds: 0,
            ChannelNames: ["ecg"],
            Samples: [new double[] { 0.0, 1.0, 0.0 }.AsMemory()],
            SequenceNumber: 0);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await node.OnPacketAsync(HartRateTSBlock.InputPortId, input, CancellationToken.None));

        Assert.Contains("positive sample period", exception.Message, StringComparison.Ordinal);
    }

    private static void AssertIsNaN(double value)
    {
        Assert.True(double.IsNaN(value));
    }

    private sealed class DoubleComparer(int precision) : IEqualityComparer<double>
    {
        public bool Equals(double x, double y) => Math.Round(x, precision) == Math.Round(y, precision);

        public int GetHashCode(double value) => Math.Round(value, precision).GetHashCode();
    }
}