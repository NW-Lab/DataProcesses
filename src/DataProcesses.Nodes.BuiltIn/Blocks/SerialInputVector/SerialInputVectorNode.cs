using System.Globalization;
using System.IO.Ports;
using System.Runtime.CompilerServices;

using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.SerialInputVector;

public sealed class SerialInputVectorNode : INode
{
    private readonly SerialInputVectorSettings settings;
    private readonly Func<DateTimeOffset> getTimestamp;
    private readonly Func<SerialInputVectorSettings, CancellationToken, IAsyncEnumerable<string>> lineSourceFactory;
    private INodeContext? context;

    public SerialInputVectorNode(
        SerialInputVectorSettings settings,
        Func<DateTimeOffset>? getTimestamp = null,
        Func<SerialInputVectorSettings, CancellationToken, IAsyncEnumerable<string>>? lineSourceFactory = null)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.settings.Validate();
        this.getTimestamp = getTimestamp ?? (() => DateTimeOffset.UtcNow);
        this.lineSourceFactory = lineSourceFactory ?? ReadLinesAsync;
    }

    public NodeDefinition Definition => SerialInputVectorBlock.Definition;

    public ValueTask InitializeAsync(INodeContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        return ValueTask.CompletedTask;
    }

    public ValueTask OnPacketAsync(string inputPortId, IDataPacket packet, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new InvalidOperationException("Serial Input Vector does not define input ports.");
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var initializedContext = context
            ?? throw new InvalidOperationException("The node must be initialized before it starts.");

        long sequenceNumber = 0;
        double? firstMillis = null;
        double? previousMillis = null;
        DateTimeOffset? streamStartTimestamp = null;
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
            streamStartTimestamp ??= getTimestamp().AddMilliseconds(-firstMillis.Value);
            previousMillis = row.Millis;

            await initializedContext.EmitAsync(
                SerialInputVectorBlock.VectorPortId,
                new NumericVectorFrame(
                    Name: "imu",
                    Values: row.Values.AsMemory(),
                    SequenceNumber: sequenceNumber++,
                    Timestamp: streamStartTimestamp.Value.AddMilliseconds(row.Millis)),
                cancellationToken).ConfigureAwait(false);
        }
    }

    public ValueTask StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    private async IAsyncEnumerable<string> ReadLinesAsync(
        SerialInputVectorSettings inputSettings,
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

    private static SerialVectorRow ParseRow(string line, int lineNumber)
    {
        var columns = line.Split(',', StringSplitOptions.TrimEntries);
        if (columns.Length != 4)
        {
            throw new InvalidDataException($"Serial CSV line {lineNumber} must contain millis, x, y, and z values.");
        }

        var values = new double[4];
        for (var index = 0; index < values.Length; index++)
        {
            if (!double.TryParse(columns[index], NumberStyles.Float, CultureInfo.InvariantCulture, out values[index])
                || !double.IsFinite(values[index]))
            {
                throw new InvalidDataException($"Serial CSV line {lineNumber} has an invalid value at column {index + 1}.");
            }
        }

        return new SerialVectorRow(values[0], [values[1], values[2], values[3]]);
    }

    private sealed record SerialVectorRow(double Millis, double[] Values);
}