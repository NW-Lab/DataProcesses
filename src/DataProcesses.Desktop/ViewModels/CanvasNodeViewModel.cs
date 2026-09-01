using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Text.Json;
using System.Text.Json.Nodes;

using Avalonia;
using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.Input;

using DataProcesses.Core;
using DataProcesses.Nodes.BuiltIn.Blocks.BleInputSt;
using DataProcesses.Nodes.BuiltIn.Blocks.BleInputVector;
using DataProcesses.Nodes.BuiltIn.Blocks.BreathSt;
using DataProcesses.Nodes.BuiltIn.Blocks.FilterSt;
using DataProcesses.Nodes.BuiltIn.Blocks.MovingAverage;
using DataProcesses.Nodes.BuiltIn.Blocks.SerialInputSt;
using DataProcesses.Nodes.BuiltIn.Blocks.SerialInputVector;
using DataProcesses.Nodes.BuiltIn.Blocks.StreamChartSt;
using DataProcesses.Nodes.BuiltIn.Blocks.StreamChartVector;
using DataProcesses.Nodes.BuiltIn.Blocks.TestSignalVec;
using DataProcesses.Plugin.Abstractions;

using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace DataProcesses.Desktop.ViewModels;

public sealed class CanvasNodeViewModel : ViewModelBase
{
    private const string TestSignalTypeId = "dataprocesses.test-signal";
    private const string TestSignalVecTypeId = "dataprocesses.test-signal-vec";
    private const string TestSignalImgTypeId = "dataprocesses.test-signal-img";
    private const string TriggerTypeId = "dataprocesses.trigger";
    private const string CameraInputImageTypeId = "dataprocesses.input.camera-image";
    private const string MovieInputImageTypeId = "dataprocesses.input.movie-image";
    private const string UVCameraInputImageTypeId = "dataprocesses.input.uv-camera-image";
    private const string CsvInputTypeId = "dataprocesses.input.csv";
    private const string SerialInputStTypeId = "dataprocesses.input.serial-st";
    private const string SerialInputVectorTypeId = "dataprocesses.input.serial-vector";
    private const string BleInputStTypeId = "dataprocesses.input.ble-st";
    private const string BleInputVectorTypeId = "dataprocesses.input.ble-vector";
    private const string CsvOutputTypeId = "dataprocesses.output.csv";

    private double x;
    private double y;
    private string name;
    private string description;
    private string settingsJson;
    private bool isSelected;
    private bool isEnabled;
    private bool showOnDashboard;
    private int dashboardGridWidth;
    private int dashboardGridHeight;
    private long triggerManualTriggerNonce;
    private readonly IReadOnlyList<PortDefinition> inputDefinitions;
    private readonly IReadOnlyList<PortDefinition> outputDefinitions;

    public CanvasNodeViewModel(NodeInstance instance, NodeDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(definition);

        Id = instance.Id;
        TypeId = instance.TypeId;
        Definition = definition;
        x = instance.X;
        y = instance.Y;
        name = string.IsNullOrWhiteSpace(instance.Name) ? Title : instance.Name;
        description = instance.Description ?? string.Empty;
        settingsJson = instance.SettingsJson;
        isEnabled = instance.IsEnabled;
        showOnDashboard = instance.ShowOnDashboard ?? definition.DashboardWidget?.IsVisibleByDefault ?? false;
        dashboardGridWidth = Math.Max(1, instance.DashboardGridWidth ?? definition.DashboardWidget?.GridWidth ?? 2);
        dashboardGridHeight = Math.Max(1, instance.DashboardGridHeight ?? definition.DashboardWidget?.GridHeight ?? 2);
        IconImage = NodeIconLoader.Load(definition.IconPath);
        inputDefinitions = definition.Ports.Where(static port => port.Direction == PortDirection.Input).ToArray();
        outputDefinitions = definition.Ports.Where(static port => port.Direction == PortDirection.Output).ToArray();
        Inputs = new ObservableCollection<CanvasPortViewModel>(
            inputDefinitions.Select(port => new CanvasPortViewModel(this, port)));
        Outputs = new ObservableCollection<CanvasPortViewModel>(
            outputDefinitions.Select(port => new CanvasPortViewModel(this, port)));

        if (IsCsvInputNode)
        {
            RefreshCsvInputOutputPorts();
        }

        BleInputStResetNordicUuidsCommand = new RelayCommand(ResetBleInputStNordicUuids);
        BleInputStRefreshDevicesCommand = new AsyncRelayCommand(RefreshBleInputStDevicesAsync);
        AddBleInputStDeviceChoice(BleInputStDeviceId, BleInputStDeviceName);
    }

    public string Id { get; }

    public string TypeId { get; }

    public NodeDefinition Definition { get; }

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? Title : Name;

    public string Title => string.IsNullOrWhiteSpace(Definition.Title) ? Definition.DisplayName : Definition.Title;

    public string IconPath => NodeIconLoader.ResolvePath(Definition.IconPath);

    public Bitmap? IconImage { get; }

    public bool HasIcon => IconImage is not null;

    public bool IsTestSignal => string.Equals(TypeId, TestSignalTypeId, StringComparison.Ordinal);

    public bool IsTestSignalVec => string.Equals(TypeId, TestSignalVecTypeId, StringComparison.Ordinal);

    public bool IsTestSignalImg => string.Equals(TypeId, TestSignalImgTypeId, StringComparison.Ordinal);

    public bool IsTestSignalOrVector => IsTestSignal || IsTestSignalVec;

    public bool IsTriggerNode => string.Equals(TypeId, TriggerTypeId, StringComparison.Ordinal);

    public bool IsCameraInputImageNode => string.Equals(TypeId, CameraInputImageTypeId, StringComparison.Ordinal);

    public bool IsUVCameraInputImageNode => string.Equals(TypeId, UVCameraInputImageTypeId, StringComparison.Ordinal);

    public bool IsCameraSourceNode => IsCameraInputImageNode || IsUVCameraInputImageNode;

    public bool IsMovieInputImageNode => string.Equals(TypeId, MovieInputImageTypeId, StringComparison.Ordinal);

    public bool IsManualTriggerNode => IsTriggerNode || IsCameraInputImageNode;

    public bool IsStartStopNode => string.Equals(TypeId, TestSignalVecTypeId, StringComparison.Ordinal)
        || string.Equals(TypeId, TestSignalImgTypeId, StringComparison.Ordinal);

    public bool IsDashboardToggleNode => IsStartStopNode || IsTestSignal;

    public bool IsDashboardActionNode => IsManualTriggerNode || IsDashboardToggleNode;

    public bool IsCsvInputNode => string.Equals(TypeId, CsvInputTypeId, StringComparison.Ordinal);

    public bool IsSerialInputStNode => string.Equals(TypeId, SerialInputStTypeId, StringComparison.Ordinal);

    public bool IsSerialInputVectorNode => string.Equals(TypeId, SerialInputVectorTypeId, StringComparison.Ordinal);

    public bool IsBleInputStNode => string.Equals(TypeId, BleInputStTypeId, StringComparison.Ordinal);

