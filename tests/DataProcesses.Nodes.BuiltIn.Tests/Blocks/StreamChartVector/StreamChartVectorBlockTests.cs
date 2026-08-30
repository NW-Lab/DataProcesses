using DataProcesses.Nodes.BuiltIn.Blocks.StreamChartVector;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.StreamChartVector;

public sealed class StreamChartVectorBlockTests
{
    [Fact]
    public void Definition_UsesOneNumericVectorInput()
    {
        var port = Assert.Single(StreamChartVectorBlock.Definition.Ports);

        Assert.Equal(StreamChartVectorBlock.InputPortId, port.Id);
        Assert.Equal(PortDirection.Input, port.Direction);
        Assert.Equal(PortDataKind.FastStream, port.DataKind);
        Assert.Equal(PortDataSchema.NumericVector1D, port.DataSchema);
        Assert.DoesNotContain(StreamChartVectorBlock.Definition.Ports, static candidate => candidate.Direction == PortDirection.Output);
    }

    [Fact]
    public void Settings_DefaultToFiveSecondJetWindow()
    {
        var settings = StreamChartVectorSettings.Default;

        Assert.Equal(StreamChartVectorColorMap.Jet, settings.ColorMap);
        Assert.True(settings.AutoScale);
        Assert.True(settings.Interpolate);
        Assert.Equal(5_000, settings.TimeSpanMilliseconds);
    }

    [Fact]
    public void Settings_FromJson_ReadsEveryField()
    {
        var settings = StreamChartVectorSettings.FromJson(
            """
            {
              "colorMap": "viridis",
              "autoScale": false,
              "minValue": -2,
              "maxValue": 8,
              "interpolate": false,
              "timeSpanMillis": 2500
            }
            """);

        Assert.Equal(StreamChartVectorColorMap.Viridis, settings.ColorMap);
        Assert.False(settings.AutoScale);
        Assert.Equal(-2, settings.MinimumValue);
        Assert.Equal(8, settings.MaximumValue);
        Assert.False(settings.Interpolate);
        Assert.Equal(2_500, settings.TimeSpanMilliseconds);
    }

    [Fact]
    public void Settings_FromJson_RejectsOutOfRangeTimeSpan()
    {
        Assert.Throws<ArgumentOutOfRangeException>(static () =>
            StreamChartVectorSettings.FromJson("""{ "timeSpanMillis": 10 }"""));
    }

    [Fact]
    public async Task OnPacketAsync_TracksVisibleWindow()
    {
        var node = new StreamChartVectorNode(StreamChartVectorSettings.Default with { TimeSpanMilliseconds = 1_000 });
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);

        for (var index = 0; index < 5; index++)
        {
            await node.OnPacketAsync(
                StreamChartVectorBlock.InputPortId,
                CreateVector(index, milliseconds: index * 100),
                CancellationToken.None);
        }

        var snapshot = Assert.IsType<StreamChartVectorSnapshot>(node.LatestSnapshot);
        Assert.Equal("vector", snapshot.Name);
        Assert.Equal(5, snapshot.ColumnCount);
        Assert.Equal(4, snapshot.RowCount);
        Assert.Equal(400, snapshot.LatestMilliseconds);
        Assert.Equal(1_000, snapshot.TimeSpanMilliseconds);
    }

    [Fact]
    public async Task OnPacketAsync_DropsSamplesOutsideTimeWindow()
    {
        var node = new StreamChartVectorNode(StreamChartVectorSettings.Default with { TimeSpanMilliseconds = 200 });
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);

        for (var index = 0; index < 10; index++)
        {
            await node.OnPacketAsync(
                StreamChartVectorBlock.InputPortId,
                CreateVector(index, milliseconds: index * 100),
                CancellationToken.None);
        }

        var snapshot = Assert.IsType<StreamChartVectorSnapshot>(node.LatestSnapshot);
        Assert.Equal(3, snapshot.ColumnCount);
    }

    [Fact]
    public async Task OnPacketAsync_RejectsNonVectorPackets()
    {
        var node = new StreamChartVectorNode(StreamChartVectorSettings.Default);
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        var payload = System.Text.Json.JsonSerializer.SerializeToElement(new { value = 1 });
        var message = new JsonMessage("test", payload, DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await node.OnPacketAsync(StreamChartVectorBlock.InputPortId, message, CancellationToken.None));
    }

    [Fact]
    public void Render_ProducesRgb24BufferWithOneRowPerVectorIndex()
    {
        var history = new StreamChartVectorHistory();
        history.Append(0, [0, 0.5, 1], 1_000);
        history.Append(500, [0, 0.5, 1], 1_000);

        var image = history.Render(StreamChartVectorSettings.Default, pixelWidth: 16);

        Assert.Equal(16, image.Width);
        Assert.Equal(3, image.Height);
        Assert.Equal(16 * 3 * 3, image.PixelsRgb24.Length);
        Assert.Equal(0, image.MinimumValue);
        Assert.Equal(1, image.MaximumValue);
    }

    [Fact]
    public void Render_UsesFixedRangeWhenAutoScaleIsDisabled()
    {
        var history = new StreamChartVectorHistory();
        history.Append(0, [10, 20], 1_000);

        var image = history.Render(
            StreamChartVectorSettings.Default with { AutoScale = false, MinimumValue = 0, MaximumValue = 100 },
            pixelWidth: 4);

        Assert.Equal(0, image.MinimumValue);
        Assert.Equal(100, image.MaximumValue);
    }

    [Fact]
    public void Render_DrawsVectorIndexZeroAtTheBottom()
    {
        var history = new StreamChartVectorHistory();
        history.Append(0, [0, 1], 1_000);

        var settings = StreamChartVectorSettings.Default with
        {
            AutoScale = false,
            MinimumValue = 0,
            MaximumValue = 1,
            ColorMap = StreamChartVectorColorMap.Grayscale,
        };
        var image = history.Render(settings, pixelWidth: 1);
        var pixels = image.PixelsRgb24.Span;

        Assert.Equal(255, pixels[0]);
        Assert.Equal(0, pixels[3]);
    }

    [Fact]
    public void Render_HoldsPreviousSampleWhenInterpolationIsDisabled()
    {
        var history = new StreamChartVectorHistory();
        history.Append(0, [0], 1_000);
        history.Append(1_000, [1], 1_000);

        var settings = StreamChartVectorSettings.Default with
        {
            AutoScale = false,
            MinimumValue = 0,
            MaximumValue = 1,
            ColorMap = StreamChartVectorColorMap.Grayscale,
            Interpolate = false,
        };
        var image = history.Render(settings, pixelWidth: 10);
        var pixels = image.PixelsRgb24.Span;

        Assert.Equal(0, pixels[0]);
        Assert.Equal(0, pixels[(image.Width - 2) * 3]);
    }

    private static NumericVectorFrame CreateVector(int index, double milliseconds)
    {
        return new NumericVectorFrame(
            Name: "vector",
            Values: new double[] { index, index + 1, index + 2, index + 3 }.AsMemory(),
            SequenceNumber: index,
            Timestamp: DateTimeOffset.UnixEpoch.AddMilliseconds(milliseconds));
    }
}
