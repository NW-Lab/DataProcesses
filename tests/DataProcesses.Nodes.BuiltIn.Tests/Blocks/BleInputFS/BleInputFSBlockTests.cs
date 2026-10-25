using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

using DataProcesses.Nodes.BuiltIn.Blocks.BleInputFS;
using DataProcesses.Plugin.Abstractions;

using Xunit.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.BleInputFS;

public sealed class BleInputFSBlockTests(ITestOutputHelper output)
{
    [Fact]
    public void DefinitionAndFactory_DeclareStableSourceContract()
    {
        var factory = new BleInputFSNodeFactory();
        Assert.Equal("dataprocesses.input.ble-fs", factory.Definition.TypeId);
        Assert.Equal(NodeType.Input, factory.Definition.NodeType);
        var port = Assert.Single(factory.Definition.Ports);
        Assert.Equal("stream", port.Id);
        Assert.Equal(PortDirection.Output, port.Direction);
        Assert.Equal(PortDataKind.FastStream, port.DataKind);
        Assert.Equal(PortDataSchema.FixedRateTimeSeries1D, port.DataSchema);
        Assert.False(port.IsRequired);
        Assert.NotSame(factory.CreateNode("a"), factory.CreateNode("b", "{}"));
        Assert.Throws<ArgumentException>(() => factory.CreateNode(""));
    }

    [Fact]
    public void Settings_ParseAllInspectorFields()
    {
        var settings = BleInputFSSettings.FromJson("""
            {"deviceId":"synthetic","deviceName":"Test device","autoConnect":false,
             "serviceUuid":"0000180d-0000-1000-8000-00805f9b34fb",
             "notifyCharacteristicUuid":"00002a37-0000-1000-8000-00805f9b34fb",
             "channelCount":3,"timeoutMilliseconds":2000,"sampleRateHertz":44100,
             "format":"Binary","numericType":"Int16","byteOrder":"BigEndian","future":true}
            """);
        Assert.Equal("synthetic", settings.DeviceId);
        Assert.Equal("Test device", settings.DeviceNameOrId);
        Assert.False(settings.AutoConnect);
        Assert.Equal("0000180d-0000-1000-8000-00805f9b34fb", settings.ServiceUuid);
        Assert.Equal("00002a37-0000-1000-8000-00805f9b34fb", settings.NotifyCharacteristicUuid);
        Assert.Equal(3, settings.ChannelCount);
        Assert.Equal(2000, settings.TimeoutMilliseconds);
        Assert.Equal(44100, settings.SampleRateHertz);
        Assert.Equal(BleInputFSFormat.Binary, settings.Format);
        Assert.Equal(BleInputFSNumericType.Int16, settings.NumericType);
        Assert.Equal(BleInputFSByteOrder.BigEndian, settings.ByteOrder);
        Assert.Equal(BleInputFSSettings.Default, BleInputFSSettings.FromJson("{}"));
        Assert.Equal(BleInputFSSettings.Default, BleInputFSSettings.FromJson(""));
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("""{"sampleRateHertz":0}""")]
    [InlineData("""{"sampleRateHertz":1000001}""")]
    [InlineData("""{"sampleRateHertz":1.5}""")]
    [InlineData("""{"sampleRateHertz":"1000"}""")]
    [InlineData("""{"channelCount":0}""")]
    [InlineData("""{"channelCount":17}""")]
    [InlineData("""{"timeoutMilliseconds":0}""")]
    [InlineData("""{"serviceUuid":"invalid"}""")]
    [InlineData("""{"notifyCharacteristicUuid":"invalid"}""")]
    [InlineData("""{"format":"Raw"}""")]
    [InlineData("""{"format":"1"}""")]
    [InlineData("""{"numericType":4}""")]
    [InlineData("""{"byteOrder":"Native"}""")]
    public void Settings_RejectInvalidValues(string json)
    {
        Assert.ThrowsAny<ArgumentException>(() => BleInputFSSettings.FromJson(json));
    }

