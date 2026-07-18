using System.Collections.ObjectModel;

using DataProcesses.Core;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Desktop.ViewModels;

public sealed class CanvasNodeViewModel : ViewModelBase
{
    private double x;
    private double y;
    private string name;
    private string description;
    private string settingsJson;
    private bool isSelected;
    private bool isEnabled;

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

    public string IconPath => ResolveIconPath(Definition.IconPath);

    public bool HasIcon => !string.IsNullOrWhiteSpace(IconPath);

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

    public string SettingsJson
    {
        get => settingsJson;
        set => SetProperty(ref settingsJson, value);
    }

    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }

    public NodeInstance ToNodeInstance()
    {
        return new NodeInstance(Id, TypeId, X, Y, SettingsJson, Name, Description, IsEnabled);
    }

    private static string ResolveIconPath(string? iconPath)
    {
        if (string.IsNullOrWhiteSpace(iconPath))
        {
            return string.Empty;
        }

        var candidate = Path.Combine(AppContext.BaseDirectory, iconPath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(candidate) ? candidate : string.Empty;
    }
}