using DataProcesses.Nodes.BuiltIn.Blocks.FilterSt;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.FilterSt;

public sealed class FilterStBlockTests
{
    [Fact]
    public void Definition_UsesFastStreamInputAndOutput()
    {
        var ports = FilterStBlock.Definition.Ports;

        Assert.Collection(
            ports,
            input =>
            {
                Assert.Equal(FilterStBlock.InputPortId, input.Id);
                Assert.Equal(PortDirection.Input, input.Direction);
                Assert.Equal(PortDataKind.FastStream, input.DataKind);
                Assert.Equal(PortDataSchema.TimeSeries1D, input.DataSchema);
            },
            output =>
            {
                Assert.Equal(FilterStBlock.OutputPortId, output.Id);
                Assert.Equal(PortDirection.Output, output.Direction);
                Assert.Equal(PortDataKind.FastStream, output.DataKind);
                Assert.Equal(PortDataSchema.TimeSeries1D, output.DataSchema);
            });
    }

    [Fact]
    public void Settings_FromJson_ReadsFilterTypeFrequenciesAndOrder()
    {
        var settings = FilterStSettings.FromJson(
            """
            {
              "filterType": "bandStop",
              "cutoffFrequencyHertz": 8.0,
              "lowerCutoffFrequencyHertz": 3.0,
              "upperCutoffFrequencyHertz": 12.0,
              "order": 6
            }
            """);

        Assert.Equal(FilterStKind.BandStop, settings.FilterType);
        Assert.Equal(8.0, settings.CutoffFrequencyHertz);
        Assert.Equal(3.0, settings.LowerCutoffFrequencyHertz);
        Assert.Equal(12.0, settings.UpperCutoffFrequencyHertz);
        Assert.Equal(6, settings.Order);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(11)]
    public void Settings_RejectOutOfRangeOrder(int order)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FilterStSettings.FromJson($"{{\"order\":{order}}}"));
    }

    [Fact]
    public async Task OnPacketAsync_AppliesConfiguredLowPassFilter()
    {
        var context = new RecordingNodeContext();
        var node = new FilterStNode(new FilterStSettings(
            FilterType: FilterStKind.LowPass,
            CutoffFrequencyHertz: 159.15494309189535,
            Order: 2));
        await node.InitializeAsync(context, CancellationToken.None);

        await node.OnPacketAsync(FilterStBlock.InputPortId, CreateFrame([0.0, 1.0, 1.0]), CancellationToken.None);

        var emitted = Assert.Single(context.EmittedPackets);
        Assert.Equal(FilterStBlock.OutputPortId, emitted.OutputPortId);
        var filtered = Assert.IsType<FastStreamFrame>(emitted.Packet);
        Assert.Equal(new[] { 0.0, 0.25, 0.5 }, filtered.Samples[0].ToArray(), new DoubleComparer(6));
    }

    [Fact]
    public async Task OnPacketAsync_AppliesConfiguredHighPassFilter()
    {
        var context = new RecordingNodeContext();
        var node = new FilterStNode(new FilterStSettings(
            FilterType: FilterStKind.HighPass,
            CutoffFrequencyHertz: 159.15494309189535,
            Order: 2));
        await node.InitializeAsync(context, CancellationToken.None);

        await node.OnPacketAsync(FilterStBlock.InputPortId, CreateFrame([0.0, 1.0, 1.0, 1.0]), CancellationToken.None);

        var filtered = Assert.IsType<FastStreamFrame>(Assert.Single(context.EmittedPackets).Packet);
        Assert.Equal(new[] { 0.0, 0.25, 0.0, -0.0625 }, filtered.Samples[0].ToArray(), new DoubleComparer(6));
    }

    [Fact]
    public async Task OnPacketAsync_AppliesBandPassAndBandStopFilters()
    {
        var bandPassContext = new RecordingNodeContext();
        var bandPassNode = new FilterStNode(new FilterStSettings(FilterStKind.BandPass, LowerCutoffFrequencyHertz: 2.0, UpperCutoffFrequencyHertz: 20.0, Order: 2));
        await bandPassNode.InitializeAsync(bandPassContext, CancellationToken.None);

        var bandStopContext = new RecordingNodeContext();
        var bandStopNode = new FilterStNode(new FilterStSettings(FilterStKind.BandStop, LowerCutoffFrequencyHertz: 2.0, UpperCutoffFrequencyHertz: 20.0, Order: 2));
        await bandStopNode.InitializeAsync(bandStopContext, CancellationToken.None);
        var input = CreateFrame([0.0, 1.0, 0.0, -1.0, 0.0]);

        await bandPassNode.OnPacketAsync(FilterStBlock.InputPortId, input, CancellationToken.None);
        await bandStopNode.OnPacketAsync(FilterStBlock.InputPortId, input, CancellationToken.None);

        var bandPass = Assert.IsType<FastStreamFrame>(Assert.Single(bandPassContext.EmittedPackets).Packet);
        var bandStop = Assert.IsType<FastStreamFrame>(Assert.Single(bandStopContext.EmittedPackets).Packet);
        Assert.Equal(input.SampleCount, bandPass.SampleCount);
        Assert.Equal(input.SampleCount, bandStop.SampleCount);
        Assert.NotEqual(bandPass.Samples[0].ToArray(), bandStop.Samples[0].ToArray());
    }

    [Fact]
    public async Task OnPacketAsync_RejectsCutoffAtOrAboveNyquist()
    {
        var context = new RecordingNodeContext();
        var node = new FilterStNode(new FilterStSettings(CutoffFrequencyHertz: 600.0));
        await node.InitializeAsync(context, CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await node.OnPacketAsync(FilterStBlock.InputPortId, CreateFrame([1.0, 2.0]), CancellationToken.None));
    }

    private static FastStreamFrame CreateFrame(double[] samples)
    {
        return new FastStreamFrame(
            StartTimeUnixNanoseconds: 0,
            SamplePeriodNanoseconds: 1_000_000,
            ChannelNames: ["signal"],
            Samples: [samples.AsMemory()],
            SequenceNumber: 0);
    }

    private sealed class DoubleComparer(int precision) : IEqualityComparer<double>
    {
        public bool Equals(double x, double y) => Math.Round(x, precision) == Math.Round(y, precision);

        public int GetHashCode(double value) => Math.Round(value, precision).GetHashCode();
    }
}