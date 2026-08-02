using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.CsvOutput;

public sealed class CsvOutputNode : IConnectionAwareNode
{
    private static readonly ConcurrentDictionary<string, CsvOutputRuntimeState> RuntimeStateByNodeId = new(StringComparer.Ordinal);

    private readonly string nodeId;
    private readonly CsvOutputSettings settings;
    private readonly Func<DateTimeOffset> getTimestamp;
    private bool isInitialized;

    public CsvOutputNode(string nodeId, CsvOutputSettings settings, Func<DateTimeOffset>? getTimestamp = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        this.nodeId = nodeId;
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.settings.Validate();
        this.getTimestamp = getTimestamp ?? (() => DateTimeOffset.UtcNow);
    }

    public NodeDefinition Definition => CsvOutputBlock.Definition;

    public ValueTask InitializeAsync(INodeContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);
        isInitialized = true;
        return ValueTask.CompletedTask;
    }

    public ValueTask OnPacketAsync(
        string inputPortId,
        IDataPacket packet,
        CancellationToken cancellationToken)
    {
        return OnPacketAsync(inputPortId, packet, sourceNodeId: string.Empty, sourcePortId: string.Empty, connectionTag: null, cancellationToken);
    }

    public ValueTask OnPacketAsync(
        string inputPortId,
        IDataPacket packet,
        string sourceNodeId,
        string sourcePortId,
        string? connectionTag,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPortId);
        ArgumentNullException.ThrowIfNull(packet);

        if (!isInitialized)
        {
            throw new InvalidOperationException("The node must be initialized before it receives packets.");
        }

        if (!string.Equals(inputPortId, CsvOutputBlock.InputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        if (packet is not FastStreamFrame frame)
        {
            throw new ArgumentException("CSV Output accepts Fast Stream input only.", nameof(packet));
        }

        var value = TryReadLatestValue(frame, out var latestValue) ? latestValue : (double?)null;
        if (value is null)
        {
            return ValueTask.CompletedTask;
        }

        var state = RuntimeStateByNodeId.GetOrAdd(nodeId, static _ => new CsvOutputRuntimeState());
        lock (state)
        {
            var slot = state.ResolveSlot(sourceNodeId, sourcePortId, connectionTag, settings.InputBindings);
            if (slot < 0)
            {
                return ValueTask.CompletedTask;
            }

            state.LatestValues[slot] = value.Value;
        }

        return ValueTask.CompletedTask;
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!isInitialized)
        {
            throw new InvalidOperationException("The node must be initialized before it starts.");
        }

        if (string.IsNullOrWhiteSpace(settings.FilePath))
        {
            throw new InvalidOperationException("CSV Output requires a file path.");
        }

        var now = getTimestamp();
        var state = RuntimeStateByNodeId.GetOrAdd(nodeId, static _ => new CsvOutputRuntimeState());

        bool writeHeader;
        string headerLine;
        List<string> lines;

        lock (state)
        {
            if (state.ExecutionSessionId != settings.ExecutionSessionId)
            {
                state.ResetForSession(settings.ExecutionSessionId, now, settings.InputBindings);
                writeHeader = true;
            }
            else
            {
                writeHeader = false;
            }

            var elapsedMilliseconds = (now - state.RunStartedAt).TotalMilliseconds;
            var dueTickIndex = (long)Math.Floor(elapsedMilliseconds / settings.SpanMilliseconds);
            if (dueTickIndex <= state.LastTickIndex)
            {
                lines = [];
            }
            else
            {
                lines = new List<string>((int)Math.Min(dueTickIndex - state.LastTickIndex, 4096));
                for (var tick = state.LastTickIndex + 1; tick <= dueTickIndex; tick++)
                {
                    var millis = tick * settings.SpanMilliseconds;
                    lines.Add(state.BuildDataRow(millis));
                }

                state.LastTickIndex = dueTickIndex;
            }

            headerLine = state.BuildHeaderLine();
        }

        if (!writeHeader && lines.Count == 0)
        {
            return;
        }

        EnsureOutputDirectoryExists(settings.FilePath);
        var fileMode = writeHeader && settings.WriteMode == CsvOutputWriteMode.NewFile
            ? FileMode.Create
            : FileMode.Append;

        await using var stream = new FileStream(settings.FilePath, fileMode, FileAccess.Write, FileShare.ReadWrite);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        if (writeHeader)
        {
            await writer.WriteLineAsync(headerLine).ConfigureAwait(false);
        }

        foreach (var line in lines)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(line).ConfigureAwait(false);
        }
    }

    public ValueTask StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    private static bool TryReadLatestValue(FastStreamFrame frame, out double value)
    {
        if (frame.ChannelCount <= 0 || frame.SampleCount <= 0)
        {
            value = default;
            return false;
        }

        var channelIndex = 0;
        for (var index = 0; index < frame.ChannelNames.Count; index++)
        {
            if (string.Equals(frame.ChannelNames[index], "value", StringComparison.OrdinalIgnoreCase))
            {
                channelIndex = index;
                break;
            }
        }

        var samples = frame.Samples[channelIndex].Span;
        if (samples.Length == 0)
        {
            value = default;
            return false;
        }

        value = samples[^1];
        return double.IsFinite(value);
    }

    private static string FormatValue(double value)
    {
        return value.ToString("G17", CultureInfo.InvariantCulture);
    }

    private static void EnsureOutputDirectoryExists(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private sealed class CsvOutputRuntimeState
    {
        private readonly Dictionary<string, int> slotBySourceKey = new(StringComparer.Ordinal);
        private readonly List<string> headerLabels = [];

        public long ExecutionSessionId { get; private set; } = long.MinValue;

        public DateTimeOffset RunStartedAt { get; private set; } = DateTimeOffset.UnixEpoch;

        public long LastTickIndex { get; set; } = -1;

        public List<double> LatestValues { get; } = [];

        public void ResetForSession(
            long executionSessionId,
            DateTimeOffset now,
            IReadOnlyList<CsvOutputInputBinding> inputBindings)
        {
            ExecutionSessionId = executionSessionId;
            RunStartedAt = now;
            LastTickIndex = -1;
            slotBySourceKey.Clear();
            headerLabels.Clear();
            LatestValues.Clear();

            var slot = 0;
            foreach (var binding in inputBindings)
            {
                var sourceKey = BuildSourceKey(binding.SourceNodeId, binding.SourcePortId);
                if (string.IsNullOrWhiteSpace(sourceKey) || slotBySourceKey.ContainsKey(sourceKey))
                {
                    continue;
                }

                slotBySourceKey[sourceKey] = slot;
                headerLabels.Add(ResolveHeaderLabel(binding.Tag, slot));
                LatestValues.Add(0d);
                slot++;
            }
        }

        public int ResolveSlot(
            string sourceNodeId,
            string sourcePortId,
            string? connectionTag,
            IReadOnlyList<CsvOutputInputBinding> inputBindings)
        {
            var key = BuildSourceKey(sourceNodeId, sourcePortId);
            if (!string.IsNullOrWhiteSpace(key) && slotBySourceKey.TryGetValue(key, out var existingSlot))
            {
                return existingSlot;
            }

            if (!string.IsNullOrWhiteSpace(key))
            {
                var preferredSlot = FindPreferredSlotFromSettings(key, inputBindings);
                if (preferredSlot >= 0)
                {
                    slotBySourceKey[key] = preferredSlot;
                    EnsureSlotCapacity(preferredSlot);
                    headerLabels[preferredSlot] = ResolveHeaderLabel(connectionTag, preferredSlot);

                    return preferredSlot;
                }
            }

            var nextSlot = headerLabels.Count;
            if (!string.IsNullOrWhiteSpace(key))
            {
                slotBySourceKey[key] = nextSlot;
            }

            headerLabels.Add(ResolveHeaderLabel(connectionTag, nextSlot));
            LatestValues.Add(0d);
            return nextSlot;
        }

        public string BuildHeaderLine()
        {
            if (headerLabels.Count == 0)
            {
                return "#millis";
            }

            return "#millis," + string.Join(',', headerLabels);
        }

        public string BuildDataRow(long millis)
        {
            if (LatestValues.Count == 0)
            {
                return millis.ToString(CultureInfo.InvariantCulture);
            }

            var row = new StringBuilder();
            row.Append(millis.ToString(CultureInfo.InvariantCulture));

            for (var index = 0; index < LatestValues.Count; index++)
            {
                row.Append(',');
                row.Append(FormatValue(LatestValues[index]));
            }

            return row.ToString();
        }

        private static int FindPreferredSlotFromSettings(string sourceKey, IReadOnlyList<CsvOutputInputBinding> inputBindings)
        {
            var slot = 0;
            foreach (var binding in inputBindings)
            {
                var bindingSourceKey = BuildSourceKey(binding.SourceNodeId, binding.SourcePortId);
                if (string.Equals(bindingSourceKey, sourceKey, StringComparison.Ordinal))
                {
                    return slot;
                }

                slot++;
            }

            return -1;
        }

        private static string ResolveHeaderLabel(string? connectionTag, int slot)
        {
            if (!string.IsNullOrWhiteSpace(connectionTag))
            {
                return SanitizeHeaderToken(connectionTag);
            }

            return FormattableString.Invariant($"CH{slot + 1}Value");
        }

        private void EnsureSlotCapacity(int slot)
        {
            while (headerLabels.Count <= slot)
            {
                headerLabels.Add(ResolveHeaderLabel(connectionTag: null, headerLabels.Count));
            }

            while (LatestValues.Count <= slot)
            {
                LatestValues.Add(0d);
            }
        }

        private static string BuildSourceKey(string sourceNodeId, string sourcePortId)
        {
            if (string.IsNullOrWhiteSpace(sourceNodeId) || string.IsNullOrWhiteSpace(sourcePortId))
            {
                return string.Empty;
            }

            return $"{sourceNodeId}/{sourcePortId}";
        }

        private static string SanitizeHeaderToken(string value)
        {
            var trimmed = value.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return "unnamed";
            }

            return trimmed.Replace(',', '_');
        }
    }
}
