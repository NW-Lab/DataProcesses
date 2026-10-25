using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Text.Json;
using System.Text.Json.Nodes;

using Avalonia;
using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.Input;

using DataProcesses.Core;
using DataProcesses.Nodes.BuiltIn.Blocks.BleInputTS;
using DataProcesses.Nodes.BuiltIn.Blocks.BleInputFS;
using DataProcesses.Nodes.BuiltIn.Blocks.BleInputVector;
using DataProcesses.Nodes.BuiltIn.Blocks.BreathTS;
using DataProcesses.Nodes.BuiltIn.Blocks.FilterTS;
using DataProcesses.Nodes.BuiltIn.Blocks.FilterFS;
using DataProcesses.Nodes.BuiltIn.Blocks.MovingAverage;
using DataProcesses.Nodes.BuiltIn.Blocks.MicInputFS;
using DataProcesses.Nodes.BuiltIn.Blocks.AudioOutFS;
using DataProcesses.Nodes.BuiltIn.Blocks.MqttIn;
using DataProcesses.Nodes.BuiltIn.Blocks.MqttOut;
using DataProcesses.Nodes.BuiltIn.Blocks.SerialInputTS;
using DataProcesses.Nodes.BuiltIn.Blocks.SerialInputVector;
using DataProcesses.Nodes.BuiltIn.Blocks.StreamChartTS;
using DataProcesses.Nodes.BuiltIn.Blocks.StreamChartVector;
using DataProcesses.Nodes.BuiltIn.Blocks.TestSignalVec;
using DataProcesses.Nodes.BuiltIn.Blocks.TestSignalFS;
using DataProcesses.Plugin.Abstractions;

using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace DataProcesses.Desktop.ViewModels;

public sealed class CanvasNodeViewModel : ViewModelBase
{
    private const string TestSignalTypeId = "dataprocesses.test-signal";
    private const string TestSignalFSTypeId = "dataprocesses.test-signal-fs";
    private const string TestSignalVecTypeId = "dataprocesses.test-signal-vec";
    private const string TestSignalImgTypeId = "dataprocesses.test-signal-img";
    private const string TriggerTypeId = "dataprocesses.trigger";
    private const string CameraInputImageTypeId = "dataprocesses.input.camera-image";
    private const string MovieInputImageTypeId = "dataprocesses.input.movie-image";
    private const string UVCameraInputImageTypeId = "dataprocesses.input.uv-camera-image";
    private const string CsvInputTypeId = "dataprocesses.input.csv";
    private const string SerialInputTSTypeId = "dataprocesses.input.serial-st";
    private const string SerialInputVectorTypeId = "dataprocesses.input.serial-vector";
    private const string BleInputTSTypeId = "dataprocesses.input.ble-st";
    private const string BleInputVectorTypeId = "dataprocesses.input.ble-vector";
    private const string MicInputFSTypeId = "dataprocesses.input.mic-fs";
    private const string AudioOutFSTypeId = "dataprocesses.output.audio-fs";
    private const string CsvOutputTypeId = "dataprocesses.output.csv";
    private const string FilterFSTypeId = "dataprocesses.filter.filter-fs";

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

        BleInputTSResetNordicUuidsCommand = new RelayCommand(ResetBleInputTSNordicUuids);
        BleInputTSRefreshDevicesCommand = new AsyncRelayCommand(RefreshBleInputTSDevicesAsync);
        AddBleInputTSDeviceChoice(BleInputTSDeviceId, BleInputTSDeviceName);
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
    public bool IsTestSignalFSNode => string.Equals(TypeId, TestSignalFSTypeId, StringComparison.Ordinal);

    public bool IsTestSignalVec => string.Equals(TypeId, TestSignalVecTypeId, StringComparison.Ordinal);

    public bool IsTestSignalImg => string.Equals(TypeId, TestSignalImgTypeId, StringComparison.Ordinal);

    public bool IsTestSignalOrVector => IsTestSignal || IsTestSignalVec;

    public bool IsTestSignalInspectorNode => IsTestSignalOrVector || IsTestSignalFSNode;

    public bool IsTriggerNode => string.Equals(TypeId, TriggerTypeId, StringComparison.Ordinal);

    public bool IsCameraInputImageNode => string.Equals(TypeId, CameraInputImageTypeId, StringComparison.Ordinal);

    public bool IsUVCameraInputImageNode => string.Equals(TypeId, UVCameraInputImageTypeId, StringComparison.Ordinal);

    public bool IsCameraSourceNode => IsCameraInputImageNode || IsUVCameraInputImageNode;

    public bool IsMovieInputImageNode => string.Equals(TypeId, MovieInputImageTypeId, StringComparison.Ordinal);

    public bool IsManualTriggerNode => IsTriggerNode || IsCameraInputImageNode;

    public bool IsStartStopNode => string.Equals(TypeId, TestSignalVecTypeId, StringComparison.Ordinal)
        || string.Equals(TypeId, TestSignalImgTypeId, StringComparison.Ordinal);

    public bool IsDashboardToggleNode => IsStartStopNode || IsTestSignal;
    public bool IsDashboardActionNode => IsManualTriggerNode || IsDashboardToggleNode || IsAudioOutFSNode;

    public bool IsCsvInputNode => string.Equals(TypeId, CsvInputTypeId, StringComparison.Ordinal);

    public bool IsSerialInputTSNode => string.Equals(TypeId, SerialInputTSTypeId, StringComparison.Ordinal);

    public bool IsSerialInputVectorNode => string.Equals(TypeId, SerialInputVectorTypeId, StringComparison.Ordinal);

