using System.Text;

using DataProcesses.Nodes.BuiltIn;
using DataProcesses.Nodes.BuiltIn.Blocks.BleInputSt;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.BleInputSt;

public sealed class BleInputStBlockTests
{
    [Fact]
    public void Definition_DeclaresOneFastStreamOutput()
    {
        var port = Assert.Single(BleInputStBlock.Definition.Ports);

        Assert.Equal(BleInputStBlock.StreamPortId, port.Id);
        Assert.Equal(PortDirection.Output, port.Direction);
        Assert.Equal(PortDataKind.FastStream, port.DataKind);
        Assert.Equal(PortDataSchema.TimeSeries1D, port.DataSchema);
    }

    [Fact]
    public void DefaultSettings_UseNordicUartNotifyCharacteristic()
    {
        var settings = BleInputStSettings.Default;

        Assert.Equal(BleInputStSettings.NordicUartServiceUuid, settings.ServiceUuid);
        Assert.Equal(BleInputStSettings.NordicUartTxCharacteristicUuid, settings.NotifyCharacteristicUuid);
        Assert.Equal(5000, settings.TimeoutMilliseconds);
        Assert.True(settings.AutoConnect);
    }

    [Fact]
    public async Task StartAsync_ConvertsArduinoNotificationsToMultiChannelFrames()
    {
        var settings = new BleInputStSettings(DeviceId: "ble-device-1", DeviceName: "Arduino", ChannelCount: 2);
        var context = new RecordingNodeContext();
        var node = new BleInputStNode(
            settings,
            getTimestamp: () => DateTimeOffset.UnixEpoch,
            notificationSourceFactory: (_, _) => CreateNotificationSource(["0,1.", "5,2.5\n10,1.7,2.7\n"]));

        await node.InitializeAsync(context, CancellationToken.None);
        await node.StartAsync(CancellationToken.None);

        Assert.Equal(2, context.EmittedPackets.Count);
        var first = Assert.IsType<FastStreamFrame>(context.EmittedPackets[0].Packet);
        var second = Assert.IsType<FastStreamFrame>(context.EmittedPackets[1].Packet);

        Assert.Equal(BleInputStBlock.StreamPortId, context.EmittedPackets[0].OutputPortId);
        Assert.Equal(["data1", "data2"], first.ChannelNames);
        Assert.Equal(1.5, first.Samples[0].Span[0]);
        Assert.Equal(2.5, first.Samples[1].Span[0]);
        Assert.Equal(1_000_000L, first.SamplePeriodNanoseconds);
        Assert.Equal(10_000_000L, second.SamplePeriodNanoseconds);
        Assert.Equal(1, second.SequenceNumber);
    }

    [Fact]
    public async Task StartAsync_DisconnectsWhenNotificationTimeoutElapses()
    {
        var settings = new BleInputStSettings(TimeoutMilliseconds: 1);
        var context = new RecordingNodeContext();
        var node = new BleInputStNode(
            settings,
            notificationSourceFactory: (_, _) => new IdleNotificationSource());

        await node.InitializeAsync(context, CancellationToken.None);
        await node.StartAsync(CancellationToken.None);

        Assert.Empty(context.EmittedPackets);
    }

    [Fact]
    public void FromJson_RoundTripsPersistedDeviceAndCustomUuids()
    {
        var settings = BleInputStSettings.FromJson("""
            {
              "deviceId": "device-123",
              "deviceName": "Arduino Nano 33 BLE",
              "autoConnect": false,
              "serviceUuid": "0000180d-0000-1000-8000-00805f9b34fb",
              "notifyCharacteristicUuid": "00002a37-0000-1000-8000-00805f9b34fb",
              "channelCount": 3,
              "timeoutMilliseconds": 2500
            }
            """);

        Assert.Equal("device-123", settings.DeviceId);
        Assert.Equal("Arduino Nano 33 BLE", settings.DeviceName);
        Assert.False(settings.AutoConnect);
        Assert.Equal("0000180d-0000-1000-8000-00805f9b34fb", settings.ServiceUuid);
        Assert.Equal("00002a37-0000-1000-8000-00805f9b34fb", settings.NotifyCharacteristicUuid);
        Assert.Equal(3, settings.ChannelCount);
        Assert.Equal(2500, settings.TimeoutMilliseconds);
    }

    [Fact]
    public void BuiltInCatalog_RegistersBleInputStBlock()
    {
        var plugin = new BuiltInNodePlugin();

        Assert.Contains(plugin.NodeFactories, factory => factory.Definition.TypeId == BleInputStBlock.TypeId);
    }

    private static async IAsyncEnumerable<ReadOnlyMemory<byte>> CreateNotificationSource(IEnumerable<string> notifications)
    {
        foreach (var notification in notifications)
        {
            yield return Encoding.UTF8.GetBytes(notification).AsMemory();
            await Task.Yield();
        }
    }

    private sealed class IdleNotificationSource : IAsyncEnumerable<ReadOnlyMemory<byte>>, IAsyncEnumerator<ReadOnlyMemory<byte>>
    {
        public ReadOnlyMemory<byte> Current => ReadOnlyMemory<byte>.Empty;

        public IAsyncEnumerator<ReadOnlyMemory<byte>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return this;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(Task.Delay(Timeout.InfiniteTimeSpan).ContinueWith(static _ => false, TaskScheduler.Default));
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}