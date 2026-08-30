namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamChartSt;

/// <summary>
/// Rendered time-series chart image produced from <see cref="StreamChartStHistory"/>.
/// </summary>
/// <param name="Width">Image width in pixels.</param>
/// <param name="Height">Image height in pixels.</param>
/// <param name="PixelsRgb24">Interleaved RGB24 bytes in HxWx3 order.</param>
/// <param name="MinimumValue">Lower bound of the Y-axis range.</param>
/// <param name="MaximumValue">Upper bound of the Y-axis range.</param>
public sealed record StreamChartStImage(
    int Width,
    int Height,
    ReadOnlyMemory<byte> PixelsRgb24,
    double MinimumValue,
    double MaximumValue);

/// <summary>
/// Maintains rolling history buffers for up to 4 Fast Stream channels and renders
/// a multi-channel time-series graph image.
/// </summary>
public sealed class StreamChartStHistory
{
    public const int DefaultPixelWidth = 480;
    public const int DefaultPixelHeight = 240;
    public const int MaxPointsPerChannel = 4096;

    private static readonly (byte R, byte G, byte B)[] ChannelColors =
    [
        (56, 189, 248),   // CH1: Sky Blue
        (251, 146, 60),   // CH2: Orange
        (74, 222, 128),   // CH3: Green
        (244, 63, 94),    // CH4: Rose/Red
    ];

    private readonly List<SamplePoint>[] channelPoints = new List<SamplePoint>[StreamChartStBlock.MaxStreamInputs];
    private readonly long?[] channelFirstTimestampNano = new long?[StreamChartStBlock.MaxStreamInputs];
    private long? firstStreamTimestampNano;

    public StreamChartStHistory()
    {
        for (var i = 0; i < channelPoints.Length; i++)
        {
            channelPoints[i] = [];
        }
    }

    private readonly record struct SamplePoint(double Millis, double Value);

    public int GetPointCount(int channelIndex)
    {
        if (channelIndex < 1 || channelIndex > StreamChartStBlock.MaxStreamInputs) return 0;
        return channelPoints[channelIndex - 1].Count;
    }

    public void Clear()
    {
        firstStreamTimestampNano = null;
        for (var i = 0; i < channelPoints.Length; i++)
        {
            channelPoints[i].Clear();
            channelFirstTimestampNano[i] = null;
        }
    }

    /// <summary>
    /// Appends samples for a specified channel from a FastStreamFrame.
    /// </summary>
    public void Append(
        int channelIndex,
        long frameStartTimeNano,
        long samplePeriodNano,
        ReadOnlySpan<double> samples,
        StreamChartStSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (channelIndex < 1 || channelIndex > StreamChartStBlock.MaxStreamInputs)
        {
            throw new ArgumentOutOfRangeException(nameof(channelIndex));
        }

        if (samples.IsEmpty)
        {
            return;
        }

        var zeroIdx = channelIndex - 1;
        if (channelIndex == 1 && firstStreamTimestampNano is null)
        {
            firstStreamTimestampNano = frameStartTimeNano;
        }

        if (channelFirstTimestampNano[zeroIdx] is null)
        {
            channelFirstTimestampNano[zeroIdx] = frameStartTimeNano;
        }

        var baseTimeNano = settings.TimeAlignmentMode == StreamChartTimeAlignment.AlignToFirstStream
            ? (firstStreamTimestampNano ?? frameStartTimeNano)
            : channelFirstTimestampNano[zeroIdx]!.Value;

        var points = channelPoints[zeroIdx];
        var step = Math.Max(1, samples.Length / 512);

        for (var i = 0; i < samples.Length; i += step)
        {
            var sampleTimeNano = frameStartTimeNano + (i * samplePeriodNano);
            var millis = (sampleTimeNano - baseTimeNano) / 1_000_000.0;
            points.Add(new SamplePoint(millis, samples[i]));
        }

        // Trim older than window
        Trim(settings.TimeSpanMilliseconds);
    }

    private void Trim(double timeSpanMillis)
    {
        var maxMillis = GetLatestMilliseconds();
        if (maxMillis is null) return;

        var threshold = maxMillis.Value - timeSpanMillis;
        for (var i = 0; i < channelPoints.Length; i++)
        {
            var points = channelPoints[i];
            if (points.Count == 0) continue;

            var removeCount = 0;
            while (removeCount < points.Count && points[removeCount].Millis < threshold)
            {
                removeCount++;
            }

            if (removeCount > 0 && removeCount < points.Count)
            {
                // Keep one previous point for line continuity
                points.RemoveRange(0, Math.Max(0, removeCount - 1));
            }
            else if (removeCount >= points.Count)
            {
                var last = points[^1];
                points.Clear();
                points.Add(last);
            }

            if (points.Count > MaxPointsPerChannel)
            {
                points.RemoveRange(0, points.Count - MaxPointsPerChannel);
            }
        }
    }

    public double? GetLatestMilliseconds()
    {
        double? max = null;
        for (var i = 0; i < channelPoints.Length; i++)
        {
            var points = channelPoints[i];
            if (points.Count > 0)
            {
                var last = points[^1].Millis;
                if (max is null || last > max.Value)
                {
                    max = last;
                }
            }
        }
        return max;
    }

