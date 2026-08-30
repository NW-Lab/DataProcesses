using System.Text.Json;

using DataProcesses.Nodes.BuiltIn.Blocks.UVCameraInputImage;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.UVCameraInputImage;

public sealed class UVCameraInputImageBlockTests
{
    [Fact]
    public async Task StartAsync_WhenPlaying_Emits4kFramesWithIncreasingSequenceNumbers()
    {
        var timestamp = DateTimeOffset.UnixEpoch;
        var context = new RecordingNodeContext();
        var node = CreateNode("uv-playing", isPlay: true, () => timestamp);
        await node.InitializeAsync(context, CancellationToken.None);

        await node.StartAsync(CancellationToken.None);
        timestamp = timestamp.AddMilliseconds(100);
        await node.StartAsync(CancellationToken.None);

        var frames = context.EmittedPackets.Select(packet => Assert.IsType<ImageFrame>(packet.Packet)).ToArray();
        Assert.Equal(new long[] { 0, 1 }, frames.Select(frame => frame.SequenceNumber));
        Assert.All(frames, frame => Assert.Equal((3840, 2160), (frame.Width, frame.Height)));
    }

    [Fact]
    public async Task OnPacketAsync_IsPlayFalse_StopsCapture()
    {
        var context = new RecordingNodeContext();
        var node = CreateNode("uv-stopped", isPlay: true);
        await node.InitializeAsync(context, CancellationToken.None);

        await node.OnPacketAsync(UVCameraInputImageBlock.ControlInputPortId, CreateMessage("{\"isPlay\":false}"), CancellationToken.None);
        await node.StartAsync(CancellationToken.None);

        Assert.Empty(context.EmittedPackets);
    }

    [Fact]
    public async Task OnPacketAsync_IsPlayTrue_StartsCapture()
    {
        var context = new RecordingNodeContext();
        var node = CreateNode("uv-started", isPlay: false);
        await node.InitializeAsync(context, CancellationToken.None);

        await node.OnPacketAsync(UVCameraInputImageBlock.ControlInputPortId, CreateMessage("{\"isPlay\":true}"), CancellationToken.None);
        await node.StartAsync(CancellationToken.None);

        Assert.Single(context.EmittedPackets);
    }

    [Fact]
    public void Settings_RejectsDimensionsAbove4kAndInvalidWhiteBalanceTemperature()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => UVCameraInputImageSettings.FromJson("{\"width\":3841}"));
        Assert.Throws<ArgumentOutOfRangeException>(() => UVCameraInputImageSettings.FromJson("{\"height\":2161}"));
        Assert.Throws<ArgumentOutOfRangeException>(() => UVCameraInputImageSettings.FromJson("{\"whiteBalanceTemperature\":10001}"));
    }

    private static UVCameraInputImageNode CreateNode(string nodeId, bool isPlay, Func<DateTimeOffset>? getTimestamp = null)
    {
        return new UVCameraInputImageNode(
            nodeId,
            UVCameraInputImageSettings.Default with { IsPlay = isPlay, RequestedWidth = 3840, RequestedHeight = 2160 },
            (_, width, height, _, _, _) => new ImageFrame("uv", width, height, ImagePixelFormat.Rgb24, new byte[width * height * 3], 0),
            getTimestamp ?? (() => DateTimeOffset.UnixEpoch));
    }

    private static JsonMessage CreateMessage(string payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson);
        return new JsonMessage("test", document.RootElement.Clone(), DateTimeOffset.UnixEpoch);
    }
}