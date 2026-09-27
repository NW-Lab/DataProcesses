using DataProcesses.Nodes.BuiltIn;
using DataProcesses.Nodes.BuiltIn.Blocks.TestSignalFS;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.TestSignalFS;

public sealed class TestSignalFSBlockTests
{
    [Fact]
    public void Definition_DeclaresOneFastStreamTimeSeriesOutput()
    {
        var port = Assert.Single(TestSignalFSBlock.Definition.Ports);

        Assert.Equal(TestSignalFSBlock.StreamOutputPortId, port.Id);
        Assert.Equal(PortDirection.Output, port.Direction);
        Assert.Equal(PortDataKind.FastStream, port.DataKind);
        Assert.Equal(PortDataSchema.TimeSeries1D, port.DataSchema);
    }

    [Fact]
    public void BuiltInCatalog_RegistersTestSignalFS()
    {
        var plugin = new BuiltInNodePlugin();
        var factory = Assert.Single(plugin.NodeFactories, factory => factory.Definition.TypeId == TestSignalFSBlock.TypeId);

        Assert.Equal("TestSignal(FS)ブロック", factory.Definition.Title);
        Assert.Equal("FS", factory.Definition.Subtitle);
    }

    [Fact]
    public async Task StartAsync_EmitsSineWaveWithExact44100HertzRate()
    {
        var context = new RecordingNodeContext();
        var node = new TestSignalFSNode(TestSignalFSSettings.Default, () => DateTimeOffset.UnixEpoch);
        await node.InitializeAsync(context, CancellationToken.None);

        await node.StartAsync(CancellationToken.None);

        var emitted = Assert.Single(context.EmittedPackets);
        Assert.Equal(TestSignalFSBlock.StreamOutputPortId, emitted.OutputPortId);
        var frame = Assert.IsType<FastStreamFrame>(emitted.Packet);
        Assert.Equal(44_100, frame.SampleRateHertz);
        Assert.Equal(22_676, frame.SamplePeriodNanoseconds);
        Assert.Equal(0, frame.StartTimeUnixNanoseconds);
        Assert.Equal(0, frame.SequenceNumber);
        Assert.Equal(["signal"], frame.ChannelNames);
        var samples = Assert.Single(frame.Samples).Span;
        Assert.Equal(TestSignalFSSettings.SampleCount, samples.Length);
        Assert.Equal(0.0, samples[0], precision: 12);
        Assert.Equal(Math.Sin(2.0 * Math.PI * 440.0 / 44_100), samples[1], precision: 12);
    }

    [Fact]
    public async Task StartAsync_SquareWaveUsesConfiguredPeakIntensity()
    {
        var settings = new TestSignalFSSettings(
            WaveType: TestSignalFSWaveType.Square,
            FrequencyHertz: 11_025,
            PeakIntensity: 2.5,
            SampleRateHertz: 44_100);
        var context = new RecordingNodeContext();
        var node = new TestSignalFSNode(settings, () => DateTimeOffset.UnixEpoch);
        await node.InitializeAsync(context, CancellationToken.None);

        await node.StartAsync(CancellationToken.None);

        var frame = Assert.IsType<FastStreamFrame>(Assert.Single(context.EmittedPackets).Packet);
        var samples = Assert.Single(frame.Samples).Span;
        Assert.Equal([2.5, 2.5, 2.5, -2.5], samples[..4].ToArray());
    }

    [Fact]
    public void Settings_RejectFrequencyAboveNyquistAndInvalidSampleRate()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TestSignalFSSettings(FrequencyHertz: 23_000).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => new TestSignalFSSettings(SampleRateHertz: 0).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => new TestSignalFSSettings(PeakIntensity: -1.0).Validate());
    }

    [Fact]
    public void Factory_DeserializesWaveFrequencyPeakAndSampleRate()
    {
        var factory = new TestSignalFSNodeFactory();
        var node = factory.CreateNode(
            "signal-fs",
            """{"waveType":"square","frequency":1000,"peakIntensity":2.5,"sampleRateHertz":48000}""");

        Assert.IsType<TestSignalFSNode>(node);
        var parsedSettings = TestSignalFSSettings.FromJson(
            """{"waveType":"square","frequency":1000,"peakIntensity":2.5,"sampleRateHertz":48000}""");
        Assert.Equal(TestSignalFSWaveType.Square, parsedSettings.WaveType);
        Assert.Equal(1_000, parsedSettings.FrequencyHertz);
        Assert.Equal(2.5, parsedSettings.PeakIntensity);
        Assert.Equal(48_000, parsedSettings.SampleRateHertz);
    }

    [Fact]
    public async Task StartAsync_ThrowsWhenCancelled()
    {
        var node = new TestSignalFSNode(TestSignalFSSettings.Default);
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await node.StartAsync(cancellationTokenSource.Token));
    }
}