using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.BreathImage;

/// <summary>
/// Estimates respiratory rate from sequential image frames using a central ROI YCgCo Cg signal.
/// </summary>
public sealed class BreathImageNode : INode
{
    private readonly BreathImageSettings settings;
    private readonly List<CgSample> samples = [];

    private INodeContext? _context;
    private long? _lastImageTimestampNanoseconds;

    public BreathImageNode(BreathImageSettings? settings = null)
    {
        this.settings = settings ?? BreathImageSettings.Default;
        this.settings.Validate();
    }

    public NodeDefinition Definition => BreathImageBlock.Definition;

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

        if (!string.Equals(inputPortId, BreathImageBlock.InputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        if (packet is not ImageFrame image)
        {
            throw new ArgumentException("BreathImage accepts Image input only.", nameof(packet));
        }

        var context = _context
            ?? throw new InvalidOperationException("The node must be initialized before it receives packets.");

        var timestampNanoseconds = ResolveTimestampNanoseconds(image);
        var samplePeriodNanoseconds = ResolveOutputSamplePeriodNanoseconds(timestampNanoseconds);
        var cg = CalculateCentralRegionCgAverage(image, settings.RegionScale);
        samples.Add(new CgSample(timestampNanoseconds, cg));
        TrimWindow(timestampNanoseconds);

        var breathRateBpm = EstimateBreathRateBpm();
        var outputFrame = new FastStreamFrame(
            timestampNanoseconds,
            samplePeriodNanoseconds,
            ["breath-rate-brpm"],
            [new double[] { breathRateBpm }.AsMemory()],
            image.SequenceNumber);

        _lastImageTimestampNanoseconds = timestampNanoseconds;
        await context.EmitAsync(BreathImageBlock.OutputPortId, outputFrame, cancellationToken);
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

    private double EstimateBreathRateBpm()
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

        var signal = BuildDetrendedCgSignal();
        return FindDominantBreathRate(signal, sampleRateHertz);
    }

    private double[] BuildDetrendedCgSignal()
    {
        var signal = new double[samples.Count];
        var firstValue = samples[0].Cg;
        var lastValue = samples[^1].Cg;
        var denominator = Math.Max(1, samples.Count - 1);

        for (var index = 0; index < samples.Count; index++)
        {
            var trend = firstValue + ((lastValue - firstValue) * index / denominator);
            signal[index] = samples[index].Cg - trend;
        }

        RemoveMean(signal);
        return signal;
    }

    private double FindDominantBreathRate(double[] signal, double sampleRateHertz)
    {
        var minFrequency = settings.MinimumBreathRateBpm / 60.0;
        var maxFrequency = settings.MaximumBreathRateBpm / 60.0;
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

    private static double CalculateCentralRegionCgAverage(ImageFrame image, double regionScale)
    {
        var left = (int)Math.Floor(image.Width * (1.0 - regionScale) / 2.0);
        var top = (int)Math.Floor(image.Height * (1.0 - regionScale) / 2.0);
        var right = Math.Max(left + 1, (int)Math.Ceiling(image.Width * (1.0 + regionScale) / 2.0));
        var bottom = Math.Max(top + 1, (int)Math.Ceiling(image.Height * (1.0 + regionScale) / 2.0));
        right = Math.Min(right, image.Width);
        bottom = Math.Min(bottom, image.Height);

        var pixels = image.PixelsInterleaved.Span;
        var bytesPerPixel = GetBytesPerPixel(image.PixelFormat);
        var stride = image.Width * bytesPerPixel;
        var cg = 0.0;
        var count = 0;

        for (var y = top; y < bottom; y++)
        {
            for (var x = left; x < right; x++)
            {
                var offset = (y * stride) + (x * bytesPerPixel);
                switch (image.PixelFormat)
                {
                    case ImagePixelFormat.Gray8:
                        break;
                    case ImagePixelFormat.Rgb24:
                    case ImagePixelFormat.Rgba32:
                        var red = pixels[offset];
                        var green = pixels[offset + 1];
                        var blue = pixels[offset + 2];
                        cg += green - ((red + blue) / 2.0);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(image), "Unsupported image pixel format.");
                }

                count++;
            }
        }

        return count == 0 ? 0.0 : cg / count;
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

    private readonly record struct CgSample(long TimestampNanoseconds, double Cg);
}