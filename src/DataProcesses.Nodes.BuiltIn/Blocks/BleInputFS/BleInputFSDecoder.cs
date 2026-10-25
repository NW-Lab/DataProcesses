using System.Buffers.Binary;
using System.Globalization;
using System.Text;

using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.BleInputFS;

internal sealed class BleInputFSDecoder
{
    private const int SamplesPerFrame = 256;
    private const int MaximumLineBytes = 4096;
    private readonly BleInputFSSettings settings;
    private readonly Func<DateTimeOffset> getTimestamp;
    private readonly string[] channelNames;
    private readonly byte[] pending;
    private readonly int valueBytes;
    private int pendingCount;
    private double[][]? samples;
    private int sampleCount;
    private long sequence;
    private long? hostOrigin;
    private long segmentOrigin;
    private long segmentIndex;
    private long frameOrigin;
    private long? firstCsvTime;
    private long? previousCsvTime;
    private long lineNumber;

    public BleInputFSDecoder(BleInputFSSettings settings, Func<DateTimeOffset> getTimestamp)
    {
        this.settings = settings;
        this.getTimestamp = getTimestamp;
        channelNames = new string[settings.ChannelCount];
        for (var channel = 0; channel < channelNames.Length; channel++)
        {
            channelNames[channel] = $"data{channel + 1}";
        }

        valueBytes = settings.NumericType switch
        {
            BleInputFSNumericType.Int16 => 2,
            BleInputFSNumericType.Int32 or BleInputFSNumericType.Float32 => 4,
            BleInputFSNumericType.Float64 => 8,
            _ => throw new ArgumentOutOfRangeException(nameof(settings)),
        };
        pending = new byte[settings.Format == BleInputFSFormat.Csv ? MaximumLineBytes : valueBytes * settings.ChannelCount];
    }

