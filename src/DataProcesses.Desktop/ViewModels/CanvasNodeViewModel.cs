using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Nodes;

using Avalonia.Media.Imaging;

using DataProcesses.Core;
using DataProcesses.Nodes.BuiltIn.Blocks.StreamChartSt;
using DataProcesses.Nodes.BuiltIn.Blocks.StreamChartVector;
using DataProcesses.Nodes.BuiltIn.Blocks.TestSignalVec;
using DataProcesses.Plugin.Abstractions;

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

    public bool IsCsvOutputNode => string.Equals(TypeId, CsvOutputTypeId, StringComparison.Ordinal);

    public bool IsStreamChartVectorNode => string.Equals(TypeId, StreamChartVectorBlock.TypeId, StringComparison.Ordinal);

    public bool IsStreamChartStNode => string.Equals(TypeId, StreamChartStBlock.TypeId, StringComparison.Ordinal);

    public IReadOnlyList<string> TestSignalWaveTypes => IsTestSignalVec
        ? ["oneShot", "sine"]
        : ["sine", "square"];

    public IReadOnlyList<string> TestSignalImageTypes { get; } = ["number", "circle"];

    public IReadOnlyList<string> TestSignalImageModes { get; } = ["mono", "color"];

    public IReadOnlyList<string> TriggerPayloadValueTypes { get; } = ["datetime", "boolean", "string", "numberArray", "number"];

    public IReadOnlyList<bool> TriggerBooleanChoices { get; } = [true, false];

    public IReadOnlyList<string> CsvInputSourceTypes { get; } = ["file", "com"];

    public IReadOnlyList<string> CsvInputFilePlaybackModes { get; } = ["immediate", "millis"];

    public IReadOnlyList<string> CsvOutputWriteModes { get; } = ["append", "new"];

    public string TestSignalWaveType
    {
        get => NormalizeTestSignalWaveType(ReadSettingsString("waveType", GetDefaultWaveType()));
        set => UpdateSettingsString("waveType", NormalizeTestSignalWaveType(value));
    }

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