    [Fact]
    public async Task Csv_ReassemblesRowsAndSplitsAtTimingGaps()
    {
        var context = await RunAsync(new(), Text("100,1.", "5,2.5\r\n101,3,4\n105,5,6\n106,7,8\n"));
        Assert.Equal(2, context.EmittedPackets.Count);
        var first = Frame(context, 0);
        var second = Frame(context, 1);
        Assert.Equal(["data1", "data2"], first.ChannelNames);
        Assert.Equal([1.5, 3.0], first.Samples[0].ToArray());
        Assert.Equal([2.5, 4.0], first.Samples[1].ToArray());
        Assert.Equal(1000, first.SampleRateHertz);
        Assert.Equal(1_000_000, first.SamplePeriodNanoseconds);
        Assert.Equal(0, first.StartTimeUnixNanoseconds);
        Assert.Equal(5_000_000, second.StartTimeUnixNanoseconds);
        Assert.Equal(1, second.SequenceNumber);
    }

    [Theory]
    [InlineData("0,1\n")]
    [InlineData("0,1,2,3\n")]
    [InlineData("-1,1,2\n")]
    [InlineData("NaN,1,2\n")]
    [InlineData("0,Infinity,2\n")]
    [InlineData("0,1,2\n0,3,4\n")]
    [InlineData("1,1,2\n0,3,4\n")]
    [InlineData("0,1,2")]
    public async Task Csv_RejectsMalformedOrTruncatedData(string text)
    {
        await Assert.ThrowsAsync<InvalidDataException>(() => RunAsync(new(), Text(text)));
    }

    [Fact]
    public async Task Csv_BoundsUnterminatedLines()
    {
        await Assert.ThrowsAsync<InvalidDataException>(() => RunAsync(new(), Text(new string('1', 4097))));
    }

