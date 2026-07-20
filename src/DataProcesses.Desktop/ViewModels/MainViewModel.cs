using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DataProcesses.Desktop.Services;
using DataProcesses.Engine;
using DataProcesses.Nodes.BuiltIn;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private bool isFlowViewSelected = true;
    private WorkspaceRunMode currentMode = WorkspaceRunMode.Edit;

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
        Dashboard = new DashboardViewModel();
        FlowEditor = new FlowEditorViewModel(
            factories,
            new FlowRunner(factories),
            new ProjectFileService(),
            Dashboard.GetDocuments,
            Dashboard.LoadDocuments,
            Dashboard.MarkAllClean);

        ShowFlowViewCommand = new RelayCommand(() => IsFlowViewSelected = true);
        ShowDashboardViewCommand = new RelayCommand(() => IsFlowViewSelected = false);
        RunCommand = new AsyncRelayCommand(() => StartRunModeAsync(isDebug: false));
        DebugRunCommand = new AsyncRelayCommand(() => StartRunModeAsync(isDebug: true));
        StopCommand = new RelayCommand(StopExecution);

        ApplyMode(WorkspaceRunMode.Edit);
    }

    public FlowEditorViewModel FlowEditor { get; }

    public DashboardViewModel Dashboard { get; }

    public IRelayCommand ShowFlowViewCommand { get; }

    public IRelayCommand ShowDashboardViewCommand { get; }

    public IAsyncRelayCommand RunCommand { get; }

    public IAsyncRelayCommand DebugRunCommand { get; }

    public IRelayCommand StopCommand { get; }

    public bool IsFlowViewSelected
    {
        get => isFlowViewSelected;
        set
        {
            if (SetProperty(ref isFlowViewSelected, value))
            {
                OnPropertyChanged(nameof(IsDashboardViewSelected));
            }
        }
    }

    public bool IsDashboardViewSelected => !IsFlowViewSelected;

    public WorkspaceRunMode CurrentMode
    {
        get => currentMode;
        private set
        {
            if (SetProperty(ref currentMode, value))
            {
                OnPropertyChanged(nameof(CurrentModeLabel));
                OnPropertyChanged(nameof(RunButtonBackground));
                OnPropertyChanged(nameof(DebugButtonBackground));
                OnPropertyChanged(nameof(StopButtonBackground));
            }
        }
    }

    public string CurrentModeLabel => CurrentMode switch
    {
        WorkspaceRunMode.Edit => "Mode: Edit",
        WorkspaceRunMode.Run => "Mode: Run",
        WorkspaceRunMode.RunDebug => "Mode: Run(Debug)",
        _ => "Mode: Edit",
    };

    public string RunButtonBackground => CurrentMode == WorkspaceRunMode.Run ? "#2D9B4E" : "#384A67";

    public string DebugButtonBackground => CurrentMode == WorkspaceRunMode.RunDebug ? "#A87313" : "#384A67";

    public string StopButtonBackground => CurrentMode == WorkspaceRunMode.Edit ? "#C23A3A" : "#384A67";

    private async Task StartRunModeAsync(bool isDebug)
    {
        var mode = isDebug ? WorkspaceRunMode.RunDebug : WorkspaceRunMode.Run;
        ApplyMode(mode);
        await FlowEditor.StartExecutionAsync(isDebug).ConfigureAwait(true);
    }

    private void StopExecution()
    {
        FlowEditor.StopExecution();
        ApplyMode(WorkspaceRunMode.Edit);
    }

    private void ApplyMode(WorkspaceRunMode mode)
    {
        CurrentMode = mode;
        FlowEditor.ApplyWorkspaceMode(mode);
        Dashboard.ApplyWorkspaceMode(mode);
    }
}