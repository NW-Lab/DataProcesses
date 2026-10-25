using DataProcesses.Nodes.BuiltIn.Blocks.FftTS;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.FftTS;

public sealed class FftTSBlockTests
{
    [Fact]
    public void Definition_UsesTimeSeriesInputAndNumericVectorOutput()
    {
        Assert.Collection(
            FftTSBlock.Definition.Ports,
            input =>
            {
                Assert.Equal(FftTSBlock.InputPortId, input.Id);
                Assert.Equal(PortDirection.Input, input.Direction);
                Assert.Equal(PortDataKind.FastStream, input.DataKind);
                Assert.Equal(PortDataSchema.TimeSeries1D, input.DataSchema);
            },
            output =>
            {
                Assert.Equal(FftTSBlock.OutputPortId, output.Id);
                Assert.Equal(PortDirection.Output, output.Direction);
                Assert.Equal(PortDataKind.FastStream, output.DataKind);
                Assert.Equal(PortDataSchema.NumericVector1D, output.DataSchema);
            });
            Assert.True(FftTSBlock.Definition.DashboardWidget?.IsVisibleByDefault);
            Assert.Equal(3, FftTSBlock.Definition.DashboardWidget?.GridWidth);
            Assert.Equal(2, FftTSBlock.Definition.DashboardWidget?.GridHeight);
    }

    [Fact]
    public async Task OnPacketAsync_EmitsDashboardFriendlyMagnitudeVector()
    {
        var context = new RecordingNodeContext();
        var node = new FftTSNode();
        await node.InitializeAsync(context, CancellationToken.None);
        var input = new FastStreamFrame(
            StartTimeUnixNanoseconds: 2_000_000_000,
            SamplePeriodNanoseconds: 250_000_000,
            ChannelNames: ["signal"],
            Samples: [new double[] { 1.0, 0.0, -1.0, 0.0 }.AsMemory()],
            SequenceNumber: 7);

        await node.OnPacketAsync(FftTSBlock.InputPortId, input, CancellationToken.None);

        var emitted = Assert.Single(context.EmittedPackets);
        Assert.Equal(FftTSBlock.OutputPortId, emitted.OutputPortId);
        var vector = Assert.IsType<NumericVectorFrame>(emitted.Packet);
        Assert.Equal("fft-magnitude", vector.Name);
        Assert.Equal(new[] { 0.0, 1.0, 0.0 }, vector.Values.ToArray(), new DoubleComparer(6));
        Assert.Equal(7, vector.SequenceNumber);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(2), vector.Timestamp);
    }

    [Fact]
    public async Task OnPacketAsync_UsesFirstChannelForSingleDashboardVector()
    {
        var context = new RecordingNodeContext();
        var node = new FftTSNode();
        await node.InitializeAsync(context, CancellationToken.None);
        var input = new FastStreamFrame(
            StartTimeUnixNanoseconds: 0,
            SamplePeriodNanoseconds: 250_000_000,
            ChannelNames: ["signal-a", "signal-b"],
            Samples:
            [
                new double[] { 1.0, 0.0, -1.0, 0.0 }.AsMemory(),
                new double[] { 0.0, 0.0, 1.0, 0.0 }.AsMemory(),
            ],
            SequenceNumber: 3);

        await node.OnPacketAsync(FftTSBlock.InputPortId, input, CancellationToken.None);

        var vector = Assert.IsType<NumericVectorFrame>(Assert.Single(context.EmittedPackets).Packet);
        Assert.Equal(new[] { 0.0, 1.0, 0.0 }, vector.Values.ToArray(), new DoubleComparer(6));
    }

    [Fact]
    public async Task OnPacketAsync_EmitsEmptyVectorForEmptyFrame()
    {
        var context = new RecordingNodeContext();
        var node = new FftTSNode();
        await node.InitializeAsync(context, CancellationToken.None);
        var input = new FastStreamFrame(0, 1_000_000_000, ["signal"], [Array.Empty<double>().AsMemory()], 0);

        await node.OnPacketAsync(FftTSBlock.InputPortId, input, CancellationToken.None);

        var vector = Assert.IsType<NumericVectorFrame>(Assert.Single(context.EmittedPackets).Packet);
        Assert.Empty(vector.Values.ToArray());
    }

    [Fact]
    public async Task OnPacketAsync_RejectsNonPositiveSamplePeriod()
    {
        var context = new RecordingNodeContext();
        var node = new FftTSNode();
        await node.InitializeAsync(context, CancellationToken.None);
        var input = new FastStreamFrame(0, 0, ["signal"], [new double[] { 1.0, 2.0 }.AsMemory()], 0);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await node.OnPacketAsync(FftTSBlock.InputPortId, input, CancellationToken.None));

        Assert.Contains("positive sample period", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnPacketAsync_RejectsFrameWithoutChannels()
    {
        var context = new RecordingNodeContext();
        var node = new FftTSNode();
        await node.InitializeAsync(context, CancellationToken.None);
        var input = new FastStreamFrame(0, 1_000_000_000, [], [], 0);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await node.OnPacketAsync(FftTSBlock.InputPortId, input, CancellationToken.None));

        Assert.Contains("at least one Fast Stream channel", exception.Message, StringComparison.Ordinal);
    }

    private sealed class DoubleComparer(int precision) : IEqualityComparer<double>
    {
        public bool Equals(double x, double y) => Math.Round(x, precision) == Math.Round(y, precision);

        public int GetHashCode(double value) => Math.Round(value, precision).GetHashCode();
    }
}