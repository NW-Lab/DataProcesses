using DataProcesses.Nodes.BuiltIn.Blocks.BreathTS;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.BreathTS;

public sealed class BreathTSBlockTests
{
    [Fact]
    public void Definition_UsesFastStreamInputAndRateOutputWithJsonEvents()
    {
        var ports = BreathTSBlock.Definition.Ports;

        Assert.Collection(
            ports,
            input =>
            {
                Assert.Equal(BreathTSBlock.InputPortId, input.Id);
                Assert.Equal(PortDirection.Input, input.Direction);
                Assert.Equal(PortDataKind.FastStream, input.DataKind);
                Assert.Equal(PortDataSchema.TimeSeries1D, input.DataSchema);
            },
            rateOutput =>
            {
                Assert.Equal(BreathTSBlock.RateOutputPortId, rateOutput.Id);
                Assert.Equal(PortDirection.Output, rateOutput.Direction);
                Assert.Equal(PortDataKind.FastStream, rateOutput.DataKind);
                Assert.Equal(PortDataSchema.TimeSeries1D, rateOutput.DataSchema);
            },
            eventOutput =>
            {
                Assert.Equal(BreathTSBlock.EventOutputPortId, eventOutput.Id);
                Assert.Equal(PortDirection.Output, eventOutput.Direction);
                Assert.Equal(PortDataKind.JsonMessage, eventOutput.DataKind);
                Assert.False(eventOutput.IsRequired);
                Assert.Equal(PortDataSchema.JsonEnvelope, eventOutput.DataSchema);
            });
    }

    [Fact]
    public void Settings_FromJson_SelectsLedOxygenMethod()
    {
        var settings = BreathTSSettings.FromJson("{\"method\":\"ledOxygen\",\"emitAnomalyEvents\":false}");

        Assert.Equal(BreathTSDetectionMethod.LedOxygen, settings.Method);
        Assert.False(settings.EmitAnomalyEvents);
    }

    [Fact]
    public void Settings_RejectInvalidIntervals()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BreathTSSettings.FromJson("{\"minimumBreathIntervalMilliseconds\":4000,\"maximumBreathIntervalMilliseconds\":3000}"));
    }

    [Fact]
    public async Task OnPacketAsync_EmitsBreathsPerMinuteAfterTwoBeltPeaks()
    {
        var context = new RecordingNodeContext();
        var node = new BreathTSNode(BreathTSSettings.Default with { EmitAnomalyEvents = false });
        await node.InitializeAsync(context, CancellationToken.None);
        var input = new FastStreamFrame(
            StartTimeUnixNanoseconds: 0,
            SamplePeriodNanoseconds: 500_000_000,
            ChannelNames: ["belt"],
            Samples: [new double[] { 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0 }.AsMemory()],
            SequenceNumber: 7);

        await node.OnPacketAsync(BreathTSBlock.InputPortId, input, CancellationToken.None);

        var emitted = Assert.Single(context.EmittedPackets);
        Assert.Equal(BreathTSBlock.RateOutputPortId, emitted.OutputPortId);
        var output = Assert.IsType<FastStreamFrame>(emitted.Packet);
        Assert.Equal(new[] { "breath-rate-brpm" }, output.ChannelNames);

        var breathRates = output.Samples[0].ToArray();
        Assert.All(breathRates[..7], AssertIsNaN);
        Assert.Equal(24.0, breathRates[7], 6);
    }

    [Fact]
    public async Task OnPacketAsync_LedOxygenMethodDetectsTroughs()
    {
        var context = new RecordingNodeContext();
        var node = new BreathTSNode(BreathTSSettings.Default with
        {
            Method = BreathTSDetectionMethod.LedOxygen,
            EmitAnomalyEvents = false,
        });
        await node.InitializeAsync(context, CancellationToken.None);
        var input = new FastStreamFrame(
            StartTimeUnixNanoseconds: 0,
            SamplePeriodNanoseconds: 500_000_000,
            ChannelNames: ["spo2"],
            Samples: [new double[] { 1.0, 0.0, 1.0, 1.0, 1.0, 1.0, 0.0, 1.0 }.AsMemory()],
            SequenceNumber: 8);

        await node.OnPacketAsync(BreathTSBlock.InputPortId, input, CancellationToken.None);

        var emitted = Assert.Single(context.EmittedPackets);
        var output = Assert.IsType<FastStreamFrame>(emitted.Packet);
        var breathRates = output.Samples[0].ToArray();
        Assert.Equal(24.0, breathRates[7], 6);
    }

    [Fact]
    public async Task OnPacketAsync_EmitsJsonEventForCoughLikeSpike()
    {
        var context = new RecordingNodeContext();
        var node = new BreathTSNode(BreathTSSettings.Default with
        {
            CoughSpikeThresholdFraction = 0.6,
            MinimumBreathIntervalMilliseconds = 500.0,
        });
        await node.InitializeAsync(context, CancellationToken.None);
        var input = new FastStreamFrame(
            StartTimeUnixNanoseconds: 1_000_000_000,
            SamplePeriodNanoseconds: 100_000_000,
            ChannelNames: ["belt"],
            Samples: [new double[] { 0.0, 0.1, 0.2, 4.0, 0.2, 0.1 }.AsMemory()],
            SequenceNumber: 9);

        await node.OnPacketAsync(BreathTSBlock.InputPortId, input, CancellationToken.None);

        Assert.Equal(2, context.EmittedPackets.Count);
        var eventPacket = context.EmittedPackets[1];
        Assert.Equal(BreathTSBlock.EventOutputPortId, eventPacket.OutputPortId);
        var message = Assert.IsType<JsonMessage>(eventPacket.Packet);
        Assert.Equal("dataprocesses.breath-st.anomaly", message.Topic);
        Assert.Equal("cough-like-spike", message.Payload.GetProperty("eventType").GetString());
        Assert.Equal("breathBelt", message.Payload.GetProperty("method").GetString());
        Assert.Equal(3, message.Payload.GetProperty("sampleIndex").GetInt32());
    }

    [Fact]
    public async Task OnPacketAsync_RejectsNonPositiveSamplePeriod()
    {
        var context = new RecordingNodeContext();
        var node = new BreathTSNode();
        await node.InitializeAsync(context, CancellationToken.None);
        var input = new FastStreamFrame(
            StartTimeUnixNanoseconds: 0,
            SamplePeriodNanoseconds: 0,
            ChannelNames: ["belt"],
            Samples: [new double[] { 0.0, 1.0, 0.0 }.AsMemory()],
            SequenceNumber: 0);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await node.OnPacketAsync(BreathTSBlock.InputPortId, input, CancellationToken.None));

        Assert.Contains("positive sample period", exception.Message, StringComparison.Ordinal);
    }

    private static void AssertIsNaN(double value)
    {
        Assert.True(double.IsNaN(value));
    }
}