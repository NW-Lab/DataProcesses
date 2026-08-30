using System.IO.Compression;
using DataProcesses.Nodes.BuiltIn.Blocks.StreamChartSt;
using DataProcesses.Plugin.Abstractions;
using Xunit;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.StreamChartSt;

public sealed class StreamChartStBlockTests
{
    [Fact]
    public void Definition_DeclaresCorrectMetadataAndPorts()
    {
        var definition = StreamChartStBlock.Definition;
        Assert.Equal(StreamChartStBlock.TypeId, definition.TypeId);
        Assert.Equal("StreamChartSt", definition.DisplayName);
        Assert.Equal("Output", definition.Category);
        Assert.Equal(NodeType.Output, definition.NodeType);
        Assert.Equal(4, definition.Ports.Count);

        for (var i = 1; i <= 4; i++)
        {
            var port = definition.Ports[i - 1];
            Assert.Equal($"stream-{i}", port.Id);
            Assert.Equal(PortDirection.Input, port.Direction);
            Assert.Equal(PortDataKind.FastStream, port.DataKind);
            Assert.Equal(PortDataSchema.TimeSeries1D, port.DataSchema);
            Assert.False(port.IsRequired);
        }

        Assert.NotNull(definition.DashboardWidget);
        Assert.True(definition.DashboardWidget.IsVisibleByDefault);
        Assert.Equal(3, definition.DashboardWidget.GridWidth);
        Assert.Equal(3, definition.DashboardWidget.GridHeight);
    }

    [Fact]
    public void Settings_DefaultValuesMatchSpecification()
    {
        var settings = StreamChartStSettings.Default;
        Assert.Equal(StreamChartTimeAlignment.Independent, settings.TimeAlignmentMode);
        Assert.Equal(5000.0, settings.TimeSpanMilliseconds);
        Assert.Equal("CH1", settings.Channel1Name);
        Assert.Equal("CH2", settings.Channel2Name);
        Assert.Equal("CH3", settings.Channel3Name);
        Assert.Equal("CH4", settings.Channel4Name);
    }

    [Fact]
    public void Settings_FromJson_ParsesSettingsCorrectly()
    {
        var json = """
        {
            "timeAlignmentMode": "alignToFirstStream",
            "timeSpanMillis": 10000.0,
            "channel1Name": "Voltage",
            "channel2Name": "Current",
            "channel3Name": "Temp",
            "channel4Name": "Pressure"
        }
        """;

        var settings = StreamChartStSettings.FromJson(json);
        Assert.Equal(StreamChartTimeAlignment.AlignToFirstStream, settings.TimeAlignmentMode);
        Assert.Equal(10000.0, settings.TimeSpanMilliseconds);
        Assert.Equal("Voltage", settings.Channel1Name);
        Assert.Equal("Current", settings.Channel2Name);
        Assert.Equal("Temp", settings.Channel3Name);
        Assert.Equal("Pressure", settings.Channel4Name);
    }