    /// <summary>
    /// Renders the multi-channel chart to an RGB24 pixel buffer.
    /// </summary>
    public StreamChartStImage Render(
        StreamChartStSettings settings,
        int width = DefaultPixelWidth,
        int height = DefaultPixelHeight)
    {
        ArgumentNullException.ThrowIfNull(settings);
        width = Math.Max(64, width);
        height = Math.Max(64, height);

        var pixels = new byte[width * height * 3];

        // Background: #0F172A (slate-900)
        for (var i = 0; i < width * height; i++)
        {
            pixels[i * 3 + 0] = 15;
            pixels[i * 3 + 1] = 23;
            pixels[i * 3 + 2] = 42;
        }

        var (minY, maxY) = CalculateYRange();
        var ySpan = maxY - minY;
        if (ySpan < 1e-6)
        {
            minY -= 1.0;
            maxY += 1.0;
            ySpan = maxY - minY;
        }

        const int marginLeft = 36;
        const int marginRight = 16;
        const int marginTop = 24;
        const int marginBottom = 28;

        var plotWidth = width - marginLeft - marginRight;
        var plotHeight = height - marginTop - marginBottom;

        if (plotWidth <= 0 || plotHeight <= 0)
        {
            return new StreamChartStImage(width, height, pixels, minY, maxY);
        }

        void SetPixel(int x, int y, byte r, byte g, byte b)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return;
            var idx = (y * width + x) * 3;
            pixels[idx + 0] = r;
            pixels[idx + 1] = g;
            pixels[idx + 2] = b;
        }

        void DrawLine(int x0, int y0, int x1, int y1, byte r, byte g, byte b, int thickness = 1)
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
                        SetPixel(xx, yy, r, g, b);
                    }
                }
                if (cx == x1 && cy == y1) break;
                var e2 = 2 * err;
                if (e2 >= dy) { err += dy; cx += sx; }
                if (e2 <= dx) { err += dx; cy += sy; }
            }
        }

        // Plot Area Background: #1E293B (slate-800)
        for (var py = marginTop; py < marginTop + plotHeight; py++)
        {
            for (var px = marginLeft; px < marginLeft + plotWidth; px++)
            {
                var idx = (py * width + px) * 3;
                pixels[idx + 0] = 30;
                pixels[idx + 1] = 41;
                pixels[idx + 2] = 59;
            }
        }

        // Grid lines (#334155)
        for (var step = 1; step <= 3; step++)
        {
            var gy = marginTop + (step * plotHeight / 4);
            DrawLine(marginLeft, gy, marginLeft + plotWidth - 1, gy, 51, 65, 85, 1);
        }

        for (var step = 1; step <= 3; step++)
        {
            var gx = marginLeft + (step * plotWidth / 4);
            DrawLine(gx, marginTop, gx, marginTop + plotHeight - 1, 51, 65, 85, 1);
        }

        // Border around plot area (#64748B)
        DrawLine(marginLeft, marginTop, marginLeft + plotWidth - 1, marginTop, 100, 116, 139, 1);
        DrawLine(marginLeft, marginTop + plotHeight - 1, marginLeft + plotWidth - 1, marginTop + plotHeight - 1, 100, 116, 139, 1);
        DrawLine(marginLeft, marginTop, marginLeft, marginTop + plotHeight - 1, 100, 116, 139, 1);
        DrawLine(marginLeft + plotWidth - 1, marginTop, marginLeft + plotWidth - 1, marginTop + plotHeight - 1, 100, 116, 139, 1);

        // Draw Channel Traces
        var latestMillis = GetLatestMilliseconds() ?? settings.TimeSpanMilliseconds;
        var startMillis = Math.Max(0, latestMillis - settings.TimeSpanMilliseconds);
        var endMillis = startMillis + settings.TimeSpanMilliseconds;
        var timeSpan = endMillis - startMillis;

        for (var ch = 0; ch < channelPoints.Length; ch++)
        {
            var points = channelPoints[ch];
            if (points.Count == 0) continue;

            var color = ChannelColors[ch % ChannelColors.Length];
            int? prevPx = null;
            int? prevPy = null;

            foreach (var pt in points)
            {
                if (pt.Millis < startMillis - (timeSpan / plotWidth)) continue;

                var normX = (pt.Millis - startMillis) / timeSpan;
                var normY = (pt.Value - minY) / ySpan;

                var px = marginLeft + (int)Math.Round(normX * (plotWidth - 1));
                var py = marginTop + plotHeight - 1 - (int)Math.Round(normY * (plotHeight - 1));

                px = Math.Clamp(px, marginLeft, marginLeft + plotWidth - 1);
                py = Math.Clamp(py, marginTop, marginTop + plotHeight - 1);

                if (prevPx is not null && prevPy is not null)
                {
                    DrawLine(prevPx.Value, prevPy.Value, px, py, color.R, color.G, color.B, 2);
                }

                prevPx = px;
                prevPy = py;
            }
        }

        // Draw Legend / channel indicators at top
        var legendX = marginLeft;
        for (var ch = 0; ch < StreamChartStBlock.MaxStreamInputs; ch++)
        {
            var color = ChannelColors[ch % ChannelColors.Length];
            // Draw a small colored badge
            for (var by = 8; by <= 16; by++)
            {
                for (var bx = legendX; bx <= legendX + 10; bx++)
                {
                    SetPixel(bx, by, color.R, color.G, color.B);
                }
            }
            legendX += 48;
            if (legendX > width - marginRight - 30) break;
        }

        return new StreamChartStImage(width, height, pixels, minY, maxY);
    }

    private (double MinY, double MaxY) CalculateYRange()
    {
        double? min = null;
        double? max = null;

        for (var i = 0; i < channelPoints.Length; i++)
        {
            var points = channelPoints[i];
            foreach (var pt in points)
            {
                if (min is null || pt.Value < min.Value) min = pt.Value;
                if (max is null || pt.Value > max.Value) max = pt.Value;
            }
        }

        if (min is null || max is null)
        {
            return (-1.0, 1.0);
        }

        var padding = (max.Value - min.Value) * 0.1;
        if (padding < 1e-3) padding = 0.5;

        return (min.Value - padding, max.Value + padding);
    }
}