    public bool IsBleInputTSNode => string.Equals(TypeId, BleInputTSTypeId, StringComparison.Ordinal);

    public bool IsBleInputVectorNode => string.Equals(TypeId, BleInputVectorTypeId, StringComparison.Ordinal);

    public bool IsBleInputFSNode => string.Equals(TypeId, BleInputFSBlock.TypeId, StringComparison.Ordinal);

    public bool IsBleInputNode => IsBleInputTSNode || IsBleInputVectorNode || IsBleInputFSNode;

    public bool IsBleChannelCountVisible => IsBleInputTSNode || IsBleInputFSNode;

    public bool IsBleInputFSBinary => IsBleInputFSNode && BleInputFSFormat == "Binary";

    public IReadOnlyList<string> BleInputFSFormats { get; } = Enum.GetNames<BleInputFSFormat>();

    public IReadOnlyList<string> BleInputFSNumericTypes { get; } = Enum.GetNames<BleInputFSNumericType>();

    public IReadOnlyList<string> BleInputFSByteOrders { get; } = Enum.GetNames<BleInputFSByteOrder>();

    public bool IsCsvOutputNode => string.Equals(TypeId, CsvOutputTypeId, StringComparison.Ordinal);

    public bool IsStreamChartVectorNode => string.Equals(TypeId, StreamChartVectorBlock.TypeId, StringComparison.Ordinal);

    public bool IsStreamChartTSNode => string.Equals(TypeId, StreamChartTSBlock.TypeId, StringComparison.Ordinal);

    public bool IsBreathTSNode => string.Equals(TypeId, BreathTSBlock.TypeId, StringComparison.Ordinal);

    public bool IsMicInputFSNode => string.Equals(TypeId, MicInputFSTypeId, StringComparison.Ordinal);
    public bool IsAudioOutFSNode => string.Equals(TypeId, AudioOutFSTypeId, StringComparison.Ordinal);

    public bool IsSignalInspectorNode => IsTestSignalInspectorNode || IsMicInputFSNode;

    public bool IsFilterTSNode => string.Equals(TypeId, FilterTSBlock.TypeId, StringComparison.Ordinal);

    public bool IsFilterFSNode => string.Equals(TypeId, FilterFSTypeId, StringComparison.Ordinal);

    public bool IsMqttInNode => string.Equals(TypeId, MqttInBlock.TypeId, StringComparison.Ordinal);

    public bool IsMqttOutNode => string.Equals(TypeId, MqttOutBlock.TypeId, StringComparison.Ordinal);

    public bool IsMovingAverageNode => string.Equals(TypeId, MovingAverageBlock.TypeId, StringComparison.Ordinal);

    public IReadOnlyList<string> TestSignalWaveTypes => IsTestSignalVec
        ? ["oneShot", "sine"]
        : ["sine", "square"];

    public IReadOnlyList<string> MicInputFSChannelModes => MicInputFSSettings.ChannelModeOptions;

    public IReadOnlyList<string> TestSignalImageTypes { get; } = ["number", "circle"];

    public IReadOnlyList<string> TestSignalImageModes { get; } = ["mono", "color"];

    public IReadOnlyList<string> TriggerPayloadValueTypes { get; } = ["datetime", "boolean", "string", "numberArray", "number"];

    public IReadOnlyList<bool> TriggerBooleanChoices { get; } = [true, false];

    public IReadOnlyList<string> CsvInputSourceTypes { get; } = ["file", "com"];

    public IReadOnlyList<string> CsvInputFilePlaybackModes { get; } = ["immediate", "millis"];

    public IReadOnlyList<string> SerialInputTSComPorts => GetSerialInputTSComPorts();

    public ObservableCollection<BleInputTSDeviceChoice> BleInputTSDeviceChoices { get; } = [];

    public IReadOnlyList<string> CsvOutputWriteModes { get; } = ["append", "new"];

    public IReadOnlyList<string> BreathTSDetectionMethods { get; } = ["breathBelt", "ledOxygen"];

    public IReadOnlyList<string> FilterTSTypes { get; } = ["lowPass", "highPass", "bandPass", "bandStop"];

    public IReadOnlyList<string> FilterFSTypes => FilterFSSettings.FilterTypeOptions;

    public IReadOnlyList<int> FilterTSOrders { get; } = Enumerable.Range(FilterTSSettings.MinimumOrder, FilterTSSettings.MaximumOrder - FilterTSSettings.MinimumOrder + 1).ToArray();

    public IReadOnlyList<int> FilterFSOrders => FilterTSOrders;

    public IReadOnlyList<string> MovingAverageWindowModes { get; } = ["samples", "duration"];

    public IRelayCommand BleInputTSResetNordicUuidsCommand { get; }

    public IAsyncRelayCommand BleInputTSRefreshDevicesCommand { get; }

    public string TestSignalWaveType
    {
        get => NormalizeTestSignalWaveType(ReadSettingsString("waveType", GetDefaultWaveType()));
        set => UpdateSettingsString("waveType", NormalizeTestSignalWaveType(value));
    }

    public string BreathTSDetectionMethod
    {
        get => NormalizeBreathTSDetectionMethod(ReadSettingsString("method", "breathBelt"));
        set => UpdateSettingsString("method", NormalizeBreathTSDetectionMethod(value));
    }

    public bool BreathTSEmitAnomalyEvents
    {
        get => ReadSettingsBoolean("emitAnomalyEvents", true);
        set => UpdateSettingsBoolean("emitAnomalyEvents", value);
    }

