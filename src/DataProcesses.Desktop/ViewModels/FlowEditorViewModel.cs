using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.Input;

using DataProcesses.Core;
using DataProcesses.Desktop.Services;
using DataProcesses.Engine;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Desktop.ViewModels;

public sealed class FlowEditorViewModel : ViewModelBase
{
    private sealed record FlowWorkspaceState(
        FlowDocument Document,
        IReadOnlyList<ValidationIssueViewModel> ValidationIssues,
        IReadOnlyList<ExecutionLogEntryViewModel> ExecutionLogs,
        FlowExecutionState ExecutionState,
        bool IsDirty);

    private readonly IReadOnlyDictionary<string, INodeFactory> factoriesByTypeId;
    private readonly FlowRunner runner;
    private readonly ProjectFileService projectFileService;
    private readonly Func<IReadOnlyList<DashboardDocument>> getDashboardDocuments;
    private readonly Action<IReadOnlyList<DashboardDocument>> loadDashboardDocuments;
    private readonly Action markDashboardsClean;
    private readonly List<FlowDocument> additionalFlows = [];
    private readonly Dictionary<Guid, FlowWorkspaceState> flowWorkspaceById = [];
    private Guid flowId = Guid.NewGuid();
    private FlowListItemViewModel? selectedFlow;
    private bool isLoadingFlows;
    private bool isSaving;
    private bool isApplyingFlowState;
    private bool isCanvasEditingEnabled = true;
    private bool isDebugExecution;
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
        ProjectFileService projectFileService,
        Func<IReadOnlyList<DashboardDocument>>? getDashboardDocuments = null,
        Action<IReadOnlyList<DashboardDocument>>? loadDashboardDocuments = null,
        Action? markDashboardsClean = null)
    {
        ArgumentNullException.ThrowIfNull(factories);
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(projectFileService);

        var factoryList = factories.ToArray();
        factoriesByTypeId = factoryList.ToDictionary(static factory => factory.Definition.TypeId, StringComparer.Ordinal);
        this.runner = runner;
        this.projectFileService = projectFileService;
        this.getDashboardDocuments = getDashboardDocuments ?? (() => Array.Empty<DashboardDocument>());
        this.loadDashboardDocuments = loadDashboardDocuments ?? (_ => { });
        this.markDashboardsClean = markDashboardsClean ?? (() => { });
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
        AddFlowCommand = new RelayCommand(AddFlow);
        RemoveFlowCommand = new RelayCommand(RemoveSelectedFlow);
        ZoomInCommand = new RelayCommand(() => Zoom = Math.Min(2, Zoom + 0.1));
        ZoomOutCommand = new RelayCommand(() => Zoom = Math.Max(0.4, Zoom - 0.1));

        var initialFlow = new FlowListItemViewModel(flowId, FlowName);
        Flows.Add(initialFlow);
        SelectedFlow = initialFlow;
    }

    public NodePaletteViewModel Palette { get; }

    public InspectorViewModel Inspector { get; }

    public ObservableCollection<CanvasNodeViewModel> Nodes { get; } = [];

    public ObservableCollection<CanvasConnectionViewModel> Connections { get; } = [];

    public ObservableCollection<ValidationIssueViewModel> ValidationIssues { get; } = [];

    public ObservableCollection<ExecutionLogEntryViewModel> ExecutionLogs { get; } = [];

    public ObservableCollection<FlowListItemViewModel> Flows { get; } = [];

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

    public IRelayCommand AddFlowCommand { get; }

    public IRelayCommand RemoveFlowCommand { get; }

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
        set
        {
            if (SetProperty(ref flowName, value) && SelectedFlow is not null)
            {
                SelectedFlow.Name = value;
                if (!isLoadingFlows && !isApplyingFlowState)
                {
                    MarkCurrentFlowDirty();
                }
            }
        }
    }

    public FlowListItemViewModel? SelectedFlow
    {
        get => selectedFlow;
        set
        {
            if (ReferenceEquals(selectedFlow, value))
            {
                return;
            }

            if (!isLoadingFlows)
            {
                PersistCurrentFlow();
            }

            if (SetProperty(ref selectedFlow, value) && value is not null)
            {
                LoadFlowById(value.Id);
            }
        }
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

    public bool IsCanvasEditingEnabled
    {
        get => isCanvasEditingEnabled;
        private set => SetProperty(ref isCanvasEditingEnabled, value);
    }

    public void ApplyWorkspaceMode(WorkspaceRunMode mode)
    {
        IsCanvasEditingEnabled = mode == WorkspaceRunMode.Edit;

        if (mode == WorkspaceRunMode.Edit)
        {
            isDebugExecution = false;
            InteractionStatus = "Edit mode: drag a Block from the Node Library onto the canvas.";
        }
        else
        {
            InteractionStatus = mode == WorkspaceRunMode.RunDebug
                ? "Debug run mode: editing is locked while execution runs."
                : "Run mode: editing is locked while execution runs.";
        }
    }

    public async Task StartExecutionAsync(bool debugMode)
    {
        if (ExecutionState is FlowExecutionState.Starting or FlowExecutionState.Running)
        {
            return;
        }

        isDebugExecution = debugMode;
        await RunAsync().ConfigureAwait(true);
    }

    public void StopExecution()
    {
        if (ExecutionState is FlowExecutionState.Starting or FlowExecutionState.Running)
        {
            StopRun();
        }

        isDebugExecution = false;
    }

    public void MoveNode(CanvasNodeViewModel node, double deltaX, double deltaY)
    {
        if (!IsCanvasEditingEnabled)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(node);

        node.X += deltaX / Zoom;
        node.Y += deltaY / Zoom;
        RefreshConnections();
        MarkCurrentFlowDirty();
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
        if (!IsCanvasEditingEnabled)
        {
            return;
        }

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
            new NodeInstance(GetNextNodeId(), paletteNode.TypeId, Math.Max(0, x), Math.Max(0, y), "{}", Name: paletteNode.Title),
            paletteNode.Definition);

        Nodes.Add(node);
        SelectNode(node);
        RefreshConnections();
        MarkCurrentFlowDirty();
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
        if (!IsCanvasEditingEnabled)
        {
            return;
        }

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
        MarkCurrentFlowDirty();
    }

    private void HandlePortClick(CanvasPortViewModel? port)
    {
        if (!IsCanvasEditingEnabled)
        {
            return;
        }

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
        MarkCurrentFlowDirty();
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
        InteractionStatus = isDebugExecution
            ? "Running flow in debug mode."
            : "Running flow.";
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
        PersistCurrentFlow();
        isSaving = true;

        try
        {
            await projectFileService.SaveAsync(
                ProjectDirectory,
                ProjectName,
                [..additionalFlows],
                getDashboardDocuments(),
                CancellationToken.None).ConfigureAwait(true);

            MarkAllFlowsClean();
            markDashboardsClean();
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task LoadAsync()
    {
        var loadedProject = await projectFileService.LoadAsync(ProjectDirectory, CancellationToken.None).ConfigureAwait(true);
        ProjectName = loadedProject.Project.Name;
        loadDashboardDocuments(loadedProject.Dashboards);

        isLoadingFlows = true;
        try
        {
            Flows.Clear();
            additionalFlows.Clear();
            flowWorkspaceById.Clear();

            if (loadedProject.Flows.Count == 0)
            {
                InitializeSingleNewFlow();
                return;
            }

            additionalFlows.AddRange(loadedProject.Flows);
            foreach (var flow in loadedProject.Flows)
            {
                flowWorkspaceById[flow.Id] = new FlowWorkspaceState(
                    flow,
                    [],
                    [],
                    FlowExecutionState.Stopped,
                    false);
            }

            foreach (var flow in loadedProject.Flows)
            {
                Flows.Add(new FlowListItemViewModel(flow.Id, flow.Name));
            }

            SelectedFlow = Flows[0];
        }
        finally
        {
            isLoadingFlows = false;
        }
    }

    private void NewFlow()
    {
        ResetCurrentFlowState();
    }

    private void AddFlow()
    {
        PersistCurrentFlow();

        var newFlowId = Guid.NewGuid();
        var newFlowName = $"Flow {Flows.Count + 1}";

        isLoadingFlows = true;
        try
        {
            var item = new FlowListItemViewModel(newFlowId, newFlowName);
            Flows.Add(item);
            SelectedFlow = item;
        }
        finally
        {
            isLoadingFlows = false;
        }

        flowId = newFlowId;
        FlowName = newFlowName;
        ResetCurrentFlowState();
        MarkCurrentFlowClean();
    }

    private void RemoveSelectedFlow()
    {
        if (Flows.Count <= 1 || SelectedFlow is null)
        {
            return;
        }

        var removedFlowId = SelectedFlow.Id;
        var removeIndex = Flows.IndexOf(SelectedFlow);

        isLoadingFlows = true;
        try
        {
            Flows.RemoveAt(removeIndex);
            additionalFlows.RemoveAll(flow => flow.Id == removedFlowId);
            flowWorkspaceById.Remove(removedFlowId);

            var nextIndex = Math.Max(0, removeIndex - 1);
            SelectedFlow = Flows[nextIndex];
        }
        finally
        {
            isLoadingFlows = false;
        }
    }

    private void InitializeSingleNewFlow()
    {
        flowId = Guid.NewGuid();
        FlowName = "Untitled flow";
        var item = new FlowListItemViewModel(flowId, FlowName);
        Flows.Add(item);
        SelectedFlow = item;
        ResetCurrentFlowState();
        PersistCurrentFlow();
    }

    private void ResetCurrentFlowState()
    {
        Nodes.Clear();
        Connections.Clear();
        ValidationIssues.Clear();
        ExecutionLogs.Clear();
        SelectedNode = null;
        pendingConnectionSource = null;
        SelectedPaletteNode = null;
        nextNodeX = 80;
        nextNodeY = 80;
        ExecutionState = FlowExecutionState.Stopped;
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
        isApplyingFlowState = true;

        try
        {
            flowId = flow.Id;
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

            if (flowWorkspaceById.TryGetValue(flow.Id, out var workspace))
            {
                ValidationIssues.Clear();
                foreach (var issue in workspace.ValidationIssues)
                {
                    ValidationIssues.Add(issue);
                }

                ExecutionLogs.Clear();
                foreach (var log in workspace.ExecutionLogs)
                {
                    ExecutionLogs.Add(log);
                }

                ExecutionState = workspace.ExecutionState;
                if (SelectedFlow is not null)
                {
                    SelectedFlow.IsDirty = workspace.IsDirty;
                }
            }
            else
            {
                Validate();
                MarkCurrentFlowClean();
            }
        }
        finally
        {
            isApplyingFlowState = false;
        }
    }

    private void PersistCurrentFlow()
    {
        var current = GetDocument();
        var index = additionalFlows.FindIndex(flow => flow.Id == current.Id);

        if (index >= 0)
        {
            additionalFlows[index] = current;
        }
        else
        {
            additionalFlows.Add(current);
        }

        PersistWorkspaceState(current);
    }

    private void LoadFlowById(Guid flowIdToLoad)
    {
        var flow = additionalFlows.FirstOrDefault(candidate => candidate.Id == flowIdToLoad);
        if (flow is null)
        {
            return;
        }

        LoadFlow(flow);
    }

    private void PersistWorkspaceState(FlowDocument document)
    {
        flowWorkspaceById[document.Id] = new FlowWorkspaceState(
            document,
            [..ValidationIssues],
            [..ExecutionLogs],
            ExecutionState,
            SelectedFlow?.IsDirty ?? false);
    }

    private void MarkCurrentFlowDirty()
    {
        if (isLoadingFlows || isSaving || SelectedFlow is null)
        {
            return;
        }

        SelectedFlow.IsDirty = true;
        PersistWorkspaceState(GetDocument());
    }

    private void MarkCurrentFlowClean()
    {
        if (SelectedFlow is null)
        {
            return;
        }

        SelectedFlow.IsDirty = false;
        PersistWorkspaceState(GetDocument());
    }

    private void MarkAllFlowsClean()
    {
        foreach (var flow in Flows)
        {
            flow.IsDirty = false;
        }

        foreach (var flowDocument in additionalFlows)
        {
            if (flowWorkspaceById.TryGetValue(flowDocument.Id, out var workspace))
            {
                flowWorkspaceById[flowDocument.Id] = workspace with { IsDirty = false };
            }
        }
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