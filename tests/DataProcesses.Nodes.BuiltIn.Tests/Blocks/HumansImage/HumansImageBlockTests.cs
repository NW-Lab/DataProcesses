using DataProcesses.Nodes.BuiltIn.Blocks.HumansImage;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.HumansImage;

public sealed class HumansImageBlockTests
{
    [Fact]
    public void Definition_UsesImageInputAndCountStreamOutput()
    {
        var ports = HumansImageBlock.Definition.Ports;

        Assert.Collection(
            ports,
            input =>
            {
                Assert.Equal(HumansImageBlock.InputPortId, input.Id);
                Assert.Equal(PortDirection.Input, input.Direction);
                Assert.Equal(PortDataKind.FastStream, input.DataKind);
                Assert.Equal(PortDataSchema.Image2D, input.DataSchema);
            },
            output =>
            {
                Assert.Equal(HumansImageBlock.OutputPortId, output.Id);
                Assert.Equal(PortDirection.Output, output.Direction);
                Assert.Equal(PortDataKind.FastStream, output.DataKind);
                Assert.Equal(PortDataSchema.TimeSeries1D, output.DataSchema);
            });
    }

    [Fact]
    public void Settings_FromJson_ReadsFaceCandidateThresholds()
    {
        var settings = HumansImageSettings.FromJson(
            "{\"minimumFacePixelCount\":9,\"minimumFaceWidthPixels\":3,\"minimumFaceHeightPixels\":3,\"minimumSkinRatio\":0.75}");

        Assert.Equal(9, settings.MinimumFacePixelCount);
        Assert.Equal(3, settings.MinimumFaceWidthPixels);
        Assert.Equal(3, settings.MinimumFaceHeightPixels);
        Assert.Equal(0.75, settings.MinimumSkinRatio);
    }

    [Fact]
    public async Task OnPacketAsync_CountsSeparateFaceColoredRegions()
    {
        var context = new RecordingNodeContext();
        var node = new HumansImageNode(new HumansImageSettings(
            MinimumFacePixelCount: 9,
            MinimumFaceWidthPixels: 3,
            MinimumFaceHeightPixels: 3,
            MinimumSkinRatio: 0.75));
        await node.InitializeAsync(context, CancellationToken.None);

        var image = CreateRgbImageWithFaces(16, 8, [(1, 1, 4, 4), (10, 2, 4, 4)], sequenceNumber: 7);

        await node.OnPacketAsync(HumansImageBlock.InputPortId, image, CancellationToken.None);

        var output = Assert.IsType<FastStreamFrame>(Assert.Single(context.EmittedPackets).Packet);
        Assert.Equal(HumansImageBlock.OutputPortId, context.EmittedPackets[0].OutputPortId);
        Assert.Equal(new[] { "humans-count" }, output.ChannelNames);
        Assert.Equal(7, output.SequenceNumber);
        Assert.Equal(2.0, output.Samples[0].Span[0]);
    }

    [Fact]
    public async Task OnPacketAsync_IgnoresSmallSkinColoredNoise()
    {
        var context = new RecordingNodeContext();
        var node = new HumansImageNode(new HumansImageSettings(MinimumFacePixelCount: 9));
        await node.InitializeAsync(context, CancellationToken.None);

        var image = CreateRgbImageWithFaces(8, 8, [(1, 1, 2, 2)], sequenceNumber: 1);
        await node.OnPacketAsync(HumansImageBlock.InputPortId, image, CancellationToken.None);

        var output = Assert.IsType<FastStreamFrame>(Assert.Single(context.EmittedPackets).Packet);
        Assert.Equal(0.0, output.Samples[0].Span[0]);
    }

    [Fact]
    public async Task OnPacketAsync_RejectsNonImagePackets()
    {
        var context = new RecordingNodeContext();
        var node = new HumansImageNode();
        await node.InitializeAsync(context, CancellationToken.None);
        var vector = new NumericVectorFrame(
            Name: "values",
            Values: new double[] { 1.0 }.AsMemory(),
            SequenceNumber: 1);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await node.OnPacketAsync(HumansImageBlock.InputPortId, vector, CancellationToken.None));
    }

    private static ImageFrame CreateRgbImageWithFaces(
        int width,
        int height,
        (int Left, int Top, int Width, int Height)[] faceRegions,
        long sequenceNumber)
    {
        var pixels = new byte[width * height * 3];
        for (var offset = 0; offset < pixels.Length; offset += 3)
        {
            pixels[offset] = 35;
            pixels[offset + 1] = 48;
            pixels[offset + 2] = 68;
        }

        foreach (var face in faceRegions)
        {
            for (var y = face.Top; y < face.Top + face.Height; y++)
            {
                for (var x = face.Left; x < face.Left + face.Width; x++)
                {
                    var offset = ((y * width) + x) * 3;
                    pixels[offset] = 220;
                    pixels[offset + 1] = 170;
                    pixels[offset + 2] = 135;
                }
            }
        }

        return new ImageFrame(
            name: "camera",
            width: width,
            height: height,
            pixelFormat: ImagePixelFormat.Rgb24,
            pixelsInterleaved: pixels.AsMemory(),
            sequenceNumber: sequenceNumber,
            timestamp: DateTimeOffset.UnixEpoch.AddMilliseconds(sequenceNumber));
    }
}