using DataProcesses.Nodes.BuiltIn.Blocks.FilterFS;
using DataProcesses.Nodes.BuiltIn.Blocks.FilterTS;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.FilterFS;

public sealed class FilterFSBlockTests
{
    [Fact]
    public void Definition_DeclaresFastStreamInputAndOutput()
    {
        Assert.Collection(
            FilterFSBlock.Definition.Ports,
            input =>
            {
                Assert.Equal(FilterFSBlock.InputPortId, input.Id);
                Assert.Equal(PortDirection.Input, input.Direction);
                Assert.Equal(PortDataKind.FastStream, input.DataKind);
                Assert.Equal(PortDataSchema.FixedRateTimeSeries1D, input.DataSchema);
            },
            output =>
            {
                Assert.Equal(FilterFSBlock.OutputPortId, output.Id);
                Assert.Equal(PortDirection.Output, output.Direction);
                Assert.Equal(PortDataKind.FastStream, output.DataKind);
                Assert.Equal(PortDataSchema.FixedRateTimeSeries1D, output.DataSchema);
            });
    }

    [Fact]
    public void Settings_OffersFiveFilterTypesAndParsesNotch()
    {
        var settings = FilterFSSettings.FromJson(
            """{"filterType":"notch","cutoffFrequencyHertz":50,"notchBandwidthHertz":4,"order":4}""");

        Assert.Equal(new[] { "lowPass", "highPass", "bandPass", "bandStop", "notch" }, FilterFSSettings.FilterTypeOptions);
        Assert.Equal(FilterFSKind.Notch, settings.FilterType);
        Assert.Equal(50.0, settings.CutoffFrequencyHertz);
        Assert.Equal(4.0, settings.NotchBandwidthHertz);
        Assert.Equal(4, settings.Order);
    }

    [Fact]
    public void Settings_NotchConvertsCenterAndBandwidthToBandStopCutoffs()
    {
        var settings = new FilterFSSettings(
            FilterType: FilterFSKind.Notch,
            CutoffFrequencyHertz: 50.0,
            NotchBandwidthHertz: 4.0,
            Order: 4);

        var filterSettings = settings.ToFilterTSSettings();

        Assert.Equal(FilterTSKind.BandStop, filterSettings.FilterType);
        Assert.Equal(48.0, filterSettings.LowerCutoffFrequencyHertz);
        Assert.Equal(52.0, filterSettings.UpperCutoffFrequencyHertz);
        Assert.Equal(4, filterSettings.Order);
    }

    [Fact]
    public void Settings_RejectsInvalidOrderAndNotchBandwidth()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FilterFSSettings(Order: 11).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => new FilterFSSettings(
            FilterType: FilterFSKind.Notch,
            CutoffFrequencyHertz: 2.0,
            NotchBandwidthHertz: 4.0).Validate());
        new FilterFSSettings(FilterType: FilterFSKind.LowPass, CutoffFrequencyHertz: 0.5).Validate();
    }

    [Fact]
    public async Task OnPacketAsync_UsesSharedDSPAndPreservesFastStreamMetadata()
    {
        var settings = new FilterFSSettings(
            FilterType: FilterFSKind.LowPass,
            CutoffFrequencyHertz: 44_100.0 / (2.0 * Math.PI),
            Order: 2);
        var context = new RecordingNodeContext();
        var node = new FilterFSNode(settings);
        await node.InitializeAsync(context, CancellationToken.None);
        var input = new FastStreamFrame(
            StartTimeUnixNanoseconds: 123_000_000_000,
            SamplePeriodNanoseconds: 22_676,
            ChannelNames: ["signal"],
            Samples: [new double[] { 0.0, 1.0, 1.0 }.AsMemory()],
            SequenceNumber: 5,
            SampleRateHertz: 44_100);

        await node.OnPacketAsync(FilterFSBlock.InputPortId, input, CancellationToken.None);

        var emitted = Assert.Single(context.EmittedPackets);
        Assert.Equal(FilterFSBlock.OutputPortId, emitted.OutputPortId);
        var output = Assert.IsType<FastStreamFrame>(emitted.Packet);
        Assert.Equal(44_100, output.SampleRateHertz);
        Assert.Equal(123_000_000_000, output.StartTimeUnixNanoseconds);
        Assert.Equal(5, output.SequenceNumber);
        Assert.Equal(new[] { 0.0, 0.25, 0.5 }, output.Samples[0].ToArray(), new DoubleComparer(6));
    }

    [Fact]
    public async Task OnPacketAsync_RejectsFrameWithoutSampleRate()
    {
        var context = new RecordingNodeContext();
        var node = new FilterFSNode(FilterFSSettings.Default);
        await node.InitializeAsync(context, CancellationToken.None);
        var periodOnlyFrame = new FastStreamFrame(0, 1_000_000, ["signal"], [new double[] { 0.0, 1.0 }.AsMemory()], 0);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await node.OnPacketAsync(FilterFSBlock.InputPortId, periodOnlyFrame, CancellationToken.None));
        Assert.Empty(context.EmittedPackets);
    }

    private sealed class DoubleComparer(int precision) : IEqualityComparer<double>
    {
        public bool Equals(double x, double y) => Math.Round(x, precision) == Math.Round(y, precision);

        public int GetHashCode(double value) => Math.Round(value, precision).GetHashCode();
    }
}