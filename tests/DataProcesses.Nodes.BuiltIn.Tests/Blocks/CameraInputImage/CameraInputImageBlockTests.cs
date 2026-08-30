using System.Text.Json;

using DataProcesses.Nodes.BuiltIn.Blocks.CameraInputImage;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.CameraInputImage;

public sealed class CameraInputImageBlockTests
{
    [Fact]
    public void Definition_UsesOptionalJsonTriggerAndImageOutput()
    {
        Assert.Collection(
            CameraInputImageBlock.Definition.Ports,
            trigger =>
            {
                Assert.Equal(CameraInputImageBlock.TriggerInputPortId, trigger.Id);
                Assert.Equal(PortDirection.Input, trigger.Direction);
                Assert.Equal(PortDataKind.JsonMessage, trigger.DataKind);
                Assert.Equal(PortDataSchema.JsonEnvelope, trigger.DataSchema);
                Assert.False(trigger.IsRequired);
            },
            image =>
            {
                Assert.Equal(CameraInputImageBlock.ImageOutputPortId, image.Id);
                Assert.Equal(PortDirection.Output, image.Direction);
                Assert.Equal(PortDataKind.FastStream, image.DataKind);
                Assert.Equal(PortDataSchema.Image2D, image.DataSchema);
            });
    }

    [Fact]
    public async Task OnPacketAsync_TrueTrigger_EmitsCapturedRgbImage()
    {
        var context = new RecordingNodeContext();
        var node = CreateNode("camera-trigger");
        await node.InitializeAsync(context, CancellationToken.None);

        await node.OnPacketAsync(
            CameraInputImageBlock.TriggerInputPortId,
            CreateMessage("{\"Trigger\":true}"),
            CancellationToken.None);

        var emitted = Assert.Single(context.EmittedPackets);
        var frame = Assert.IsType<ImageFrame>(emitted.Packet);
        Assert.Equal(CameraInputImageBlock.ImageOutputPortId, emitted.OutputPortId);
        Assert.Equal(ImagePixelFormat.Rgb24, frame.PixelFormat);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6 }, frame.PixelsInterleaved.ToArray());
        Assert.Equal(DateTimeOffset.UnixEpoch, frame.Timestamp);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"Trigger\":false}")]
    [InlineData("{\"trigger\":true}")]
    public async Task OnPacketAsync_NonMatchingTrigger_DoesNotCapture(string payloadJson)
    {
        var context = new RecordingNodeContext();
        var node = CreateNode("camera-non-trigger");
        await node.InitializeAsync(context, CancellationToken.None);

        await node.OnPacketAsync(
            CameraInputImageBlock.TriggerInputPortId,
            CreateMessage(payloadJson),
            CancellationToken.None);

        Assert.Empty(context.EmittedPackets);
    }

    [Fact]
    public async Task StartAsync_ManualTriggerNonce_EmitsImageOnce()
    {
        var context = new RecordingNodeContext();
        var node = CreateNode("camera-manual", manualTriggerNonce: 1);
        await node.InitializeAsync(context, CancellationToken.None);

        await node.StartAsync(CancellationToken.None);
        await node.StartAsync(CancellationToken.None);

        Assert.Single(context.EmittedPackets);
    }

    [Fact]
    public void Settings_RejectNegativeDeviceIndex()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CameraInputImageSettings.FromJson("{\"deviceIndex\":-1}"));
    }

    [Fact]
    public void Settings_ReadsContinuousCaptureAndManualWhiteBalance()
    {
        var settings = CameraInputImageSettings.FromJson(
            "{\"continuousCapture\":true,\"fps\":24,\"isWhiteBalanceAuto\":false,\"whiteBalanceTemperature\":5200}");

        Assert.True(settings.ContinuousCapture);
        Assert.Equal(24, settings.FramesPerSecond);
        Assert.False(settings.IsWhiteBalanceAuto);
        Assert.Equal(5200, settings.WhiteBalanceTemperature);
    }

    [Fact]
    public async Task OnPacketAsync_Passes4kRequestToCamera()
    {
        var context = new RecordingNodeContext();
        var node = new CameraInputImageNode(
            "camera-4k",
            CameraInputImageSettings.Default with { RequestedWidth = 3840, RequestedHeight = 2160 },
            (_, width, height, _, _, _) => new ImageFrame("test", width, height, ImagePixelFormat.Rgb24, new byte[width * height * 3], 0));
        await node.InitializeAsync(context, CancellationToken.None);

        await node.OnPacketAsync(CameraInputImageBlock.TriggerInputPortId, CreateMessage("{\"Trigger\":true}"), CancellationToken.None);

        var frame = Assert.IsType<ImageFrame>(Assert.Single(context.EmittedPackets).Packet);
        Assert.Equal(3840, frame.Width);
        Assert.Equal(2160, frame.Height);
    }

    private static CameraInputImageNode CreateNode(string nodeId, long manualTriggerNonce = 0)
    {
        return new CameraInputImageNode(
            nodeId,
            CameraInputImageSettings.Default with { ManualTriggerNonce = manualTriggerNonce },
            (_, _, _, _, _, _) => new ImageFrame("test", 2, 1, ImagePixelFormat.Rgb24, new byte[] { 1, 2, 3, 4, 5, 6 }, 0),
            () => DateTimeOffset.UnixEpoch);
    }

    private static JsonMessage CreateMessage(string payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson);
        return new JsonMessage("test", document.RootElement.Clone(), DateTimeOffset.UnixEpoch);
    }
}