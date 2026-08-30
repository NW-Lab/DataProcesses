namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamChartVector;

/// <summary>
/// Rendered waterfall image produced from a <see cref="StreamChartVectorHistory"/>.
/// </summary>
/// <param name="Width">Image width in pixels; one pixel column is one time slice.</param>
/// <param name="Height">Image height in pixels; one pixel row is one vector index.</param>
/// <param name="PixelsRgb24">Interleaved RGB bytes in HxWx3 order.</param>
/// <param name="MinimumValue">Lower bound of the applied intensity range.</param>
/// <param name="MaximumValue">Upper bound of the applied intensity range.</param>
public sealed record StreamChartVectorImage(
    int Width,
    int Height,
    ReadOnlyMemory<byte> PixelsRgb24,
    double MinimumValue,
    double MaximumValue);

/// <summary>
/// Time-bounded ring of numeric vectors rendered as an intensity-over-time chart.
/// The horizontal axis is millisecond time, the vertical axis is the vector index,
/// and color encodes the sample value.
/// </summary>
public sealed class StreamChartVectorHistory
{
    public const int MaximumRows = 512;
    public const int MaximumColumns = 4_096;
    public const int DefaultPixelWidth = 640;

    private readonly List<Column> columns = [];

    private readonly record struct Column(double Milliseconds, double[] Values);

    public int ColumnCount => columns.Count;

    public int RowCount => columns.Count == 0 ? 0 : columns[^1].Values.Length;

    public double? LatestMilliseconds => columns.Count == 0 ? null : columns[^1].Milliseconds;

    /// <summary>
    /// Appends a vector sampled at <paramref name="milliseconds"/> and drops samples
    /// older than the visible window.
    /// </summary>
    public void Append(double milliseconds, ReadOnlySpan<double> values, double windowMilliseconds)
    {
        if (!double.IsFinite(milliseconds))
        {
            throw new ArgumentOutOfRangeException(nameof(milliseconds), milliseconds, "Milliseconds must be finite.");
        }

        if (!double.IsFinite(windowMilliseconds) || windowMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(windowMilliseconds), windowMilliseconds, "Window must be positive and finite.");
        }

        if (columns.Count > 0 && milliseconds < columns[^1].Milliseconds)
        {
            columns.Clear();
        }

        columns.Add(new Column(milliseconds, Downsample(values)));
        Trim(milliseconds, windowMilliseconds);
    }

    public void Clear() => columns.Clear();

    /// <summary>
    /// Renders the visible window ending at the newest appended sample.
    /// </summary>
    public StreamChartVectorImage Render(StreamChartVectorSettings settings, int pixelWidth = DefaultPixelWidth)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (pixelWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelWidth), pixelWidth, "Pixel width must be positive.");
        }

        var rowCount = columns.Count == 0 ? 0 : columns.Max(static column => column.Values.Length);
        if (columns.Count == 0 || rowCount == 0)
        {
            return new StreamChartVectorImage(pixelWidth, 1, new byte[pixelWidth * 3], settings.MinimumValue, settings.MaximumValue);
        }

        var endMilliseconds = columns[^1].Milliseconds;
        var startMilliseconds = endMilliseconds - settings.TimeSpanMilliseconds;
        var (minimum, maximum) = ResolveRange(settings);

        var pixels = new byte[pixelWidth * rowCount * 3];
        var range = maximum - minimum;
        var inverseRange = range > 0 ? 1.0 / range : 0.0;

        for (var x = 0; x < pixelWidth; x++)
        {
            // The rightmost pixel column always samples the newest slice.
            var time = startMilliseconds + ((x + 1.0) * settings.TimeSpanMilliseconds / pixelWidth);
            if (time < columns[0].Milliseconds)
            {
                continue;
            }

            var lowerIndex = FindLowerIndex(time);
            var lower = columns[lowerIndex];
            var upperIndex = Math.Min(lowerIndex + 1, columns.Count - 1);
            var upper = columns[upperIndex];
            var weight = 0.0;

            if (settings.Interpolate && upperIndex != lowerIndex)
            {
                var span = upper.Milliseconds - lower.Milliseconds;
                weight = span > 0 ? Math.Clamp((time - lower.Milliseconds) / span, 0, 1) : 0;
            }

            for (var row = 0; row < rowCount; row++)
            {
                var value = SampleAt(lower.Values, row);
                if (weight > 0)
                {
                    value += (SampleAt(upper.Values, row) - value) * weight;
                }

                var normalized = inverseRange > 0 ? (value - minimum) * inverseRange : 0.0;
                var (red, green, blue) = StreamChartVectorPalette.Map(settings.ColorMap, normalized);

                // Vector index zero is drawn at the bottom of the chart.
                var offset = (((rowCount - 1 - row) * pixelWidth) + x) * 3;
                pixels[offset] = red;
                pixels[offset + 1] = green;
                pixels[offset + 2] = blue;
            }
        }

        return new StreamChartVectorImage(pixelWidth, rowCount, pixels, minimum, maximum);
    }

    private (double Minimum, double Maximum) ResolveRange(StreamChartVectorSettings settings)
    {
        if (!settings.AutoScale)
        {
            return (settings.MinimumValue, settings.MaximumValue);
        }

        var minimum = double.PositiveInfinity;
        var maximum = double.NegativeInfinity;

        foreach (var column in columns)
        {
            foreach (var value in column.Values)
            {
                if (!double.IsFinite(value))
                {
                    continue;
                }

                minimum = Math.Min(minimum, value);
                maximum = Math.Max(maximum, value);
            }
        }

        if (!double.IsFinite(minimum) || !double.IsFinite(maximum) || maximum <= minimum)
        {
            return (0, 1);
        }

        return (minimum, maximum);
    }

    private int FindLowerIndex(double time)
    {
        var low = 0;
        var high = columns.Count - 1;
        var result = 0;

        while (low <= high)
        {
            var middle = low + ((high - low) / 2);
            if (columns[middle].Milliseconds <= time)
            {
                result = middle;
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return result;
    }

    private void Trim(double latestMilliseconds, double windowMilliseconds)
    {
        var oldestVisible = latestMilliseconds - windowMilliseconds;
        var removeCount = 0;

        // Keep one sample before the window so the leftmost pixel column can be filled.
        while (removeCount + 1 < columns.Count && columns[removeCount + 1].Milliseconds <= oldestVisible)
        {
            removeCount++;
        }

        if (removeCount > 0)
        {
            columns.RemoveRange(0, removeCount);
        }

        if (columns.Count > MaximumColumns)
        {
            columns.RemoveRange(0, columns.Count - MaximumColumns);
        }
    }

    private static double SampleAt(double[] values, int row)
    {
        return row < values.Length ? values[row] : 0.0;
    }

    private static double[] Downsample(ReadOnlySpan<double> source)
    {
        if (source.Length <= MaximumRows)
        {
            return source.ToArray();
        }

        var sampled = new double[MaximumRows];
        for (var index = 0; index < sampled.Length; index++)
        {
            var sourceIndex = (int)((long)index * (source.Length - 1) / (sampled.Length - 1));
            sampled[index] = source[sourceIndex];
        }

        return sampled;
    }
}
