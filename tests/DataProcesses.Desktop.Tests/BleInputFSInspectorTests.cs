using System.Globalization;

using DataProcesses.Core;
using DataProcesses.Desktop.Resources;
using DataProcesses.Desktop.ViewModels;
using DataProcesses.Nodes.BuiltIn.Blocks.BleInputFS;

namespace DataProcesses.Desktop.Tests;

public sealed class BleInputFSInspectorTests
{
    [Fact]
    public void Inspector_PersistsAllSettingsAndUpdatesBinaryVisibility()
    {
        var node = new CanvasNodeViewModel(new NodeInstance("ble-fs", BleInputFSBlock.TypeId, 0, 0, "{}"), BleInputFSBlock.Definition);
        Assert.True(node.IsBleInputNode);
        Assert.True(node.IsBleChannelCountVisible);
        Assert.True(node.IsBleInputFSNode);
        Assert.False(node.IsBleInputFSBinary);
        var changed = new List<string?>();
        node.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
        node.BleInputFSFormat = "Binary";
        node.BleInputFSNumericType = "Int16";
        node.BleInputFSByteOrder = "BigEndian";
        node.BleInputFSSampleRateHertz = 44100;
        node.BleInputTSChannelCount = 3;
        node.BleInputTSTimeoutMilliseconds = 2000;
        node.BleInputTSDeviceId = "synthetic-device";
        node.BleInputTSDeviceName = "Synthetic BLE";
        node.BleInputTSAutoConnect = false;
        Assert.True(node.IsBleInputFSBinary);
        Assert.Contains(nameof(CanvasNodeViewModel.IsBleInputFSBinary), changed);
        var settings = BleInputFSSettings.FromJson(node.SettingsJson);
        Assert.Equal(BleInputFSFormat.Binary, settings.Format);
        Assert.Equal(BleInputFSNumericType.Int16, settings.NumericType);
        Assert.Equal(BleInputFSByteOrder.BigEndian, settings.ByteOrder);
        Assert.Equal(44100, settings.SampleRateHertz);
        Assert.Equal(3, settings.ChannelCount);
        Assert.Equal(2000, settings.TimeoutMilliseconds);
        Assert.False(settings.AutoConnect);
        Assert.Equal("synthetic-device", settings.DeviceId);
        Assert.Equal("Synthetic BLE", settings.DeviceName);
        node.BleInputFSFormat = "Csv";
        Assert.False(node.IsBleInputFSBinary);
        Assert.Equal("Int16", node.BleInputFSNumericType);
        node.BleInputTSResetNordicUuidsCommand.Execute(null);
        Assert.Equal(BleInputFSSettings.Default.ServiceUuid, BleInputFSSettings.FromJson(node.SettingsJson).ServiceUuid);
    }

    [Fact]
    public void Reload_RestoresFormatAndDependentVisibility()
    {
        var node = new CanvasNodeViewModel(new NodeInstance("ble", BleInputFSBlock.TypeId, 0, 0,
            """{"format":"binary","numericType":"float64","byteOrder":"bigendian","sampleRateHertz":48000}"""), BleInputFSBlock.Definition);
        Assert.True(node.IsBleInputFSBinary);
        Assert.Equal("Float64", node.BleInputFSNumericType);
        Assert.Equal("BigEndian", node.BleInputFSByteOrder);
        Assert.Equal(48000, node.BleInputFSSampleRateHertz);
        node.SettingsJson = "{}";
        Assert.False(node.IsBleInputFSBinary);
        Assert.Equal(1000, node.BleInputFSSampleRateHertz);
    }

    [Theory]
    [InlineData("en", "Input format")]
    [InlineData("ja", "入力形式")]
    public void Resources_LocalizeInspectorLabels(string culture, string expected)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            Assert.Equal(expected, BleInputFSStrings.Format);
            Assert.False(string.IsNullOrWhiteSpace(BleInputFSStrings.SampleRate));
            Assert.False(string.IsNullOrWhiteSpace(BleInputFSStrings.NumericType));
            Assert.False(string.IsNullOrWhiteSpace(BleInputFSStrings.ByteOrder));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
