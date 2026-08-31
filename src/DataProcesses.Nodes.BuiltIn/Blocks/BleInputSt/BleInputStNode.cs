using System.Globalization;
using System.Text;

using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.BleInputSt;

public sealed class BleInputStNode : INode
{
    private readonly BleInputStSettings settings;
    private readonly Func<DateTimeOffset> getTimestamp;
    private readonly Func<BleInputStSettings, CancellationToken, IAsyncEnumerable<ReadOnlyMemory<byte>>> notificationSourceFactory;
    private INodeContext? context;

    public BleInputStNode(
        BleInputStSettings settings,
        Func<DateTimeOffset>? getTimestamp = null,
        Func<BleInputStSettings, CancellationToken, IAsyncEnumerable<ReadOnlyMemory<byte>>>? notificationSourceFactory = null)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.settings.Validate();
        this.getTimestamp = getTimestamp ?? (() => DateTimeOffset.UtcNow);
        this.notificationSourceFactory = notificationSourceFactory ?? BleInputStNotificationSource.ReadNotificationsAsync;
    }

    public NodeDefinition Definition => BleInputStBlock.Definition;

    public ValueTask InitializeAsync(INodeContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        return ValueTask.CompletedTask;
    }

    public ValueTask OnPacketAsync(string inputPortId, IDataPacket packet, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new InvalidOperationException("BLE Input ST does not define input ports.");
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var initializedContext = context
            ?? throw new InvalidOperationException("The node must be initialized before it starts.");

        var pendingText = new StringBuilder();
        long sequenceNumber = 0;
        double? firstMillis = null;
        double? previousMillis = null;
        long? streamBaseUnixNanoseconds = null;
        var lineNumber = 0;

        using var sourceCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await using var enumerator = notificationSourceFactory(settings, sourceCancellationTokenSource.Token)
            .GetAsyncEnumerator(sourceCancellationTokenSource.Token);

        while (true)
        {
            bool hasNotification;
            try
            {
                hasNotification = await enumerator.MoveNextAsync()
                    .AsTask()
                    .WaitAsync(TimeSpan.FromMilliseconds(settings.TimeoutMilliseconds), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                await sourceCancellationTokenSource.CancelAsync().ConfigureAwait(false);
                break;
            }

            if (!hasNotification)
            {
                break;
            }

            foreach (var line in ExtractCompleteLines(pendingText, Encoding.UTF8.GetString(enumerator.Current.Span)))
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var row = ParseRow(line, lineNumber);
                if (previousMillis.HasValue && row.Millis < previousMillis.Value)
                {
                    throw new InvalidDataException($"BLE CSV line {lineNumber} has a millis value earlier than the previous row.");
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
                    BleInputStBlock.StreamPortId,
                    new FastStreamFrame(
                        StartTimeUnixNanoseconds: streamBaseUnixNanoseconds.Value + ToNanoseconds(row.Millis),
                        SamplePeriodNanoseconds: samplePeriodNanoseconds,
                        ChannelNames: CreateChannelNames(),
                        Samples: samples,
                        SequenceNumber: sequenceNumber++),
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public ValueTask StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    private static IEnumerable<string> ExtractCompleteLines(StringBuilder pendingText, string notificationText)
    {
        pendingText.Append(notificationText);

        while (true)
        {
            var lineEndIndex = FindLineEndIndex(pendingText);
            if (lineEndIndex < 0)
            {
                yield break;
            }

            var line = pendingText.ToString(0, lineEndIndex).TrimEnd('\r');
            pendingText.Remove(0, lineEndIndex + 1);
            yield return line;
        }
    }

    private static int FindLineEndIndex(StringBuilder text)
    {
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] == '\n')
            {
                return index;
            }
        }

        return -1;
    }

    private BleRow ParseRow(string line, int lineNumber)
    {
        var columns = line.Split(',', StringSplitOptions.TrimEntries);
        if (columns.Length != settings.ChannelCount + 1)
        {
            throw new InvalidDataException(
                $"BLE CSV line {lineNumber} must contain millis and exactly {settings.ChannelCount} channel values.");
        }

        if (!double.TryParse(columns[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var millis)
            || !double.IsFinite(millis))
        {
            throw new InvalidDataException($"BLE CSV line {lineNumber} has invalid millis value '{columns[0]}'.");
        }

        var values = new double[settings.ChannelCount];
        for (var index = 0; index < values.Length; index++)
        {
            if (!double.TryParse(columns[index + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                || !double.IsFinite(value))
            {
                throw new InvalidDataException($"BLE CSV line {lineNumber} has an invalid data value at channel {index + 1}.");
            }

            values[index] = value;
        }

        return new BleRow(millis, values);
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

    private sealed record BleRow(double Millis, double[] Values);
}