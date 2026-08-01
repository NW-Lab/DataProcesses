using System.ComponentModel;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;

using CommunityToolkit.Mvvm.Input;

using DataProcesses.Core;
using DataProcesses.Desktop.Services;
using DataProcesses.Engine;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Desktop.ViewModels;

public sealed class FlowEditorViewModel : ViewModelBase
{
    private const string NodeDashboardWidgetType = "dataprocesses.dashboard.node-block";
    private const string PayloadOutputNodeTypeId = "dataprocesses.output.payload";
    private const int MaximumPayloadDashboardLogLines = 500;
    private const int RunLoopDelayMilliseconds = 100;
    private const double CanvasNodeWidth = 180;
    private const double CanvasPortPanelMargin = 8;
    private const double CanvasPortGlyphSize = 18;
    private const double CanvasNodeHeaderHeight = 34;
    private const double CanvasPortRowHeight = 22;
    private const double CanvasPortCenterOffset = 17;

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
    private long? runStartedUnixNanoseconds;
    private CanvasNodeViewModel? selectedNode;
    private PaletteNodeViewModel? selectedPaletteNode;
    private CanvasPortViewModel? pendingConnectionSource;
    private CanvasPortViewModel? pendingConnectionTarget;
    private double previewConnectionEndX;
    private double previewConnectionEndY;
    private bool isConnectionAnimationActive;
    private bool showPreviewConnection;
    private string connectionHintText = "Click an output port to start connecting.";
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
        PortClickCommand = new RelayCommand<CanvasPortViewModel>(_ => { });
        SelectNodeCommand = new RelayCommand<CanvasNodeViewModel>(SelectNode);
        DeleteSelectedCommand = new RelayCommand(DeleteSelected, () => SelectedNode is not null);
        DeleteNodeCommand = new RelayCommand<CanvasNodeViewModel>(DeleteNode);
        DeleteConnectionCommand = new RelayCommand<CanvasConnectionViewModel>(DeleteConnection);
        ValidateCommand = new RelayCommand(Validate);
        RunCommand = new AsyncRelayCommand(RunAsync);
        StopCommand = new RelayCommand(StopRun, () => ExecutionState is FlowExecutionState.Running or FlowExecutionState.Starting);
        ClearExecutionLogsCommand = new RelayCommand(ClearExecutionLogs);
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

    public IRelayCommand<CanvasNodeViewModel> DeleteNodeCommand { get; }

    public IRelayCommand<CanvasConnectionViewModel> DeleteConnectionCommand { get; }

    public IRelayCommand ValidateCommand { get; }

    public IAsyncRelayCommand RunCommand { get; }

    public IRelayCommand StopCommand { get; }

    public IRelayCommand ClearExecutionLogsCommand { get; }

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