    public string FilterTSType
    {
        get => NormalizeFilterTSType(ReadSettingsString("filterType", "lowPass"));
        set
        {
            UpdateSettingsString("filterType", NormalizeFilterTSType(value));
            NotifyFilterTSSettingsChanged();
        }
    }

    public bool FilterTSIsSingleCutoffVisible => FilterTSType is "lowPass" or "highPass";

    public bool FilterTSIsBandCutoffVisible => !FilterTSIsSingleCutoffVisible;

    public double FilterTSCutoffFrequencyHertz
    {
        get => ReadSettingsDouble("cutoffFrequencyHertz", FilterTSSettings.Default.CutoffFrequencyHertz);
        set
        {
            UpdateSettingsDouble("cutoffFrequencyHertz", value, minimumExclusive: 0);
            NotifyFilterTSSettingsChanged();
        }
    }

    public double FilterTSLowerCutoffFrequencyHertz
    {
        get => ReadSettingsDouble("lowerCutoffFrequencyHertz", FilterTSSettings.Default.LowerCutoffFrequencyHertz);
        set
        {
            if (!double.IsFinite(value) || value <= 0.0 || value >= FilterTSUpperCutoffFrequencyHertz)
            {
                return;
            }

            UpdateSettingsDouble("lowerCutoffFrequencyHertz", value, minimumExclusive: 0);
            NotifyFilterTSSettingsChanged();
        }
    }

    public double FilterTSUpperCutoffFrequencyHertz
    {
        get => ReadSettingsDouble("upperCutoffFrequencyHertz", FilterTSSettings.Default.UpperCutoffFrequencyHertz);
        set
        {
            if (!double.IsFinite(value) || value <= FilterTSLowerCutoffFrequencyHertz)
            {
                return;
            }

            UpdateSettingsDouble("upperCutoffFrequencyHertz", value, minimumExclusive: 0);
            NotifyFilterTSSettingsChanged();
        }
    }

    public int FilterTSOrder
    {
        get => Math.Clamp(
            (int)Math.Round(ReadSettingsDouble("order", FilterTSSettings.Default.Order), MidpointRounding.AwayFromZero),
            FilterTSSettings.MinimumOrder,
            FilterTSSettings.MaximumOrder);
        set
        {
            UpdateSettingsInt("order", value, FilterTSSettings.MinimumOrder, FilterTSSettings.MaximumOrder);
            NotifyFilterTSSettingsChanged();
        }
    }

    public IReadOnlyList<Point> FilterTSResponsePoints => CreateFilterTSResponsePoints();

    public string FilterFSFilterType
    {
        get => NormalizeFilterFSKind(ReadSettingsString("filterType", "lowPass"));
        set
        {
            UpdateSettingsString("filterType", NormalizeFilterFSKind(value));
            NotifyFilterFSSettingsChanged();
        }
    }

    public bool FilterFSIsSingleCutoffVisible => FilterFSFilterType is "lowPass" or "highPass";

    public bool FilterFSIsBandCutoffVisible => FilterFSFilterType is "bandPass" or "bandStop";

    public bool FilterFSIsNotchVisible => FilterFSFilterType == "notch";

    public double FilterFSCutoffFrequencyHertz
    {
        get => ReadSettingsDouble("cutoffFrequencyHertz", FilterFSSettings.Default.CutoffFrequencyHertz);
        set
        {
            UpdateSettingsDouble("cutoffFrequencyHertz", value, minimumExclusive: 0);
            NotifyFilterFSSettingsChanged();
        }
    }

    public double FilterFSLowerCutoffFrequencyHertz
    {
        get => ReadSettingsDouble("lowerCutoffFrequencyHertz", FilterFSSettings.Default.LowerCutoffFrequencyHertz);
        set
        {
            if (!double.IsFinite(value) || value <= 0 || value >= FilterFSUpperCutoffFrequencyHertz)
            {
                return;
            }

            UpdateSettingsDouble("lowerCutoffFrequencyHertz", value, minimumExclusive: 0);
            NotifyFilterFSSettingsChanged();
        }
    }

    public double FilterFSUpperCutoffFrequencyHertz
    {
        get => ReadSettingsDouble("upperCutoffFrequencyHertz", FilterFSSettings.Default.UpperCutoffFrequencyHertz);
        set
        {
            if (!double.IsFinite(value) || value <= FilterFSLowerCutoffFrequencyHertz)
            {
                return;
            }

            UpdateSettingsDouble("upperCutoffFrequencyHertz", value, minimumExclusive: 0);
            NotifyFilterFSSettingsChanged();
        }
    }

    public double FilterFSNotchBandwidthHertz
    {
        get => ReadSettingsDouble("notchBandwidthHertz", FilterFSSettings.Default.NotchBandwidthHertz);
        set
        {
            if (!double.IsFinite(value) || value <= 0 || value >= FilterFSCutoffFrequencyHertz * 2.0)
            {
                return;
            }

            UpdateSettingsDouble("notchBandwidthHertz", value, minimumExclusive: 0);
            NotifyFilterFSSettingsChanged();
        }
    }

    public int FilterFSOrder
    {
        get => Math.Clamp((int)Math.Round(ReadSettingsDouble("order", FilterFSSettings.Default.Order), MidpointRounding.AwayFromZero), FilterFSSettings.MinimumOrder, FilterFSSettings.MaximumOrder);
        set
        {
            UpdateSettingsInt("order", value, FilterFSSettings.MinimumOrder, FilterFSSettings.MaximumOrder);
            NotifyFilterFSSettingsChanged();
        }
    }