    [Fact]
    public void Settings_Validate_RejectsInvalidTimeSpan()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new StreamChartStSettings(TimeSpanMilliseconds: 0.5).Validate());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new StreamChartStSettings(TimeSpanMilliseconds: double.NaN).Validate());
    }

    [Fact]
    public void Factory_CreatesConfiguredNode()
    {
        var factory = new StreamChartStNodeFactory();
        var node = (StreamChartStNode)factory.CreateNode("test-node", """{ "timeSpanMillis": 3000, "channel1Name": "Accel" }""");

        Assert.Equal(3000.0, node.Settings.TimeSpanMilliseconds);
        Assert.Equal("Accel", node.Settings.Channel1Name);
    }

    [Fact]
    public async Task Node_ProcessesPacketsAcrossMultipleChannels_IndependentMode()
    {
        var settings = new StreamChartStSettings(
            TimeAlignmentMode: StreamChartTimeAlignment.Independent,
            TimeSpanMilliseconds: 5000.0);

        var node = new StreamChartStNode(settings);
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);

        var frame1 = new FastStreamFrame(
            StartTimeUnixNanoseconds: 1_000_000_000L,
            SamplePeriodNanoseconds: 1_000_000L,
            ChannelNames: ["Signal1"],
            Samples: [new double[] { 1.0, 2.0, 3.0 }],
            SequenceNumber: 1);

        var frame2 = new FastStreamFrame(
            StartTimeUnixNanoseconds: 2_000_000_000L,
            SamplePeriodNanoseconds: 1_000_000L,
            ChannelNames: ["Signal2"],
            Samples: [new double[] { 10.0, 20.0, 30.0 }],
            SequenceNumber: 2);

        await node.OnPacketAsync("stream-1", frame1, CancellationToken.None);
        await node.OnPacketAsync("stream-2", frame2, CancellationToken.None);

        var snapshot = node.LatestSnapshot;
        Assert.NotNull(snapshot);
        Assert.Equal(StreamChartTimeAlignment.Independent, snapshot.TimeAlignmentMode);
        Assert.Equal(5000.0, snapshot.TimeSpanMilliseconds);
        Assert.NotNull(snapshot.Channels[0]);
        Assert.NotNull(snapshot.Channels[1]);
        Assert.Null(snapshot.Channels[2]);
        Assert.Null(snapshot.Channels[3]);

        // Channel 1 millis relative to 1s
        Assert.Equal(0.0, snapshot.Channels[0]!.Millis.Span[0], tolerance: 1e-4);
        // Channel 2 millis relative to 2s (independent start)
        Assert.Equal(0.0, snapshot.Channels[1]!.Millis.Span[0], tolerance: 1e-4);
    }

    [Fact]
    public async Task Node_ProcessesPacketsAcrossMultipleChannels_AlignToFirstStreamMode()
    {
        var settings = new StreamChartStSettings(
            TimeAlignmentMode: StreamChartTimeAlignment.AlignToFirstStream,
            TimeSpanMilliseconds: 5000.0);

        var node = new StreamChartStNode(settings);
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);

        var frame1 = new FastStreamFrame(
            StartTimeUnixNanoseconds: 1_000_000_000L,
            SamplePeriodNanoseconds: 1_000_000L,
            ChannelNames: ["Signal1"],
            Samples: [new double[] { 1.0, 2.0, 3.0 }],
            SequenceNumber: 1);

        var frame2 = new FastStreamFrame(
            StartTimeUnixNanoseconds: 1_500_000_000L,
            SamplePeriodNanoseconds: 1_000_000L,
            ChannelNames: ["Signal2"],
            Samples: [new double[] { 10.0, 20.0, 30.0 }],
            SequenceNumber: 2);

        await node.OnPacketAsync("stream-1", frame1, CancellationToken.None);
        await node.OnPacketAsync("stream-2", frame2, CancellationToken.None);

        var snapshot = node.LatestSnapshot;
        Assert.NotNull(snapshot);
        Assert.Equal(StreamChartTimeAlignment.AlignToFirstStream, snapshot.TimeAlignmentMode);

        // Channel 1 starts at 0ms
        Assert.Equal(0.0, snapshot.Channels[0]!.Millis.Span[0], tolerance: 1e-4);
        // Channel 2 starts at 500ms relative to Channel 1 (1.5s - 1.0s = 500ms)
        Assert.Equal(500.0, snapshot.Channels[1]!.Millis.Span[0], tolerance: 1e-4);
    }

    [Fact]
    public void History_RendersChartImageWithCorrectDimensions()
    {
        var history = new StreamChartStHistory();
        var settings = StreamChartStSettings.Default;

        history.Append(1, 0, 1_000_000, [0.0, 1.0, 0.5, -0.5], settings);
        history.Append(2, 0, 1_000_000, [0.5, -0.2, 0.8, 0.0], settings);

        var image = history.Render(settings, 320, 160);
        Assert.Equal(320, image.Width);
        Assert.Equal(160, image.Height);
        Assert.Equal(320 * 160 * 3, image.PixelsRgb24.Length);
        Assert.True(image.MaximumValue > image.MinimumValue);
    }

    [Fact]
    public void GenerateIconPngIfMissingOrSmall()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "DataProcesses.slnx")))
        {
            current = current.Parent;
        }

        Assert.NotNull(current);
        var projectDir = Path.Combine(current.FullName, "src", "DataProcesses.Nodes.BuiltIn", "Blocks", "StreamChartSt");
        var iconPath = Path.Combine(projectDir, "icon.png");

        const int w = 64;
        const int h = 64;
        var pixels = new byte[w * h * 4];

        void SetPixel(int x, int y, byte r, byte g, byte b, byte a)
        {
            if (x < 0 || x >= w || y < 0 || y >= h) return;
            var idx = (y * w + x) * 4;
            pixels[idx] = r;
            pixels[idx + 1] = g;
            pixels[idx + 2] = b;
            pixels[idx + 3] = a;
        }

        void FillRect(int x0, int y0, int x1, int y1, byte r, byte g, byte b, byte a)
        {
            for (var y = Math.Max(0, y0); y < Math.Min(h, y1); y++)
            {
                for (var x = Math.Max(0, x0); x < Math.Min(w, x1); x++)
                {
                    SetPixel(x, y, r, g, b, a);
                }
            }
        }

        void DrawLine(int x0, int y0, int x1, int y1, byte r, byte g, byte b, byte a, int thickness = 1)
        {
            var dx = Math.Abs(x1 - x0);
            var dy = -Math.Abs(y1 - y0);
            var sx = x0 < x1 ? 1 : -1;
            var sy = y0 < y1 ? 1 : -1;
            var err = dx + dy;
            var cx = x0;
            var cy = y0;
            while (true)
            {
                for (var yy = cy - thickness + 1; yy <= cy + thickness - 1; yy++)
                {
                    for (var xx = cx - thickness + 1; xx <= cx + thickness - 1; xx++)
                    {
                        SetPixel(xx, yy, r, g, b, a);
                    }
                }
                if (cx == x1 && cy == y1) break;
                var e2 = 2 * err;
                if (e2 >= dy) { err += dy; cx += sx; }
                if (e2 <= dx) { err += dx; cy += sy; }
            }
        }

        FillRect(0, 0, w, h, 245, 247, 250, 255);
        FillRect(6, 6, w - 6, h - 6, 30, 41, 59, 255);
        FillRect(8, 8, w - 8, h - 8, 15, 23, 42, 255);

        // Grid
        for (var gx = 18; gx <= 48; gx += 10) DrawLine(gx, 10, gx, 54, 51, 65, 85, 255, 1);
        for (var gy = 20; gy <= 44; gy += 12) DrawLine(10, gy, 54, gy, 51, 65, 85, 255, 1);

        // Axes
        DrawLine(10, 54, 54, 54, 100, 116, 139, 255, 1);
        DrawLine(10, 10, 10, 54, 100, 116, 139, 255, 1);

        // Wave 1 (Cyan)
        var ch1 = new (int, int)[] { (10, 32), (16, 20), (22, 16), (28, 24), (34, 40), (40, 46), (46, 38), (52, 22), (54, 18) };
        for (var i = 0; i < ch1.Length - 1; i++) DrawLine(ch1[i].Item1, ch1[i].Item2, ch1[i + 1].Item1, ch1[i + 1].Item2, 56, 189, 248, 255, 2);

        // Wave 2 (Orange)
        var ch2 = new (int, int)[] { (10, 46), (16, 44), (22, 38), (28, 22), (34, 18), (40, 26), (46, 44), (52, 48), (54, 42) };
        for (var i = 0; i < ch2.Length - 1; i++) DrawLine(ch2[i].Item1, ch2[i].Item2, ch2[i + 1].Item1, ch2[i + 1].Item2, 251, 146, 60, 255, 2);

        // Encode PNG
        var rawData = new byte[h * (1 + w * 4)];
        var rawPos = 0;
        for (var y = 0; y < h; y++)
        {
            rawData[rawPos++] = 0; // filter byte
            Array.Copy(pixels, y * w * 4, rawData, rawPos, w * 4);
            rawPos += w * 4;
        }

        using var compressedStream = new MemoryStream();
        using (var deflate = new DeflateStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
        {
            deflate.Write(rawData, 0, rawData.Length);
        }

        // Adler32
        uint s1 = 1;
        uint s2 = 0;
        foreach (var b in rawData)
        {
            s1 = (s1 + b) % 65521;
            s2 = (s2 + s1) % 65521;
        }
        var adler = (s2 << 16) | s1;

        using var idatPayload = new MemoryStream();
        idatPayload.WriteByte(0x78);
        idatPayload.WriteByte(0x9C);
        var compBytes = compressedStream.ToArray();
        idatPayload.Write(compBytes, 0, compBytes.Length);
        idatPayload.WriteByte((byte)((adler >> 24) & 0xFF));
        idatPayload.WriteByte((byte)((adler >> 16) & 0xFF));
        idatPayload.WriteByte((byte)((adler >> 8) & 0xFF));
        idatPayload.WriteByte((byte)(adler & 0xFF));

        Directory.CreateDirectory(projectDir);
        using (var fs = new FileStream(iconPath, FileMode.Create, FileAccess.Write))
        {
            fs.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]); // PNG header

            void WriteChunk(string type, byte[]? data)
            {
                var len = data?.Length ?? 0;
                fs.WriteByte((byte)((len >> 24) & 0xFF));
                fs.WriteByte((byte)((len >> 16) & 0xFF));
                fs.WriteByte((byte)((len >> 8) & 0xFF));
                fs.WriteByte((byte)(len & 0xFF));

                var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
                fs.Write(typeBytes, 0, 4);

                var crcPayload = new byte[4 + len];
                Array.Copy(typeBytes, 0, crcPayload, 0, 4);
                if (data is not null && len > 0)
                {
                    fs.Write(data, 0, len);
                    Array.Copy(data, 0, crcPayload, 4, len);
                }

                var crc = ComputeCrc32(crcPayload);
                fs.WriteByte((byte)((crc >> 24) & 0xFF));
                fs.WriteByte((byte)((crc >> 16) & 0xFF));
                fs.WriteByte((byte)((crc >> 8) & 0xFF));
                fs.WriteByte((byte)(crc & 0xFF));
            }

            var ihdr = new byte[] { 0, 0, 0, 64, 0, 0, 0, 64, 8, 6, 0, 0, 0 };
            WriteChunk("IHDR", ihdr);
            WriteChunk("IDAT", idatPayload.ToArray());
            WriteChunk("IEND", null);
            fs.Flush();
        }

        Assert.True(File.Exists(iconPath));
        Assert.True(new FileInfo(iconPath).Length > 100);
    }

    private static uint ComputeCrc32(byte[] bytes)
    {
        uint crc = 0xFFFFFFFF;
        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            crc ^= b;
            for (var k = 0; k < 8; k++)
            {
                if ((crc & 1) != 0) crc = (crc >> 1) ^ 0xEDB88320;
                else crc >>= 1;
            }
        }
        return ~crc;
    }
}