    public string GetExecutionLogsClipboardText()
    {
        if (ExecutionLogs.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var log in ExecutionLogs)
        {
            builder.AppendLine(log.ClipboardText);
        }

        return builder.ToString();
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

    public bool ShowPreviewConnection
    {
        get => showPreviewConnection;
        private set => SetProperty(ref showPreviewConnection, value);
    }

    public CanvasPortViewModel? PendingConnectionTarget
    {
        get => pendingConnectionTarget;
        private set => SetProperty(ref pendingConnectionTarget, value);
    }

    public bool IsConnectionAnimationActive
    {
        get => isConnectionAnimationActive;
        private set => SetProperty(ref isConnectionAnimationActive, value);
    }

    public string ConnectionHintText
    {
        get => connectionHintText;
        private set => SetProperty(ref connectionHintText, value);
    }

    public double PreviewConnectionStartX => pendingConnectionSource is null
        ? 0
        : GetPortCenterX(pendingConnectionSource);

    public double PreviewConnectionStartY => pendingConnectionSource is null
        ? 0
        : GetPortCenterY(pendingConnectionSource);

    public double PreviewConnectionEndX
    {
        get => previewConnectionEndX;
        set
        {
            if (SetProperty(ref previewConnectionEndX, value))
            {
                OnPropertyChanged(nameof(PreviewConnectionPath));
            }
        }
    }

    public double PreviewConnectionEndY
    {
        get => previewConnectionEndY;
        set
        {
            if (SetProperty(ref previewConnectionEndY, value))
            {
                OnPropertyChanged(nameof(PreviewConnectionPath));
            }
        }
    }

    public string PreviewConnectionPath =>
        FormattableString.Invariant($"M {PreviewConnectionStartX} {PreviewConnectionStartY} C {PreviewConnectionStartX + 70} {PreviewConnectionStartY - 20}, {PreviewConnectionEndX - 70} {PreviewConnectionEndY + 20}, {PreviewConnectionEndX} {PreviewConnectionEndY}");

    public bool IsCanvasEditingEnabled
    {
        get => isCanvasEditingEnabled;
        private set => SetProperty(ref isCanvasEditingEnabled, value);
    }

    public void StartPendingConnection(CanvasPortViewModel port)
    {
        if (!IsCanvasEditingEnabled || port is null)
        {
            return;
        }

        pendingConnectionSource = port.IsOutput ? port : null;
        pendingConnectionTarget = null;
        ShowPreviewConnection = port.IsOutput;
        IsConnectionAnimationActive = port.IsOutput;
        ConnectionHintText = port.IsOutput
            ? $"Connecting from {port.Node.DisplayName}.{port.DisplayName}. Hold and release on another port to finish."
            : "Click an output port to start connecting.";
        InteractionStatus = ConnectionHintText;
        OnPropertyChanged(nameof(PendingConnectionLabel));
        OnPropertyChanged(nameof(PreviewConnectionStartX));
        OnPropertyChanged(nameof(PreviewConnectionStartY));
        OnPropertyChanged(nameof(PreviewConnectionPath));
        PreviewConnectionEndX = PreviewConnectionStartX;
        PreviewConnectionEndY = PreviewConnectionStartY;
    }

    public void UpdatePendingConnectionTarget(CanvasPortViewModel? port)
    {
        if (pendingConnectionSource is null)
        {
            return;
        }

        pendingConnectionTarget = port;

        if (port is null)
        {
            return;
        }

        PreviewConnectionEndX = GetPortCenterX(port);
        PreviewConnectionEndY = GetPortCenterY(port);
    }

    private static double GetPortCenterX(CanvasPortViewModel port)
    {
        return port.IsOutput
            ? port.Node.X + CanvasNodeWidth - CanvasPortPanelMargin - (CanvasPortGlyphSize / 2)
            : port.Node.X + CanvasPortPanelMargin + (CanvasPortGlyphSize / 2);
    }

    private static double GetPortCenterY(CanvasPortViewModel port)
    {
        var ports = port.IsOutput ? port.Node.Outputs : port.Node.Inputs;
        var index = 0;
        foreach (var candidate in ports)
        {
            if (string.Equals(candidate.Id, port.Id, StringComparison.Ordinal))
            {
                break;
            }

            index++;
        }

        return port.Node.Y + CanvasNodeHeaderHeight + index * CanvasPortRowHeight + CanvasPortCenterOffset;
    }

    public void HandlePortConnection(CanvasPortViewModel? sourcePort, CanvasPortViewModel? targetPort)
    {
        if (sourcePort is null || targetPort is null || !IsCanvasEditingEnabled)
        {
            CancelPendingConnection();
            return;
        }

        if (sourcePort.IsOutput && targetPort.IsInput)
        {
            if (!ConnectionValidator.CanConnect(sourcePort.Definition, targetPort.Definition))
            {
                ValidationIssues.Add(new ValidationIssueViewModel(new FlowValidationIssue(
                    FlowValidationSeverity.Error,
                    FlowValidationIssueCode.IncompatiblePorts,
                    $"Cannot connect {sourcePort.DisplayName} to {targetPort.DisplayName}.",
                    targetPort.Node.Id)));
                ConnectionHintText = $"Incompatible connection: {sourcePort.DisplayName} → {targetPort.DisplayName}.";
                InteractionStatus = ConnectionHintText;
                CancelPendingConnection();
                return;
            }

            var connection = new Connection(sourcePort.Node.Id, sourcePort.Id, targetPort.Node.Id, targetPort.Id, sourcePort.DataKind);
            var connections = Connections.Select(static connectionViewModel => connectionViewModel.Connection).Append(connection).ToArray();
            RebuildConnections(connections);
            Validate();
            MarkCurrentFlowDirty();
            ConnectionHintText = $"Connected {sourcePort.Node.DisplayName}.{sourcePort.DisplayName} → {targetPort.Node.DisplayName}.{targetPort.DisplayName}.";
            InteractionStatus = ConnectionHintText;
        }
        else
        {
            CancelPendingConnection();
            return;
        }

        pendingConnectionSource = null;
        pendingConnectionTarget = null;
        ShowPreviewConnection = false;
        IsConnectionAnimationActive = false;
        OnPropertyChanged(nameof(PendingConnectionLabel));
        OnPropertyChanged(nameof(PreviewConnectionPath));
    }

    public void CancelPendingConnection()
    {
        pendingConnectionSource = null;
        pendingConnectionTarget = null;
        ShowPreviewConnection = false;
        IsConnectionAnimationActive = false;
        ConnectionHintText = "Click an output port to start connecting.";
        InteractionStatus = ConnectionHintText;
        OnPropertyChanged(nameof(PendingConnectionLabel));
        OnPropertyChanged(nameof(PreviewConnectionPath));
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

        AddCanvasNode(node);
        SelectNode(node);
        RefreshConnections();
        SynchronizeDashboardWidgetForNode(node);
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

        DeleteNode(SelectedNode);
    }

    private void DeleteNode(CanvasNodeViewModel? node)
    {
        if (!IsCanvasEditingEnabled || node is null)
        {
            return;
        }

        SelectedNode = null;
        node.PropertyChanged -= CanvasNodePropertyChanged;
        Nodes.Remove(node);
        RebuildConnections(GetDocument().Connections.Where(connection =>
            !string.Equals(connection.SourceNodeId, node.Id, StringComparison.Ordinal)
            && !string.Equals(connection.TargetNodeId, node.Id, StringComparison.Ordinal)).ToArray());
        RemoveDashboardWidgetForNode(node);
        MarkCurrentFlowDirty();
    }

    private void DeleteConnection(CanvasConnectionViewModel? connectionViewModel)
    {
        if (!IsCanvasEditingEnabled || connectionViewModel is null)
        {
            return;
        }

        var remainingConnections = Connections
            .Select(static connection => connection.Connection)
            .Where(connection => !IsSameConnection(connection, connectionViewModel.Connection))
            .ToArray();

        if (remainingConnections.Length == Connections.Count)
        {
            return;
        }

        RebuildConnections(remainingConnections);
        Validate();
        MarkCurrentFlowDirty();
        InteractionStatus = "Connection deleted.";
    }

    private static bool IsSameConnection(Connection left, Connection right)
    {
        return string.Equals(left.SourceNodeId, right.SourceNodeId, StringComparison.Ordinal)
            && string.Equals(left.SourcePortId, right.SourcePortId, StringComparison.Ordinal)
            && string.Equals(left.TargetNodeId, right.TargetNodeId, StringComparison.Ordinal)
            && string.Equals(left.TargetPortId, right.TargetPortId, StringComparison.Ordinal)
            && left.DataKind == right.DataKind;
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
            ShowPreviewConnection = true;
            IsConnectionAnimationActive = true;
            ConnectionHintText = $"Connecting from {port.Node.DisplayName}.{port.DisplayName}. Click an input port to finish.";
            InteractionStatus = $"Connecting {port.Node.DisplayName}.{port.DisplayName}.";
            OnPropertyChanged(nameof(PendingConnectionLabel));
            OnPropertyChanged(nameof(PreviewConnectionStartX));
            OnPropertyChanged(nameof(PreviewConnectionStartY));
            PreviewConnectionEndX = port.Node.X + 220;
            PreviewConnectionEndY = port.Node.Y + 60;
            return;
        }

        if (pendingConnectionSource is null)
        {
            ConnectionHintText = "Click an output port to start connecting.";
            InteractionStatus = "Click an output port to start connecting.";
            return;
        }

        var source = pendingConnectionSource;
        pendingConnectionSource = null;
        ShowPreviewConnection = false;
        IsConnectionAnimationActive = false;
        ConnectionHintText = $"Connected {source.Node.DisplayName}.{source.DisplayName} → {port.Node.DisplayName}.{port.DisplayName}.";
        InteractionStatus = $"Connected {source.Node.DisplayName}.{source.DisplayName} → {port.Node.DisplayName}.{port.DisplayName}.";
        OnPropertyChanged(nameof(PendingConnectionLabel));

        if (!ConnectionValidator.CanConnect(source.Definition, port.Definition))
        {
            ValidationIssues.Add(new ValidationIssueViewModel(new FlowValidationIssue(
                FlowValidationSeverity.Error,
                FlowValidationIssueCode.IncompatiblePorts,
                $"Cannot connect {source.DisplayName} to {port.DisplayName}.",
                port.Node.Id)));
            ConnectionHintText = $"Incompatible connection: {source.DisplayName} → {port.DisplayName}.";
            InteractionStatus = ConnectionHintText;
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
        var cancellationToken = runCancellationTokenSource.Token;
        InteractionStatus = isDebugExecution
            ? "Running flow in debug mode."
            : "Running flow.";
        ExecutionState = FlowExecutionState.Starting;
        runStartedUnixNanoseconds = null;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await runner.RunAsync(GetDocument(), cancellationToken).ConfigureAwait(true);
                ApplyRunResult(result);

                if (result.State == FlowExecutionState.Faulted)
                {
                    ExecutionState = result.State;
                    InteractionStatus = "Flow run faulted.";
                    return;
                }

                ExecutionState = FlowExecutionState.Running;
                await Task.Delay(RunLoopDelayMilliseconds, cancellationToken).ConfigureAwait(true);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            if (cancellationToken.IsCancellationRequested)
            {
                ExecutionState = FlowExecutionState.Stopped;
                InteractionStatus = "Flow stopped.";
                runStartedUnixNanoseconds = null;
            }
        }
    }