    public bool IsBleInputVectorNode => string.Equals(TypeId, BleInputVectorTypeId, StringComparison.Ordinal);

    public bool IsBleInputNode => IsBleInputStNode || IsBleInputVectorNode;

    public bool IsCsvOutputNode => string.Equals(TypeId, CsvOutputTypeId, StringComparison.Ordinal);

    public bool IsStreamChartVectorNode => string.Equals(TypeId, StreamChartVectorBlock.TypeId, StringComparison.Ordinal);

    public bool IsStreamChartStNode => string.Equals(TypeId, StreamChartStBlock.TypeId, StringComparison.Ordinal);

    public bool IsBreathStNode => string.Equals(TypeId, BreathStBlock.TypeId, StringComparison.Ordinal);

    public bool IsFilterStNode => string.Equals(TypeId, FilterStBlock.TypeId, StringComparison.Ordinal);

    public bool IsMovingAverageNode => string.Equals(TypeId, MovingAverageBlock.TypeId, StringComparison.Ordinal);

    public IReadOnlyList<string> TestSignalWaveTypes => IsTestSignalVec
        ? ["oneShot", "sine"]
        : ["sine", "square"];

    public IReadOnlyList<string> TestSignalImageTypes { get; } = ["number", "circle"];

    public IReadOnlyList<string> TestSignalImageModes { get; } = ["mono", "color"];

    public IReadOnlyList<string> TriggerPayloadValueTypes { get; } = ["datetime", "boolean", "string", "numberArray", "number"];

    public IReadOnlyList<bool> TriggerBooleanChoices { get; } = [true, false];

    public IReadOnlyList<string> CsvInputSourceTypes { get; } = ["file", "com"];

    public IReadOnlyList<string> CsvInputFilePlaybackModes { get; } = ["immediate", "millis"];

    public IReadOnlyList<string> SerialInputStComPorts => GetSerialInputStComPorts();

    public ObservableCollection<BleInputStDeviceChoice> BleInputStDeviceChoices { get; } = [];

    public IReadOnlyList<string> CsvOutputWriteModes { get; } = ["append", "new"];

    public IReadOnlyList<string> BreathStDetectionMethods { get; } = ["breathBelt", "ledOxygen"];

    public IReadOnlyList<string> FilterStTypes { get; } = ["lowPass", "highPass", "bandPass", "bandStop"];

    public IReadOnlyList<int> FilterStOrders { get; } = Enumerable.Range(FilterStSettings.MinimumOrder, FilterStSettings.MaximumOrder - FilterStSettings.MinimumOrder + 1).ToArray();

    public IReadOnlyList<string> MovingAverageWindowModes { get; } = ["samples", "duration"];

    public IRelayCommand BleInputStResetNordicUuidsCommand { get; }

    public IAsyncRelayCommand BleInputStRefreshDevicesCommand { get; }

    public string TestSignalWaveType
    {
        get => NormalizeTestSignalWaveType(ReadSettingsString("waveType", GetDefaultWaveType()));
        set => UpdateSettingsString("waveType", NormalizeTestSignalWaveType(value));
    }

    public string BreathStDetectionMethod
    {
        get => NormalizeBreathStDetectionMethod(ReadSettingsString("method", "breathBelt"));
        set => UpdateSettingsString("method", NormalizeBreathStDetectionMethod(value));
    }

    public bool BreathStEmitAnomalyEvents
    {
        get => ReadSettingsBoolean("emitAnomalyEvents", true);
        set => UpdateSettingsBoolean("emitAnomalyEvents", value);
    }

    public string FilterStType
    {
        get => NormalizeFilterStType(ReadSettingsString("filterType", "lowPass"));
        set
        {
            UpdateSettingsString("filterType", NormalizeFilterStType(value));
            NotifyFilterStSettingsChanged();
        }
    }

    public bool FilterStIsSingleCutoffVisible => FilterStType is "lowPass" or "highPass";

    public bool FilterStIsBandCutoffVisible => !FilterStIsSingleCutoffVisible;

    public double FilterStCutoffFrequencyHertz
    {
        get => ReadSettingsDouble("cutoffFrequencyHertz", FilterStSettings.Default.CutoffFrequencyHertz);
        set
        {
            UpdateSettingsDouble("cutoffFrequencyHertz", value, minimumExclusive: 0);
            NotifyFilterStSettingsChanged();
        }
    }

    public double FilterStLowerCutoffFrequencyHertz
    {
        get => ReadSettingsDouble("lowerCutoffFrequencyHertz", FilterStSettings.Default.LowerCutoffFrequencyHertz);
        set
        {
            if (!double.IsFinite(value) || value <= 0.0 || value >= FilterStUpperCutoffFrequencyHertz)
            {
                return;
            }

            UpdateSettingsDouble("lowerCutoffFrequencyHertz", value, minimumExclusive: 0);
            NotifyFilterStSettingsChanged();
        }
    }

    public double FilterStUpperCutoffFrequencyHertz
    {
        get => ReadSettingsDouble("upperCutoffFrequencyHertz", FilterStSettings.Default.UpperCutoffFrequencyHertz);
        set
        {
            if (!double.IsFinite(value) || value <= FilterStLowerCutoffFrequencyHertz)
            {
                return;
            }

            UpdateSettingsDouble("upperCutoffFrequencyHertz", value, minimumExclusive: 0);
            NotifyFilterStSettingsChanged();
        }
    }

    public int FilterStOrder
    {
        get => Math.Clamp(
            (int)Math.Round(ReadSettingsDouble("order", FilterStSettings.Default.Order), MidpointRounding.AwayFromZero),
            FilterStSettings.MinimumOrder,
            FilterStSettings.MaximumOrder);
        set
        {
            UpdateSettingsInt("order", value, FilterStSettings.MinimumOrder, FilterStSettings.MaximumOrder);
            NotifyFilterStSettingsChanged();
        }
    }

    public IReadOnlyList<Point> FilterStResponsePoints => CreateFilterStResponsePoints();

    public string FilterStResponseCaption => FilterStIsSingleCutoffVisible
        ? $"0-{Math.Max(10.0, FilterStCutoffFrequencyHertz * 2.5):0.#} Hz"
        : $"0-{Math.Max(10.0, FilterStUpperCutoffFrequencyHertz * 2.5):0.#} Hz";

    public string MovingAverageWindowMode
    {
        get => NormalizeMovingAverageWindowMode(ReadSettingsString("windowMode", "samples"));
        set
        {
            UpdateSettingsString("windowMode", NormalizeMovingAverageWindowMode(value));
            OnPropertyChanged(nameof(IsMovingAverageSampleWindowVisible));
            OnPropertyChanged(nameof(IsMovingAverageDurationWindowVisible));
        }
    }

    public double MovingAverageWindowSize
    {
        get => ReadSettingsDouble("windowSize", MovingAverageSettings.Default.WindowSize);
        set => UpdateSettingsInt("windowSize", (int)Math.Round(value, MidpointRounding.AwayFromZero), 1, int.MaxValue);
    }

