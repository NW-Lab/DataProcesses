using System.Collections.ObjectModel;

using DataProcesses.Core;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Desktop.ViewModels;

public sealed class CanvasNodeViewModel : ViewModelBase
{
    private double x;
    private double y;
    private string settingsJson;
    private bool isSelected;

    public CanvasNodeViewModel(NodeInstance instance, NodeDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(definition);

        Id = instance.Id;
        TypeId = instance.TypeId;
        Definition = definition;
        x = instance.X;
        y = instance.Y;
        settingsJson = instance.SettingsJson;
        Inputs = new ObservableCollection<CanvasPortViewModel>(
            definition.Ports.Where(static port => port.Direction == PortDirection.Input).Select(port => new CanvasPortViewModel(this, port)));
        Outputs = new ObservableCollection<CanvasPortViewModel>(
            definition.Ports.Where(static port => port.Direction == PortDirection.Output).Select(port => new CanvasPortViewModel(this, port)));
    }

    public string Id { get; }

    public string TypeId { get; }

    public NodeDefinition Definition { get; }

    public string DisplayName => Definition.DisplayName;

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
        return new NodeInstance(Id, TypeId, X, Y, SettingsJson);
    }
}