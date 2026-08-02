using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Runtime.CompilerServices;

using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.CsvInput;

public sealed class CsvInputNode : INode
{
    private readonly CsvInputSettings settings;
    private readonly Func<DateTimeOffset> getTimestamp;
    private readonly Func<TimeSpan, CancellationToken, Task> delayAsync;
    private readonly Func<CsvInputSettings, CancellationToken, IAsyncEnumerable<string>> lineSourceFactory;

    private INodeContext? context;

    public CsvInputNode(
        CsvInputSettings settings,
        Func<DateTimeOffset>? getTimestamp = null,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null,
        Func<CsvInputSettings, CancellationToken, IAsyncEnumerable<string>>? lineSourceFactory = null)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.settings.Validate();
        this.getTimestamp = getTimestamp ?? (() => DateTimeOffset.UtcNow);
        this.delayAsync = delayAsync ?? DefaultDelayAsync;
        this.lineSourceFactory = lineSourceFactory ?? ReadLinesAsync;
    }

    public NodeDefinition Definition => CsvInputBlock.Definition;

    public ValueTask InitializeAsync(INodeContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        return ValueTask.CompletedTask;
    }

    public ValueTask OnPacketAsync(string inputPortId, IDataPacket packet, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new InvalidOperationException("CSV Input does not define input ports.");
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var initializedContext = context
            ?? throw new InvalidOperationException("The node must be initialized before it starts.");

        var sequenceByPort = new long[settings.OutputCount];
        var previousMillis = new double?[settings.OutputCount];

        double? firstMillis = null;
        long? streamBaseUnixNanoseconds = null;
        double? previousPlaybackMillis = null;
        var lineNumber = 0;

        await foreach (var rawLine in lineSourceFactory(settings, cancellationToken)
                           .WithCancellation(cancellationToken)
                           .ConfigureAwait(false))
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            if (settings.HasHeaderRow && lineNumber == 1)
            {
                continue;
            }

            var row = ParseRow(rawLine, lineNumber);

            if (settings.SourceType == CsvInputSourceType.File
                && settings.FilePlaybackMode == CsvFilePlaybackMode.Millis
                && previousPlaybackMillis.HasValue)
            {
                var deltaMillis = row.Millis - previousPlaybackMillis.Value;
                if (deltaMillis > 0)
                {
                    await delayAsync(TimeSpan.FromMilliseconds(deltaMillis), cancellationToken).ConfigureAwait(false);
                }
            }

            firstMillis ??= row.Millis;
            previousPlaybackMillis = row.Millis;

            streamBaseUnixNanoseconds ??= getTimestamp().ToUnixTimeMilliseconds() * 1_000_000L
                                          - ToNanoseconds(firstMillis.Value);

            for (var channelIndex = 0; channelIndex < settings.OutputCount; channelIndex++)
            {
                var millis = row.Millis;
                var value = row.Values[channelIndex];

                var samplePeriodNanoseconds = previousMillis[channelIndex].HasValue
                    ? Math.Max(1L, ToNanoseconds(millis - previousMillis[channelIndex]!.Value))
                    : 1_000_000L;
                previousMillis[channelIndex] = millis;

                var frame = new FastStreamFrame(
                    StartTimeUnixNanoseconds: streamBaseUnixNanoseconds.Value + ToNanoseconds(millis),
                    SamplePeriodNanoseconds: samplePeriodNanoseconds,
                    ChannelNames: ["millis", "value"],
                    Samples:
                    [
                        new[] { millis }.AsMemory(),
                        new[] { value }.AsMemory(),
                    ],
                    SequenceNumber: sequenceByPort[channelIndex]++);

                await initializedContext.EmitAsync(
                    CsvInputBlock.GetStreamPortId(channelIndex + 1),
                    frame,
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public ValueTask StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    private async IAsyncEnumerable<string> ReadLinesAsync(
        CsvInputSettings inputSettings,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (inputSettings.SourceType == CsvInputSourceType.File)
        {
            if (string.IsNullOrWhiteSpace(inputSettings.FilePath))
            {
                throw new InvalidDataException("CSV file path is required when sourceType is file.");
            }

            if (!File.Exists(inputSettings.FilePath))
            {
                throw new FileNotFoundException($"CSV file was not found: {inputSettings.FilePath}", inputSettings.FilePath);
            }

            using var reader = new StreamReader(inputSettings.FilePath);
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line is null)
                {
                    yield break;
                }

                yield return line;
            }
        }

        using var serialPort = new SerialPort(inputSettings.ComPortName, inputSettings.BaudRate)
        {
            NewLine = "\n",
            ReadTimeout = 250,
        };
        serialPort.Open();

        while (!cancellationToken.IsCancellationRequested)
        {
            string? line = null;
            try
            {
                line = serialPort.ReadLine();
            }
            catch (TimeoutException)
            {
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                yield return line;
            }
        }
    }

    private CsvRow ParseRow(string line, int lineNumber)
    {
        var columns = line.Split(',', StringSplitOptions.TrimEntries);
        if (columns.Length < settings.OutputCount + 1)
        {
            throw new InvalidDataException(
                $"CSV line {lineNumber} must contain millis and {settings.OutputCount} channel values.");
        }

        if (!double.TryParse(columns[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var millis)
            || !double.IsFinite(millis))
        {
            throw new InvalidDataException($"CSV line {lineNumber} has invalid millis value '{columns[0]}'.");
        }

        var values = new double[settings.OutputCount];
        for (var index = 0; index < settings.OutputCount; index++)
        {
            var token = columns[index + 1];
            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                || !double.IsFinite(value))
            {
                throw new InvalidDataException(
                    $"CSV line {lineNumber} has invalid channel value '{token}' at CH{index + 1}.");
            }

            values[index] = value;
        }

        return new CsvRow(millis, values);
    }

    private static long ToNanoseconds(double milliseconds)
    {
        return (long)Math.Round(milliseconds * 1_000_000.0, MidpointRounding.AwayFromZero);
    }

    private static Task DefaultDelayAsync(TimeSpan duration, CancellationToken cancellationToken)
    {
        return Task.Delay(duration, cancellationToken);
    }

    private sealed record CsvRow(double Millis, IReadOnlyList<double> Values);
}