    public double MovingAverageWindowDurationMilliseconds
    {
        get => ReadSettingsDouble("windowDurationMilliseconds", MovingAverageSettings.Default.WindowDurationMilliseconds);
        set => UpdateSettingsDouble("windowDurationMilliseconds", value, minimumExclusive: 0);
    }

    public bool IsMovingAverageSampleWindowVisible => string.Equals(MovingAverageWindowMode, "samples", StringComparison.Ordinal);

    public bool IsMovingAverageDurationWindowVisible => string.Equals(MovingAverageWindowMode, "duration", StringComparison.Ordinal);

    public double TestSignalFrequencyHertz
    {
        get => ReadSettingsDouble("frequency", 10.0);
        set => UpdateSettingsDouble("frequency", value, minimumExclusive: 0);
    }

    public double TestSignalSamplePeriodMilliseconds
    {
        get => ReadSettingsDouble("samplePeriodMillis", 1.0);
        set => UpdateSettingsDouble("samplePeriodMillis", value, minimumExclusive: 0);
    }

    public double TestSignalVectorLength
    {
        get => ReadSettingsDouble("length", TestSignalVecSettings.DefaultLength);
        set => UpdateSettingsInt(
            "length",
            (int)Math.Round(value, MidpointRounding.AwayFromZero),
            minimumInclusive: TestSignalVecSettings.MinimumLength,
            maximumInclusive: TestSignalVecSettings.MaximumLength);
    }

    public string TestSignalImageMode
    {
        get => NormalizeTestSignalImageMode(ReadSettingsString("kind", "mono"));
        set => UpdateSettingsString("kind", NormalizeTestSignalImageMode(value));
    }

    public string TestSignalImageType
    {
        get => NormalizeTestSignalImageType(ReadSettingsString("type", "number"));
        set => UpdateSettingsString("type", NormalizeTestSignalImageType(value));
    }

    public double TestSignalImageFrequencyHertz
    {
        get => ReadSettingsDouble("frequency", 1.0);
        set => UpdateSettingsDouble("frequency", value, minimumExclusive: 0);
    }

    public double TestSignalImageWidth
    {
        get => ReadSettingsDouble("width", 100);
        set => UpdateSettingsInt("width", (int)Math.Round(value, MidpointRounding.AwayFromZero), minimumInclusive: 1, maximumInclusive: 1024);
    }

    public double TestSignalImageHeight
    {
        get => ReadSettingsDouble("height", 100);
        set => UpdateSettingsInt("height", (int)Math.Round(value, MidpointRounding.AwayFromZero), minimumInclusive: 1, maximumInclusive: 1024);
    }

    public bool TestSignalIsEnabled => ReadSettingsBoolean("isEnabled", true);

    public bool TriggerEmitOnExecutionStart
    {
        get => ReadSettingsBoolean("emitOnExecutionStart", true);
        set => UpdateSettingsBoolean("emitOnExecutionStart", value);
    }

    public bool TriggerEmitPeriodically
    {
        get => ReadSettingsBoolean("emitPeriodically", false);
        set => UpdateSettingsBoolean("emitPeriodically", value);
    }

    public double TriggerInitialDelayMilliseconds
    {
        get => ReadSettingsDouble("initialDelayMilliseconds", 0);
        set => UpdateSettingsDoubleInclusive("initialDelayMilliseconds", value, minimumInclusive: 0);
    }

    public double TriggerRepeatIntervalMilliseconds
    {
        get => ReadSettingsDouble("repeatIntervalMilliseconds", 1000);
        set => UpdateSettingsDouble("repeatIntervalMilliseconds", value, minimumExclusive: 0);
    }

    public string TriggerTopic
    {
        get => ReadSettingsString("topic", "dataprocesses.trigger");
        set => UpdateSettingsString("topic", string.IsNullOrWhiteSpace(value) ? "dataprocesses.trigger" : value.Trim());
    }

    public string TriggerPayloadPath
    {
        get => ReadSettingsString("payloadPath", "payload.value");
        set => UpdateSettingsString("payloadPath", string.IsNullOrWhiteSpace(value) ? "payload.value" : value.Trim());
    }

    public string TriggerPayloadValueType
    {
        get => NormalizeTriggerPayloadValueType(ReadSettingsString("payloadValueType", "datetime"));
        set => UpdateSettingsString("payloadValueType", NormalizeTriggerPayloadValueType(value));
    }

    public bool TriggerBoolValue
    {
        get => ReadSettingsBoolean("boolValue", true);
        set => UpdateSettingsBoolean("boolValue", value);
    }

    public string TriggerStringValue
    {
        get => ReadSettingsString("stringValue", "trigger");
        set => UpdateSettingsString("stringValue", value ?? string.Empty);
    }

    public double TriggerNumberValue
    {
        get => ReadSettingsDouble("numberValue", 1.0);
        set => UpdateSettingsDoubleInclusive("numberValue", value, double.NegativeInfinity);
    }

    public string TriggerNumberArrayText
    {
        get => ReadSettingsString("numberArrayText", "1,2,3");
        set => UpdateSettingsString("numberArrayText", value ?? string.Empty);
    }

    public bool IsTriggerBoolEditorVisible => string.Equals(TriggerPayloadValueType, "boolean", StringComparison.Ordinal);

    public bool IsTriggerStringEditorVisible => string.Equals(TriggerPayloadValueType, "string", StringComparison.Ordinal);

    public bool IsTriggerNumberEditorVisible => string.Equals(TriggerPayloadValueType, "number", StringComparison.Ordinal);

    public bool IsTriggerNumberArrayEditorVisible => string.Equals(TriggerPayloadValueType, "numberArray", StringComparison.Ordinal);

    public string CsvInputSourceType
    {
        get => NormalizeCsvInputSourceType(ReadSettingsString("sourceType", "file"));
        set
        {
            UpdateSettingsString("sourceType", NormalizeCsvInputSourceType(value));
            OnPropertyChanged(nameof(CsvInputIsFileSourceVisible));
            OnPropertyChanged(nameof(CsvInputIsComSourceVisible));
            OnPropertyChanged(nameof(CsvInputIsFilePlaybackVisible));
        }
    }

    public string CsvInputFilePath
    {
        get => ReadSettingsString("filePath", string.Empty);
        set => UpdateSettingsString("filePath", value ?? string.Empty);
    }

    public string CsvInputComPortName
    {
        get => ReadSettingsString("comPortName", "COM3");
        set => UpdateSettingsString("comPortName", string.IsNullOrWhiteSpace(value) ? "COM3" : value.Trim());
    }

    public double CsvInputBaudRate
    {
        get => ReadSettingsDouble("baudRate", 115200);
        set => UpdateSettingsInt("baudRate", (int)Math.Round(value, MidpointRounding.AwayFromZero), minimumInclusive: 1, maximumInclusive: int.MaxValue);
    }

    public double CsvInputOutputCount
    {
        get => ReadSettingsDouble("outputCount", 2);
        set => UpdateSettingsInt("outputCount", (int)Math.Round(value, MidpointRounding.AwayFromZero), minimumInclusive: 1, maximumInclusive: 16);
    }

