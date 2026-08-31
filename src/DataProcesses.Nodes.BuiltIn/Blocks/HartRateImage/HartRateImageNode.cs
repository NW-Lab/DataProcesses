using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.HartRateImage;

/// <summary>
/// Estimates heart rate from sequential image frames using a simplified rPPG POS pipeline.
/// </summary>
public sealed class HartRateImageNode : INode
{
    private readonly HartRateImageSettings settings;
    private readonly List<ColorSample> samples = [];

    private INodeContext? _context;
    private long? _lastImageTimestampNanoseconds;

    public HartRateImageNode(HartRateImageSettings? settings = null)
    {
        this.settings = settings ?? HartRateImageSettings.Default;
        this.settings.Validate();
    }

    public NodeDefinition Definition => HartRateImageBlock.Definition;

    public ValueTask InitializeAsync(
        INodeContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _context = context ?? throw new ArgumentNullException(nameof(context));
        return ValueTask.CompletedTask;
    }

    public async ValueTask OnPacketAsync(
        string inputPortId,
        IDataPacket packet,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPortId);
        ArgumentNullException.ThrowIfNull(packet);
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(inputPortId, HartRateImageBlock.InputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        if (packet is not ImageFrame image)
        {
            throw new ArgumentException("HartRateImage accepts Image input only.", nameof(packet));
        }

        var context = _context
            ?? throw new InvalidOperationException("The node must be initialized before it receives packets.");

        var timestampNanoseconds = ResolveTimestampNanoseconds(image);
        var samplePeriodNanoseconds = ResolveOutputSamplePeriodNanoseconds(timestampNanoseconds);
        var averageColor = CalculateCentralRegionAverage(image, settings.RegionScale);
        samples.Add(new ColorSample(timestampNanoseconds, averageColor.Red, averageColor.Green, averageColor.Blue));
        TrimWindow(timestampNanoseconds);

        var heartRateBpm = EstimateHeartRateBpm();
        var outputFrame = new FastStreamFrame(
            timestampNanoseconds,
            samplePeriodNanoseconds,
            ["heart-rate-bpm"],
            [new double[] { heartRateBpm }.AsMemory()],
            image.SequenceNumber);

        _lastImageTimestampNanoseconds = timestampNanoseconds;
        await context.EmitAsync(HartRateImageBlock.OutputPortId, outputFrame, cancellationToken);
    }

