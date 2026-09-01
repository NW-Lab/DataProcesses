using System.Text;

using DataProcesses.Nodes.BuiltIn;
using DataProcesses.Nodes.BuiltIn.Blocks.BleInputSt;
using DataProcesses.Nodes.BuiltIn.Blocks.BleInputVector;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.BleInputVector;

public sealed class BleInputVectorBlockTests
{
    [Fact]
    public void Definition_DeclaresOneNumericVectorOutput()
    {
        var port = Assert.Single(BleInputVectorBlock.Definition.Ports);

        Assert.Equal(BleInputVectorBlock.VectorPortId, port.Id);
        Assert.Equal(PortDirection.Output, port.Direction);
        Assert.Equal(PortDataKind.FastStream, port.DataKind);
        Assert.Equal(PortDataSchema.NumericVector1D, port.DataSchema);
    }

    [Fact]
    public void DefaultSettings_UseNordicUartNotifyCharacteristic()
    {
        var settings = BleInputVectorSettings.Default;

        Assert.Equal(BleInputStSettings.NordicUartServiceUuid, settings.ServiceUuid);
        Assert.Equal(BleInputStSettings.NordicUartTxCharacteristicUuid, settings.NotifyCharacteristicUuid);
        Assert.Equal(5000, settings.TimeoutMilliseconds);
        Assert.True(settings.AutoConnect);
    }

    [Fact]
    public async Task StartAsync_EmitsOneImuVectorForEachBleNotificationRow()
    {
        var context = new RecordingNodeContext();
        var node = new BleInputVectorNode(
            new BleInputVectorSettings(DeviceId: "ble-device-1", DeviceName: "Arduino"),
            getTimestamp: () => DateTimeOffset.UnixEpoch,
            notificationSourceFactory: (_, _) => CreateNotificationSource(["0,0.01,", "-0.02,9.81\n10,0.02,-0.01,9.80\n"]));

        await node.InitializeAsync(context, CancellationToken.None);
        await node.StartAsync(CancellationToken.None);

        Assert.Equal(2, context.EmittedPackets.Count);
        var first = Assert.IsType<NumericVectorFrame>(context.EmittedPackets[0].Packet);
        var second = Assert.IsType<NumericVectorFrame>(context.EmittedPackets[1].Packet);

        Assert.Equal(BleInputVectorBlock.VectorPortId, context.EmittedPackets[0].OutputPortId);
        Assert.Equal("imu", first.Name);
        Assert.Equal(new double[] { 0.01, -0.02, 9.81 }, first.Values.ToArray());
        Assert.Equal(DateTimeOffset.UnixEpoch, first.Timestamp);
        Assert.Equal(DateTimeOffset.UnixEpoch.AddMilliseconds(10), second.Timestamp);
        Assert.Equal(1, second.SequenceNumber);
    }

    [Fact]
    public async Task StartAsync_RejectsRowsThatAreNotImuVectors()
    {
        var node = new BleInputVectorNode(
            new BleInputVectorSettings(),
            notificationSourceFactory: (_, _) => CreateNotificationSource(["0,1,2\n"]));
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);

        await Assert.ThrowsAsync<InvalidDataException>(async () => await node.StartAsync(CancellationToken.None));
    }

    [Fact]
    public void FromJson_RoundTripsPersistedDeviceAndCustomUuids()
    {
        var settings = BleInputVectorSettings.FromJson("""
            {
              "deviceId": "device-123",
              "deviceName": "Arduino Nano 33 BLE",
              "autoConnect": false,
              "serviceUuid": "0000180d-0000-1000-8000-00805f9b34fb",
              "notifyCharacteristicUuid": "00002a37-0000-1000-8000-00805f9b34fb",
              "timeoutMilliseconds": 2500
            }
            """);

        Assert.Equal("device-123", settings.DeviceId);
        Assert.Equal("Arduino Nano 33 BLE", settings.DeviceName);
        Assert.False(settings.AutoConnect);
        Assert.Equal("0000180d-0000-1000-8000-00805f9b34fb", settings.ServiceUuid);
        Assert.Equal("00002a37-0000-1000-8000-00805f9b34fb", settings.NotifyCharacteristicUuid);
        Assert.Equal(2500, settings.TimeoutMilliseconds);
    }

    [Fact]
    public void BuiltInCatalog_RegistersBleInputVectorBlock()
    {
        var plugin = new BuiltInNodePlugin();

        Assert.Contains(plugin.NodeFactories, factory => factory.Definition.TypeId == BleInputVectorBlock.TypeId);
    }

    private static async IAsyncEnumerable<ReadOnlyMemory<byte>> CreateNotificationSource(IEnumerable<string> notifications)
    {
        foreach (var notification in notifications)
        {
            yield return Encoding.UTF8.GetBytes(notification).AsMemory();
            await Task.Yield();
        }
    }
}