    public bool CsvInputHasHeaderRow
    {
        get => ReadSettingsBoolean("hasHeaderRow", true);
        set => UpdateSettingsBoolean("hasHeaderRow", value);
    }

    public string CsvInputFilePlaybackMode
    {
        get => NormalizeCsvInputFilePlaybackMode(ReadSettingsString("filePlaybackMode", "immediate"));
        set => UpdateSettingsString("filePlaybackMode", NormalizeCsvInputFilePlaybackMode(value));
    }

    public bool CsvInputIsFileSourceVisible => string.Equals(CsvInputSourceType, "file", StringComparison.Ordinal);

    public bool CsvInputIsComSourceVisible => string.Equals(CsvInputSourceType, "com", StringComparison.Ordinal);

    public bool CsvInputIsFilePlaybackVisible => CsvInputIsFileSourceVisible;

    public string SerialInputStComPortName
    {
        get => ReadSettingsString("comPortName", SerialInputStSettings.Default.ComPortName);
        set => UpdateSettingsString(
            "comPortName",
            string.IsNullOrWhiteSpace(value) ? SerialInputStSettings.Default.ComPortName : value.Trim());
    }

    public double SerialInputStBaudRate
    {
        get => ReadSettingsDouble("baudRate", SerialInputStSettings.Default.BaudRate);
        set => UpdateSettingsInt("baudRate", (int)Math.Round(value, MidpointRounding.AwayFromZero), 1, int.MaxValue);
    }

    public double SerialInputStChannelCount
    {
        get => ReadSettingsDouble("channelCount", SerialInputStSettings.Default.ChannelCount);
        set => UpdateSettingsInt(
            "channelCount",
            (int)Math.Round(value, MidpointRounding.AwayFromZero),
            SerialInputStSettings.MinimumChannelCount,
            SerialInputStSettings.MaximumChannelCount);
    }

    public string SerialInputVectorComPortName
    {
        get => ReadSettingsString("comPortName", SerialInputVectorSettings.Default.ComPortName);
        set => UpdateSettingsString(
            "comPortName",
            string.IsNullOrWhiteSpace(value) ? SerialInputVectorSettings.Default.ComPortName : value.Trim());
    }

    public double SerialInputVectorBaudRate
    {
        get => ReadSettingsDouble("baudRate", SerialInputVectorSettings.Default.BaudRate);
        set => UpdateSettingsInt("baudRate", (int)Math.Round(value, MidpointRounding.AwayFromZero), 1, int.MaxValue);
    }

    public string BleInputStDeviceId
    {
        get => ReadSettingsString("deviceId", BleInputStSettings.Default.DeviceId);
        set
        {
            UpdateSettingsString("deviceId", value?.Trim() ?? string.Empty);
            AddBleInputStDeviceChoice(BleInputStDeviceId, BleInputStDeviceName);
            OnPropertyChanged(nameof(BleInputStSelectedDevice));
        }
    }

    public string BleInputStDeviceName
    {
        get => ReadSettingsString("deviceName", BleInputStSettings.Default.DeviceName);
        set
        {
            UpdateSettingsString("deviceName", value?.Trim() ?? string.Empty);
            AddBleInputStDeviceChoice(BleInputStDeviceId, BleInputStDeviceName);
            OnPropertyChanged(nameof(BleInputStSelectedDevice));
        }
    }

    public BleInputStDeviceChoice? BleInputStSelectedDevice
    {
        get
        {
            var deviceId = BleInputStDeviceId;
            return string.IsNullOrWhiteSpace(deviceId)
                ? null
                : BleInputStDeviceChoices.FirstOrDefault(choice => string.Equals(choice.DeviceId, deviceId, StringComparison.Ordinal));
        }

        set
        {
            if (value is null)
            {
                return;
            }

            UpdateSettingsString("deviceId", value.DeviceId);
            UpdateSettingsString("deviceName", value.Name);
            AddBleInputStDeviceChoice(value.DeviceId, value.Name);
            OnPropertyChanged();
        }
    }

    public bool BleInputStAutoConnect
    {
        get => ReadSettingsBoolean("autoConnect", BleInputStSettings.Default.AutoConnect);
        set => UpdateSettingsBoolean("autoConnect", value);
    }

    public string BleInputStServiceUuid
    {
        get => ReadSettingsString("serviceUuid", BleInputStSettings.Default.ServiceUuid);
        set => UpdateSettingsString(
            "serviceUuid",
            string.IsNullOrWhiteSpace(value) ? BleInputStSettings.Default.ServiceUuid : value.Trim());
    }

    public string BleInputStNotifyCharacteristicUuid
    {
        get => ReadSettingsString("notifyCharacteristicUuid", BleInputStSettings.Default.NotifyCharacteristicUuid);
        set => UpdateSettingsString(
            "notifyCharacteristicUuid",
            string.IsNullOrWhiteSpace(value) ? BleInputStSettings.Default.NotifyCharacteristicUuid : value.Trim());
    }

    public double BleInputStChannelCount
    {
        get => ReadSettingsDouble("channelCount", BleInputStSettings.Default.ChannelCount);
        set => UpdateSettingsInt(
            "channelCount",
            (int)Math.Round(value, MidpointRounding.AwayFromZero),
            BleInputStSettings.MinimumChannelCount,
            BleInputStSettings.MaximumChannelCount);
    }

    public double BleInputStTimeoutMilliseconds
    {
        get => ReadSettingsDouble("timeoutMilliseconds", BleInputStSettings.Default.TimeoutMilliseconds);
        set => UpdateSettingsInt(
            "timeoutMilliseconds",
            (int)Math.Round(value, MidpointRounding.AwayFromZero),
            BleInputStSettings.MinimumTimeoutMilliseconds,
            BleInputStSettings.MaximumTimeoutMilliseconds);
    }

    public string CsvOutputFilePath
    {
        get => ReadSettingsString("filePath", string.Empty);
        set => UpdateSettingsString("filePath", value ?? string.Empty);
    }

    public string MovieInputImagePath
    {
        get => ReadSettingsString("moviePath", string.Empty);
        set => UpdateSettingsString("moviePath", value ?? string.Empty);
    }

    public double CameraInputImageDeviceIndex
    {
        get => ReadSettingsDouble("deviceIndex", 0);
        set => UpdateSettingsInt("deviceIndex", (int)Math.Round(value, MidpointRounding.AwayFromZero), minimumInclusive: 0, maximumInclusive: int.MaxValue);
    }

    public double CameraInputImageWidth
    {
        get => ReadSettingsDouble("width", 1920);
        set => UpdateSettingsInt("width", (int)Math.Round(value, MidpointRounding.AwayFromZero), minimumInclusive: 1, maximumInclusive: 3840);
    }

    public double CameraInputImageHeight
    {
        get => ReadSettingsDouble("height", 1080);
        set => UpdateSettingsInt("height", (int)Math.Round(value, MidpointRounding.AwayFromZero), minimumInclusive: 1, maximumInclusive: 2160);
    }

