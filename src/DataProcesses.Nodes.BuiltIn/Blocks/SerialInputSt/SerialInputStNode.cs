using System.Globalization;
using System.IO.Ports;
using System.Runtime.CompilerServices;

using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.SerialInputSt;

public sealed class SerialInputStNode : INode
{
    private readonly SerialInputStSettings settings;
    private readonly Func<DateTimeOffset> getTimestamp;
    private readonly Func<SerialInputStSettings, CancellationToken, IAsyncEnumerable<string>> lineSourceFactory;
    private INodeContext? context;

    public SerialInputStNode(
        SerialInputStSettings settings,
        Func<DateTimeOffset>? getTimestamp = null,
        Func<SerialInputStSettings, CancellationToken, IAsyncEnumerable<string>>? lineSourceFactory = null)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.settings.Validate();
        this.getTimestamp = getTimestamp ?? (() => DateTimeOffset.UtcNow);
        this.lineSourceFactory = lineSourceFactory ?? ReadLinesAsync;
    }

    public NodeDefinition Definition => SerialInputStBlock.Definition;

    public ValueTask InitializeAsync(INodeContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        return ValueTask.CompletedTask;
    }

    public ValueTask OnPacketAsync(string inputPortId, IDataPacket packet, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new InvalidOperationException("Serial Input ST does not define input ports.");
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var initializedContext = context
            ?? throw new InvalidOperationException("The node must be initialized before it starts.");

        long sequenceNumber = 0;
        double? firstMillis = null;
        double? previousMillis = null;
        long? streamBaseUnixNanoseconds = null;
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

            var row = ParseRow(rawLine, lineNumber);
            if (previousMillis.HasValue && row.Millis < previousMillis.Value)
            {
                throw new InvalidDataException($"Serial CSV line {lineNumber} has a millis value earlier than the previous row.");
            }

            firstMillis ??= row.Millis;
            streamBaseUnixNanoseconds ??= getTimestamp().ToUnixTimeMilliseconds() * 1_000_000L
                                          - ToNanoseconds(firstMillis.Value);

            var samplePeriodNanoseconds = previousMillis.HasValue
                ? Math.Max(1L, ToNanoseconds(row.Millis - previousMillis.Value))
                : 1_000_000L;
            previousMillis = row.Millis;

            var samples = new ReadOnlyMemory<double>[settings.ChannelCount];
            for (var index = 0; index < row.Values.Length; index++)
            {
                samples[index] = new[] { row.Values[index] }.AsMemory();
            }

            await initializedContext.EmitAsync(
                SerialInputStBlock.StreamPortId,
                new FastStreamFrame(
                    StartTimeUnixNanoseconds: streamBaseUnixNanoseconds.Value + ToNanoseconds(row.Millis),
                    SamplePeriodNanoseconds: samplePeriodNanoseconds,
                    ChannelNames: CreateChannelNames(),
                    Samples: samples,
                    SequenceNumber: sequenceNumber++),
                cancellationToken).ConfigureAwait(false);
        }
    }

    public ValueTask StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    private async IAsyncEnumerable<string> ReadLinesAsync(
        SerialInputStSettings inputSettings,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
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

    private SerialRow ParseRow(string line, int lineNumber)
    {
        var columns = line.Split(',', StringSplitOptions.TrimEntries);
        if (columns.Length != settings.ChannelCount + 1)
        {
            throw new InvalidDataException(
                $"Serial CSV line {lineNumber} must contain millis and exactly {settings.ChannelCount} channel values.");
        }

        if (!double.TryParse(columns[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var millis)
            || !double.IsFinite(millis))
        {
            throw new InvalidDataException($"Serial CSV line {lineNumber} has invalid millis value '{columns[0]}'.");
        }

        var values = new double[settings.ChannelCount];
        for (var index = 0; index < values.Length; index++)
        {
            if (!double.TryParse(columns[index + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                || !double.IsFinite(value))
            {
                throw new InvalidDataException($"Serial CSV line {lineNumber} has an invalid data value at channel {index + 1}.");
            }

            values[index] = value;
        }

        return new SerialRow(millis, values);
    }

    private IReadOnlyList<string> CreateChannelNames()
    {
        var channelNames = new string[settings.ChannelCount];
        for (var index = 0; index < channelNames.Length; index++)
        {
            channelNames[index] = $"data{index + 1}";
        }

        return channelNames;
    }

    private static long ToNanoseconds(double milliseconds)
    {
        return (long)Math.Round(milliseconds * 1_000_000.0, MidpointRounding.AwayFromZero);
    }

    private sealed record SerialRow(double Millis, double[] Values);
}