    public IReadOnlyList<int> MqttQualityOfServiceLevels { get; } = [0, 1, 2];

    public string MqttInBrokerHost
    {
        get => ReadSettingsString("brokerHost", "localhost");
        set => UpdateSettingsString("brokerHost", string.IsNullOrWhiteSpace(value) ? "localhost" : value.Trim());
    }

    public int MqttInPort
    {
        get => Math.Clamp((int)ReadSettingsDouble("port", 1883), 1, 65535);
        set => UpdateSettingsInt("port", value, 1, 65535);
    }

    public string MqttInClientId
    {
        get => ReadSettingsString("clientId", string.Empty);
        set => UpdateSettingsString("clientId", value?.Trim() ?? string.Empty);
    }

    public string MqttInUsername
    {
        get => ReadSettingsString("username", string.Empty);
        set => UpdateSettingsString("username", value ?? string.Empty);
    }

    public string MqttInPassword
    {
        get => ReadSettingsString("password", string.Empty);
        set => UpdateSettingsString("password", value ?? string.Empty);
    }

    public bool MqttInUseTls
    {
        get => ReadSettingsBoolean("useTls", false);
        set => UpdateSettingsBoolean("useTls", value);
    }

    public string MqttInTopic
    {
        get => ReadSettingsString("topic", "dataprocesses/messages");
        set => UpdateSettingsString("topic", string.IsNullOrWhiteSpace(value) ? "dataprocesses/messages" : value.Trim());
    }

    public int MqttInQualityOfService
    {
        get => Math.Clamp((int)ReadSettingsDouble("qualityOfService", 0), 0, 2);
        set => UpdateSettingsInt("qualityOfService", value, 0, 2);
    }

    public string MqttOutBrokerHost
    {
        get => ReadSettingsString("brokerHost", "localhost");
        set => UpdateSettingsString("brokerHost", string.IsNullOrWhiteSpace(value) ? "localhost" : value.Trim());
    }

    public int MqttOutPort
    {
        get => Math.Clamp((int)ReadSettingsDouble("port", 1883), 1, 65535);
        set => UpdateSettingsInt("port", value, 1, 65535);
    }

    public string MqttOutClientId
    {
        get => ReadSettingsString("clientId", string.Empty);
        set => UpdateSettingsString("clientId", value?.Trim() ?? string.Empty);
    }

    public string MqttOutUsername
    {
        get => ReadSettingsString("username", string.Empty);
        set => UpdateSettingsString("username", value ?? string.Empty);
    }

    public string MqttOutPassword
    {
        get => ReadSettingsString("password", string.Empty);
        set => UpdateSettingsString("password", value ?? string.Empty);
    }

    public bool MqttOutUseTls
    {
        get => ReadSettingsBoolean("useTls", false);
        set => UpdateSettingsBoolean("useTls", value);
    }

    public string MqttOutTopic
    {
        get => ReadSettingsString("topic", string.Empty);
        set => UpdateSettingsString("topic", value?.Trim() ?? string.Empty);
    }

    public int MqttOutQualityOfService
    {
        get => Math.Clamp((int)ReadSettingsDouble("qualityOfService", 0), 0, 2);
        set => UpdateSettingsInt("qualityOfService", value, 0, 2);
    }

    public bool MqttOutRetain
    {
        get => ReadSettingsBoolean("retain", false);
        set => UpdateSettingsBoolean("retain", value);
    }

    public string FilterTSResponseCaption => FilterTSIsSingleCutoffVisible
        ? $"0-{Math.Max(10.0, FilterTSCutoffFrequencyHertz * 2.5):0.#} Hz"
        : $"0-{Math.Max(10.0, FilterTSUpperCutoffFrequencyHertz * 2.5):0.#} Hz";

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

    public double TestSignalFSFrequencyHertz
    {
        get => ReadSettingsDouble("frequency", TestSignalFSSettings.Default.FrequencyHertz);
        set
        {
            if (!double.IsFinite(value) || value <= 0.0 || value > TestSignalFSSampleRateHertz / 2.0)
            {
                OnPropertyChanged(nameof(TestSignalFSFrequencyHertz));
                return;
            }

            UpdateSettingsDouble("frequency", value, minimumExclusive: 0);
        }
    }

    public double TestSignalFSPeakIntensity
    {
        get => ReadSettingsDouble("peakIntensity", TestSignalFSSettings.Default.PeakIntensity);
        set => UpdateSettingsDoubleInclusive("peakIntensity", value, minimumInclusive: 0);
    }

    public double TestSignalFSSampleRateHertz
    {
        get => ReadSettingsDouble("sampleRateHertz", TestSignalFSSettings.Default.SampleRateHertz);
        set
        {
            if (!double.IsFinite(value))
            {
                OnPropertyChanged(nameof(TestSignalFSSampleRateHertz));
                return;
            }

            var sampleRateHertz = (int)Math.Round(value, MidpointRounding.AwayFromZero);
            if (sampleRateHertz < 1 || sampleRateHertz > TestSignalFSSettings.MaximumSampleRateHertz)
            {
                OnPropertyChanged(nameof(TestSignalFSSampleRateHertz));
                return;
            }

            var settings = ReadSettingsObject();
            settings["sampleRateHertz"] = sampleRateHertz;
            if (TestSignalFSFrequencyHertz > sampleRateHertz / 2.0)
            {
                settings["frequency"] = sampleRateHertz / 2.0;
            }

            SettingsJson = settings.ToJsonString();
        }
    }

