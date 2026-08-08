using DataProcesses.Nodes.BuiltIn.Blocks.ImageOutput;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.ImageOutput;

public sealed class ImageOutputBlockTests
{
    [Fact]
    public void Definition_UsesOneImageInput()
    {
        var port = Assert.Single(ImageOutputBlock.Definition.Ports);

        Assert.Equal(ImageOutputBlock.InputPortId, port.Id);
        Assert.Equal(PortDirection.Input, port.Direction);
        Assert.Equal(PortDataKind.FastStream, port.DataKind);
        Assert.Equal(PortDataSchema.Image2D, port.DataSchema);
    }

    [Fact]
    public async Task OnPacketAsync_CapturesLatestImageSnapshot()
    {
        var node = new ImageOutputNode();
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        var pixels = new byte[]
        {
            255, 0, 0,
            0, 255, 0,
            0, 0, 255,
            255, 255, 255,
        };
        var image = new ImageFrame(
            name: "camera",
            width: 2,
            height: 2,
            pixelFormat: ImagePixelFormat.Rgb24,
            pixelsInterleaved: pixels.AsMemory(),
            sequenceNumber: 9,
            timestamp: DateTimeOffset.UnixEpoch);

        await node.OnPacketAsync(ImageOutputBlock.InputPortId, image, CancellationToken.None);

        var snapshot = Assert.IsType<ImageOutputSnapshot>(node.LatestSnapshot);
        Assert.Equal("camera", snapshot.Name);
        Assert.Equal(2, snapshot.Width);
        Assert.Equal(2, snapshot.Height);
        Assert.Equal(ImagePixelFormat.Rgb24, snapshot.PixelFormat);
        Assert.Equal(12, snapshot.SourceByteLength);
        Assert.Equal(9, snapshot.SequenceNumber);
        Assert.Equal(12, snapshot.PreviewPixels.Length);
        Assert.True(snapshot.PreviewPixels.Equals(pixels.AsMemory()));
    }

    [Fact]
    public async Task OnPacketAsync_TruncatesPreviewWhenImageExceedsMaximum()
    {
        var node = new ImageOutputNode();
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        var pixels = new byte[ImageOutputNode.MaximumPreviewBytes + 32];
        var image = new ImageFrame(
            name: "camera-large",
            width: pixels.Length,
            height: 1,
            pixelFormat: ImagePixelFormat.Gray8,
            pixelsInterleaved: pixels.AsMemory(),
            sequenceNumber: 10);

        await node.OnPacketAsync(ImageOutputBlock.InputPortId, image, CancellationToken.None);

        var snapshot = Assert.IsType<ImageOutputSnapshot>(node.LatestSnapshot);
        Assert.Equal(ImageOutputNode.MaximumPreviewBytes, snapshot.PreviewPixels.Length);
    }

    [Fact]
    public async Task OnPacketAsync_RejectsNonImagePackets()
    {
        var node = new ImageOutputNode();
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        var vector = new NumericVectorFrame(
            Name: "fft",
            Values: new double[] { 1, 2, 3 }.AsMemory(),
            SequenceNumber: 1);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await node.OnPacketAsync(ImageOutputBlock.InputPortId, vector, CancellationToken.None));
    }
}