    [Theory]
    [InlineData(BleInputFSNumericType.Int16, BleInputFSByteOrder.LittleEndian)]
    [InlineData(BleInputFSNumericType.Int16, BleInputFSByteOrder.BigEndian)]
    [InlineData(BleInputFSNumericType.Int32, BleInputFSByteOrder.LittleEndian)]
    [InlineData(BleInputFSNumericType.Int32, BleInputFSByteOrder.BigEndian)]
    [InlineData(BleInputFSNumericType.Float32, BleInputFSByteOrder.LittleEndian)]
    [InlineData(BleInputFSNumericType.Float32, BleInputFSByteOrder.BigEndian)]
    [InlineData(BleInputFSNumericType.Float64, BleInputFSByteOrder.LittleEndian)]
    [InlineData(BleInputFSNumericType.Float64, BleInputFSByteOrder.BigEndian)]
    public async Task Binary_DecodesInterleavedSamplesAcrossNotifications(BleInputFSNumericType type, BleInputFSByteOrder order)
    {
        var bytes = Encode(type, order, [-123, 456, 789, -10]);
        var settings = new BleInputFSSettings(Format: BleInputFSFormat.Binary, NumericType: type, ByteOrder: order);
        var context = await RunAsync(settings, [bytes.AsMemory(0, 1), bytes.AsMemory(1)]);
        var frame = Frame(context, 0);
        Assert.Equal([-123.0, 789], frame.Samples[0].ToArray());
        Assert.Equal([456.0, -10], frame.Samples[1].ToArray());
        Assert.Equal(1000, frame.SampleRateHertz);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public async Task Binary_RejectsNonfiniteValues(double value)
    {
        var bytes = Encode(BleInputFSNumericType.Float64, BleInputFSByteOrder.LittleEndian, [value, 1]);
        await Assert.ThrowsAsync<InvalidDataException>(() => RunAsync(
            new(Format: BleInputFSFormat.Binary, NumericType: BleInputFSNumericType.Float64), [bytes]));
    }

    [Fact]
    public async Task Binary_RejectsTruncatedChannelTuple()
    {
        await Assert.ThrowsAsync<InvalidDataException>(() => RunAsync(new(Format: BleInputFSFormat.Binary), [new byte[7]]));
    }

    [Fact]
    public async Task Binary_UsesExactRateAcrossFramesWithoutAccumulatingRoundedPeriods()
    {
        var bytes = new byte[600 * 2 * 4];
        var context = await RunAsync(new(Format: BleInputFSFormat.Binary, SampleRateHertz: 44100),
            [bytes.AsMemory(0, 257 * 8), bytes.AsMemory(257 * 8)]);
        long index = 0;
        for (var frameIndex = 0; frameIndex < context.EmittedPackets.Count; frameIndex++)
        {
            var frame = Frame(context, frameIndex);
            Assert.InRange(frame.SampleCount, 1, 256);
            Assert.Equal(frameIndex, frame.SequenceNumber);
            Assert.Equal(index * 1_000_000_000L / 44100, frame.StartTimeUnixNanoseconds);
            for (var sample = 0; sample < frame.SampleCount; sample++)
            {
                Assert.InRange(Math.Abs(frame.GetSampleTimestampUnixNanoseconds(sample)
                    - ((index + sample) * 1_000_000_000L / 44100)), 0, 1);
            }

            index += frame.SampleCount;
        }

        Assert.Equal(600, index);
    }

    [Fact]
    public async Task EmptySource_ProducesNoFrames()
    {
        Assert.Empty((await RunAsync(new(), [])).EmittedPackets);
    }

    [Fact]
    public async Task SourceFailure_PropagatesWithoutSuccessFallback()
    {
        var expected = new IOException("Synthetic GATT failure");
        async IAsyncEnumerable<ReadOnlyMemory<byte>> Fail()
        {
            yield return await Task.FromException<ReadOnlyMemory<byte>>(expected);
        }

        var node = new BleInputFSNode(new(), notificationSourceFactory: (_, _) => Fail());
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        var actual = await Assert.ThrowsAsync<IOException>(() => node.StartAsync(CancellationToken.None).AsTask());
        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task Emission_AwaitsDownstreamBeforeReadingNextNotification()
    {
        var reads = 0;
        async IAsyncEnumerable<ReadOnlyMemory<byte>> Notifications()
        {
            foreach (var chunk in Text("0,1,2\n", "1,3,4\n"))
            {
                reads++;
                yield return chunk;
            }

            await Task.CompletedTask;
        }

        using var cancellation = new CancellationTokenSource();
        var context = new BlockingContext();
        var node = new BleInputFSNode(new(), notificationSourceFactory: (_, _) => Notifications());
        await node.InitializeAsync(context, CancellationToken.None);
        var run = node.StartAsync(cancellation.Token).AsTask();
        try
        {
            await context.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(1, reads);
            Assert.False(run.IsCompleted);
        }
        finally
        {
            await cancellation.CancelAsync();
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.Equal(1, reads);
    }

    [Fact]
    public async Task RepeatedRuns_ResetSequenceAndBuffers()
    {
        var context = new RecordingNodeContext();
        var node = CreateNode(new(), Text("0,1,2\n"));
        await node.InitializeAsync(context, CancellationToken.None);
        await node.StartAsync(CancellationToken.None);
        await node.StartAsync(CancellationToken.None);
        Assert.Equal(0, Frame(context, 0).SequenceNumber);
        Assert.Equal(0, Frame(context, 1).SequenceNumber);
    }

    [Fact]
    public async Task Lifecycle_RequiresInitializationAndRejectsInput()
    {
        var node = CreateNode(new(), []);
        await Assert.ThrowsAsync<InvalidOperationException>(() => node.StartAsync(CancellationToken.None).AsTask());
        await Assert.ThrowsAsync<InvalidOperationException>(() => node.OnPacketAsync("in",
            new FastStreamFrame(0, 1, [], [], 0), CancellationToken.None).AsTask());
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => node.StartAsync(canceled.Token).AsTask());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Lifecycle_CancellationAndStopDisposeSource(bool stop)
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var disposed = false;
        async IAsyncEnumerable<ReadOnlyMemory<byte>> Idle([EnumeratorCancellation] CancellationToken token)
        {
            try
            {
                started.SetResult();
                await Task.Delay(Timeout.Infinite, token);
                yield break;
            }
            finally
            {
                disposed = true;
            }
        }

        using var cancellation = new CancellationTokenSource();
        var node = new BleInputFSNode(new(), notificationSourceFactory: (_, token) => Idle(token));
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        var run = node.StartAsync(cancellation.Token).AsTask();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Assert.ThrowsAsync<InvalidOperationException>(() => node.StartAsync(CancellationToken.None).AsTask());
        if (stop)
        {
            await node.StopAsync(CancellationToken.None);
        }
        else
        {
            await cancellation.CancelAsync();
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.True(disposed);
    }

    [Fact]
    public async Task Timeout_IsAnExplicitErrorAndDisposesSource()
    {
        var disposed = false;
        async IAsyncEnumerable<ReadOnlyMemory<byte>> Idle([EnumeratorCancellation] CancellationToken token)
        {
            try
            {
                await Task.Delay(Timeout.Infinite, token);
                yield break;
            }
            finally
            {
                disposed = true;
            }
        }

        var node = new BleInputFSNode(new(TimeoutMilliseconds: 20), notificationSourceFactory: (_, token) => Idle(token));
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        await Assert.ThrowsAsync<TimeoutException>(() => node.StartAsync(CancellationToken.None).AsTask().WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.True(disposed);
    }

    [Fact]
    public async Task Binary_RecordsThroughputAndFrameLevelAllocations()
    {
        const int sampleCount = 131072;
        var bytes = new byte[sampleCount * 2 * 4];
        var context = new CountingContext();
        var node = CreateNode(new(Format: BleInputFSFormat.Binary), [bytes]);
        await node.InitializeAsync(context, CancellationToken.None);
        await node.StartAsync(CancellationToken.None);
        context.SampleCount = 0;
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        var stopwatch = Stopwatch.StartNew();
        await node.StartAsync(CancellationToken.None);
        stopwatch.Stop();
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        Assert.Equal(sampleCount, context.SampleCount);
        Assert.InRange(allocated / (double)sampleCount, 16, 64);
        output.WriteLine($"Binary 2ch Float32: {sampleCount} samples, {stopwatch.Elapsed.TotalMilliseconds:F3} ms, {allocated} bytes ({allocated / (double)sampleCount:F2} bytes/sample).");
    }

    private static FastStreamFrame Frame(RecordingNodeContext context, int index)
    {
        Assert.Equal(BleInputFSBlock.StreamPortId, context.EmittedPackets[index].OutputPortId);
        return Assert.IsType<FastStreamFrame>(context.EmittedPackets[index].Packet);
    }

    private static ReadOnlyMemory<byte>[] Text(params string[] values) => values.Select(value => (ReadOnlyMemory<byte>)Encoding.UTF8.GetBytes(value)).ToArray();

    private static BleInputFSNode CreateNode(BleInputFSSettings settings, ReadOnlyMemory<byte>[] chunks) =>
        new(settings, () => DateTimeOffset.UnixEpoch, (_, token) => Source(chunks, token));

    private static async Task<RecordingNodeContext> RunAsync(BleInputFSSettings settings, ReadOnlyMemory<byte>[] chunks)
    {
        var node = CreateNode(settings, chunks);
        var context = new RecordingNodeContext();
        await node.InitializeAsync(context, CancellationToken.None);
        await node.StartAsync(CancellationToken.None);
        return context;
    }

    private static async IAsyncEnumerable<ReadOnlyMemory<byte>> Source(ReadOnlyMemory<byte>[] chunks, [EnumeratorCancellation] CancellationToken token)
    {
        foreach (var chunk in chunks)
        {
            token.ThrowIfCancellationRequested();
            yield return chunk;
        }

        await Task.CompletedTask;
    }

    private static byte[] Encode(BleInputFSNumericType type, BleInputFSByteOrder order, double[] values)
    {
        var size = type == BleInputFSNumericType.Int16 ? 2 : type == BleInputFSNumericType.Float64 ? 8 : 4;
        var bytes = new byte[size * values.Length];
        for (var index = 0; index < values.Length; index++)
        {
            var target = bytes.AsSpan(index * size, size);
            switch (type)
            {
                case BleInputFSNumericType.Int16: BinaryPrimitives.WriteInt16LittleEndian(target, (short)values[index]); break;
                case BleInputFSNumericType.Int32: BinaryPrimitives.WriteInt32LittleEndian(target, (int)values[index]); break;
                case BleInputFSNumericType.Float32: BinaryPrimitives.WriteSingleLittleEndian(target, (float)values[index]); break;
                case BleInputFSNumericType.Float64: BinaryPrimitives.WriteDoubleLittleEndian(target, values[index]); break;
            }

            if (order == BleInputFSByteOrder.BigEndian)
            {
                target.Reverse();
            }
        }

        return bytes;
    }

    private sealed class CountingContext : INodeContext
    {
        public string NodeId => "benchmark";
        public int SampleCount { get; set; }
        public ValueTask EmitAsync(string outputPortId, IDataPacket packet, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SampleCount += Assert.IsType<FastStreamFrame>(packet).SampleCount;
            return ValueTask.CompletedTask;
        }

    }

    private sealed class BlockingContext : INodeContext
    {
        public string NodeId => "blocked";
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async ValueTask EmitAsync(string outputPortId, IDataPacket packet, CancellationToken cancellationToken)
        {
            Started.TrySetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
    }
}