    public ValueTask StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        samples.Clear();
        _lastImageTimestampNanoseconds = null;
        return ValueTask.CompletedTask;
    }

    private long ResolveTimestampNanoseconds(ImageFrame image)
    {
        if (image.Timestamp is { } timestamp)
        {
            return (timestamp - DateTimeOffset.UnixEpoch).Ticks * 100L;
        }

        var defaultPeriod = DefaultFramePeriodNanoseconds;
        return _lastImageTimestampNanoseconds is null
            ? image.SequenceNumber * defaultPeriod
            : _lastImageTimestampNanoseconds.Value + defaultPeriod;
    }

    private long ResolveOutputSamplePeriodNanoseconds(long timestampNanoseconds)
    {
        if (_lastImageTimestampNanoseconds is { } previousTimestamp)
        {
            var interval = timestampNanoseconds - previousTimestamp;
            if (interval > 0)
            {
                return interval;
            }
        }

        return DefaultFramePeriodNanoseconds;
    }

    private long DefaultFramePeriodNanoseconds => (long)Math.Round(1_000_000_000.0 / settings.DefaultFrameRateHertz);

    private void TrimWindow(long latestTimestampNanoseconds)
    {
        var oldestAllowedTimestamp = latestTimestampNanoseconds - (long)(settings.WindowSeconds * 1_000_000_000.0);
        var removeCount = 0;

        while (removeCount < samples.Count && samples[removeCount].TimestampNanoseconds < oldestAllowedTimestamp)
        {
            removeCount++;
        }

        if (removeCount > 0)
        {
            samples.RemoveRange(0, removeCount);
        }
    }

    private double EstimateHeartRateBpm()
    {
        if (samples.Count < settings.MinimumSampleCount)
        {
            return double.NaN;
        }

        var firstTimestamp = samples[0].TimestampNanoseconds;
        var lastTimestamp = samples[^1].TimestampNanoseconds;
        if (lastTimestamp <= firstTimestamp)
        {
            return double.NaN;
        }

        var sampleRateHertz = (samples.Count - 1) * 1_000_000_000.0 / (lastTimestamp - firstTimestamp);
        if (!double.IsFinite(sampleRateHertz) || sampleRateHertz <= 0.0)
        {
            return double.NaN;
        }

        var pulseSignal = BuildPosSignal();
        RemoveMean(pulseSignal);
        return FindDominantHeartRate(pulseSignal, sampleRateHertz);
    }

    private double[] BuildPosSignal()
    {
        var meanRed = 0.0;
        var meanGreen = 0.0;
        var meanBlue = 0.0;

        for (var index = 0; index < samples.Count; index++)
        {
            meanRed += samples[index].Red;
            meanGreen += samples[index].Green;
            meanBlue += samples[index].Blue;
        }

        meanRed /= samples.Count;
        meanGreen /= samples.Count;
        meanBlue /= samples.Count;

        if (meanRed <= 0.0 || meanGreen <= 0.0 || meanBlue <= 0.0)
        {
            return new double[samples.Count];
        }

        var x = new double[samples.Count];
        var y = new double[samples.Count];

        for (var index = 0; index < samples.Count; index++)
        {
            var normalizedRed = (samples[index].Red / meanRed) - 1.0;
            var normalizedGreen = (samples[index].Green / meanGreen) - 1.0;
            var normalizedBlue = (samples[index].Blue / meanBlue) - 1.0;
            x[index] = normalizedGreen - normalizedBlue;
            y[index] = normalizedGreen + normalizedBlue - (2.0 * normalizedRed);
        }

        var xDeviation = CalculateStandardDeviation(x);
        var yDeviation = CalculateStandardDeviation(y);
        var alpha = yDeviation > 0.0 ? xDeviation / yDeviation : 0.0;
        var signal = new double[samples.Count];

        for (var index = 0; index < signal.Length; index++)
        {
            signal[index] = x[index] + (alpha * y[index]);
        }

        return signal;
    }

    private double FindDominantHeartRate(double[] signal, double sampleRateHertz)
    {
        var minFrequency = settings.MinimumHeartRateBpm / 60.0;
        var maxFrequency = settings.MaximumHeartRateBpm / 60.0;
        var minBin = Math.Max(1, (int)Math.Ceiling(minFrequency * signal.Length / sampleRateHertz));
        var maxBin = Math.Min(signal.Length / 2, (int)Math.Floor(maxFrequency * signal.Length / sampleRateHertz));

        if (maxBin < minBin)
        {
            return double.NaN;
        }

        var bestBin = 0;
        var bestPower = 0.0;

        for (var bin = minBin; bin <= maxBin; bin++)
        {
            var power = CalculatePower(signal, bin);
            if (power > bestPower)
            {
                bestPower = power;
                bestBin = bin;
            }
        }

        if (bestBin == 0 || bestPower <= 0.0)
        {
            return double.NaN;
        }

        return bestBin * sampleRateHertz / signal.Length * 60.0;
    }

    private static RgbAverage CalculateCentralRegionAverage(ImageFrame image, double regionScale)
    {
        var left = (int)Math.Floor(image.Width * (1.0 - regionScale) / 2.0);
        var top = (int)Math.Floor(image.Height * (1.0 - regionScale) / 2.0);
        var right = Math.Max(left + 1, (int)Math.Ceiling(image.Width * (1.0 + regionScale) / 2.0));
        var bottom = Math.Max(top + 1, (int)Math.Ceiling(image.Height * (1.0 + regionScale) / 2.0));
        right = Math.Min(right, image.Width);
        bottom = Math.Min(bottom, image.Height);

        var pixels = image.PixelsInterleaved.Span;
        var stride = image.Width * GetBytesPerPixel(image.PixelFormat);
        var red = 0.0;
        var green = 0.0;
        var blue = 0.0;
        var count = 0;

        for (var y = top; y < bottom; y++)
        {
            for (var x = left; x < right; x++)
            {
                var offset = (y * stride) + (x * GetBytesPerPixel(image.PixelFormat));
                switch (image.PixelFormat)
                {
                    case ImagePixelFormat.Gray8:
                        red += pixels[offset];
                        green += pixels[offset];
                        blue += pixels[offset];
                        break;
                    case ImagePixelFormat.Rgb24:
                    case ImagePixelFormat.Rgba32:
                        red += pixels[offset];
                        green += pixels[offset + 1];
                        blue += pixels[offset + 2];
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(image), "Unsupported image pixel format.");
                }

                count++;
            }
        }

        if (count == 0)
        {
            return new RgbAverage(0.0, 0.0, 0.0);
        }

        return new RgbAverage(red / count, green / count, blue / count);
    }

    private static int GetBytesPerPixel(ImagePixelFormat pixelFormat)
    {
        return pixelFormat switch
        {
            ImagePixelFormat.Gray8 => 1,
            ImagePixelFormat.Rgb24 => 3,
            ImagePixelFormat.Rgba32 => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(pixelFormat)),
        };
    }

    private static double CalculateStandardDeviation(double[] values)
    {
        var mean = 0.0;
        for (var index = 0; index < values.Length; index++)
        {
            mean += values[index];
        }

        mean /= values.Length;
        var variance = 0.0;
        for (var index = 0; index < values.Length; index++)
        {
            var centered = values[index] - mean;
            variance += centered * centered;
        }

        return Math.Sqrt(variance / values.Length);
    }

    private static void RemoveMean(double[] values)
    {
        var mean = 0.0;
        for (var index = 0; index < values.Length; index++)
        {
            mean += values[index];
        }

        mean /= values.Length;

        for (var index = 0; index < values.Length; index++)
        {
            values[index] -= mean;
        }
    }

    private static double CalculatePower(double[] signal, int bin)
    {
        var real = 0.0;
        var imaginary = 0.0;

        for (var sampleIndex = 0; sampleIndex < signal.Length; sampleIndex++)
        {
            var window = 0.5 - (0.5 * Math.Cos(2.0 * Math.PI * sampleIndex / (signal.Length - 1)));
            var angle = 2.0 * Math.PI * bin * sampleIndex / signal.Length;
            var sample = signal[sampleIndex] * window;
            real += sample * Math.Cos(angle);
            imaginary -= sample * Math.Sin(angle);
        }

        return (real * real) + (imaginary * imaginary);
    }

    private readonly record struct ColorSample(
        long TimestampNanoseconds,
        double Red,
        double Green,
        double Blue);

    private readonly record struct RgbAverage(double Red, double Green, double Blue);
}