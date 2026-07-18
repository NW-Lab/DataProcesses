using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.Input;

using DataProcesses.Core;
using DataProcesses.Desktop.Services;
using DataProcesses.Engine;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Desktop.ViewModels;

public sealed class FlowEditorViewModel : ViewModelBase
{
    private readonly IReadOnlyDictionary<string, INodeFactory> factoriesByTypeId;
    private readonly FlowRunner runner;
    private readonly ProjectFileService projectFileService;
    private readonly Guid flowId = Guid.NewGuid();
    private CanvasNodeViewModel? selectedNode;
    private PaletteNodeViewModel? selectedPaletteNode;
    private CanvasPortViewModel? pendingConnectionSource;
    private CancellationTokenSource? runCancellationTokenSource;
    private string flowName = "Untitled flow";
    private string projectName = "Untitled project";
    private string projectDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "DataProcesses",
        "UntitledProject");
    private string interactionStatus = "Drag a Block from the Node Library onto the canvas.";
    private FlowExecutionState executionState = FlowExecutionState.Stopped;
    private double nextNodeX = 80;
    private double nextNodeY = 80;
    private double zoom = 1;

    public FlowEditorViewModel(
        IEnumerable<INodeFactory> factories,
        FlowRunner runner,
        ProjectFileService projectFileService)
    {
        ArgumentNullException.ThrowIfNull(factories);
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(projectFileService);

        var factoryList = factories.ToArray();
        factoriesByTypeId = factoryList.ToDictionary(static factory => factory.Definition.TypeId, StringComparer.Ordinal);
        this.runner = runner;
        this.projectFileService = projectFileService;
        Palette = new NodePaletteViewModel(factoryList);
        Inspector = new InspectorViewModel();

    AddNodeCommand = new RelayCommand<PaletteNodeViewModel>(AddNode);
    SelectPaletteNodeCommand = new RelayCommand<PaletteNodeViewModel>(SelectPaletteNode);
        PortClickCommand = new RelayCommand<CanvasPortViewModel>(HandlePortClick);
        SelectNodeCommand = new RelayCommand<CanvasNodeViewModel>(SelectNode);
        DeleteSelectedCommand = new RelayCommand(DeleteSelected, () => SelectedNode is not null);
        ValidateCommand = new RelayCommand(Validate);
        RunCommand = new AsyncRelayCommand(RunAsync);
        StopCommand = new RelayCommand(StopRun, () => ExecutionState is FlowExecutionState.Running or FlowExecutionState.Starting);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        NewFlowCommand = new RelayCommand(NewFlow);
        ZoomInCommand = new RelayCommand(() => Zoom = Math.Min(2, Zoom + 0.1));
        ZoomOutCommand = new RelayCommand(() => Zoom = Math.Max(0.4, Zoom - 0.1));
    }

    public NodePaletteViewModel Palette { get; }

    public InspectorViewModel Inspector { get; }

    public ObservableCollection<CanvasNodeViewModel> Nodes { get; } = [];

    public ObservableCollection<CanvasConnectionViewModel> Connections { get; } = [];

    public ObservableCollection<ValidationIssueViewModel> ValidationIssues { get; } = [];

    public ObservableCollection<ExecutionLogEntryViewModel> ExecutionLogs { get; } = [];

    public IRelayCommand<PaletteNodeViewModel> AddNodeCommand { get; }

    public IRelayCommand<PaletteNodeViewModel> SelectPaletteNodeCommand { get; }

    public IRelayCommand<CanvasPortViewModel> PortClickCommand { get; }

    public IRelayCommand<CanvasNodeViewModel> SelectNodeCommand { get; }

    public IRelayCommand DeleteSelectedCommand { get; }

    public IRelayCommand ValidateCommand { get; }

    public IAsyncRelayCommand RunCommand { get; }

    public IRelayCommand StopCommand { get; }

    public IAsyncRelayCommand SaveCommand { get; }

    public IAsyncRelayCommand LoadCommand { get; }

    public IRelayCommand NewFlowCommand { get; }

    public IRelayCommand ZoomInCommand { get; }

    public IRelayCommand ZoomOutCommand { get; }

    public CanvasNodeViewModel? SelectedNode
    {
        get => selectedNode;
        set
        {
            if (!SetProperty(ref selectedNode, value))
            {
                return;
            }

            foreach (var node in Nodes)
            {
                node.IsSelected = ReferenceEquals(node, selectedNode);
            }

            Inspector.SelectedNode = selectedNode;
            OnPropertyChanged(nameof(HasSelectedNode));
            DeleteSelectedCommand.NotifyCanExecuteChanged();
        }
    }

    public bool HasSelectedNode => SelectedNode is not null;

    public PaletteNodeViewModel? SelectedPaletteNode
    {
        get => selectedPaletteNode;
        private set
        {
            if (SetProperty(ref selectedPaletteNode, value))
            {
                OnPropertyChanged(nameof(CanvasPlacementHint));
            }
        }
    }

    public string CanvasPlacementHint => SelectedPaletteNode is null
        ? "Drag a Block from the Node Library onto the canvas."
        : $"Drag {SelectedPaletteNode.DisplayName} onto the canvas to place it.";

    public string InteractionStatus
    {
        get => interactionStatus;
        set => SetProperty(ref interactionStatus, value);
    }

    public string FlowName
    {
        get => flowName;
        set => SetProperty(ref flowName, value);
    }

    public string ProjectName
    {
        get => projectName;
        set => SetProperty(ref projectName, value);
    }

    public string ProjectDirectory
    {
        get => projectDirectory;
        set => SetProperty(ref projectDirectory, value);
    }

    public FlowExecutionState ExecutionState
    {
        get => executionState;
        private set
        {
            if (SetProperty(ref executionState, value))
            {
                StopCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public double Zoom
    {
        get => zoom;
        set => SetProperty(ref zoom, value);
    }

    public string PendingConnectionLabel => pendingConnectionSource is null
        ? "No pending connection"
        : $"Connecting from {pendingConnectionSource.Node.DisplayName}.{pendingConnectionSource.DisplayName}";

    public void MoveNode(CanvasNodeViewModel node, double deltaX, double deltaY)
    {
        ArgumentNullException.ThrowIfNull(node);

        node.X += deltaX / Zoom;
        node.Y += deltaY / Zoom;
        RefreshConnections();
    }

    public CanvasNodeViewModel PlacePaletteNode(PaletteNodeViewModel paletteNode, double canvasX, double canvasY)
    {
        ArgumentNullException.ThrowIfNull(paletteNode);

        var node = AddNodeAt(paletteNode, canvasX, canvasY);
        InteractionStatus = $"Placed {paletteNode.DisplayName} at {canvasX:0}, {canvasY:0}.";
        return node;
    }

    private void AddNode(PaletteNodeViewModel? paletteNode)
    {
        if (paletteNode is null)
        {
            return;
        }

        AddNodeAt(paletteNode, nextNodeX, nextNodeY);
        nextNodeX += 48;
        nextNodeY += 36;
    }

    private CanvasNodeViewModel AddNodeAt(PaletteNodeViewModel paletteNode, double x, double y)
    {
        var node = new CanvasNodeViewModel(
            new NodeInstance(GetNextNodeId(), paletteNode.TypeId, Math.Max(0, x), Math.Max(0, y), "{}"),
            paletteNode.Definition);

        Nodes.Add(node);
        SelectNode(node);
        RefreshConnections();
        return node;
    }

    private void SelectPaletteNode(PaletteNodeViewModel? paletteNode)
    {
        SelectedPaletteNode = paletteNode;
        InteractionStatus = paletteNode is null
            ? "Drag a Block from the Node Library onto the canvas."
            : $"Drag {paletteNode.DisplayName} onto the canvas, then release it.";
    }

    private void SelectNode(CanvasNodeViewModel? node)
    {
        SelectedNode = node;
    }

    private void DeleteSelected()
    {
        if (SelectedNode is null)
        {
            return;
        }

        var node = SelectedNode;
        SelectedNode = null;
        Nodes.Remove(node);
        RebuildConnections(GetDocument().Connections.Where(connection =>
            !string.Equals(connection.SourceNodeId, node.Id, StringComparison.Ordinal)
            && !string.Equals(connection.TargetNodeId, node.Id, StringComparison.Ordinal)).ToArray());
    }

    private void HandlePortClick(CanvasPortViewModel? port)
    {
        if (port is null)
        {
            return;
        }

        SelectNode(port.Node);

        if (port.IsOutput)
        {
            pendingConnectionSource = port;
            OnPropertyChanged(nameof(PendingConnectionLabel));
            return;
        }

        if (pendingConnectionSource is null)
        {
            return;
        }

        var source = pendingConnectionSource;
        pendingConnectionSource = null;
        OnPropertyChanged(nameof(PendingConnectionLabel));

        if (!ConnectionValidator.CanConnect(source.Definition, port.Definition))
        {
            ValidationIssues.Add(new ValidationIssueViewModel(new FlowValidationIssue(
                FlowValidationSeverity.Error,
                FlowValidationIssueCode.IncompatiblePorts,
                $"Cannot connect {source.DisplayName} to {port.DisplayName}.",
                port.Node.Id)));
            return;
        }

        var connection = new Connection(source.Node.Id, source.Id, port.Node.Id, port.Id, source.DataKind);
        var connections = Connections.Select(static connectionViewModel => connectionViewModel.Connection).Append(connection).ToArray();
        RebuildConnections(connections);
        Validate();
    }

    private void Validate()
    {
        ValidationIssues.Clear();
        var result = FlowValidator.Validate(GetDocument(), factoriesByTypeId.Values.Select(static factory => factory.Definition));

        foreach (var issue in result.Issues)
        {
            ValidationIssues.Add(new ValidationIssueViewModel(issue));
        }
    }

    private async Task RunAsync()
    {
        ExecutionLogs.Clear();
        runCancellationTokenSource?.Dispose();
        runCancellationTokenSource = new CancellationTokenSource();
        ExecutionState = FlowExecutionState.Starting;
        var result = await runner.RunAsync(GetDocument(), runCancellationTokenSource.Token).ConfigureAwait(true);
        ExecutionState = result.State;

        ValidationIssues.Clear();
        foreach (var issue in result.ValidationIssues)
        {
            ValidationIssues.Add(new ValidationIssueViewModel(issue));
        }

        foreach (var log in result.Logs)
        {
            ExecutionLogs.Add(new ExecutionLogEntryViewModel(log));
        }
    }

    private void StopRun()
    {
        runCancellationTokenSource?.Cancel();
        ExecutionState = FlowExecutionState.Stopping;
    }

    private async Task SaveAsync()
    {
        await projectFileService.SaveAsync(ProjectDirectory, ProjectName, [GetDocument()], CancellationToken.None).ConfigureAwait(true);
    }

    private async Task LoadAsync()
    {
        var loadedProject = await projectFileService.LoadAsync(ProjectDirectory, CancellationToken.None).ConfigureAwait(true);
        ProjectName = loadedProject.Project.Name;

        var flow = loadedProject.Flows.FirstOrDefault();
        if (flow is null)
        {
            NewFlow();
            return;
        }

        LoadFlow(flow);
    }

    private void NewFlow()
    {
        Nodes.Clear();
        Connections.Clear();
        ValidationIssues.Clear();
        ExecutionLogs.Clear();
        SelectedNode = null;
        pendingConnectionSource = null;
        SelectedPaletteNode = null;
        FlowName = "Untitled flow";
        nextNodeX = 80;
        nextNodeY = 80;
        OnPropertyChanged(nameof(PendingConnectionLabel));
    }

    private FlowDocument GetDocument()
    {
        return new FlowDocument(
            flowId,
            FlowName,
            Nodes.Select(static node => node.ToNodeInstance()).ToArray(),
            Connections.Select(static connection => connection.Connection).ToArray());
    }

    private void LoadFlow(FlowDocument flow)
    {
        FlowName = flow.Name;
        Nodes.Clear();

        foreach (var node in flow.Nodes)
        {
            if (factoriesByTypeId.TryGetValue(node.TypeId, out var factory))
            {
                Nodes.Add(new CanvasNodeViewModel(node, factory.Definition));
            }
        }

        RebuildConnections(flow.Connections);
        SelectedNode = Nodes.FirstOrDefault();
        Validate();
    }

    private void RebuildConnections(IEnumerable<Connection> connections)
    {
        Connections.Clear();

        foreach (var connection in connections)
        {
            var sourceNode = Nodes.FirstOrDefault(node => string.Equals(node.Id, connection.SourceNodeId, StringComparison.Ordinal));
            var targetNode = Nodes.FirstOrDefault(node => string.Equals(node.Id, connection.TargetNodeId, StringComparison.Ordinal));
            var sourcePort = sourceNode?.Outputs.FirstOrDefault(port => string.Equals(port.Id, connection.SourcePortId, StringComparison.Ordinal));
            var targetPort = targetNode?.Inputs.FirstOrDefault(port => string.Equals(port.Id, connection.TargetPortId, StringComparison.Ordinal));

            if (sourceNode is not null && targetNode is not null && sourcePort is not null && targetPort is not null)
            {
                Connections.Add(new CanvasConnectionViewModel(connection, sourceNode, sourcePort, targetNode, targetPort));
            }
        }
    }

    private void RefreshConnections()
    {
        foreach (var connection in Connections)
        {
            connection.Refresh();
        }
    }

    private string GetNextNodeId()
    {
        var index = Nodes.Count + 1;

        while (Nodes.Any(node => string.Equals(node.Id, $"node-{index}", StringComparison.Ordinal)))
        {
            index++;
        }

        return $"node-{index}";
    }
}