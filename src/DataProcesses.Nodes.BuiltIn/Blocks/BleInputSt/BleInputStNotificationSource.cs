using System.Runtime.CompilerServices;
using System.Threading.Channels;

#if WINDOWS
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
#endif

namespace DataProcesses.Nodes.BuiltIn.Blocks.BleInputSt;

internal static class BleInputStNotificationSource
{
    public static IAsyncEnumerable<ReadOnlyMemory<byte>> ReadNotificationsAsync(
        IBleInputGattSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

#if WINDOWS
        return ReadWindowsNotificationsAsync(settings, cancellationToken);
#else
        cancellationToken.ThrowIfCancellationRequested();
        throw new PlatformNotSupportedException("BLE GATT notifications are currently implemented only for Windows builds.");
#endif
    }

#if WINDOWS
    private static async IAsyncEnumerable<ReadOnlyMemory<byte>> ReadWindowsNotificationsAsync(
        IBleInputGattSettings settings,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.DeviceId))
        {
            throw new InvalidOperationException("BLE Input ST requires a selected BLE device id.");
        }

        var device = await BluetoothLEDevice.FromIdAsync(settings.DeviceId).AsTask(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"BLE device '{settings.DeviceNameOrId}' was not found or access was denied.");

        var characteristic = default(GattCharacteristic);
        var notifications = Channel.CreateUnbounded<ReadOnlyMemory<byte>>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

        void CharacteristicValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            using var reader = DataReader.FromBuffer(args.CharacteristicValue);
            var byteCount = checked((int)reader.UnconsumedBufferLength);
            var bytes = new byte[byteCount];
            reader.ReadBytes(bytes);
            notifications.Writer.TryWrite(bytes);
        }

        using var cancellationRegistration = cancellationToken.Register(static state =>
        {
            var writer = (ChannelWriter<ReadOnlyMemory<byte>>)state!;
            writer.TryComplete();
        }, notifications.Writer);

        try
        {
            var serviceUuid = Guid.Parse(settings.ServiceUuid);
            var characteristicUuid = Guid.Parse(settings.NotifyCharacteristicUuid);
            var serviceResult = await device.GetGattServicesForUuidAsync(serviceUuid, BluetoothCacheMode.Uncached)
                .AsTask(cancellationToken)
                .ConfigureAwait(false);
            if (serviceResult.Status != GattCommunicationStatus.Success || serviceResult.Services.Count == 0)
            {
                throw new InvalidOperationException($"BLE service '{settings.ServiceUuid}' was not found on device '{settings.DeviceNameOrId}'. Status: {serviceResult.Status}.");
            }

            using var service = serviceResult.Services[0];
            var characteristicResult = await service.GetCharacteristicsForUuidAsync(characteristicUuid, BluetoothCacheMode.Uncached)
                .AsTask(cancellationToken)
                .ConfigureAwait(false);
            if (characteristicResult.Status != GattCommunicationStatus.Success || characteristicResult.Characteristics.Count == 0)
            {
                throw new InvalidOperationException($"BLE notify characteristic '{settings.NotifyCharacteristicUuid}' was not found on device '{settings.DeviceNameOrId}'. Status: {characteristicResult.Status}.");
            }

            characteristic = characteristicResult.Characteristics[0];
            var descriptorValue = GetNotificationDescriptorValue(characteristic);
            characteristic.ValueChanged += CharacteristicValueChanged;

            var subscriptionStatus = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(descriptorValue)
                .AsTask(cancellationToken)
                .ConfigureAwait(false);
            if (subscriptionStatus != GattCommunicationStatus.Success)
            {
                throw new InvalidOperationException($"BLE notify subscription failed for device '{settings.DeviceNameOrId}'. Status: {subscriptionStatus}.");
            }

            while (await notifications.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (notifications.Reader.TryRead(out var notification))
                {
                    yield return notification;
                }
            }
        }
        finally
        {
            if (characteristic is not null)
            {
                characteristic.ValueChanged -= CharacteristicValueChanged;
                try
                {
                    await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None)
                        .AsTask()
                        .ConfigureAwait(false);
                }
                catch (Exception) when (cancellationToken.IsCancellationRequested)
                {
                }
            }

            device.Dispose();
        }
    }

    private static GattClientCharacteristicConfigurationDescriptorValue GetNotificationDescriptorValue(
        GattCharacteristic characteristic)
    {
        if ((characteristic.CharacteristicProperties & GattCharacteristicProperties.Notify) == GattCharacteristicProperties.Notify)
        {
            return GattClientCharacteristicConfigurationDescriptorValue.Notify;
        }

        if ((characteristic.CharacteristicProperties & GattCharacteristicProperties.Indicate) == GattCharacteristicProperties.Indicate)
        {
            return GattClientCharacteristicConfigurationDescriptorValue.Indicate;
        }

        throw new InvalidOperationException("The selected BLE characteristic does not support notify or indicate.");
    }
#endif
}