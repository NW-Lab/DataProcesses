using System.Text.Json;

using DataProcesses.Nodes.BuiltIn.Blocks.MovieInputImage;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.MovieInputImage;

public sealed class MovieInputImageBlockTests
{
    [Fact]
    public void Definition_UsesOptionalJsonControlAndImageOutput()
    {
        Assert.Collection(
            MovieInputImageBlock.Definition.Ports,
            control =>
            {
                Assert.Equal(MovieInputImageBlock.ControlInputPortId, control.Id);
                Assert.Equal(PortDirection.Input, control.Direction);
                Assert.Equal(PortDataKind.JsonMessage, control.DataKind);
                Assert.False(control.IsRequired);
            },
            image => Assert.Equal(PortDataSchema.Image2D, image.DataSchema));
    }

    [Fact]
    public async Task StartAsync_WhenPlaying_EmitsSequentialFramesAtConfiguredFps()
    {
        var timestamp = DateTimeOffset.UnixEpoch;
        var context = new RecordingNodeContext();
        var node = CreateNode("movie-playing", isPlay: true, () => timestamp);
        await node.InitializeAsync(context, CancellationToken.None);

        await node.StartAsync(CancellationToken.None);
        timestamp = timestamp.AddMilliseconds(50);
        await node.StartAsync(CancellationToken.None);
        timestamp = timestamp.AddMilliseconds(50);
        await node.StartAsync(CancellationToken.None);

        var frames = context.EmittedPackets.Select(packet => Assert.IsType<ImageFrame>(packet.Packet)).ToArray();
        Assert.Equal(new long[] { 0, 1 }, frames.Select(frame => frame.SequenceNumber));
    }

    [Fact]
    public async Task OnPacketAsync_IsPlayFalse_StopsPlayback()
    {
        var context = new RecordingNodeContext();
        var node = CreateNode("movie-stop", isPlay: true);
        await node.InitializeAsync(context, CancellationToken.None);

        await node.OnPacketAsync(MovieInputImageBlock.ControlInputPortId, CreateMessage("{\"isPlay\":false}"), CancellationToken.None);
        await node.StartAsync(CancellationToken.None);

        Assert.Empty(context.EmittedPackets);
    }

    [Fact]
    public async Task OnPacketAsync_IsPlayTrue_StartsPlayback()
    {
        var context = new RecordingNodeContext();
        var node = CreateNode("movie-start", isPlay: false);
        await node.InitializeAsync(context, CancellationToken.None);

        await node.OnPacketAsync(MovieInputImageBlock.ControlInputPortId, CreateMessage("{\"isPlay\":true}"), CancellationToken.None);
        await node.StartAsync(CancellationToken.None);

        Assert.Single(context.EmittedPackets);
    }

    [Fact]
    public async Task OnPacketAsync_NonBooleanIsPlay_ThrowsActionableError()
    {
        var node = CreateNode("movie-invalid-control");
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await node.OnPacketAsync(MovieInputImageBlock.ControlInputPortId, CreateMessage("{\"isPlay\":\"true\"}"), CancellationToken.None));

        Assert.Contains("isPlay", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Settings_RejectOutOfRangeFps()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MovieInputImageSettings.FromJson("{\"fps\":0}"));
        Assert.Throws<ArgumentOutOfRangeException>(() => MovieInputImageSettings.FromJson("{\"fps\":61}"));
    }

    [Fact]
    public async Task StartAsync_PassesConfiguredOutputResolutionToFrameReader()
    {
        var context = new RecordingNodeContext();
        var node = new MovieInputImageNode(
            "movie-resolution",
            MovieInputImageSettings.Default with { OutputWidth = 320, OutputHeight = 180 },
            (_, frameIndex, width, height, _) => new ImageFrame("test", width, height, ImagePixelFormat.Rgb24, new byte[width * height * 3], frameIndex));
        await node.InitializeAsync(context, CancellationToken.None);

        await node.StartAsync(CancellationToken.None);

        var frame = Assert.IsType<ImageFrame>(Assert.Single(context.EmittedPackets).Packet);
        Assert.Equal(320, frame.Width);
        Assert.Equal(180, frame.Height);
    }

    private static MovieInputImageNode CreateNode(string nodeId, bool isPlay = true, Func<DateTimeOffset>? getTimestamp = null)
    {
        return new MovieInputImageNode(
            nodeId,
            MovieInputImageSettings.Default with { IsPlay = isPlay },
            (_, frameIndex, _, _, _) => new ImageFrame("test", 1, 1, ImagePixelFormat.Rgb24, new byte[] { 1, 2, 3 }, frameIndex),
            getTimestamp ?? (() => DateTimeOffset.UnixEpoch));
    }

    private static JsonMessage CreateMessage(string payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson);
        return new JsonMessage("test", document.RootElement.Clone(), DateTimeOffset.UnixEpoch);
    }
}