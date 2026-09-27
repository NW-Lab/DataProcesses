using DataProcesses.Nodes.BuiltIn.Blocks.FastFourierTransform;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.FastFourierTransform;

public sealed class FastFourierTransformBlockTests
{
    [Fact]
    public void Definition_UsesFastStreamInputAndSpectrumOutput()
    {
        var ports = FastFourierTransformBlock.Definition.Ports;

        Assert.Collection(
            ports,
            input =>
            {
                Assert.Equal(FastFourierTransformBlock.InputPortId, input.Id);
                Assert.Equal(PortDirection.Input, input.Direction);
                Assert.Equal(PortDataKind.FastStream, input.DataKind);
                Assert.Equal(PortDataSchema.TimeSeries1D, input.DataSchema);
            },
            output =>
            {
                Assert.Equal(FastFourierTransformBlock.OutputPortId, output.Id);
                Assert.Equal(PortDirection.Output, output.Direction);
                Assert.Equal(PortDataKind.FastStream, output.DataKind);
                Assert.Equal(PortDataSchema.Spectrum1D, output.DataSchema);
            });
    }

    [Fact]
    public async Task OnPacketAsync_EmitsOneSidedMagnitudeSpectrum()
    {
        var context = new RecordingNodeContext();
        var node = new FastFourierTransformNode();
        await node.InitializeAsync(context, CancellationToken.None);
        var input = new FastStreamFrame(
            StartTimeUnixNanoseconds: 0,
            SamplePeriodNanoseconds: 250_000_000,
            ChannelNames: ["signal"],
            Samples: [new double[] { 1.0, 0.0, -1.0, 0.0 }.AsMemory()],
            SequenceNumber: 7);

        await node.OnPacketAsync(FastFourierTransformBlock.InputPortId, input, CancellationToken.None);

        var emitted = Assert.Single(context.EmittedPackets);
        Assert.Equal(FastFourierTransformBlock.OutputPortId, emitted.OutputPortId);
        var spectrum = Assert.IsType<SpectrumFrame>(emitted.Packet);
        Assert.Equal(1.0, spectrum.FrequencyResolutionHertz, 6);
        Assert.Equal(new[] { 0.0, 1.0, 0.0 }, spectrum.Magnitudes[0].ToArray(), new DoubleComparer(6));
        Assert.Equal(7, spectrum.SequenceNumber);
    }

    [Fact]
    public void FastStreamFrame_UsesExactSampleRateForTimestamps()
    {
        var frame = new FastStreamFrame(
            StartTimeUnixNanoseconds: 0,
            SamplePeriodNanoseconds: 22_676,
            ChannelNames: ["audio"],
            Samples: [new double[44_101].AsMemory()],
            SequenceNumber: 0,
            SampleRateHertz: 44_100);

        Assert.Equal(44_100.0, frame.GetSampleRateHertz());
        Assert.Equal(22_675, frame.GetSampleTimestampUnixNanoseconds(1));
        Assert.Equal(1_000_000_000, frame.GetSampleTimestampUnixNanoseconds(44_100));
    }

    [Fact]
    public async Task OnPacketAsync_UsesExactSampleRateForFrequencyResolution()
    {
        var context = new RecordingNodeContext();
        var node = new FastFourierTransformNode();
        await node.InitializeAsync(context, CancellationToken.None);
        var input = new FastStreamFrame(
            StartTimeUnixNanoseconds: 0,
            SamplePeriodNanoseconds: 22_676,
            ChannelNames: ["signal"],
            Samples: [new double[] { 1.0, 0.0, -1.0, 0.0 }.AsMemory()],
            SequenceNumber: 8,
            SampleRateHertz: 44_100);

        await node.OnPacketAsync(FastFourierTransformBlock.InputPortId, input, CancellationToken.None);

        var spectrum = Assert.IsType<SpectrumFrame>(Assert.Single(context.EmittedPackets).Packet);
        Assert.Equal(11_025.0, spectrum.FrequencyResolutionHertz);
        Assert.Equal(44_100, spectrum.SourceSampleRateHertz);
    }

    private sealed class DoubleComparer(int precision) : IEqualityComparer<double>
    {
        public bool Equals(double x, double y) => Math.Round(x, precision) == Math.Round(y, precision);

        public int GetHashCode(double value) => Math.Round(value, precision).GetHashCode();
    }
}