    public IReadOnlyList<FastStreamFrame> Decode(ReadOnlySpan<byte> notification, CancellationToken cancellationToken)
    {
        var frames = new List<FastStreamFrame>();
        if (settings.Format == BleInputFSFormat.Binary)
        {
            while (!notification.IsEmpty)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var take = Math.Min(notification.Length, pending.Length - pendingCount);
                notification[..take].CopyTo(pending.AsSpan(pendingCount));
                pendingCount += take;
                notification = notification[take..];
                if (pendingCount == pending.Length)
                {
                    BeginSample();
                    for (var channel = 0; channel < settings.ChannelCount; channel++)
                    {
                        samples![channel][sampleCount] = ReadNumber(pending.AsSpan(channel * valueBytes, valueBytes));
                    }

                    pendingCount = 0;
                    EndSample(frames);
                }
            }
        }
        else
        {
            foreach (var value in notification)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (value == (byte)'\n')
                {
                    lineNumber++;
                    ParseCsv(frames);
                    pendingCount = 0;
                }
                else
                {
                    if (pendingCount == pending.Length)
                    {
                        throw new InvalidDataException($"BleInputFS CSV line exceeds {MaximumLineBytes} bytes.");
                    }

                    if (value > 127)
                    {
                        throw new InvalidDataException("BleInputFS CSV requires ASCII numeric fields.");
                    }

                    pending[pendingCount++] = value;
                }
            }
        }

        Flush(frames);
        return frames;
    }

    public void Complete()
    {
        if (pendingCount != 0)
        {
            throw new InvalidDataException("BleInputFS ended with an incomplete CSV line or binary channel sample.");
        }
    }

    private void ParseCsv(List<FastStreamFrame> frames)
    {
        var line = Encoding.ASCII.GetString(pending, 0, pendingCount).AsSpan().Trim();
        if (line.IsEmpty)
        {
            return;
        }

        var comma = line.IndexOf(',');
        if (comma < 0)
        {
            throw CsvError();
        }

        var milliseconds = ParseFinite(line[..comma]);
        if (milliseconds < 0 || milliseconds > long.MaxValue / 1_000_000.0)
        {
            throw CsvError();
        }

        var time = checked((long)Math.Round(milliseconds * 1_000_000, MidpointRounding.AwayFromZero));
        if (previousCsvTime.HasValue && time <= previousCsvTime.Value)
        {
            throw new InvalidDataException($"BleInputFS CSV line {lineNumber} requires strictly increasing millis.");
        }

        firstCsvTime ??= time;
        hostOrigin ??= checked(getTimestamp().ToUnixTimeMilliseconds() * 1_000_000);
        var timestamp = checked(hostOrigin.Value + time - firstCsvTime.Value);
        if (!previousCsvTime.HasValue || Math.Abs(timestamp - NextTimestamp()) > 1)
        {
            Flush(frames);
            segmentOrigin = timestamp;
            segmentIndex = 0;
        }

        BeginSample();
        line = line[(comma + 1)..];
        for (var channel = 0; channel < settings.ChannelCount; channel++)
        {
            comma = line.IndexOf(',');
            if ((channel < settings.ChannelCount - 1) != (comma >= 0))
            {
                throw CsvError();
            }

            samples![channel][sampleCount] = ParseFinite(comma >= 0 ? line[..comma] : line);
            if (comma >= 0)
            {
                line = line[(comma + 1)..];
            }
        }

        previousCsvTime = time;
        EndSample(frames);
    }

    private void BeginSample()
    {
        if (!hostOrigin.HasValue)
        {
            hostOrigin = checked(getTimestamp().ToUnixTimeMilliseconds() * 1_000_000);
            segmentOrigin = hostOrigin.Value;
        }

        if (samples is null)
        {
            samples = new double[settings.ChannelCount][];
            for (var channel = 0; channel < samples.Length; channel++)
            {
                samples[channel] = new double[SamplesPerFrame];
            }

            frameOrigin = NextTimestamp();
        }
    }

    private long NextTimestamp() => checked(segmentOrigin + segmentIndex * 1_000_000_000L / settings.SampleRateHertz);

    private void EndSample(List<FastStreamFrame> frames)
    {
        sampleCount++;
        segmentIndex++;
        if (sampleCount == SamplesPerFrame)
        {
            Flush(frames);
        }
    }

    private void Flush(List<FastStreamFrame> frames)
    {
        if (sampleCount == 0)
        {
            return;
        }

        var channels = new ReadOnlyMemory<double>[settings.ChannelCount];
        for (var channel = 0; channel < channels.Length; channel++)
        {
            channels[channel] = samples![channel].AsMemory(0, sampleCount);
        }

        frames.Add(new FastStreamFrame(frameOrigin,
            Math.Max(1, 1_000_000_000L / settings.SampleRateHertz), channelNames, channels,
            sequence++, settings.SampleRateHertz));
        samples = null;
        sampleCount = 0;
    }

    private double ReadNumber(ReadOnlySpan<byte> value)
    {
        var little = settings.ByteOrder == BleInputFSByteOrder.LittleEndian;
        double number = settings.NumericType switch
        {
            BleInputFSNumericType.Int16 => little ? BinaryPrimitives.ReadInt16LittleEndian(value) : BinaryPrimitives.ReadInt16BigEndian(value),
            BleInputFSNumericType.Int32 => little ? BinaryPrimitives.ReadInt32LittleEndian(value) : BinaryPrimitives.ReadInt32BigEndian(value),
            BleInputFSNumericType.Float32 => little ? BinaryPrimitives.ReadSingleLittleEndian(value) : BinaryPrimitives.ReadSingleBigEndian(value),
            BleInputFSNumericType.Float64 => little ? BinaryPrimitives.ReadDoubleLittleEndian(value) : BinaryPrimitives.ReadDoubleBigEndian(value),
            _ => throw new InvalidOperationException("Unsupported BleInputFS numeric type."),
        };
        if (!double.IsFinite(number))
        {
            throw new InvalidDataException("BleInputFS binary samples must be finite.");
        }

        return number;
    }

    private double ParseFinite(ReadOnlySpan<char> value)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || !double.IsFinite(number))
        {
            throw CsvError();
        }

        return number;
    }

    private InvalidDataException CsvError() => new($"BleInputFS CSV line {lineNumber} must contain nonnegative millis and exactly {settings.ChannelCount} finite channel values.");
}