    public double TestSignalFSNyquistFrequencyHertz => TestSignalFSSampleRateHertz / 2.0;

    public double MicInputFSSampleRateHertz
    {
        get => ReadSettingsDouble("sampleRateHertz", MicInputFSSettings.Default.SampleRateHertz);
        set
        {
            if (!double.IsFinite(value))
            {
                OnPropertyChanged(nameof(MicInputFSSampleRateHertz));
                return;
            }

            UpdateSettingsInt(
                "sampleRateHertz",
                (int)Math.Round(value, MidpointRounding.AwayFromZero),
                MicInputFSSettings.MinimumSampleRateHertz,
                MicInputFSSettings.MaximumSampleRateHertz);
        }
    }

    public string MicInputFSChannelMode
    {
        get => NormalizeMicInputFSChannelMode(ReadSettingsString("channelMode", "Stereo"));
        set => UpdateSettingsString("channelMode", NormalizeMicInputFSChannelMode(value));
    }

    public string AudioOutFSRecordingFolder
    {
        get => ReadSettingsString("recordingFolder", AudioOutFSSettings.Default.RecordingFolder);
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                UpdateSettingsString("recordingFolder", value.Trim());
            }
        }
    }

    public string AudioOutFSFileNamePrefix
    {
        get => ReadSettingsString("fileNamePrefix", AudioOutFSSettings.Default.FileNamePrefix);
        set
        {
            if (!string.IsNullOrWhiteSpace(value)
                && value.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
                && !value.Contains('/')
                && !value.Contains('\\'))
            {
                UpdateSettingsString("fileNamePrefix", value.Trim());
            }
        }
    }

    public bool AudioOutFSIsMuted => ReadSettingsBoolean("isMuted", false);

    public bool AudioOutFSIsRecording => ReadSettingsBoolean("isRecording", false);

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

    public string SerialInputTSComPortName
    {
        get => ReadSettingsString("comPortName", SerialInputTSSettings.Default.ComPortName);
        set => UpdateSettingsString(
            "comPortName",
            string.IsNullOrWhiteSpace(value) ? SerialInputTSSettings.Default.ComPortName : value.Trim());
    }

    public double SerialInputTSBaudRate
    {
        get => ReadSettingsDouble("baudRate", SerialInputTSSettings.Default.BaudRate);
        set => UpdateSettingsInt("baudRate", (int)Math.Round(value, MidpointRounding.AwayFromZero), 1, int.MaxValue);
    }

    public double SerialInputTSChannelCount
    {
        get => ReadSettingsDouble("channelCount", SerialInputTSSettings.Default.ChannelCount);
        set => UpdateSettingsInt(
            "channelCount",
            (int)Math.Round(value, MidpointRounding.AwayFromZero),
            SerialInputTSSettings.MinimumChannelCount,
            SerialInputTSSettings.MaximumChannelCount);
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

    public string BleInputFSFormat
    {
        get => ReadBleInputFSEnum("format", BleInputFSSettings.Default.Format);
        set => UpdateSettingsString("format", value);
    }

    public string BleInputFSNumericType
    {
        get => ReadBleInputFSEnum("numericType", BleInputFSSettings.Default.NumericType);
        set => UpdateSettingsString("numericType", value);
    }

    public string BleInputFSByteOrder
    {
        get => ReadBleInputFSEnum("byteOrder", BleInputFSSettings.Default.ByteOrder);
        set => UpdateSettingsString("byteOrder", value);
    }

    public double BleInputFSSampleRateHertz
    {
        get => ReadSettingsDouble("sampleRateHertz", BleInputFSSettings.Default.SampleRateHertz);
        set => UpdateSettingsInt("sampleRateHertz", (int)Math.Round(value, MidpointRounding.AwayFromZero),
            BleInputFSSettings.MinimumSampleRateHertz, BleInputFSSettings.MaximumSampleRateHertz);
    }

    private string ReadBleInputFSEnum<T>(string key, T defaultValue) where T : struct, Enum
    {
        var value = ReadSettingsString(key, defaultValue.ToString());
        return Enum.TryParse<T>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed.ToString()
            : value;
    }

    public string BleInputTSDeviceId
    {
        get => ReadSettingsString("deviceId", BleInputTSSettings.Default.DeviceId);
        set
        {
            UpdateSettingsString("deviceId", value?.Trim() ?? string.Empty);
            AddBleInputTSDeviceChoice(BleInputTSDeviceId, BleInputTSDeviceName);
            OnPropertyChanged(nameof(BleInputTSSelectedDevice));
        }
    }

    public string BleInputTSDeviceName
    {
        get => ReadSettingsString("deviceName", BleInputTSSettings.Default.DeviceName);
        set
        {
            UpdateSettingsString("deviceName", value?.Trim() ?? string.Empty);
            AddBleInputTSDeviceChoice(BleInputTSDeviceId, BleInputTSDeviceName);
            OnPropertyChanged(nameof(BleInputTSSelectedDevice));
        }
    }

    public BleInputTSDeviceChoice? BleInputTSSelectedDevice
    {
        get
        {
            var deviceId = BleInputTSDeviceId;
            return string.IsNullOrWhiteSpace(deviceId)
                ? null
                : BleInputTSDeviceChoices.FirstOrDefault(choice => string.Equals(choice.DeviceId, deviceId, StringComparison.Ordinal));
        }

        set
        {
            if (value is null)
            {
                return;
            }

            UpdateSettingsString("deviceId", value.DeviceId);
            UpdateSettingsString("deviceName", value.Name);
            AddBleInputTSDeviceChoice(value.DeviceId, value.Name);
            OnPropertyChanged();
        }
    }

    public bool BleInputTSAutoConnect
    {
        get => ReadSettingsBoolean("autoConnect", BleInputTSSettings.Default.AutoConnect);
        set => UpdateSettingsBoolean("autoConnect", value);
    }

    public string BleInputTSServiceUuid
    {
        get => ReadSettingsString("serviceUuid", BleInputTSSettings.Default.ServiceUuid);
        set => UpdateSettingsString(
            "serviceUuid",
            string.IsNullOrWhiteSpace(value) ? BleInputTSSettings.Default.ServiceUuid : value.Trim());
    }

    public string BleInputTSNotifyCharacteristicUuid
    {
        get => ReadSettingsString("notifyCharacteristicUuid", BleInputTSSettings.Default.NotifyCharacteristicUuid);
        set => UpdateSettingsString(
            "notifyCharacteristicUuid",
            string.IsNullOrWhiteSpace(value) ? BleInputTSSettings.Default.NotifyCharacteristicUuid : value.Trim());
    }

    public double BleInputTSChannelCount
    {
        get => ReadSettingsDouble("channelCount", BleInputTSSettings.Default.ChannelCount);
        set => UpdateSettingsInt(
            "channelCount",
            (int)Math.Round(value, MidpointRounding.AwayFromZero),
            BleInputTSSettings.MinimumChannelCount,
            BleInputTSSettings.MaximumChannelCount);
    }

    public double BleInputTSTimeoutMilliseconds
    {
        get => ReadSettingsDouble("timeoutMilliseconds", BleInputTSSettings.Default.TimeoutMilliseconds);
        set => UpdateSettingsInt(
            "timeoutMilliseconds",
            (int)Math.Round(value, MidpointRounding.AwayFromZero),
            BleInputTSSettings.MinimumTimeoutMilliseconds,
            BleInputTSSettings.MaximumTimeoutMilliseconds);
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

    public IReadOnlyList<string> StreamChartTSTimeAlignmentModes { get; } = ["independent", "alignToFirstStream"];

    public string StreamChartTSTimeAlignmentMode
    {
        get => NormalizeStreamChartTSTimeAlignmentMode(ReadSettingsString("timeAlignmentMode", "independent"));
        set => UpdateSettingsString("timeAlignmentMode", NormalizeStreamChartTSTimeAlignmentMode(value));
    }

    public double StreamChartTSTimeSpanMilliseconds
    {
        get => ReadSettingsDouble("timeSpanMillis", StreamChartTSSettings.DefaultTimeSpanMilliseconds);
        set => UpdateSettingsDouble(
            "timeSpanMillis",
            Math.Max(StreamChartTSSettings.MinimumTimeSpanMilliseconds, value),
            minimumExclusive: 0);
    }

    public string StreamChartTSChannel1Name
    {
        get => ReadSettingsString("channel1Name", "CH1");
        set => UpdateSettingsString("channel1Name", string.IsNullOrWhiteSpace(value) ? "CH1" : value.Trim());
    }

    public string StreamChartTSChannel2Name
    {
        get => ReadSettingsString("channel2Name", "CH2");
        set => UpdateSettingsString("channel2Name", string.IsNullOrWhiteSpace(value) ? "CH2" : value.Trim());
    }

    public string StreamChartTSChannel3Name
    {
        get => ReadSettingsString("channel3Name", "CH3");
        set => UpdateSettingsString("channel3Name", string.IsNullOrWhiteSpace(value) ? "CH3" : value.Trim());
    }

    public string StreamChartTSChannel4Name
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
                OnPropertyChanged(nameof(BreathTSDetectionMethod));
                OnPropertyChanged(nameof(BreathTSEmitAnomalyEvents));
                OnPropertyChanged(nameof(MovingAverageWindowMode));
                OnPropertyChanged(nameof(MovingAverageWindowSize));
                OnPropertyChanged(nameof(MovingAverageWindowDurationMilliseconds));
                OnPropertyChanged(nameof(IsMovingAverageSampleWindowVisible));
                OnPropertyChanged(nameof(IsMovingAverageDurationWindowVisible));
                OnPropertyChanged(nameof(FilterTSType));
                OnPropertyChanged(nameof(FilterTSIsSingleCutoffVisible));
                OnPropertyChanged(nameof(FilterTSIsBandCutoffVisible));
                OnPropertyChanged(nameof(FilterTSCutoffFrequencyHertz));
                OnPropertyChanged(nameof(FilterTSLowerCutoffFrequencyHertz));
                OnPropertyChanged(nameof(FilterTSUpperCutoffFrequencyHertz));
                OnPropertyChanged(nameof(FilterTSOrder));
                OnPropertyChanged(nameof(FilterTSResponsePoints));
                OnPropertyChanged(nameof(FilterTSResponseCaption));
                                OnPropertyChanged(nameof(FilterFSFilterType));
                                OnPropertyChanged(nameof(FilterFSIsSingleCutoffVisible));
                                OnPropertyChanged(nameof(FilterFSIsBandCutoffVisible));
                                OnPropertyChanged(nameof(FilterFSIsNotchVisible));
                                OnPropertyChanged(nameof(FilterFSCutoffFrequencyHertz));
                                OnPropertyChanged(nameof(FilterFSLowerCutoffFrequencyHertz));
                                OnPropertyChanged(nameof(FilterFSUpperCutoffFrequencyHertz));
                                OnPropertyChanged(nameof(FilterFSNotchBandwidthHertz));
                                OnPropertyChanged(nameof(FilterFSOrder));
                OnPropertyChanged(nameof(TestSignalFrequencyHertz));
                NotifyMqttInspectorSettingsChanged();
                OnPropertyChanged(nameof(TestSignalSamplePeriodMilliseconds));
                OnPropertyChanged(nameof(TestSignalFSFrequencyHertz));
                OnPropertyChanged(nameof(TestSignalFSPeakIntensity));
                OnPropertyChanged(nameof(TestSignalFSSampleRateHertz));
                OnPropertyChanged(nameof(TestSignalFSNyquistFrequencyHertz));
                OnPropertyChanged(nameof(MicInputFSSampleRateHertz));
                OnPropertyChanged(nameof(MicInputFSChannelMode));
                OnPropertyChanged(nameof(AudioOutFSRecordingFolder));
                OnPropertyChanged(nameof(AudioOutFSFileNamePrefix));
                OnPropertyChanged(nameof(AudioOutFSIsMuted));
                OnPropertyChanged(nameof(AudioOutFSIsRecording));
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
                OnPropertyChanged(nameof(SerialInputTSComPortName));
                OnPropertyChanged(nameof(SerialInputTSComPorts));
                OnPropertyChanged(nameof(SerialInputTSBaudRate));
                OnPropertyChanged(nameof(SerialInputTSChannelCount));
                OnPropertyChanged(nameof(SerialInputVectorComPortName));
                OnPropertyChanged(nameof(SerialInputVectorBaudRate));
                OnPropertyChanged(nameof(BleInputTSDeviceId));
                OnPropertyChanged(nameof(BleInputTSDeviceName));
                OnPropertyChanged(nameof(BleInputTSDeviceChoices));
                OnPropertyChanged(nameof(BleInputTSSelectedDevice));
                OnPropertyChanged(nameof(BleInputTSAutoConnect));
                OnPropertyChanged(nameof(BleInputTSServiceUuid));
                OnPropertyChanged(nameof(BleInputTSNotifyCharacteristicUuid));
                OnPropertyChanged(nameof(BleInputTSChannelCount));
                OnPropertyChanged(nameof(BleInputTSTimeoutMilliseconds));
                OnPropertyChanged(nameof(BleInputFSFormat));
                OnPropertyChanged(nameof(BleInputFSNumericType));
                OnPropertyChanged(nameof(BleInputFSByteOrder));
                OnPropertyChanged(nameof(BleInputFSSampleRateHertz));
                OnPropertyChanged(nameof(IsBleInputFSBinary));
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

    public void ToggleAudioOutFSMute()
    {
        if (!IsAudioOutFSNode)
        {
            return;
        }

        var settings = ReadSettingsObject();
        settings["isMuted"] = !AudioOutFSIsMuted;
        SettingsJson = settings.ToJsonString();
    }

    public void ToggleAudioOutFSRecording()
    {
        if (!IsAudioOutFSNode)
        {
            return;
        }

        var settings = ReadSettingsObject();
        settings["isRecording"] = !AudioOutFSIsRecording;
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

    private IReadOnlyList<string> GetSerialInputTSComPorts()
    {
        try
        {
            var detectedPorts = SerialPort.GetPortNames()
                .Append(SerialInputTSComPortName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static port => port, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return detectedPorts;
        }
        catch (IOException)
        {
            return [SerialInputTSComPortName];
        }
    }

    private void ResetBleInputTSNordicUuids()
    {
        if (!IsBleInputNode)
        {
            return;
        }

        var settings = ReadSettingsObject();
        settings["serviceUuid"] = BleInputTSSettings.NordicUartServiceUuid;
        settings["notifyCharacteristicUuid"] = BleInputTSSettings.NordicUartTxCharacteristicUuid;
        SettingsJson = settings.ToJsonString();
    }

    private async Task RefreshBleInputTSDevicesAsync()
    {
        if (!IsBleInputNode)
        {
            return;
        }

        var currentDeviceId = BleInputTSDeviceId;
        var currentDeviceName = BleInputTSDeviceName;
        BleInputTSDeviceChoices.Clear();
        AddBleInputTSDeviceChoice(currentDeviceId, currentDeviceName);

        var devices = await DeviceInformation.FindAllAsync(BluetoothLEDevice.GetDeviceSelector())
            .AsTask()
            .ConfigureAwait(true);
        foreach (var device in devices
                     .OrderBy(static device => device.Name, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(static device => device.Id, StringComparer.OrdinalIgnoreCase))
        {
            AddBleInputTSDeviceChoice(device.Id, device.Name);
        }

        OnPropertyChanged(nameof(BleInputTSSelectedDevice));
    }

    private void AddBleInputTSDeviceChoice(string deviceId, string deviceName)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return;
        }

        var trimmedDeviceId = deviceId.Trim();
        var trimmedDeviceName = deviceName.Trim();
        for (var index = 0; index < BleInputTSDeviceChoices.Count; index++)
        {
            if (!string.Equals(BleInputTSDeviceChoices[index].DeviceId, trimmedDeviceId, StringComparison.Ordinal))
            {
                continue;
            }

            if (!string.Equals(BleInputTSDeviceChoices[index].Name, trimmedDeviceName, StringComparison.Ordinal))
            {
                BleInputTSDeviceChoices[index] = BleInputTSDeviceChoices[index] with { Name = trimmedDeviceName };
            }

            return;
        }

        BleInputTSDeviceChoices.Add(new BleInputTSDeviceChoice(trimmedDeviceId, trimmedDeviceName));
    }

    public sealed record BleInputTSDeviceChoice(string DeviceId, string Name)
    {
        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? DeviceId : $"{Name} ({DeviceId})";
    }

    private void NotifyFilterTSSettingsChanged()
    {
        OnPropertyChanged(nameof(FilterTSType));
        OnPropertyChanged(nameof(FilterTSIsSingleCutoffVisible));
        OnPropertyChanged(nameof(FilterTSIsBandCutoffVisible));
        OnPropertyChanged(nameof(FilterTSCutoffFrequencyHertz));
        OnPropertyChanged(nameof(FilterTSLowerCutoffFrequencyHertz));
        OnPropertyChanged(nameof(FilterTSUpperCutoffFrequencyHertz));
        OnPropertyChanged(nameof(FilterTSOrder));
        OnPropertyChanged(nameof(FilterTSResponsePoints));
        OnPropertyChanged(nameof(FilterTSResponseCaption));
    }

    private void NotifyMqttInspectorSettingsChanged()
    {
        OnPropertyChanged(nameof(MqttInBrokerHost));
        OnPropertyChanged(nameof(MqttInPort));
        OnPropertyChanged(nameof(MqttInClientId));
        OnPropertyChanged(nameof(MqttInUsername));
        OnPropertyChanged(nameof(MqttInPassword));
        OnPropertyChanged(nameof(MqttInUseTls));
        OnPropertyChanged(nameof(MqttInTopic));
        OnPropertyChanged(nameof(MqttInQualityOfService));
        OnPropertyChanged(nameof(MqttOutBrokerHost));
        OnPropertyChanged(nameof(MqttOutPort));
        OnPropertyChanged(nameof(MqttOutClientId));
        OnPropertyChanged(nameof(MqttOutUsername));
        OnPropertyChanged(nameof(MqttOutPassword));
        OnPropertyChanged(nameof(MqttOutUseTls));
        OnPropertyChanged(nameof(MqttOutTopic));
        OnPropertyChanged(nameof(MqttOutQualityOfService));
        OnPropertyChanged(nameof(MqttOutRetain));
    }

    private void NotifyFilterFSSettingsChanged()
    {
        OnPropertyChanged(nameof(FilterFSFilterType));
        OnPropertyChanged(nameof(FilterFSIsSingleCutoffVisible));
        OnPropertyChanged(nameof(FilterFSIsBandCutoffVisible));
        OnPropertyChanged(nameof(FilterFSIsNotchVisible));
        OnPropertyChanged(nameof(FilterFSCutoffFrequencyHertz));
        OnPropertyChanged(nameof(FilterFSLowerCutoffFrequencyHertz));
        OnPropertyChanged(nameof(FilterFSUpperCutoffFrequencyHertz));
        OnPropertyChanged(nameof(FilterFSNotchBandwidthHertz));
        OnPropertyChanged(nameof(FilterFSOrder));
    }

    private IReadOnlyList<Point> CreateFilterTSResponsePoints()
    {
        const double width = 248.0;
        const double height = 72.0;
        const int pointCount = 48;

        var points = new Point[pointCount];
        var maxFrequency = FilterTSIsSingleCutoffVisible
            ? Math.Max(10.0, FilterTSCutoffFrequencyHertz * 2.5)
            : Math.Max(10.0, FilterTSUpperCutoffFrequencyHertz * 2.5);

        for (var index = 0; index < points.Length; index++)
        {
            var ratio = index / (double)(points.Length - 1);
            var frequency = maxFrequency * ratio;
            var magnitude = GetFilterTSPreviewMagnitude(frequency);
            points[index] = new Point(width * ratio, height - Math.Clamp(magnitude, 0.0, 1.0) * height);
        }

        return points;
    }

    private double GetFilterTSPreviewMagnitude(double frequencyHertz)
    {
        var order = FilterTSOrder;
        return FilterTSType switch
        {
            "highPass" => GetHighPassPreviewMagnitude(frequencyHertz, FilterTSCutoffFrequencyHertz, order),
            "bandPass" => GetHighPassPreviewMagnitude(frequencyHertz, FilterTSLowerCutoffFrequencyHertz, order)
                * GetLowPassPreviewMagnitude(frequencyHertz, FilterTSUpperCutoffFrequencyHertz, order),
            "bandStop" => Math.Min(1.0,
                GetLowPassPreviewMagnitude(frequencyHertz, FilterTSLowerCutoffFrequencyHertz, order)
                + GetHighPassPreviewMagnitude(frequencyHertz, FilterTSUpperCutoffFrequencyHertz, order)),
            _ => GetLowPassPreviewMagnitude(frequencyHertz, FilterTSCutoffFrequencyHertz, order),
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

    private static string NormalizeMicInputFSChannelMode(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "left" => "Left",
            "right" => "Right",
            _ => "Stereo",
        };
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

    private static string NormalizeBreathTSDetectionMethod(string? value)
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

    private static string NormalizeFilterTSType(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "highpass" or "high-pass" or "high_pass" => "highPass",
            "bandpass" or "band-pass" or "band_pass" => "bandPass",
            "bandstop" or "band-stop" or "band_stop" or "notch" => "bandStop",
            _ => "lowPass",
        };
    }

    private static string NormalizeFilterFSKind(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "highpass" or "high-pass" or "high_pass" => "highPass",
            "bandpass" or "band-pass" or "band_pass" => "bandPass",
            "bandstop" or "band-stop" or "band_stop" => "bandStop",
            "notch" => "notch",
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

    private static string NormalizeStreamChartTSTimeAlignmentMode(string? value)
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