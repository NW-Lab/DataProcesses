using DataProcesses.Nodes.BuiltIn.Blocks.CdTimeResolvedMethodSt;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.CdTimeResolvedMethodSt;

public sealed class CdTimeResolvedMethodStBlockTests
{
    [Fact]
    public void Definition_UsesTimeSeriesInputAndNumericVectorOutput()
    {
        Assert.Collection(
            CdTimeResolvedMethodStBlock.Definition.Ports,
            input =>
            {
                Assert.Equal(CdTimeResolvedMethodStBlock.InputPortId, input.Id);
                Assert.Equal(PortDirection.Input, input.Direction);
                Assert.Equal(PortDataKind.FastStream, input.DataKind);
                Assert.Equal(PortDataSchema.TimeSeries1D, input.DataSchema);
            },
            output =>
            {
                Assert.Equal(CdTimeResolvedMethodStBlock.OutputPortId, output.Id);
                Assert.Equal(PortDirection.Output, output.Direction);
                Assert.Equal(PortDataKind.FastStream, output.DataKind);
                Assert.Equal(PortDataSchema.NumericVector1D, output.DataSchema);
            });
    }

    [Fact]
    public async Task OnPacketAsync_EmitsTimeResolvedCentralDifferenceVector()
    {
        var context = new RecordingNodeContext();
        var node = new CdTimeResolvedMethodStNode();
        await node.InitializeAsync(context, CancellationToken.None);
        var input = new FastStreamFrame(
            StartTimeUnixNanoseconds: 1_000_000_000,
            SamplePeriodNanoseconds: 500_000_000,
            ChannelNames: ["signal"],
            Samples: [new double[] { 0.0, 1.0, 4.0, 9.0 }.AsMemory()],
            SequenceNumber: 8);

        await node.OnPacketAsync(CdTimeResolvedMethodStBlock.InputPortId, input, CancellationToken.None);

        var emitted = Assert.Single(context.EmittedPackets);
        Assert.Equal(CdTimeResolvedMethodStBlock.OutputPortId, emitted.OutputPortId);
        var output = Assert.IsType<NumericVectorFrame>(emitted.Packet);
        Assert.Equal("cd-time-resolved", output.Name);
        Assert.Equal(new[] { 2.0, 4.0, 8.0, 10.0 }, output.Values.ToArray());
        Assert.Equal(8, output.SequenceNumber);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1), output.Timestamp);
    }

    [Fact]
    public async Task OnPacketAsync_EmitsZeroForSingleSample()
    {
        var context = new RecordingNodeContext();
        var node = new CdTimeResolvedMethodStNode();
        await node.InitializeAsync(context, CancellationToken.None);
        var input = new FastStreamFrame(0, 1_000_000_000, ["signal"], [new double[] { 42.0 }.AsMemory()], 0);

        await node.OnPacketAsync(CdTimeResolvedMethodStBlock.InputPortId, input, CancellationToken.None);

        var output = Assert.IsType<NumericVectorFrame>(Assert.Single(context.EmittedPackets).Packet);
        Assert.Equal(new[] { 0.0 }, output.Values.ToArray());
    }

    [Fact]
    public async Task OnPacketAsync_RejectsNonPositiveSamplePeriod()
    {
        var context = new RecordingNodeContext();
        var node = new CdTimeResolvedMethodStNode();
        await node.InitializeAsync(context, CancellationToken.None);
        var input = new FastStreamFrame(0, 0, ["signal"], [new double[] { 1.0, 2.0 }.AsMemory()], 0);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await node.OnPacketAsync(CdTimeResolvedMethodStBlock.InputPortId, input, CancellationToken.None));

        Assert.Contains("positive sample period", exception.Message, StringComparison.Ordinal);
    }
}