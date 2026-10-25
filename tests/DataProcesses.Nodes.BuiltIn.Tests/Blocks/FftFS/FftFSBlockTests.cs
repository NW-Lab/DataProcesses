using DataProcesses.Nodes.BuiltIn.Blocks.FftFS;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.FftFS;

public sealed class FftFSBlockTests
{
    [Fact]
    public void Definition_DeclaresTimeSeriesInputAndSpectrumOutput()
    {
        Assert.Collection(
            FftFSBlock.Definition.Ports,
            input =>
            {
                Assert.Equal(FftFSBlock.InputPortId, input.Id);
                Assert.Equal(PortDirection.Input, input.Direction);
                Assert.Equal(PortDataKind.FastStream, input.DataKind);
                Assert.Equal(PortDataSchema.FixedRateTimeSeries1D, input.DataSchema);
            },
            output =>
            {
                Assert.Equal(FftFSBlock.OutputPortId, output.Id);
                Assert.Equal(PortDirection.Output, output.Direction);
                Assert.Equal(PortDataKind.FastStream, output.DataKind);
                Assert.Equal(PortDataSchema.Spectrum1D, output.DataSchema);
            });
    }

    [Fact]
    public async Task OnPacketAsync_UsesSampleRateForSpectrumAndPreservesMetadata()
    {
        var context = new RecordingNodeContext();
        var node = new FftFSNode();
        await node.InitializeAsync(context, CancellationToken.None);
        var input = new FastStreamFrame(
            StartTimeUnixNanoseconds: 123_000_000_000,
            SamplePeriodNanoseconds: 22_676,
            ChannelNames: ["left"],
            Samples: [new double[] { 1.0, 0.0, -1.0, 0.0 }.AsMemory()],
            SequenceNumber: 25,
            SampleRateHertz: 44_100);

        await node.OnPacketAsync(FftFSBlock.InputPortId, input, CancellationToken.None);

        var emitted = Assert.Single(context.EmittedPackets);
        Assert.Equal(FftFSBlock.OutputPortId, emitted.OutputPortId);
        var spectrum = Assert.IsType<SpectrumFrame>(emitted.Packet);
        Assert.Equal(11_025.0, spectrum.FrequencyResolutionHertz);
        Assert.Equal(44_100, spectrum.SourceSampleRateHertz);
        Assert.Equal(123_000_000_000, spectrum.SourceStartTimeUnixNanoseconds);
        Assert.Equal(22_676, spectrum.SourceSamplePeriodNanoseconds);
        Assert.Equal(25, spectrum.SequenceNumber);
        Assert.Equal(new[] { 0.0, 1.0, 0.0 }, spectrum.Magnitudes[0].ToArray(), new DoubleComparer(6));
    }

    [Fact]
    public async Task OnPacketAsync_RejectsFrameWithoutSampleRate()
    {
        var context = new RecordingNodeContext();
        var node = new FftFSNode();
        await node.InitializeAsync(context, CancellationToken.None);
        var periodOnlyFrame = new FastStreamFrame(0, 1_000_000, ["signal"], [new double[] { 1.0, 0.0, -1.0, 0.0 }.AsMemory()], 0);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await node.OnPacketAsync(FftFSBlock.InputPortId, periodOnlyFrame, CancellationToken.None));
        Assert.Empty(context.EmittedPackets);
    }

    private sealed class DoubleComparer(int precision) : IEqualityComparer<double>
    {
        public bool Equals(double x, double y) => Math.Round(x, precision) == Math.Round(y, precision);

        public int GetHashCode(double value) => Math.Round(value, precision).GetHashCode();
    }
}