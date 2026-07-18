using CommunityToolkit.Mvvm.ComponentModel;

using DataProcesses.Desktop.Services;
using DataProcesses.Engine;
using DataProcesses.Nodes.BuiltIn;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string ProjectTitle { get; set; } = "Untitled project";

    public MainViewModel()
        : this(new BuiltInNodePlugin())
    {
    }

    public MainViewModel(INodePlugin nodePlugin)
    {
        ArgumentNullException.ThrowIfNull(nodePlugin);

        var factories = nodePlugin.NodeFactories;
        FlowEditor = new FlowEditorViewModel(
            factories,
            new FlowRunner(factories),
            new ProjectFileService());
    }

    public FlowEditorViewModel FlowEditor { get; }
}