    public bool CameraInputImageContinuousCapture
    {
        get => ReadSettingsBoolean("continuousCapture", false);
        set => UpdateSettingsBoolean("continuousCapture", value);
    }

    public double CameraInputImageFramesPerSecond
    {
        get => ReadSettingsDouble("fps", 10.0);
        set => UpdateSettingsDouble("fps", value, minimumExclusive: 0);
    }

    public bool CameraWhiteBalanceAuto
    {
        get => ReadSettingsBoolean("isWhiteBalanceAuto", true);
        set
        {
            UpdateSettingsBoolean("isWhiteBalanceAuto", value);
            OnPropertyChanged(nameof(IsCameraWhiteBalanceTemperatureVisible));
        }
    }

    public bool IsCameraWhiteBalanceTemperatureVisible => IsCameraSourceNode && !CameraWhiteBalanceAuto;

    public double CameraWhiteBalanceTemperature
    {
        get => ReadSettingsDouble("whiteBalanceTemperature", 4_500);
        set => UpdateSettingsDouble("whiteBalanceTemperature", value, minimumExclusive: 0);
    }

    public double MovieInputImageFramesPerSecond
    {
        get => ReadSettingsDouble("fps", 10.0);
        set => UpdateSettingsDouble("fps", value, minimumExclusive: 0);
    }

    public double MovieInputImageWidth
    {
        get => ReadSettingsDouble("width", 640);
        set => UpdateSettingsInt("width", (int)Math.Round(value, MidpointRounding.AwayFromZero), minimumInclusive: 1, maximumInclusive: 3840);
    }

    public double MovieInputImageHeight
    {
        get => ReadSettingsDouble("height", 480);
        set => UpdateSettingsInt("height", (int)Math.Round(value, MidpointRounding.AwayFromZero), minimumInclusive: 1, maximumInclusive: 3840);
    }

    public string CsvOutputWriteMode
    {
        get => NormalizeCsvOutputWriteMode(ReadSettingsString("writeMode", "append"));
        set => UpdateSettingsString("writeMode", NormalizeCsvOutputWriteMode(value));
    }

    public double CsvOutputSpanMilliseconds
    {
        get => ReadSettingsDouble("spanMilliseconds", 100);
        set => UpdateSettingsDouble("spanMilliseconds", value, minimumExclusive: 0);
    }

    public IReadOnlyList<string> StreamChartVectorColorMaps { get; } = ["jet", "grayscale", "hot", "viridis"];

    public string StreamChartVectorColorMap
    {
        get => NormalizeStreamChartVectorColorMap(ReadSettingsString("colorMap", "jet"));
        set => UpdateSettingsString("colorMap", NormalizeStreamChartVectorColorMap(value));
    }

    public bool StreamChartVectorAutoScale
    {
        get => ReadSettingsBoolean("autoScale", true);
        set
        {
            UpdateSettingsBoolean("autoScale", value);
            OnPropertyChanged(nameof(StreamChartVectorIsManualScaleVisible));
        }
    }

    public bool StreamChartVectorIsManualScaleVisible => !StreamChartVectorAutoScale;

    public double StreamChartVectorMinimumValue
    {
        get => ReadSettingsDouble("minValue", 0);
        set => UpdateSettingsDoubleInclusive("minValue", value, double.NegativeInfinity);
    }

    public double StreamChartVectorMaximumValue
    {
        get => ReadSettingsDouble("maxValue", 1);
        set => UpdateSettingsDoubleInclusive("maxValue", value, double.NegativeInfinity);
    }

    public bool StreamChartVectorInterpolate
    {
        get => ReadSettingsBoolean("interpolate", true);
        set => UpdateSettingsBoolean("interpolate", value);
    }

    public double StreamChartVectorTimeSpanMilliseconds
    {
        get => ReadSettingsDouble("timeSpanMillis", StreamChartVectorSettings.DefaultTimeSpanMilliseconds);
        set => UpdateSettingsDoubleInclusive(
            "timeSpanMillis",
            Math.Clamp(value, StreamChartVectorSettings.MinimumTimeSpanMilliseconds, StreamChartVectorSettings.MaximumTimeSpanMilliseconds),
            StreamChartVectorSettings.MinimumTimeSpanMilliseconds);
    }

    public IReadOnlyList<string> StreamChartStTimeAlignmentModes { get; } = ["independent", "alignToFirstStream"];

    public string StreamChartStTimeAlignmentMode
    {
        get => NormalizeStreamChartStTimeAlignmentMode(ReadSettingsString("timeAlignmentMode", "independent"));
        set => UpdateSettingsString("timeAlignmentMode", NormalizeStreamChartStTimeAlignmentMode(value));
    }

    public double StreamChartStTimeSpanMilliseconds
    {
        get => ReadSettingsDouble("timeSpanMillis", StreamChartStSettings.DefaultTimeSpanMilliseconds);
        set => UpdateSettingsDouble(
            "timeSpanMillis",
            Math.Max(StreamChartStSettings.MinimumTimeSpanMilliseconds, value),
            minimumExclusive: 0);
    }

    public string StreamChartStChannel1Name
    {
        get => ReadSettingsString("channel1Name", "CH1");
        set => UpdateSettingsString("channel1Name", string.IsNullOrWhiteSpace(value) ? "CH1" : value.Trim());
    }

    public string StreamChartStChannel2Name
    {
        get => ReadSettingsString("channel2Name", "CH2");
        set => UpdateSettingsString("channel2Name", string.IsNullOrWhiteSpace(value) ? "CH2" : value.Trim());
    }

    public string StreamChartStChannel3Name
    {
        get => ReadSettingsString("channel3Name", "CH3");
        set => UpdateSettingsString("channel3Name", string.IsNullOrWhiteSpace(value) ? "CH3" : value.Trim());
    }

    public string StreamChartStChannel4Name
    {
        get => ReadSettingsString("channel4Name", "CH4");
        set => UpdateSettingsString("channel4Name", string.IsNullOrWhiteSpace(value) ? "CH4" : value.Trim());
    }

    public string Category => Definition.Category;

    public ObservableCollection<CanvasPortViewModel> Inputs { get; }

    public ObservableCollection<CanvasPortViewModel> Outputs { get; }

    public double X
    {
        get => x;
        set => SetProperty(ref x, value);
    }

    public double Y
    {
        get => y;
        set => SetProperty(ref y, value);
    }

