using DataProcesses.Nodes.BuiltIn;
using DataProcesses.Nodes.BuiltIn.Blocks.MicInputFS;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.MicInputFS;

public sealed class MicInputFSBlockTests
{
    [Fact]
    public void Definition_DeclaresFastStreamSource()
    {
        var port = Assert.Single(MicInputFSBlock.Definition.Ports);

        Assert.Equal(MicInputFSBlock.StreamOutputPortId, port.Id);
        Assert.Equal(PortDirection.Output, port.Direction);
        Assert.Equal(PortDataKind.FastStream, port.DataKind);
        Assert.Equal(PortDataSchema.FixedRateTimeSeries1D, port.DataSchema);
        Assert.Equal(NodeType.Input, MicInputFSBlock.Definition.NodeType);
    }

    [Fact]
    public void BuiltInCatalog_RegistersMicInputFS()
    {
        var plugin = new BuiltInNodePlugin();
        var factory = Assert.Single(plugin.NodeFactories, factory => factory.Definition.TypeId == MicInputFSBlock.TypeId);

        Assert.Equal(MicInputFSBlock.TypeId, factory.Definition.TypeId);
    }

    [Fact]
    public void Settings_DefaultToStereo44100AndParseChannelMode()
    {
        Assert.Equal(44_100, MicInputFSSettings.Default.SampleRateHertz);
        Assert.Equal(MicInputFSChannelMode.Stereo, MicInputFSSettings.Default.ChannelMode);

        var settings = MicInputFSSettings.FromJson("""{"sampleRateHertz":48000,"channelMode":"right"}""");

        Assert.Equal(48_000, settings.SampleRateHertz);
        Assert.Equal(MicInputFSChannelMode.Right, settings.ChannelMode);
        Assert.Throws<ArgumentOutOfRangeException>(() => new MicInputFSSettings(7_999).Validate());
    }

    [Fact]
    public async Task StartAsync_EmitsStereoChannelsWithSampleRateMetadata()
    {
        var audio = CreateStereoCapture();
        var context = new RecordingNodeContext();
        var node = CreateNode(MicInputFSChannelMode.Stereo, audio);
        await node.InitializeAsync(context, CancellationToken.None);

        await node.StartAsync(CancellationToken.None);

        var output = Assert.Single(context.EmittedPackets);
        Assert.Equal(MicInputFSBlock.StreamOutputPortId, output.OutputPortId);
        var frame = Assert.IsType<FastStreamFrame>(output.Packet);
        Assert.Equal(44_100, frame.SampleRateHertz);
        Assert.Equal(22_676, frame.SamplePeriodNanoseconds);
        Assert.Equal(2, frame.ChannelCount);
        Assert.Equal(["left", "right"], frame.ChannelNames);
        Assert.Equal(MicInputFSSettings.SamplesPerFrame, frame.SampleCount);
        Assert.Equal(0.25, frame.Samples[0].Span[1]);
        Assert.Equal(-0.25, frame.Samples[1].Span[1]);
    }

    [Fact]
    public async Task StartAsync_LeftAndRightModesEmitOnlyTheSelectedChannel()
    {
        foreach (var (mode, expectedName, expectedValue) in new[]
        {
            (MicInputFSChannelMode.Left, "left", 0.25),
            (MicInputFSChannelMode.Right, "right", -0.25),
        })
        {
            var context = new RecordingNodeContext();
            var node = CreateNode(mode, CreateStereoCapture());
            await node.InitializeAsync(context, CancellationToken.None);

            await node.StartAsync(CancellationToken.None);

            var frame = Assert.IsType<FastStreamFrame>(Assert.Single(context.EmittedPackets).Packet);
            Assert.Equal(1, frame.ChannelCount);
            Assert.Equal(expectedName, Assert.Single(frame.ChannelNames));
            Assert.Equal(expectedValue, frame.Samples[0].Span[1]);
        }
    }

    [Fact]
    public async Task StartAsync_StereoModeFallsBackToSingleMonoChannel()
    {
        var monoSamples = new double[MicInputFSSettings.SamplesPerFrame];
        Array.Fill(monoSamples, 0.5);
        var audio = new MicInputFSCapturedAudio(48_000, 1, monoSamples, 1_000_000_000);
        var context = new RecordingNodeContext();
        var node = CreateNode(MicInputFSChannelMode.Stereo, audio);
        await node.InitializeAsync(context, CancellationToken.None);

        await node.StartAsync(CancellationToken.None);

        var frame = Assert.IsType<FastStreamFrame>(Assert.Single(context.EmittedPackets).Packet);
        Assert.Equal(48_000, frame.SampleRateHertz);
        Assert.Equal("mono", Assert.Single(frame.ChannelNames));
        Assert.Equal(0.5, frame.Samples[0].Span[0]);
    }

    [Fact]
    public async Task StartAsync_RejectsIncompleteCaptureBlock()
    {
        var audio = new MicInputFSCapturedAudio(44_100, 2, new double[4], 0);
        var node = CreateNode(MicInputFSChannelMode.Stereo, audio);
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);

        await Assert.ThrowsAsync<InvalidDataException>(async () => await node.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StartAsync_HonorsCancellationBeforeCapture()
    {
        var captureCalled = false;
        var node = new MicInputFSNode(
            MicInputFSSettings.Default,
            (_, cancellationToken) =>
            {
                captureCalled = true;
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromResult(CreateStereoCapture());
            });
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await node.StartAsync(cancellation.Token));
        Assert.False(captureCalled);
    }

    [Fact]
    public async Task Factory_PreservesSequenceNumbersAcrossNodeRecreation()
    {
        var factory = new MicInputFSNodeFactory((_, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(CreateStereoCapture());
        });
        var firstContext = new RecordingNodeContext();
        var firstNode = factory.CreateNode("mic-1");
        await firstNode.InitializeAsync(firstContext, CancellationToken.None);
        await firstNode.StartAsync(CancellationToken.None);

        var secondContext = new RecordingNodeContext();
        var secondNode = factory.CreateNode("mic-1");
        await secondNode.InitializeAsync(secondContext, CancellationToken.None);
        await secondNode.StartAsync(CancellationToken.None);

        var firstFrame = Assert.IsType<FastStreamFrame>(Assert.Single(firstContext.EmittedPackets).Packet);
        var secondFrame = Assert.IsType<FastStreamFrame>(Assert.Single(secondContext.EmittedPackets).Packet);
        Assert.Equal(0, firstFrame.SequenceNumber);
        Assert.Equal(1, secondFrame.SequenceNumber);
    }

    private static MicInputFSNode CreateNode(MicInputFSChannelMode channelMode, MicInputFSCapturedAudio audio)
    {
        return new MicInputFSNode(
            MicInputFSSettings.Default with { ChannelMode = channelMode },
            (_, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromResult(audio);
            });
    }

    private static MicInputFSCapturedAudio CreateStereoCapture()
    {
        var samples = new double[MicInputFSSettings.SamplesPerFrame * 2];
        for (var sampleIndex = 0; sampleIndex < MicInputFSSettings.SamplesPerFrame; sampleIndex++)
        {
            samples[sampleIndex * 2] = 0.25;
            samples[(sampleIndex * 2) + 1] = -0.25;
        }

        return new MicInputFSCapturedAudio(44_100, 2, samples, 1_000_000_000);
    }
}