    private void ApplyRunResult(FlowRunResult result)
    {
        ValidationIssues.Clear();
        foreach (var issue in result.ValidationIssues)
        {
            ValidationIssues.Add(new ValidationIssueViewModel(issue));
        }

        foreach (var log in result.Logs)
        {
            ExecutionLogs.Add(new ExecutionLogEntryViewModel(log));
        }

        UpdateDashboardWidgetsFromRunResult(result);
    }

    private void StopRun()
    {
        runCancellationTokenSource?.Cancel();
        ExecutionState = FlowExecutionState.Stopping;
    }

    private void ClearExecutionLogs()
    {
        ExecutionLogs.Clear();
        PersistWorkspaceState(GetDocument());
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
        UnsubscribeCanvasNodes();
        Nodes.Clear();
        Connections.Clear();
        ValidationIssues.Clear();
        ExecutionLogs.Clear();
        SelectedNode = null;
        pendingConnectionSource = null;
        ShowPreviewConnection = false;
        IsConnectionAnimationActive = false;
        ConnectionHintText = "Click an output port to start connecting.";
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
            UnsubscribeCanvasNodes();
            Nodes.Clear();

            foreach (var node in flow.Nodes)
            {
                if (factoriesByTypeId.TryGetValue(node.TypeId, out var factory))
                {
                    AddCanvasNode(new CanvasNodeViewModel(node, factory.Definition));
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

    private void AddCanvasNode(CanvasNodeViewModel node)
    {
        node.PropertyChanged += CanvasNodePropertyChanged;
        Nodes.Add(node);
    }

    private void UnsubscribeCanvasNodes()
    {
        foreach (var node in Nodes)
        {
            node.PropertyChanged -= CanvasNodePropertyChanged;
        }
    }

    private void CanvasNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not CanvasNodeViewModel node || isApplyingFlowState)
        {
            return;
        }

        if (e.PropertyName is nameof(CanvasNodeViewModel.Name)
            or nameof(CanvasNodeViewModel.IsEnabled)
            or nameof(CanvasNodeViewModel.ShowOnDashboard)
            or nameof(CanvasNodeViewModel.DashboardGridWidth)
            or nameof(CanvasNodeViewModel.DashboardGridHeight))
        {
            SynchronizeDashboardWidgetForNode(node);
            MarkCurrentFlowDirty();
        }
        else if (e.PropertyName is nameof(CanvasNodeViewModel.SettingsJson))
        {
            MarkCurrentFlowDirty();
        }
    }

    private void SynchronizeDashboardWidgetForNode(CanvasNodeViewModel node, string? contentOverride = null)
    {
        ArgumentNullException.ThrowIfNull(node);

        var dashboards = getDashboardDocuments().ToList();
        if (dashboards.Count == 0 && node.ShowOnDashboard)
        {
            dashboards.Add(new DashboardDocument(Guid.NewGuid(), "Default dashboard", []));
        }

        var updatedDashboards = new List<DashboardDocument>(dashboards.Count);

        for (var dashboardIndex = 0; dashboardIndex < dashboards.Count; dashboardIndex++)
        {
            var dashboard = dashboards[dashboardIndex];
            var widgets = dashboard.Widgets
                .Where(widget => !IsDashboardWidgetForNode(widget, node.Id))
                .ToList();

            if (node.ShowOnDashboard && dashboardIndex == 0)
            {
                var existing = dashboard.Widgets.FirstOrDefault(widget => IsDashboardWidgetForNode(widget, node.Id));
                var content = contentOverride ?? ReadDashboardWidgetContent(existing?.SettingsJson);
                var widget = existing is null
                    ? CreateDashboardWidgetForNode(node, widgets, content)
                    : existing with
                    {
                        SettingsJson = CreateDashboardWidgetSettingsJson(node, content),
                    };

                widgets.Add(widget);
            }

            updatedDashboards.Add(dashboard with { Widgets = widgets });
        }

        loadDashboardDocuments(updatedDashboards);
    }

    private void RemoveDashboardWidgetForNode(CanvasNodeViewModel node)
    {
        var dashboards = getDashboardDocuments();
        if (dashboards.Count == 0)
        {
            return;
        }

        loadDashboardDocuments(dashboards
            .Select(dashboard => dashboard with
            {
                Widgets = dashboard.Widgets.Where(widget => !IsDashboardWidgetForNode(widget, node.Id)).ToArray(),
            })
            .ToArray());
    }

    private void UpdateDashboardWidgetsFromRunResult(FlowRunResult result)
    {
        var latestFastStreamByNode = result.OutputPackets
            .Where(static output => output.Packet is FastStreamFrame)
            .GroupBy(static output => output.NodeId, StringComparer.Ordinal)
            .Select(static group => group.Last())
            .ToArray();

        foreach (var output in latestFastStreamByNode)
        {
            var node = Nodes.FirstOrDefault(candidate => string.Equals(candidate.Id, output.NodeId, StringComparison.Ordinal));
            if (node is null || !node.ShowOnDashboard || output.Packet is not FastStreamFrame frame)
            {
                continue;
            }

            SynchronizeDashboardWidgetForNode(node, FormatFastStreamFrame(frame, GetRunStartedUnixNanoseconds(frame)));
        }

        var dashboardContentByNodeId = GetNodeDashboardWidgetContentByNodeId();
        var payloadLogByNodeId = new Dictionary<string, string>(StringComparer.Ordinal);
        var nodesById = Nodes.ToDictionary(static node => node.Id, StringComparer.Ordinal);

        foreach (var output in result.OutputPackets)
        {
            if (output.Packet is not JsonMessage message)
            {
                continue;
            }

            var targetNodeIds = Connections
                .Select(static connection => connection.Connection)
                .Where(connection =>
                    string.Equals(connection.SourceNodeId, output.NodeId, StringComparison.Ordinal)
                    && string.Equals(connection.SourcePortId, output.OutputPortId, StringComparison.Ordinal)
                    && connection.DataKind == PortDataKind.JsonMessage)
                .Select(static connection => connection.TargetNodeId)
                .ToArray();

            foreach (var targetNodeId in targetNodeIds)
            {
                if (!nodesById.TryGetValue(targetNodeId, out var node)
                    || !node.ShowOnDashboard
                    || !string.Equals(node.TypeId, PayloadOutputNodeTypeId, StringComparison.Ordinal))
                {
                    continue;
                }

                var entry = FormatPayloadMessage(message);
                var currentContent = payloadLogByNodeId.TryGetValue(node.Id, out var stagedContent)
                    ? stagedContent
                    : dashboardContentByNodeId.GetValueOrDefault(node.Id, string.Empty);
                payloadLogByNodeId[node.Id] = AppendLogEntry(currentContent, entry);
            }
        }

        foreach (var payloadLog in payloadLogByNodeId)
        {
            if (nodesById.TryGetValue(payloadLog.Key, out var node))
            {
                SynchronizeDashboardWidgetForNode(node, payloadLog.Value);
            }
        }
    }

    private Dictionary<string, string> GetNodeDashboardWidgetContentByNodeId()
    {
        return getDashboardDocuments()
            .SelectMany(static dashboard => dashboard.Widgets)
            .Where(widget =>
                string.Equals(widget.WidgetType, NodeDashboardWidgetType, StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(widget.SourcePortId))
            .GroupBy(static widget => widget.SourcePortId!, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => ReadDashboardWidgetContent(group.Last().SettingsJson),
                StringComparer.Ordinal);
    }

    private long GetRunStartedUnixNanoseconds(FastStreamFrame frame)
    {
        runStartedUnixNanoseconds ??= frame.StartTimeUnixNanoseconds;
        return runStartedUnixNanoseconds.Value;
    }

    private DashboardWidget CreateDashboardWidgetForNode(
        CanvasNodeViewModel node,
        IReadOnlyList<DashboardWidget> existingWidgets,
        string content)
    {
        var (gridX, gridY) = FindDashboardPlacement(existingWidgets, node.DashboardGridWidth, node.DashboardGridHeight);
        return new DashboardWidget(
            Guid.NewGuid(),
            NodeDashboardWidgetType,
            gridX,
            gridY,
            node.DashboardGridWidth,
            node.DashboardGridHeight,
            flowId.ToString("D", CultureInfo.InvariantCulture),
            node.Id,
            CreateDashboardWidgetSettingsJson(node, content));
    }

    private static (int GridX, int GridY) FindDashboardPlacement(
        IReadOnlyList<DashboardWidget> existingWidgets,
        int gridWidth,
        int gridHeight)
    {
        var maxColumns = DashboardViewModel.CanvasWidthPixels / DashboardViewModel.GridSizePixels;
        var maxRows = DashboardViewModel.CanvasHeightPixels / DashboardViewModel.GridSizePixels;

        for (var y = 0; y <= maxRows - gridHeight; y++)
        {
            for (var x = 0; x <= maxColumns - gridWidth; x++)
            {
                if (!existingWidgets.Any(widget => Overlaps(x, y, gridWidth, gridHeight, widget)))
                {
                    return (x, y);
                }
            }
        }

        return (0, Math.Max(0, maxRows - gridHeight));
    }

    private static bool Overlaps(int x, int y, int width, int height, DashboardWidget widget)
    {
        return x < widget.GridX + widget.GridWidth
            && x + width > widget.GridX
            && y < widget.GridY + widget.GridHeight
            && y + height > widget.GridY;
    }

    private static bool IsDashboardWidgetForNode(DashboardWidget widget, string nodeId)
    {
        return string.Equals(widget.WidgetType, NodeDashboardWidgetType, StringComparison.Ordinal)
            && string.Equals(widget.SourcePortId, nodeId, StringComparison.Ordinal);
    }

    private static string CreateDashboardWidgetSettingsJson(CanvasNodeViewModel node, string content)
    {
        return JsonSerializer.Serialize(new
        {
            title = node.DisplayName,
            contentKind = "text",
            content,
            displayData = new
            {
                text = content,
            },
            isSourceNodeEnabled = node.IsEnabled,
        });
    }

    private static string ReadDashboardWidgetContent(string? settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            return string.Empty;
        }

        using var document = JsonDocument.Parse(settingsJson);
        return document.RootElement.ValueKind == JsonValueKind.Object
            && document.RootElement.TryGetProperty("content", out var content)
            && content.ValueKind == JsonValueKind.String
            ? content.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string FormatFastStreamFrame(FastStreamFrame frame, long runStartedUnixNanoseconds)
    {
        if (frame.ChannelCount == 0 || frame.SampleCount == 0)
        {
            return "millis,value";
        }

        var samples = frame.Samples[0].Span;
        var lines = new List<string>(Math.Min(samples.Length, 8) + 1)
        {
            "millis,value",
        };

        var sampleCount = Math.Min(samples.Length, 8);
        var startMillis = Math.Max(0, frame.StartTimeUnixNanoseconds - runStartedUnixNanoseconds) / 1_000_000.0;
        for (var index = 0; index < sampleCount; index++)
        {
            var millis = startMillis + (index * frame.SamplePeriodNanoseconds / 1_000_000.0);
            lines.Add(FormattableString.Invariant($"{millis:0.###},{samples[index]:0.####}"));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatPayloadMessage(JsonMessage message)
    {
        var payload = JsonSerializer.Serialize(message.Payload);
        var correlationId = string.IsNullOrWhiteSpace(message.CorrelationId)
            ? "-"
            : message.CorrelationId;

        return FormattableString.Invariant(
            $"[{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss.fff zzz}] topic={message.Topic} correlationId={correlationId} payload={payload}");
    }

    private static string AppendLogEntry(string currentContent, string entry)
    {
        if (string.IsNullOrEmpty(currentContent))
        {
            return entry;
        }

        var merged = string.Join(Environment.NewLine, currentContent, entry);
        var lines = merged.Split(Environment.NewLine, StringSplitOptions.None);
        if (lines.Length <= MaximumPayloadDashboardLogLines)
        {
            return merged;
        }

        var keepFrom = lines.Length - MaximumPayloadDashboardLogLines;
        return string.Join(Environment.NewLine, lines[keepFrom..]);
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