    public string Name
    {
        get => name;
        set
        {
            if (SetProperty(ref name, value))
            {
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    public string Description
    {
        get => description;
        set => SetProperty(ref description, value);
    }

    public bool IsEnabled
    {
        get => isEnabled;
        set => SetProperty(ref isEnabled, value);
    }

    public bool ShowOnDashboard
    {
        get => showOnDashboard;
        set => SetProperty(ref showOnDashboard, value);
    }

    public int DashboardGridWidth
    {
        get => dashboardGridWidth;
        set => SetProperty(ref dashboardGridWidth, Math.Max(1, value));
    }

    public int DashboardGridHeight
    {
        get => dashboardGridHeight;
        set => SetProperty(ref dashboardGridHeight, Math.Max(1, value));
    }

    public bool DashboardTextWrapEnabled
    {
        get => ReadSettingsBoolean("dashboardTextWrapEnabled", true);
        set => UpdateSettingsBoolean("dashboardTextWrapEnabled", value);
    }

    public string SettingsJson
    {
        get => settingsJson;
        set
        {
            if (SetProperty(ref settingsJson, string.IsNullOrWhiteSpace(value) ? "{}" : value))
            {
                if (IsCsvInputNode)
                {
                    RefreshCsvInputOutputPorts();
                }

                OnPropertyChanged(nameof(TestSignalWaveType));
                OnPropertyChanged(nameof(BreathStDetectionMethod));
                OnPropertyChanged(nameof(BreathStEmitAnomalyEvents));
                OnPropertyChanged(nameof(MovingAverageWindowMode));
                OnPropertyChanged(nameof(MovingAverageWindowSize));
                OnPropertyChanged(nameof(MovingAverageWindowDurationMilliseconds));
                OnPropertyChanged(nameof(IsMovingAverageSampleWindowVisible));
                OnPropertyChanged(nameof(IsMovingAverageDurationWindowVisible));
                OnPropertyChanged(nameof(FilterStType));
                OnPropertyChanged(nameof(FilterStIsSingleCutoffVisible));
                OnPropertyChanged(nameof(FilterStIsBandCutoffVisible));
                OnPropertyChanged(nameof(FilterStCutoffFrequencyHertz));
                OnPropertyChanged(nameof(FilterStLowerCutoffFrequencyHertz));
                OnPropertyChanged(nameof(FilterStUpperCutoffFrequencyHertz));
                OnPropertyChanged(nameof(FilterStOrder));
                OnPropertyChanged(nameof(FilterStResponsePoints));
                OnPropertyChanged(nameof(FilterStResponseCaption));
                OnPropertyChanged(nameof(TestSignalFrequencyHertz));
                OnPropertyChanged(nameof(TestSignalSamplePeriodMilliseconds));
                OnPropertyChanged(nameof(TestSignalVectorLength));
                OnPropertyChanged(nameof(TestSignalImageType));
                OnPropertyChanged(nameof(TestSignalImageMode));
                OnPropertyChanged(nameof(TestSignalImageFrequencyHertz));
                OnPropertyChanged(nameof(TestSignalImageWidth));
                OnPropertyChanged(nameof(TestSignalImageHeight));
                OnPropertyChanged(nameof(TestSignalIsEnabled));
                OnPropertyChanged(nameof(TriggerEmitOnExecutionStart));
                OnPropertyChanged(nameof(TriggerEmitPeriodically));
                OnPropertyChanged(nameof(TriggerInitialDelayMilliseconds));
                OnPropertyChanged(nameof(TriggerRepeatIntervalMilliseconds));
                OnPropertyChanged(nameof(TriggerTopic));
                OnPropertyChanged(nameof(TriggerPayloadPath));
                OnPropertyChanged(nameof(TriggerPayloadValueType));
                OnPropertyChanged(nameof(TriggerBoolValue));
                OnPropertyChanged(nameof(TriggerStringValue));
                OnPropertyChanged(nameof(TriggerNumberValue));
                OnPropertyChanged(nameof(TriggerNumberArrayText));
                OnPropertyChanged(nameof(IsTriggerBoolEditorVisible));
                OnPropertyChanged(nameof(IsTriggerStringEditorVisible));
                OnPropertyChanged(nameof(IsTriggerNumberEditorVisible));
                OnPropertyChanged(nameof(IsTriggerNumberArrayEditorVisible));
                OnPropertyChanged(nameof(CsvInputSourceType));
                OnPropertyChanged(nameof(CsvInputFilePath));
                OnPropertyChanged(nameof(CsvInputComPortName));
                OnPropertyChanged(nameof(CsvInputBaudRate));
                OnPropertyChanged(nameof(CsvInputOutputCount));
                OnPropertyChanged(nameof(CsvInputHasHeaderRow));
                OnPropertyChanged(nameof(CsvInputFilePlaybackMode));
                OnPropertyChanged(nameof(CsvInputIsFileSourceVisible));
                OnPropertyChanged(nameof(CsvInputIsComSourceVisible));
                OnPropertyChanged(nameof(CsvInputIsFilePlaybackVisible));
                OnPropertyChanged(nameof(SerialInputStComPortName));
                OnPropertyChanged(nameof(SerialInputStComPorts));
                OnPropertyChanged(nameof(SerialInputStBaudRate));
                OnPropertyChanged(nameof(SerialInputStChannelCount));
                OnPropertyChanged(nameof(SerialInputVectorComPortName));
                OnPropertyChanged(nameof(SerialInputVectorBaudRate));
                OnPropertyChanged(nameof(BleInputStDeviceId));
                OnPropertyChanged(nameof(BleInputStDeviceName));
                OnPropertyChanged(nameof(BleInputStDeviceChoices));
                OnPropertyChanged(nameof(BleInputStSelectedDevice));
                OnPropertyChanged(nameof(BleInputStAutoConnect));
                OnPropertyChanged(nameof(BleInputStServiceUuid));
                OnPropertyChanged(nameof(BleInputStNotifyCharacteristicUuid));
                OnPropertyChanged(nameof(BleInputStChannelCount));
                OnPropertyChanged(nameof(BleInputStTimeoutMilliseconds));
                OnPropertyChanged(nameof(CsvOutputFilePath));
                OnPropertyChanged(nameof(MovieInputImagePath));
                OnPropertyChanged(nameof(CameraInputImageDeviceIndex));
                OnPropertyChanged(nameof(CameraInputImageWidth));
                OnPropertyChanged(nameof(CameraInputImageHeight));
                OnPropertyChanged(nameof(CameraInputImageContinuousCapture));
                OnPropertyChanged(nameof(CameraInputImageFramesPerSecond));
                OnPropertyChanged(nameof(CameraWhiteBalanceAuto));
                OnPropertyChanged(nameof(IsCameraWhiteBalanceTemperatureVisible));
                OnPropertyChanged(nameof(CameraWhiteBalanceTemperature));
                OnPropertyChanged(nameof(MovieInputImageFramesPerSecond));
                OnPropertyChanged(nameof(MovieInputImageWidth));
                OnPropertyChanged(nameof(MovieInputImageHeight));
                OnPropertyChanged(nameof(CsvOutputWriteMode));
                OnPropertyChanged(nameof(CsvOutputSpanMilliseconds));
                OnPropertyChanged(nameof(DashboardTextWrapEnabled));
            }
        }
    }

    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }

    public NodeInstance ToNodeInstance()
    {
        return new NodeInstance(
            Id,
            TypeId,
            X,
            Y,
            SettingsJson,
            Name,
            Description,
            IsEnabled,
            ShowOnDashboard,
            DashboardGridWidth,
            DashboardGridHeight);
    }

    public string BuildRuntimeSettingsJson(long triggerExecutionSessionId)
    {
        if (!IsManualTriggerNode && !IsStartStopNode && !IsMovieInputImageNode && !IsUVCameraInputImageNode)
        {
            return SettingsJson;
        }

        var settings = ReadSettingsObject();
        if (IsManualTriggerNode)
        {
            settings["executionSessionId"] = triggerExecutionSessionId;
            settings["manualTriggerNonce"] = triggerManualTriggerNonce;
        }

        if (IsMovieInputImageNode)
        {
            settings["executionSessionId"] = triggerExecutionSessionId;
        }

        if (IsUVCameraInputImageNode)
        {
            settings["executionSessionId"] = triggerExecutionSessionId;
        }

        return settings.ToJsonString();
    }

    public void RequestTriggerNow()
    {
        if (!IsManualTriggerNode)
        {
            return;
        }

        triggerManualTriggerNonce++;
    }

    public void ToggleStartStop()
    {
        if (!IsDashboardToggleNode)
        {
            return;
        }

        var settings = ReadSettingsObject();
        var currentEnabled = ReadSettingsBoolean("isEnabled", true);
        settings["isEnabled"] = !currentEnabled;

        SettingsJson = settings.ToJsonString();
    }

    public void RefreshDynamicPortsFromSettings()
    {
        RefreshCsvInputOutputPorts();
    }

    private string ReadSettingsString(string propertyName, string fallback)
    {
        if (TryGetSettingsProperty(propertyName, out var value)
            && value.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(value.GetString()))
        {
            return value.GetString()!;
        }

        return fallback;
    }

    private double ReadSettingsDouble(string propertyName, double fallback)
    {
        if (TryGetSettingsProperty(propertyName, out var value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetDouble(out var result)
            && double.IsFinite(result))
        {
            return result;
        }

        return fallback;
    }

    private bool ReadSettingsBoolean(string propertyName, bool fallback)
    {
        if (TryGetSettingsProperty(propertyName, out var value)
            && value.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return value.GetBoolean();
        }

        return fallback;
    }

    private bool TryGetSettingsProperty(string propertyName, out JsonElement value)
    {
        try
        {
            using var document = JsonDocument.Parse(SettingsJson);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty(propertyName, out value))
            {
                value = value.Clone();
                return true;
            }
        }
        catch (JsonException)
        {
        }

        value = default;
        return false;
    }

    private void UpdateSettingsString(string propertyName, string value)
    {
        var settings = ReadSettingsObject();
        settings[propertyName] = value;
        SettingsJson = settings.ToJsonString();
    }

    private void UpdateSettingsDouble(string propertyName, double value, double minimumExclusive)
    {
        if (!double.IsFinite(value) || value <= minimumExclusive)
        {
            return;
        }

        var settings = ReadSettingsObject();
        settings[propertyName] = value;
        SettingsJson = settings.ToJsonString();
    }

    private void UpdateSettingsDoubleInclusive(string propertyName, double value, double minimumInclusive)
    {
        if (!double.IsFinite(value))
        {
            return;
        }

        if (value < minimumInclusive)
        {
            return;
        }

        var settings = ReadSettingsObject();
        settings[propertyName] = value;
        SettingsJson = settings.ToJsonString();
    }

    private void UpdateSettingsBoolean(string propertyName, bool value)
    {
        var settings = ReadSettingsObject();
        settings[propertyName] = value;
        SettingsJson = settings.ToJsonString();
    }

    private void UpdateSettingsInt(string propertyName, int value, int minimumInclusive, int maximumInclusive)
    {
        if (value < minimumInclusive || value > maximumInclusive)
        {
            return;
        }

        var settings = ReadSettingsObject();
        settings[propertyName] = value;
        SettingsJson = settings.ToJsonString();
    }

    private JsonObject ReadSettingsObject()
    {
        try
        {
            return JsonNode.Parse(SettingsJson) as JsonObject ?? new JsonObject();
        }
        catch (JsonException)
        {
            return new JsonObject();
        }
    }

    private void RefreshCsvInputOutputPorts()
    {
        if (!IsCsvInputNode)
        {
            return;
        }

        var requestedCount = (int)Math.Round(ReadSettingsDouble("outputCount", 2), MidpointRounding.AwayFromZero);
        var outputCount = Math.Clamp(requestedCount, 1, outputDefinitions.Count);
        var visibleDefinitions = outputDefinitions.Take(outputCount).ToArray();

        if (Outputs.Count == visibleDefinitions.Length
            && Outputs.Zip(visibleDefinitions, static (port, definition) => string.Equals(port.Id, definition.Id, StringComparison.Ordinal)).All(static isSame => isSame))
        {
            return;
        }

        Outputs.Clear();
        foreach (var definition in visibleDefinitions)
        {
            Outputs.Add(new CanvasPortViewModel(this, definition));
        }
    }

    private IReadOnlyList<string> GetSerialInputStComPorts()
    {
        try
        {
            var detectedPorts = SerialPort.GetPortNames()
                .Append(SerialInputStComPortName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static port => port, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return detectedPorts;
        }
        catch (IOException)
        {
            return [SerialInputStComPortName];
        }
    }

    private void ResetBleInputStNordicUuids()
    {
        if (!IsBleInputNode)
        {
            return;
        }

        var settings = ReadSettingsObject();
        settings["serviceUuid"] = BleInputStSettings.NordicUartServiceUuid;
        settings["notifyCharacteristicUuid"] = BleInputStSettings.NordicUartTxCharacteristicUuid;
        SettingsJson = settings.ToJsonString();
    }

    private async Task RefreshBleInputStDevicesAsync()
    {
        if (!IsBleInputNode)
        {
            return;
        }

        var currentDeviceId = BleInputStDeviceId;
        var currentDeviceName = BleInputStDeviceName;
        BleInputStDeviceChoices.Clear();
        AddBleInputStDeviceChoice(currentDeviceId, currentDeviceName);

        var devices = await DeviceInformation.FindAllAsync(BluetoothLEDevice.GetDeviceSelector())
            .AsTask()
            .ConfigureAwait(true);
        foreach (var device in devices
                     .OrderBy(static device => device.Name, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(static device => device.Id, StringComparer.OrdinalIgnoreCase))
        {
            AddBleInputStDeviceChoice(device.Id, device.Name);
        }

        OnPropertyChanged(nameof(BleInputStSelectedDevice));
    }

    private void AddBleInputStDeviceChoice(string deviceId, string deviceName)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return;
        }

        var trimmedDeviceId = deviceId.Trim();
        var trimmedDeviceName = deviceName.Trim();
        for (var index = 0; index < BleInputStDeviceChoices.Count; index++)
        {
            if (!string.Equals(BleInputStDeviceChoices[index].DeviceId, trimmedDeviceId, StringComparison.Ordinal))
            {
                continue;
            }

            if (!string.Equals(BleInputStDeviceChoices[index].Name, trimmedDeviceName, StringComparison.Ordinal))
            {
                BleInputStDeviceChoices[index] = BleInputStDeviceChoices[index] with { Name = trimmedDeviceName };
            }

            return;
        }

        BleInputStDeviceChoices.Add(new BleInputStDeviceChoice(trimmedDeviceId, trimmedDeviceName));
    }

    public sealed record BleInputStDeviceChoice(string DeviceId, string Name)
    {
        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? DeviceId : $"{Name} ({DeviceId})";
    }

    private void NotifyFilterStSettingsChanged()
    {
        OnPropertyChanged(nameof(FilterStType));
        OnPropertyChanged(nameof(FilterStIsSingleCutoffVisible));
        OnPropertyChanged(nameof(FilterStIsBandCutoffVisible));
        OnPropertyChanged(nameof(FilterStCutoffFrequencyHertz));
        OnPropertyChanged(nameof(FilterStLowerCutoffFrequencyHertz));
        OnPropertyChanged(nameof(FilterStUpperCutoffFrequencyHertz));
        OnPropertyChanged(nameof(FilterStOrder));
        OnPropertyChanged(nameof(FilterStResponsePoints));
        OnPropertyChanged(nameof(FilterStResponseCaption));
    }

    private IReadOnlyList<Point> CreateFilterStResponsePoints()
    {
        const double width = 248.0;
        const double height = 72.0;
        const int pointCount = 48;

        var points = new Point[pointCount];
        var maxFrequency = FilterStIsSingleCutoffVisible
            ? Math.Max(10.0, FilterStCutoffFrequencyHertz * 2.5)
            : Math.Max(10.0, FilterStUpperCutoffFrequencyHertz * 2.5);

        for (var index = 0; index < points.Length; index++)
        {
            var ratio = index / (double)(points.Length - 1);
            var frequency = maxFrequency * ratio;
            var magnitude = GetFilterStPreviewMagnitude(frequency);
            points[index] = new Point(width * ratio, height - Math.Clamp(magnitude, 0.0, 1.0) * height);
        }

        return points;
    }

    private double GetFilterStPreviewMagnitude(double frequencyHertz)
    {
        var order = FilterStOrder;
        return FilterStType switch
        {
            "highPass" => GetHighPassPreviewMagnitude(frequencyHertz, FilterStCutoffFrequencyHertz, order),
            "bandPass" => GetHighPassPreviewMagnitude(frequencyHertz, FilterStLowerCutoffFrequencyHertz, order)
                * GetLowPassPreviewMagnitude(frequencyHertz, FilterStUpperCutoffFrequencyHertz, order),
            "bandStop" => Math.Min(1.0,
                GetLowPassPreviewMagnitude(frequencyHertz, FilterStLowerCutoffFrequencyHertz, order)
                + GetHighPassPreviewMagnitude(frequencyHertz, FilterStUpperCutoffFrequencyHertz, order)),
            _ => GetLowPassPreviewMagnitude(frequencyHertz, FilterStCutoffFrequencyHertz, order),
        };
    }

    private static double GetLowPassPreviewMagnitude(double frequencyHertz, double cutoffFrequencyHertz, int order)
    {
        if (frequencyHertz <= 0.0)
        {
            return 1.0;
        }

        return Math.Pow(1.0 / Math.Sqrt(1.0 + Math.Pow(frequencyHertz / cutoffFrequencyHertz, 2.0)), order);
    }

    private static double GetHighPassPreviewMagnitude(double frequencyHertz, double cutoffFrequencyHertz, int order)
    {
        if (frequencyHertz <= 0.0)
        {
            return 0.0;
        }

        var ratio = frequencyHertz / cutoffFrequencyHertz;
        return Math.Pow(ratio / Math.Sqrt(1.0 + ratio * ratio), order);
    }

    private string GetDefaultWaveType()
    {
        return IsTestSignalVec ? "oneShot" : "sine";
    }

    private string NormalizeTestSignalWaveType(string? value)
    {
        if (IsTestSignalVec)
        {
            if (string.Equals(value, "oneshot", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "oneShot", StringComparison.OrdinalIgnoreCase))
            {
                return "oneShot";
            }

            if (string.Equals(value, "sine", StringComparison.OrdinalIgnoreCase))
            {
                return "sine";
            }

            return "oneShot";
        }

        return string.Equals(value, "square", StringComparison.OrdinalIgnoreCase) ? "square" : "sine";
    }

    private static string NormalizeBreathStDetectionMethod(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "led" or "ledoxygen" or "led_oxygen" or "oxygen" or "spo2" or "bloodoxygen" or "blood_oxygen" => "ledOxygen",
            _ => "breathBelt",
        };
    }

    private static string NormalizeMovingAverageWindowMode(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "duration" or "time" => "duration",
            _ => "samples",
        };
    }

