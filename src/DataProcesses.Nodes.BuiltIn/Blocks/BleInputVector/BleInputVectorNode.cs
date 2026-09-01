using System.Globalization;
using System.Text;

using DataProcesses.Nodes.BuiltIn.Blocks.BleInputSt;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.BleInputVector;

public sealed class BleInputVectorNode : INode
{
    private readonly BleInputVectorSettings settings;
    private readonly Func<DateTimeOffset> getTimestamp;
    private readonly Func<BleInputVectorSettings, CancellationToken, IAsyncEnumerable<ReadOnlyMemory<byte>>> notificationSourceFactory;
    private INodeContext? context;

    public BleInputVectorNode(
        BleInputVectorSettings settings,
        Func<DateTimeOffset>? getTimestamp = null,
        Func<BleInputVectorSettings, CancellationToken, IAsyncEnumerable<ReadOnlyMemory<byte>>>? notificationSourceFactory = null)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.settings.Validate();
        this.getTimestamp = getTimestamp ?? (() => DateTimeOffset.UtcNow);
        this.notificationSourceFactory = notificationSourceFactory ?? ((inputSettings, cancellationToken) => BleInputStNotificationSource.ReadNotificationsAsync(inputSettings, cancellationToken));
    }

    public NodeDefinition Definition => BleInputVectorBlock.Definition;

    public ValueTask InitializeAsync(INodeContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        return ValueTask.CompletedTask;
    }

    public ValueTask OnPacketAsync(string inputPortId, IDataPacket packet, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new InvalidOperationException("BLE Input Vector does not define input ports.");
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
        DateTimeOffset? streamStartTimestamp = null;
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
                    throw new InvalidDataException($"BLE vector CSV line {lineNumber} has a millis value earlier than the previous row.");
                }

                firstMillis ??= row.Millis;
                streamStartTimestamp ??= getTimestamp().AddMilliseconds(-firstMillis.Value);
                previousMillis = row.Millis;

                await initializedContext.EmitAsync(
                    BleInputVectorBlock.VectorPortId,
                    new NumericVectorFrame(
                        Name: "imu",
                        Values: row.Values.AsMemory(),
                        SequenceNumber: sequenceNumber++,
                        Timestamp: streamStartTimestamp.Value.AddMilliseconds(row.Millis)),
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

    private static BleVectorRow ParseRow(string line, int lineNumber)
    {
        var columns = line.Split(',', StringSplitOptions.TrimEntries);
        if (columns.Length != 4)
        {
            throw new InvalidDataException($"BLE vector CSV line {lineNumber} must contain millis, x, y, and z values.");
        }

        var values = new double[4];
        for (var index = 0; index < values.Length; index++)
        {
            if (!double.TryParse(columns[index], NumberStyles.Float, CultureInfo.InvariantCulture, out values[index])
                || !double.IsFinite(values[index]))
            {
                throw new InvalidDataException($"BLE vector CSV line {lineNumber} has an invalid value at column {index + 1}.");
            }
        }

        return new BleVectorRow(values[0], [values[1], values[2], values[3]]);
    }

    private sealed record BleVectorRow(double Millis, double[] Values);
}