using DataProcesses.Nodes.BuiltIn.Blocks.BreathImage;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.BreathImage;

public sealed class BreathImageBlockTests
{
    [Fact]
    public void Definition_UsesImageInputAndBreathRateStreamOutput()
    {
        var ports = BreathImageBlock.Definition.Ports;

        Assert.Collection(
            ports,
            input =>
            {
                Assert.Equal(BreathImageBlock.InputPortId, input.Id);
                Assert.Equal(PortDirection.Input, input.Direction);
                Assert.Equal(PortDataKind.FastStream, input.DataKind);
                Assert.Equal(PortDataSchema.Image2D, input.DataSchema);
            },
            output =>
            {
                Assert.Equal(BreathImageBlock.OutputPortId, output.Id);
                Assert.Equal(PortDirection.Output, output.Direction);
                Assert.Equal(PortDataKind.FastStream, output.DataKind);
                Assert.Equal(PortDataSchema.TimeSeries1D, output.DataSchema);
            });
    }

    [Fact]
    public void Settings_FromJson_ReadsRespirationBand()
    {
        var settings = BreathImageSettings.FromJson(
            "{\"regionScale\":1.0,\"minimumSampleCount\":32,\"windowSeconds\":8.0,\"minimumBreathRateBpm\":8.0,\"maximumBreathRateBpm\":24.0}");

        Assert.Equal(1.0, settings.RegionScale);
        Assert.Equal(32, settings.MinimumSampleCount);
        Assert.Equal(8.0, settings.WindowSeconds);
        Assert.Equal(8.0, settings.MinimumBreathRateBpm);
        Assert.Equal(24.0, settings.MaximumBreathRateBpm);
    }

    [Fact]
    public async Task OnPacketAsync_EstimatesBreathRateFromCgRoiVariation()
    {
        var context = new RecordingNodeContext();
        var node = new BreathImageNode(new BreathImageSettings(
            RegionScale: 1.0,
            MinimumSampleCount: 90,
            WindowSeconds: 10.0,
            MinimumBreathRateBpm: 6.0,
            MaximumBreathRateBpm: 30.0,
            DefaultFrameRateHertz: 30.0));
        await node.InitializeAsync(context, CancellationToken.None);

        const int frameRate = 30;
        const int frameCount = 300;
        const double targetBreathRateBpm = 12.0;
        var frequencyHertz = targetBreathRateBpm / 60.0;

        for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
        {
            var seconds = frameIndex / (double)frameRate;
            var green = (byte)Math.Round(100.0 + (20.0 * Math.Sin(2.0 * Math.PI * frequencyHertz * seconds)));
            var image = CreateRgbImage(green, frameIndex, frameRate);

            await node.OnPacketAsync(BreathImageBlock.InputPortId, image, CancellationToken.None);
        }

        Assert.Equal(frameCount, context.EmittedPackets.Count);
        var output = Assert.IsType<FastStreamFrame>(context.EmittedPackets[^1].Packet);
        Assert.Equal(BreathImageBlock.OutputPortId, context.EmittedPackets[^1].OutputPortId);
        Assert.Equal(new[] { "breath-rate-brpm" }, output.ChannelNames);
        Assert.Equal(frameCount - 1, output.SequenceNumber);
        Assert.InRange(output.Samples[0].Span[0], 11.0, 13.0);
    }

    [Fact]
    public async Task OnPacketAsync_EmitsNaNUntilEnoughImageSamplesExist()
    {
        var context = new RecordingNodeContext();
        var node = new BreathImageNode(new BreathImageSettings(MinimumSampleCount: 16));
        await node.InitializeAsync(context, CancellationToken.None);

        await node.OnPacketAsync(BreathImageBlock.InputPortId, CreateRgbImage(green: 100, sequenceNumber: 0, frameRate: 30), CancellationToken.None);

        var output = Assert.IsType<FastStreamFrame>(Assert.Single(context.EmittedPackets).Packet);
        Assert.True(double.IsNaN(output.Samples[0].Span[0]));
    }

    [Fact]
    public async Task OnPacketAsync_RejectsNonImagePackets()
    {
        var context = new RecordingNodeContext();
        var node = new BreathImageNode();
        await node.InitializeAsync(context, CancellationToken.None);
        var vector = new NumericVectorFrame(
            Name: "values",
            Values: new double[] { 1.0 }.AsMemory(),
            SequenceNumber: 1);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await node.OnPacketAsync(BreathImageBlock.InputPortId, vector, CancellationToken.None));
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