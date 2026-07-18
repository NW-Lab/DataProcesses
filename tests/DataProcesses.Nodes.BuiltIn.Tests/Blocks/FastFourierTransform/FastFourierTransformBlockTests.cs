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
            },
            output =>
            {
                Assert.Equal(FastFourierTransformBlock.OutputPortId, output.Id);
                Assert.Equal(PortDirection.Output, output.Direction);
                Assert.Equal(PortDataKind.FastStream, output.DataKind);
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

    private sealed class DoubleComparer(int precision) : IEqualityComparer<double>
    {
        public bool Equals(double x, double y) => Math.Round(x, precision) == Math.Round(y, precision);

        public int GetHashCode(double value) => Math.Round(value, precision).GetHashCode();
    }
}