    private static string NormalizeFilterStType(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "highpass" or "high-pass" or "high_pass" => "highPass",
            "bandpass" or "band-pass" or "band_pass" => "bandPass",
            "bandstop" or "band-stop" or "band_stop" or "notch" => "bandStop",
            _ => "lowPass",
        };
    }

    private static string NormalizeTriggerPayloadValueType(string? value)
    {
        if (string.Equals(value, "boolean", StringComparison.OrdinalIgnoreCase))
        {
            return "boolean";
        }

        if (string.Equals(value, "string", StringComparison.OrdinalIgnoreCase))
        {
            return "string";
        }

        if (string.Equals(value, "numberarray", StringComparison.OrdinalIgnoreCase))
        {
            return "numberArray";
        }

        if (string.Equals(value, "number", StringComparison.OrdinalIgnoreCase))
        {
            return "number";
        }

        return "datetime";
    }

    private static string NormalizeCsvInputSourceType(string? value)
    {
        return string.Equals(value, "com", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "comport", StringComparison.OrdinalIgnoreCase)
            ? "com"
            : "file";
    }

    private static string NormalizeCsvInputFilePlaybackMode(string? value)
    {
        return string.Equals(value, "millis", StringComparison.OrdinalIgnoreCase)
            ? "millis"
            : "immediate";
    }

    private static string NormalizeCsvOutputWriteMode(string? value)
    {
        if (string.Equals(value, "new", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "overwrite", StringComparison.OrdinalIgnoreCase))
        {
            return "new";
        }

        return "append";
    }

    private static string NormalizeStreamChartVectorColorMap(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "grayscale" or "gray" or "grey" => "grayscale",
            "hot" => "hot",
            "viridis" => "viridis",
            _ => "jet",
        };
    }

    private static string NormalizeStreamChartStTimeAlignmentMode(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "aligntofirst" or "aligntofirststream" or "align_to_first" => "alignToFirstStream",
            _ => "independent",
        };
    }

    private static string NormalizeTestSignalImageMode(string? value)
    {
        return string.Equals(value, "color", StringComparison.OrdinalIgnoreCase)
            ? "color"
            : "mono";
    }

    private static string NormalizeTestSignalImageType(string? value)
    {
        return string.Equals(value, "circle", StringComparison.OrdinalIgnoreCase)
            ? "circle"
            : "number";
    }

}