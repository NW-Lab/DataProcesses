using DataProcesses.Nodes.BuiltIn.Blocks.BleInputTS;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.BleInputFS;

public sealed class BleInputFSNode : INode
{
    private readonly BleInputFSSettings settings;
    private readonly Func<DateTimeOffset> getTimestamp;
    private readonly Func<BleInputFSSettings, CancellationToken, IAsyncEnumerable<ReadOnlyMemory<byte>>> notificationSourceFactory;
    private readonly object lifecycleLock = new();
    private CancellationTokenSource? activeRun;
    private INodeContext? context;

    public BleInputFSNode(
        BleInputFSSettings settings,
        Func<DateTimeOffset>? getTimestamp = null,
        Func<BleInputFSSettings, CancellationToken, IAsyncEnumerable<ReadOnlyMemory<byte>>>? notificationSourceFactory = null)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        settings.Validate();
        this.getTimestamp = getTimestamp ?? (() => DateTimeOffset.UtcNow);
        this.notificationSourceFactory = notificationSourceFactory
            ?? ((configuration, token) => BleInputTSNotificationSource.ReadNotificationsAsync(configuration, token));
    }

    public NodeDefinition Definition => BleInputFSBlock.Definition;

    public ValueTask InitializeAsync(INodeContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        return ValueTask.CompletedTask;
    }

    public ValueTask OnPacketAsync(string inputPortId, IDataPacket packet, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPortId);
        ArgumentNullException.ThrowIfNull(packet);
        cancellationToken.ThrowIfCancellationRequested();
        throw new InvalidOperationException("BleInputFS does not define input ports.");
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var initializedContext = context ?? throw new InvalidOperationException("Initialize BleInputFS before starting.");
        using var run = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        lock (lifecycleLock)
        {
            if (activeRun is not null)
            {
                throw new InvalidOperationException("BleInputFS is already running.");
            }

            activeRun = run;
        }

        try
        {
            var decoder = new BleInputFSDecoder(settings, getTimestamp);
            using var notificationTimeout = CancellationTokenSource.CreateLinkedTokenSource(run.Token);
            await using var notifications = notificationSourceFactory(settings, notificationTimeout.Token)
                .GetAsyncEnumerator(notificationTimeout.Token);
            while (true)
            {
                notificationTimeout.CancelAfter(settings.TimeoutMilliseconds);
                bool hasNext;
                try
                {
                    hasNext = await notifications.MoveNextAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException exception) when (notificationTimeout.IsCancellationRequested && !run.IsCancellationRequested)
                {
                    throw new TimeoutException("BleInputFS timed out waiting for a BLE notification.", exception);
                }
                finally
                {
                    notificationTimeout.CancelAfter(Timeout.Infinite);
                }

                run.Token.ThrowIfCancellationRequested();
                if (notificationTimeout.IsCancellationRequested)
                {
                    throw new TimeoutException("BleInputFS timed out waiting for a BLE notification.");
                }
                if (!hasNext)
                {
                    decoder.Complete();
                    break;
                }

                foreach (var frame in decoder.Decode(notifications.Current.Span, run.Token))
                {
                    await initializedContext.EmitAsync(BleInputFSBlock.StreamPortId, frame, run.Token).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            lock (lifecycleLock)
            {
                activeRun = null;
            }
        }
    }

    public ValueTask StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (lifecycleLock)
        {
            activeRun?.Cancel();
        }

        return ValueTask.CompletedTask;
    }
}
