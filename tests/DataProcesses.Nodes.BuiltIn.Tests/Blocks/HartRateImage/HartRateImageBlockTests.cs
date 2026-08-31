using DataProcesses.Nodes.BuiltIn.Blocks.HartRateImage;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.HartRateImage;

public sealed class HartRateImageBlockTests
{
    [Fact]
    public void Definition_UsesImageInputAndHeartRateStreamOutput()
    {
        var ports = HartRateImageBlock.Definition.Ports;

        Assert.Collection(
            ports,
            input =>
            {
                Assert.Equal(HartRateImageBlock.InputPortId, input.Id);
                Assert.Equal(PortDirection.Input, input.Direction);
                Assert.Equal(PortDataKind.FastStream, input.DataKind);
                Assert.Equal(PortDataSchema.Image2D, input.DataSchema);
            },
            output =>
            {
                Assert.Equal(HartRateImageBlock.OutputPortId, output.Id);
                Assert.Equal(PortDirection.Output, output.Direction);
                Assert.Equal(PortDataKind.FastStream, output.DataKind);
                Assert.Equal(PortDataSchema.TimeSeries1D, output.DataSchema);
            });
    }

    [Fact]
    public void Settings_FromJson_ReadsAnalysisWindow()
    {
        var settings = HartRateImageSettings.FromJson("{\"regionScale\":1.0,\"minimumSampleCount\":32,\"windowSeconds\":4.0}");

        Assert.Equal(1.0, settings.RegionScale);
        Assert.Equal(32, settings.MinimumSampleCount);
        Assert.Equal(4.0, settings.WindowSeconds);
    }

    [Fact]
    public async Task OnPacketAsync_EstimatesHeartRateFromGreenRoiVariation()
    {
        var context = new RecordingNodeContext();
        var node = new HartRateImageNode(new HartRateImageSettings(
            RegionScale: 1.0,
            MinimumSampleCount: 32,
            WindowSeconds: 4.0,
            MinimumHeartRateBpm: 50.0,
            MaximumHeartRateBpm: 120.0,
            DefaultFrameRateHertz: 30.0));
        await node.InitializeAsync(context, CancellationToken.None);

        const int frameRate = 30;
        const int frameCount = 120;
        const double targetHeartRateBpm = 75.0;
        var frequencyHertz = targetHeartRateBpm / 60.0;

        for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
        {
            var seconds = frameIndex / (double)frameRate;
            var green = (byte)Math.Round(100.0 + (20.0 * Math.Sin(2.0 * Math.PI * frequencyHertz * seconds)));
            var image = CreateRgbImage(green, frameIndex, frameRate);

            await node.OnPacketAsync(HartRateImageBlock.InputPortId, image, CancellationToken.None);
        }

        Assert.Equal(frameCount, context.EmittedPackets.Count);
        var output = Assert.IsType<FastStreamFrame>(context.EmittedPackets[^1].Packet);
        Assert.Equal(HartRateImageBlock.OutputPortId, context.EmittedPackets[^1].OutputPortId);
        Assert.Equal(new[] { "heart-rate-bpm" }, output.ChannelNames);
        Assert.Equal(frameCount - 1, output.SequenceNumber);
        Assert.InRange(output.Samples[0].Span[0], 74.0, 76.0);
    }

    [Fact]
    public async Task OnPacketAsync_EmitsNaNUntilEnoughImageSamplesExist()
    {
        var context = new RecordingNodeContext();
        var node = new HartRateImageNode(new HartRateImageSettings(MinimumSampleCount: 16));
        await node.InitializeAsync(context, CancellationToken.None);

        await node.OnPacketAsync(HartRateImageBlock.InputPortId, CreateRgbImage(green: 100, sequenceNumber: 0, frameRate: 30), CancellationToken.None);

        var output = Assert.IsType<FastStreamFrame>(Assert.Single(context.EmittedPackets).Packet);
        Assert.True(double.IsNaN(output.Samples[0].Span[0]));
    }

    [Fact]
    public async Task OnPacketAsync_RejectsNonImagePackets()
    {
        var context = new RecordingNodeContext();
        var node = new HartRateImageNode();
        await node.InitializeAsync(context, CancellationToken.None);
        var vector = new NumericVectorFrame(
            Name: "values",
            Values: new double[] { 1.0 }.AsMemory(),
            SequenceNumber: 1);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await node.OnPacketAsync(HartRateImageBlock.InputPortId, vector, CancellationToken.None));
    }

    private static ImageFrame CreateRgbImage(byte green, long sequenceNumber, int frameRate)
    {
        var pixels = new byte[4 * 4 * 3];
        for (var offset = 0; offset < pixels.Length; offset += 3)
        {
            pixels[offset] = 80;
            pixels[offset + 1] = green;
            pixels[offset + 2] = 60;
        }

        return new ImageFrame(
            name: "camera",
            width: 4,
            height: 4,
            pixelFormat: ImagePixelFormat.Rgb24,
            pixelsInterleaved: pixels.AsMemory(),
            sequenceNumber: sequenceNumber,
            timestamp: DateTimeOffset.UnixEpoch.AddTicks(sequenceNumber * TimeSpan.TicksPerSecond / frameRate));
    }
}