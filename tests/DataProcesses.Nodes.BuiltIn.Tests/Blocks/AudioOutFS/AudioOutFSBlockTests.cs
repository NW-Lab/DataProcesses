using DataProcesses.Nodes.BuiltIn.Blocks.AudioOutFS;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.AudioOutFS;

public sealed class AudioOutFSBlockTests
{
    [Fact]
    public void Definition_DeclaresFastStreamAudioInput()
    {
        var input = Assert.Single(AudioOutFSBlock.Definition.Ports);

        Assert.Equal(AudioOutFSBlock.InputPortId, input.Id);
        Assert.Equal(PortDirection.Input, input.Direction);
        Assert.Equal(PortDataKind.FastStream, input.DataKind);
        Assert.Equal(PortDataSchema.FixedRateTimeSeries1D, input.DataSchema);
        Assert.Equal(NodeType.Output, AudioOutFSBlock.Definition.NodeType);
        Assert.True(AudioOutFSBlock.Definition.DashboardWidget!.IsVisibleByDefault);
    }

    [Fact]
    public void Settings_ParsesRecordingFolderPrefixAndStates()
    {
        var settings = AudioOutFSSettings.FromJson(
            """{"isMuted":true,"isRecording":true,"recordingFolder":"C:\\Audio","fileNamePrefix":"take"}""");

        Assert.True(settings.IsMuted);
        Assert.True(settings.IsRecording);
        Assert.Equal("C:\\Audio", settings.RecordingFolder);
        Assert.Equal("take", settings.FileNamePrefix);
        Assert.Throws<ArgumentException>(() => new AudioOutFSSettings(FileNamePrefix: "bad/name").Validate());
    }

    [Fact]
    public async Task OnPacketAsync_ForwardsValidatedFrameAndMuteRecordingSettings()
    {
        FastStreamFrame? forwardedFrame = null;
        AudioOutFSSettings? forwardedSettings = null;
        var settings = AudioOutFSSettings.Default with { IsMuted = true, IsRecording = true };
        var node = new AudioOutFSNode(
            "audio-out-1",
            settings,
            (_, frame, nodeSettings) =>
            {
                forwardedFrame = frame;
                forwardedSettings = nodeSettings;
            });
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        var input = new FastStreamFrame(
            StartTimeUnixNanoseconds: 1_000_000_000,
            SamplePeriodNanoseconds: 22_676,
            ChannelNames: ["left", "right"],
            Samples: [new double[] { 0.1, 0.2 }.AsMemory(), new double[] { -0.1, -0.2 }.AsMemory()],
            SequenceNumber: 4,
            SampleRateHertz: 44_100);

        await node.OnPacketAsync(AudioOutFSBlock.InputPortId, input, CancellationToken.None);

        Assert.Same(input, forwardedFrame);
        Assert.Same(settings, forwardedSettings);
    }

    [Fact]
    public async Task OnPacketAsync_RejectsNonAudioChannelCounts()
    {
        var node = new AudioOutFSNode("audio-out-1", AudioOutFSSettings.Default, (_, _, _) => { });
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        var threeChannelFrame = new FastStreamFrame(
            StartTimeUnixNanoseconds: 0,
            SamplePeriodNanoseconds: 1_000_000,
            ChannelNames: ["a", "b", "c"],
            Samples: [new double[4].AsMemory(), new double[4].AsMemory(), new double[4].AsMemory()],
            SequenceNumber: 0,
            SampleRateHertz: 1_000);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await node.OnPacketAsync(AudioOutFSBlock.InputPortId, threeChannelFrame, CancellationToken.None));
    }

    [Fact]
    public async Task OnPacketAsync_RejectsFrameWithoutSampleRate()
    {
        var forwarded = false;
        var node = new AudioOutFSNode("audio-out-1", AudioOutFSSettings.Default, (_, _, _) => forwarded = true);
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        var periodOnlyFrame = new FastStreamFrame(0, 22_676, ["mono"], [new double[4].AsMemory()], 0);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await node.OnPacketAsync(AudioOutFSBlock.InputPortId, periodOnlyFrame, CancellationToken.None));
        Assert.False(forwarded);
    }

    [Fact]
    public void Factory_CreatesConfiguredNodes()
    {
        var factory = new AudioOutFSNodeFactory((_, _, _) => { });
        var node = factory.CreateNode(
            "audio-out-1",
            """{"recordingFolder":"C:\\Audio","fileNamePrefix":"session"}""");

        Assert.IsType<AudioOutFSNode>(node);
    }
}