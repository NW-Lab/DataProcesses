using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Nodes;

using Avalonia.Media.Imaging;

using DataProcesses.Core;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Desktop.ViewModels;

public sealed class CanvasNodeViewModel : ViewModelBase
{
    private const string TestSignalTypeId = "dataprocesses.test-signal";

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
        Inputs = new ObservableCollection<CanvasPortViewModel>(
            definition.Ports.Where(static port => port.Direction == PortDirection.Input).Select(port => new CanvasPortViewModel(this, port)));
        Outputs = new ObservableCollection<CanvasPortViewModel>(
            definition.Ports.Where(static port => port.Direction == PortDirection.Output).Select(port => new CanvasPortViewModel(this, port)));
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

    public IReadOnlyList<string> TestSignalWaveTypes { get; } = ["sine", "square"];

    public string TestSignalWaveType
    {
        get => ReadSettingsString("waveType", "sine");
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

    public string SettingsJson
    {
        get => settingsJson;
        set
        {
            if (SetProperty(ref settingsJson, string.IsNullOrWhiteSpace(value) ? "{}" : value))
            {
                OnPropertyChanged(nameof(TestSignalWaveType));
                OnPropertyChanged(nameof(TestSignalFrequencyHertz));
                OnPropertyChanged(nameof(TestSignalSamplePeriodMilliseconds));
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

    private static string NormalizeTestSignalWaveType(string? value)
    {
        return string.Equals(value, "square", StringComparison.OrdinalIgnoreCase) ? "square" : "sine";
    }

}