using System.Text.Json;
using System.Reflection;

using DataProcesses.Core;
using DataProcesses.Desktop.Services;
using DataProcesses.Desktop.ViewModels;
using DataProcesses.Engine;
using DataProcesses.Nodes.BuiltIn.Blocks.CsvInput;
using DataProcesses.Nodes.BuiltIn.Blocks.PayloadOutput;
using DataProcesses.Nodes.BuiltIn.Blocks.StremOutputTS;
using DataProcesses.Nodes.BuiltIn.Blocks.TestSignalTS;
using DataProcesses.Nodes.BuiltIn.Blocks.Trigger;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Desktop.Tests;

public sealed class FlowEditorViewModelTests
{
    [Fact]
    public void RemoveFlow_KeepsAtLeastOneFlow()
    {
        var factory = new TestNodeFactory();
        var viewModel = new FlowEditorViewModel(
            [factory],
            new FlowRunner([factory]),
            new ProjectFileService());

        viewModel.RemoveFlowCommand.Execute(null);
        Assert.Single(viewModel.Flows);

        viewModel.AddFlowCommand.Execute(null);
        Assert.Equal(2, viewModel.Flows.Count);

        viewModel.RemoveFlowCommand.Execute(null);
        Assert.Single(viewModel.Flows);
    }

    [Fact]
    public async Task FlowDirtyFlag_SetsOnEdit_AndClearsOnSave()
    {
        var factory = new TestNodeFactory();
        var projectDirectory = Path.Combine(Path.GetTempPath(), "DataProcesses.Tests", Guid.NewGuid().ToString("N"));
        var viewModel = new FlowEditorViewModel(
            [factory],
            new FlowRunner([factory]),
            new ProjectFileService());
        viewModel.ProjectDirectory = projectDirectory;
        var paletteNode = Assert.Single(viewModel.Palette.FilteredNodes);

        try
        {
            viewModel.PlacePaletteNode(paletteNode, 180, 200);
            Assert.True(viewModel.Flows[0].IsDirty);

            await viewModel.SaveCommand.ExecuteAsync(null);
            Assert.False(viewModel.Flows[0].IsDirty);
        }
        finally
        {
            if (Directory.Exists(projectDirectory))
            {
                Directory.Delete(projectDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void AddFlow_AndSwitch_BackPreservesEachFlowCanvas()
    {
        var factory = new TestNodeFactory();
        var viewModel = new FlowEditorViewModel(
            [factory],
            new FlowRunner([factory]),
            new ProjectFileService());
        var paletteNode = Assert.Single(viewModel.Palette.FilteredNodes);

        viewModel.PlacePaletteNode(paletteNode, 100, 120);
        viewModel.AddFlowCommand.Execute(null);
        viewModel.PlacePaletteNode(paletteNode, 400, 440);

        Assert.Equal(2, viewModel.Flows.Count);

        viewModel.SelectedFlow = viewModel.Flows[0];
        var firstFlowNode = Assert.Single(viewModel.Nodes);
        Assert.Equal(100, firstFlowNode.X);
        Assert.Equal(120, firstFlowNode.Y);

        viewModel.SelectedFlow = viewModel.Flows[1];
        var secondFlowNode = Assert.Single(viewModel.Nodes);
        Assert.Equal(400, secondFlowNode.X);
        Assert.Equal(440, secondFlowNode.Y);
    }

    [Fact]
    public void SwitchFlow_PreservesValidationAndExecutionLogsPerFlow()
    {
        var factory = new TestNodeFactory();
        var viewModel = new FlowEditorViewModel(
            [factory],
            new FlowRunner([factory]),
            new ProjectFileService());

        viewModel.ValidationIssues.Add(new ValidationIssueViewModel(new FlowValidationIssue(
            FlowValidationSeverity.Warning,
            FlowValidationIssueCode.MissingRequiredInput,
            "Flow A warning")));
        viewModel.ExecutionLogs.Add(new ExecutionLogEntryViewModel(new FlowExecutionLogEntry(
            DateTimeOffset.UtcNow,
            FlowExecutionLogLevel.Information,
            "Flow A log")));

        viewModel.AddFlowCommand.Execute(null);

        viewModel.ValidationIssues.Add(new ValidationIssueViewModel(new FlowValidationIssue(
            FlowValidationSeverity.Warning,
            FlowValidationIssueCode.MissingRequiredInput,
            "Flow B warning")));
        viewModel.ExecutionLogs.Add(new ExecutionLogEntryViewModel(new FlowExecutionLogEntry(
            DateTimeOffset.UtcNow,
            FlowExecutionLogLevel.Information,
            "Flow B log")));

        viewModel.SelectedFlow = viewModel.Flows[0];
        Assert.Contains(viewModel.ValidationIssues, issue => issue.Message == "Flow A warning");
        Assert.Contains(viewModel.ExecutionLogs, log => log.Message == "Flow A log");

        viewModel.SelectedFlow = viewModel.Flows[1];
        Assert.Contains(viewModel.ValidationIssues, issue => issue.Message == "Flow B warning");
        Assert.Contains(viewModel.ExecutionLogs, log => log.Message == "Flow B log");
    }

    [Fact]
    public void ClearExecutionLogsCommand_ClearsCurrentLogs()
    {
        var factory = new TestNodeFactory();
        var viewModel = new FlowEditorViewModel(
            [factory],
            new FlowRunner([factory]),
            new ProjectFileService());
        viewModel.ExecutionLogs.Add(new ExecutionLogEntryViewModel(new FlowExecutionLogEntry(
            DateTimeOffset.UtcNow,
            FlowExecutionLogLevel.Information,
            "Log to clear")));

        viewModel.ClearExecutionLogsCommand.Execute(null);

        Assert.Empty(viewModel.ExecutionLogs);
    }

    [Fact]
    public void GetExecutionLogsClipboardText_FormatsLogLines()
    {
        var factory = new TestNodeFactory();
        var viewModel = new FlowEditorViewModel(
            [factory],
            new FlowRunner([factory]),
            new ProjectFileService());
        viewModel.ExecutionLogs.Add(new ExecutionLogEntryViewModel(new FlowExecutionLogEntry(
            new DateTimeOffset(2026, 7, 20, 8, 9, 10, TimeSpan.Zero),
            FlowExecutionLogLevel.Information,
            "Formatted log",
            "node-1")));

        var text = viewModel.GetExecutionLogsClipboardText();

        Assert.Contains("Information", text);
        Assert.Contains("node-1", text);
        Assert.Contains("Formatted log", text);
    }

    [Fact]
    public async Task SaveAndLoad_PreservesAdditionalFlows()
    {
        var factory = new TestNodeFactory();
        var service = new ProjectFileService();
        var projectDirectory = Path.Combine(Path.GetTempPath(), "DataProcesses.Tests", Guid.NewGuid().ToString("N"));
        var firstFlow = new FlowDocument(
            Guid.NewGuid(),
            "Flow A",
            [new NodeInstance("node-1", "test.block", 10, 10, "{}")],
            []);
        var secondFlow = new FlowDocument(
            Guid.NewGuid(),
            "Flow B",
            [new NodeInstance("node-2", "test.block", 20, 20, "{}")],
            []);

        try
        {
            await service.SaveAsync(projectDirectory, "Project with two flows", [firstFlow, secondFlow], [], CancellationToken.None);

            var viewModel = new FlowEditorViewModel(
                [factory],
                new FlowRunner([factory]),
                service);
            viewModel.ProjectDirectory = projectDirectory;

            await viewModel.LoadCommand.ExecuteAsync(null);
            await viewModel.SaveCommand.ExecuteAsync(null);

            var reloaded = await service.LoadAsync(projectDirectory, CancellationToken.None);
            Assert.Equal(2, reloaded.Flows.Count);
            Assert.Contains(reloaded.Flows, flow => flow.Name == "Flow A");
            Assert.Contains(reloaded.Flows, flow => flow.Name == "Flow B");
        }
        finally
        {
            if (Directory.Exists(projectDirectory))
            {
                Directory.Delete(projectDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SaveAndLoad_UsesDashboardCallbacks()
    {
        var factory = new TestNodeFactory();
        var service = new ProjectFileService();
        var projectDirectory = Path.Combine(Path.GetTempPath(), "DataProcesses.Tests", Guid.NewGuid().ToString("N"));
        IReadOnlyList<DashboardDocument>? loadedDashboards = null;
        var dashboards = new List<DashboardDocument>
        {
            new(
                Guid.NewGuid(),
                "Monitor",
                [new DashboardWidget(Guid.NewGuid(), "dataprocesses.output.stream", 1, 2, 3, 2)]),
        };

        try
        {
            var viewModel = new FlowEditorViewModel(
                [factory],
                new FlowRunner([factory]),
                service,
                () => dashboards,
                documents => loadedDashboards = documents);
            viewModel.ProjectDirectory = projectDirectory;
            viewModel.ProjectName = "Project with dashboard";

            await viewModel.SaveCommand.ExecuteAsync(null);
            await viewModel.LoadCommand.ExecuteAsync(null);

            var loadedDashboard = Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<DashboardDocument>>(loadedDashboards));
            Assert.Equal("Monitor", loadedDashboard.Name);
            var loadedWidget = Assert.Single(loadedDashboard.Widgets);
            Assert.Equal(1, loadedWidget.GridX);
            Assert.Equal(2, loadedWidget.GridY);
            Assert.Equal(3, loadedWidget.GridWidth);
            Assert.Equal(2, loadedWidget.GridHeight);
        }
        finally
        {
            if (Directory.Exists(projectDirectory))
            {
                Directory.Delete(projectDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void ConnectionFlow_CanBeCanceledAndClearsPreview()
    {
        var factory = new TestNodeFactory();
        var viewModel = new FlowEditorViewModel(
            [factory],
            new FlowRunner([factory]),
            new ProjectFileService());

        var sourceNode = viewModel.PlacePaletteNode(Assert.Single(viewModel.Palette.FilteredNodes), 0, 0);
        var targetNode = viewModel.PlacePaletteNode(Assert.Single(viewModel.Palette.FilteredNodes), 220, 0);
        var outputPort = Assert.Single(sourceNode.Outputs);

        viewModel.StartPendingConnection(outputPort);
        Assert.True(viewModel.ShowPreviewConnection);

        viewModel.CancelPendingConnection();

        Assert.False(viewModel.ShowPreviewConnection);
        Assert.False(viewModel.IsConnectionAnimationActive);
        Assert.Equal("Click an output port to start connecting.", viewModel.ConnectionHintText);
    }

    [Fact]
    public void ConnectionFlow_ReleaseOutsidePort_CancelsPreview()
    {
        var factory = new TestNodeFactory();
        var viewModel = new FlowEditorViewModel(
            [factory],
            new FlowRunner([factory]),
            new ProjectFileService());

        var sourceNode = viewModel.PlacePaletteNode(Assert.Single(viewModel.Palette.FilteredNodes), 0, 0);
        var outputPort = Assert.Single(sourceNode.Outputs);

        viewModel.StartPendingConnection(outputPort);
        Assert.True(viewModel.ShowPreviewConnection);

        viewModel.HandlePortConnection(outputPort, null);

        Assert.False(viewModel.ShowPreviewConnection);
        Assert.False(viewModel.IsConnectionAnimationActive);
        Assert.Equal("Click an output port to start connecting.", viewModel.ConnectionHintText);
    }

    [Fact]
    public void ConnectionFlow_ActivatesAnimationHintAndResetsAfterCompletion()
    {
        var factory = new TestNodeFactory();
        var viewModel = new FlowEditorViewModel(
            [factory],
            new FlowRunner([factory]),
            new ProjectFileService());

        var paletteNode = Assert.Single(viewModel.Palette.FilteredNodes);
        var sourceNode = viewModel.PlacePaletteNode(paletteNode, 0, 0);
        var targetNode = viewModel.PlacePaletteNode(paletteNode, 220, 0);

        var outputPort = Assert.Single(sourceNode.Outputs);
        var inputPort = Assert.Single(targetNode.Inputs);

        viewModel.StartPendingConnection(outputPort);
        Assert.True(viewModel.IsConnectionAnimationActive);
        Assert.Equal($"Connecting from {sourceNode.DisplayName}.{outputPort.DisplayName}. Hold and release on another port to finish.", viewModel.ConnectionHintText);

        viewModel.HandlePortConnection(outputPort, inputPort);
        Assert.False(viewModel.IsConnectionAnimationActive);
        Assert.Contains($"Connected {sourceNode.DisplayName}.{outputPort.DisplayName}", viewModel.ConnectionHintText);
    }

    [Fact]
    public void ConnectionFlow_StartFromInputPort_DoesNotShowPreview()
    {
        var factory = new TestNodeFactory();
        var viewModel = new FlowEditorViewModel(
            [factory],
            new FlowRunner([factory]),
            new ProjectFileService());

        var node = viewModel.PlacePaletteNode(Assert.Single(viewModel.Palette.FilteredNodes), 0, 0);
        var inputPort = Assert.Single(node.Inputs);

        viewModel.StartPendingConnection(inputPort);

        Assert.False(viewModel.ShowPreviewConnection);
        Assert.False(viewModel.IsConnectionAnimationActive);
    }

    [Fact]
    public void ConnectionFlow_DeleteConnectionCommand_RemovesConnection()
    {
        var factory = new TestNodeFactory();
        var viewModel = new FlowEditorViewModel(
            [factory],
            new FlowRunner([factory]),
            new ProjectFileService());

        var paletteNode = Assert.Single(viewModel.Palette.FilteredNodes);
        var sourceNode = viewModel.PlacePaletteNode(paletteNode, 0, 0);
        var targetNode = viewModel.PlacePaletteNode(paletteNode, 220, 0);

        var outputPort = Assert.Single(sourceNode.Outputs);
        var inputPort = Assert.Single(targetNode.Inputs);

        viewModel.StartPendingConnection(outputPort);
        viewModel.HandlePortConnection(outputPort, inputPort);

        var connection = Assert.Single(viewModel.Connections);
        viewModel.DeleteConnectionCommand.Execute(connection);

        Assert.Empty(viewModel.Connections);
    }

    [Fact]
    public void ConnectionFlow_UpdateConnectionTag_StoresTagOnConnection()
    {
        var factory = new TestNodeFactory();
        var viewModel = new FlowEditorViewModel(
            [factory],
            new FlowRunner([factory]),
            new ProjectFileService());

        var paletteNode = Assert.Single(viewModel.Palette.FilteredNodes);
        var sourceNode = viewModel.PlacePaletteNode(paletteNode, 0, 0);
        var targetNode = viewModel.PlacePaletteNode(paletteNode, 220, 0);

        var outputPort = Assert.Single(sourceNode.Outputs);
        var inputPort = Assert.Single(targetNode.Inputs);

        viewModel.StartPendingConnection(outputPort);
        viewModel.HandlePortConnection(outputPort, inputPort);

        var connection = Assert.Single(viewModel.Connections);
        viewModel.UpdateConnectionTag(connection, "CH1");

        var updated = Assert.Single(viewModel.Connections).Connection;
        Assert.Equal("CH1", updated.Tag);
    }

    [Fact]
    public void ConnectionFlow_CreatedConnectionUsesSourcePortDataSchema()
    {
        var sourceDefinition = new NodeDefinition(
            "vector-source",
            "Vector Source",
            "Test",
            "0.1.0",
            [new PortDefinition(
                "out",
                "Vector Out",
                PortDirection.Output,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.NumericVector1D)],
            NodeType.Input);
        var sinkDefinition = new NodeDefinition(
            "vector-sink",
            "Vector Sink",
            "Test",
            "0.1.0",
            [new PortDefinition(
                "in",
                "Vector In",
                PortDirection.Input,
                PortDataKind.FastStream,
                IsRequired: false,
                DataSchema: PortDataSchema.NumericVector1D)],
            NodeType.Output);
        var sourceFactory = new StaticDefinitionNodeFactory(sourceDefinition);
        var sinkFactory = new StaticDefinitionNodeFactory(sinkDefinition);
        var viewModel = new FlowEditorViewModel(
            [sourceFactory, sinkFactory],
            new FlowRunner([sourceFactory, sinkFactory]),
            new ProjectFileService());

        var sourceNode = viewModel.PlacePaletteNode(Assert.Single(viewModel.Palette.FilteredNodes, node => node.TypeId == sourceDefinition.TypeId), 0, 0);
        var targetNode = viewModel.PlacePaletteNode(Assert.Single(viewModel.Palette.FilteredNodes, node => node.TypeId == sinkDefinition.TypeId), 220, 0);
        var outputPort = Assert.Single(sourceNode.Outputs);
        var inputPort = Assert.Single(targetNode.Inputs);

        viewModel.HandlePortConnection(outputPort, inputPort);

        var connection = Assert.Single(viewModel.Connections).Connection;
        Assert.Equal(PortDataSchema.NumericVector1D, connection.DataSchema);
    }

    [Fact]
    public void DeleteNodeCommand_RemovesSpecifiedNode()
    {
        var factory = new TestNodeFactory();
        var viewModel = new FlowEditorViewModel(
            [factory],
            new FlowRunner([factory]),
            new ProjectFileService());

        var node = viewModel.PlacePaletteNode(Assert.Single(viewModel.Palette.FilteredNodes), 120, 120);
        Assert.Single(viewModel.Nodes);

        viewModel.DeleteNodeCommand.Execute(node);

        Assert.Empty(viewModel.Nodes);
    }

    [Fact]
    public void PlacePaletteNode_AddsNodeAtCanvasPosition()
    {
        var factory = new TestNodeFactory();
        var viewModel = new FlowEditorViewModel(
            [factory],
            new FlowRunner([factory]),
            new ProjectFileService());
        var paletteNode = Assert.Single(viewModel.Palette.FilteredNodes);

        viewModel.PlacePaletteNode(paletteNode, 320, 180);

        var node = Assert.Single(viewModel.Nodes);
        Assert.Equal("test.block", node.TypeId);
        Assert.Equal(320, node.X);
        Assert.Equal(180, node.Y);
        Assert.Same(node, viewModel.SelectedNode);
    }

    [Fact]
    public void PlacePaletteNode_AddsDashboardWidgetWhenDefaultIsEnabled()
    {
        IReadOnlyList<DashboardDocument> dashboards = [];
        var factory = new TestNodeFactory(
            dashboardWidget: new DashboardWidgetDefinition(IsVisibleByDefault: true, GridWidth: 1, GridHeight: 2));
        var viewModel = new FlowEditorViewModel(
            [factory],
            new FlowRunner([factory]),
            new ProjectFileService(),
            () => dashboards,
            documents => dashboards = documents);

        viewModel.PlacePaletteNode(Assert.Single(viewModel.Palette.FilteredNodes), 320, 180);

        var dashboard = Assert.Single(dashboards);
        var widget = Assert.Single(dashboard.Widgets);
        Assert.Equal("dataprocesses.dashboard.node-block", widget.WidgetType);
        Assert.Equal(0, widget.GridX);
        Assert.Equal(0, widget.GridY);
        Assert.Equal(1, widget.GridWidth);
        Assert.Equal(2, widget.GridHeight);
    }

    [Fact]
    public void PlacePaletteNode_UpdatesDashboardViewModelWidgetsThroughMainViewModel()
    {
        var mainViewModel = new MainViewModel();
        var testSignal = mainViewModel.FlowEditor.Palette.FilteredNodes.Single(node => node.TypeId == "dataprocesses.test-signal");

        mainViewModel.FlowEditor.PlacePaletteNode(testSignal, 320, 180);

        var widget = Assert.Single(mainViewModel.Dashboard.Widgets);
        Assert.Equal("dataprocesses.dashboard.node-block", widget.WidgetType);
        Assert.Equal(2, widget.GridWidth);
        Assert.Equal(1, widget.GridHeight);
        Assert.Equal("TestSignal(TS)ブロック", widget.Title);
    }

    [Fact]
    public void PlacePaletteNode_TriggerNodeCreatesButtonDashboardWidget()
    {
        var mainViewModel = new MainViewModel();
        var trigger = mainViewModel.FlowEditor.Palette.FilteredNodes.Single(node => node.TypeId == TriggerBlock.TypeId);

        mainViewModel.FlowEditor.PlacePaletteNode(trigger, 240, 220);

        var widget = Assert.Single(mainViewModel.Dashboard.Widgets);
        Assert.Equal(1, widget.GridWidth);
        Assert.Equal(1, widget.GridHeight);
        Assert.True(widget.IsTriggerButtonContent);
        Assert.Equal("Trigger", widget.Content);
    }

    [Fact]
    public void PlacePaletteNode_TestSignalNodeCreatesOnOffButtonDashboardWidget()
    {
        var mainViewModel = new MainViewModel();
        var testSignal = mainViewModel.FlowEditor.Palette.FilteredNodes.Single(node => node.TypeId == TestSignalBlock.TypeId);

        mainViewModel.FlowEditor.PlacePaletteNode(testSignal, 260, 220);

        var widget = Assert.Single(mainViewModel.Dashboard.Widgets);
        Assert.True(widget.IsTriggerButtonContent);
        Assert.Equal("ON", widget.Content);
    }

    [Fact]
    public void PlacePaletteNode_StreamOutputTSNodeIsShownOnDashboardByDefault()
    {
        var mainViewModel = new MainViewModel();
        var streamOutput = mainViewModel.FlowEditor.Palette.FilteredNodes.Single(node => node.TypeId == StremOutputTSBlock.TypeId);

        mainViewModel.FlowEditor.PlacePaletteNode(streamOutput, 260, 220);

        var widget = Assert.Single(mainViewModel.Dashboard.Widgets);
        Assert.Equal("StremOutputTS", widget.Title);
        Assert.Equal(3, widget.GridWidth);
        Assert.Equal(3, widget.GridHeight);
    }

    [Fact]
    public void TriggerNodeById_TogglesTestSignalSettingsAndDashboardLabel()
    {
        var mainViewModel = new MainViewModel();
        var testSignalPaletteNode = mainViewModel.FlowEditor.Palette.FilteredNodes.Single(node => node.TypeId == TestSignalBlock.TypeId);

        var node = mainViewModel.FlowEditor.PlacePaletteNode(testSignalPaletteNode, 260, 220);
        var widget = Assert.Single(mainViewModel.Dashboard.Widgets);
        Assert.Equal("ON", widget.Content);

        mainViewModel.FlowEditor.TriggerNodeById(node.Id);

        using var settings = JsonDocument.Parse(node.SettingsJson);
        Assert.False(settings.RootElement.GetProperty("isEnabled").GetBoolean());
        var refreshedWidget = Assert.Single(mainViewModel.Dashboard.Widgets);
        Assert.Equal("OFF", refreshedWidget.Content);
    }

    [Fact]
    public void PlacePaletteNode_PlacesDashboardWidgetWithoutOverlap()
    {
        IReadOnlyList<DashboardDocument> dashboards =
        [
            new DashboardDocument(
                Guid.NewGuid(),
                "Monitor",
                [new DashboardWidget(Guid.NewGuid(), "existing", 0, 0, 1, 2)]),
        ];
        var factory = new TestNodeFactory(
            dashboardWidget: new DashboardWidgetDefinition(IsVisibleByDefault: true, GridWidth: 1, GridHeight: 2));
        var viewModel = new FlowEditorViewModel(
            [factory],
            new FlowRunner([factory]),
            new ProjectFileService(),
            () => dashboards,
            documents => dashboards = documents);

        viewModel.PlacePaletteNode(Assert.Single(viewModel.Palette.FilteredNodes), 320, 180);

        var widget = Assert.Single(Assert.Single(dashboards).Widgets, widget => widget.WidgetType == "dataprocesses.dashboard.node-block");
        Assert.Equal(1, widget.GridX);
        Assert.Equal(0, widget.GridY);
    }

    [Fact]
    public void CanvasNodeSettings_UpdateDashboardWidgetWithoutOverwritingDashboardSize()
    {
        IReadOnlyList<DashboardDocument> dashboards = [];
        var factory = new TestNodeFactory(
            dashboardWidget: new DashboardWidgetDefinition(IsVisibleByDefault: true, GridWidth: 1, GridHeight: 2));
        var viewModel = new FlowEditorViewModel(
            [factory],
            new FlowRunner([factory]),
            new ProjectFileService(),
            () => dashboards,
            documents => dashboards = documents);

        var node = viewModel.PlacePaletteNode(Assert.Single(viewModel.Palette.FilteredNodes), 320, 180);
        var firstWidget = Assert.Single(Assert.Single(dashboards).Widgets);
        dashboards = [Assert.Single(dashboards) with
        {
            Widgets = [firstWidget with { GridWidth = 4, GridHeight = 3 }],
        }];

        node.Name = "Renamed source";
        node.IsEnabled = false;

        var widget = Assert.Single(Assert.Single(dashboards).Widgets);
        Assert.Equal(4, widget.GridWidth);
        Assert.Equal(3, widget.GridHeight);
        using var document = JsonDocument.Parse(widget.SettingsJson);
        Assert.Equal("Renamed source", document.RootElement.GetProperty("title").GetString());
        Assert.False(document.RootElement.GetProperty("isSourceNodeEnabled").GetBoolean());
    }

    [Fact]
    public void CanvasNodeViewModel_UpdatesTestSignalSettingsJsonFromTypedInspectorProperties()
    {
        var factory = new TestNodeFactory("dataprocesses.test-signal", "Test Signal", NodeType.Input);
        var node = new CanvasNodeViewModel(
            new NodeInstance("node-1", factory.Definition.TypeId, 0, 0, "{}"),
            factory.Definition);

        node.TestSignalWaveType = "square";
        node.TestSignalFrequencyHertz = 25.5;
        node.TestSignalSamplePeriodMilliseconds = 0.5;

        using var document = JsonDocument.Parse(node.SettingsJson);
        Assert.Equal("square", document.RootElement.GetProperty("waveType").GetString());
        Assert.Equal(25.5, document.RootElement.GetProperty("frequency").GetDouble());
        Assert.Equal(0.5, document.RootElement.GetProperty("samplePeriodMillis").GetDouble());
    }

    [Fact]
    public void CanvasNodeViewModel_BuildRuntimeSettingsJson_InjectsTriggerSessionAndManualNonce()
    {
        var node = new CanvasNodeViewModel(
            new NodeInstance("node-1", TriggerBlock.TypeId, 0, 0, "{}"),
            TriggerBlock.Definition);

        node.RequestTriggerNow();
        node.RequestTriggerNow();

        var runtimeSettingsJson = node.BuildRuntimeSettingsJson(42);

        Assert.Equal("{}", node.SettingsJson);
        using var document = JsonDocument.Parse(runtimeSettingsJson);
        Assert.Equal(42, document.RootElement.GetProperty("executionSessionId").GetInt64());
        Assert.Equal(2, document.RootElement.GetProperty("manualTriggerNonce").GetInt64());
    }

    [Fact]
    public void CsvInput_OutputCountChange_UpdatesVisiblePortsAndDropsHiddenConnections()
    {
        INodeFactory[] factories =
        [
            new CsvInputNodeFactory(),
            new StremOutputTSNodeFactory(),
        ];

        var viewModel = new FlowEditorViewModel(
            factories,
            new FlowRunner(factories),
            new ProjectFileService());

        var csvInputPaletteNode = viewModel.Palette.FilteredNodes.Single(node => node.TypeId == CsvInputBlock.TypeId);
        var streamOutputPaletteNode = viewModel.Palette.FilteredNodes.Single(node => node.TypeId == StremOutputTSBlock.TypeId);

        var csvInputNode = viewModel.PlacePaletteNode(csvInputPaletteNode, 100, 100);
        var streamOutputNode1 = viewModel.PlacePaletteNode(streamOutputPaletteNode, 380, 80);
        var streamOutputNode2 = viewModel.PlacePaletteNode(streamOutputPaletteNode, 380, 200);

        var stream1 = csvInputNode.Outputs.Single(port => port.Id == CsvInputBlock.GetStreamPortId(1));
        var stream2 = csvInputNode.Outputs.Single(port => port.Id == CsvInputBlock.GetStreamPortId(2));
        var target1 = Assert.Single(streamOutputNode1.Inputs);
        var target2 = Assert.Single(streamOutputNode2.Inputs);

        viewModel.StartPendingConnection(stream1);
        viewModel.HandlePortConnection(stream1, target1);
        viewModel.StartPendingConnection(stream2);
        viewModel.HandlePortConnection(stream2, target2);

        Assert.Equal(2, viewModel.Connections.Count);

        csvInputNode.CsvInputOutputCount = 1;

        var visibleOutput = Assert.Single(csvInputNode.Outputs);
        Assert.Equal(CsvInputBlock.GetStreamPortId(1), visibleOutput.Id);
        var remainingConnection = Assert.Single(viewModel.Connections);
        Assert.Equal(CsvInputBlock.GetStreamPortId(1), remainingConnection.Connection.SourcePortId);
    }

    [Fact]
    public async Task RunAsync_UpdatesDashboardWidgetContentFromLastFastStreamOutput()
    {
        IReadOnlyList<DashboardDocument> dashboards = [];
        var frame = new FastStreamFrame(
            StartTimeUnixNanoseconds: 0,
            SamplePeriodNanoseconds: 1_000_000,
            ChannelNames: ["signal"],
            Samples: [new double[] { 0, 0.5, 1.0 }.AsMemory()],
            SequenceNumber: 0);
        var factory = new TestNodeFactory(
            dashboardWidget: new DashboardWidgetDefinition(IsVisibleByDefault: true, GridWidth: 1, GridHeight: 2),
            packetToEmit: frame);
        var viewModel = new FlowEditorViewModel(
            [factory],
            new FlowRunner([factory]),
            new ProjectFileService(),
            () => dashboards,
            documents => dashboards = documents);

        viewModel.PlacePaletteNode(Assert.Single(viewModel.Palette.FilteredNodes), 320, 180);

        var runTask = viewModel.StartExecutionAsync(debugMode: false);
        try
        {
            await WaitForDashboardContentAsync(() => dashboards, "0.5");
        }
        finally
        {
            viewModel.StopExecution();
            await runTask;
        }

        var widget = Assert.Single(Assert.Single(dashboards).Widgets);
        using var document = JsonDocument.Parse(widget.SettingsJson);
        Assert.Equal("text", document.RootElement.GetProperty("contentKind").GetString());
        var content = document.RootElement.GetProperty("content").GetString();
        var displayText = document.RootElement.GetProperty("displayData").GetProperty("text").GetString();
        Assert.Contains("millis,value", content, StringComparison.Ordinal);
        Assert.Contains("0.5", content, StringComparison.Ordinal);
        Assert.Equal(content, displayText);
    }

    [Fact]
    public async Task RunAsync_DoesNotRenderFastStreamTextIntoTestSignalDashboardWidget()
    {
        IReadOnlyList<DashboardDocument> dashboards = [];
        INodeFactory[] factories =
        [
            new TestSignalNodeFactory(),
            new StremOutputTSNodeFactory(),
        ];
        var viewModel = new FlowEditorViewModel(
            factories,
            new FlowRunner(factories),
            new ProjectFileService(),
            () => dashboards,
            documents => dashboards = documents);

        var testSignalPaletteNode = viewModel.Palette.FilteredNodes.Single(node => node.TypeId == TestSignalBlock.TypeId);
        var streamOutputPaletteNode = viewModel.Palette.FilteredNodes.Single(node => node.TypeId == StremOutputTSBlock.TypeId);

        var testSignalNode = viewModel.PlacePaletteNode(testSignalPaletteNode, 120, 120);
        var streamOutputNode = viewModel.PlacePaletteNode(streamOutputPaletteNode, 440, 120);

        streamOutputNode.ShowOnDashboard = true;

        var sourcePort = testSignalNode.Outputs.Single(port => port.Id == TestSignalBlock.StreamOutputPortId);
        var targetPort = streamOutputNode.Inputs.Single(port => port.Id == StremOutputTSBlock.InputPortId);
        viewModel.StartPendingConnection(sourcePort);
        viewModel.HandlePortConnection(sourcePort, targetPort);

        var runTask = viewModel.StartExecutionAsync(debugMode: false);
        try
        {
            await WaitForDashboardContentAsync(() => dashboards, "millis,value");
        }
        finally
        {
            viewModel.StopExecution();
            await runTask;
        }

        var dashboard = Assert.Single(dashboards);
        var testSignalWidget = Assert.Single(dashboard.Widgets, widget => widget.SourcePortId == testSignalNode.Id);
        var streamOutputWidget = Assert.Single(dashboard.Widgets, widget => widget.SourcePortId == streamOutputNode.Id);

        using var testSignalSettings = JsonDocument.Parse(testSignalWidget.SettingsJson);
        Assert.Equal("button-trigger", testSignalSettings.RootElement.GetProperty("contentKind").GetString());
        Assert.Equal("ON", testSignalSettings.RootElement.GetProperty("content").GetString());

        using var streamOutputSettings = JsonDocument.Parse(streamOutputWidget.SettingsJson);
        Assert.Equal("text", streamOutputSettings.RootElement.GetProperty("contentKind").GetString());
        var streamContent = streamOutputSettings.RootElement.GetProperty("content").GetString();
        Assert.Contains("millis,value", streamContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_StreamOutputDashboardContent_DoesNotAdvanceWhenAutoScrollIsOff()
    {
        IReadOnlyList<DashboardDocument> dashboards = [];
        INodeFactory[] factories =
        [
            new TestSignalNodeFactory(),
            new StremOutputTSNodeFactory(),
        ];
        var viewModel = new FlowEditorViewModel(
            factories,
            new FlowRunner(factories),
            new ProjectFileService(),
            () => dashboards,
            documents => dashboards = documents);

        var testSignalPaletteNode = viewModel.Palette.FilteredNodes.Single(node => node.TypeId == TestSignalBlock.TypeId);
        var streamOutputPaletteNode = viewModel.Palette.FilteredNodes.Single(node => node.TypeId == StremOutputTSBlock.TypeId);

        var testSignalNode = viewModel.PlacePaletteNode(testSignalPaletteNode, 120, 120);
        var streamOutputNode = viewModel.PlacePaletteNode(streamOutputPaletteNode, 440, 120);
        streamOutputNode.ShowOnDashboard = true;

        var sourcePort = testSignalNode.Outputs.Single(port => port.Id == TestSignalBlock.StreamOutputPortId);
        var targetPort = streamOutputNode.Inputs.Single(port => port.Id == StremOutputTSBlock.InputPortId);
        viewModel.StartPendingConnection(sourcePort);
        viewModel.HandlePortConnection(sourcePort, targetPort);

        var initialRunTask = viewModel.StartExecutionAsync(debugMode: false);
        try
        {
            await WaitForDashboardContentAsync(() => dashboards, "millis,value");
        }
        finally
        {
            viewModel.StopExecution();
            await initialRunTask;
        }

        var dashboard = Assert.Single(dashboards);
        var streamWidget = Assert.Single(dashboard.Widgets, widget => widget.SourcePortId == streamOutputNode.Id);
        var frozenContent = "frozen-content-marker";

        using var streamSettingsBefore = JsonDocument.Parse(streamWidget.SettingsJson);
        Assert.True(streamSettingsBefore.RootElement.GetProperty("supportsAutoScroll").GetBoolean());

        var updatedSettings = JsonSerializer.Serialize(new
        {
            title = streamSettingsBefore.RootElement.GetProperty("title").GetString(),
            contentKind = streamSettingsBefore.RootElement.GetProperty("contentKind").GetString(),
            content = frozenContent,
            displayData = new
            {
                text = frozenContent,
            },
            isTextWrapEnabled = streamSettingsBefore.RootElement.GetProperty("isTextWrapEnabled").GetBoolean(),
            isSourceNodeEnabled = streamSettingsBefore.RootElement.GetProperty("isSourceNodeEnabled").GetBoolean(),
            supportsAutoScroll = true,
            isAutoScrollEnabled = false,
        });

        dashboards =
        [
            dashboard with
            {
                Widgets = dashboard.Widgets
                    .Select(widget => widget.Id == streamWidget.Id
                        ? widget with { SettingsJson = updatedSettings }
                        : widget)
                    .ToArray(),
            },
        ];

        var secondRunTask = viewModel.StartExecutionAsync(debugMode: false);
        try
        {
            await Task.Delay(400);
        }
        finally
        {
            viewModel.StopExecution();
            await secondRunTask;
        }

        var streamWidgetAfterRun = Assert
            .Single(Assert.Single(dashboards).Widgets, widget => widget.SourcePortId == streamOutputNode.Id);
        using var streamSettingsAfter = JsonDocument.Parse(streamWidgetAfterRun.SettingsJson);
        Assert.False(streamSettingsAfter.RootElement.GetProperty("isAutoScrollEnabled").GetBoolean());
        Assert.Equal(frozenContent, streamSettingsAfter.RootElement.GetProperty("content").GetString());
    }

    [Fact]
    public async Task RunAsync_AppendsTimestampedPayloadOutputContentToDashboard()
    {
        IReadOnlyList<DashboardDocument> dashboards = [];
        INodeFactory[] factories =
        [
            new TestSignalNodeFactory(),
            new PayloadOutputNodeFactory(),
        ];
        var viewModel = new FlowEditorViewModel(
            factories,
            new FlowRunner(factories),
            new ProjectFileService(),
            () => dashboards,
            documents => dashboards = documents);

        var sourcePaletteNode = viewModel.Palette.FilteredNodes.Single(node => node.TypeId == TestSignalBlock.TypeId);
        var payloadOutputPaletteNode = viewModel.Palette.FilteredNodes.Single(node => node.TypeId == PayloadOutputBlock.TypeId);

        var sourceNode = viewModel.PlacePaletteNode(sourcePaletteNode, 100, 100);
        var payloadOutputNode = viewModel.PlacePaletteNode(payloadOutputPaletteNode, 380, 100);

        var payloadSourcePort = sourceNode.Outputs.Single(port => port.Id == TestSignalBlock.PayloadOutputPortId);
        var payloadTargetPort = payloadOutputNode.Inputs.Single(port => port.Id == PayloadOutputBlock.InputPortId);

        viewModel.StartPendingConnection(payloadSourcePort);
        viewModel.HandlePortConnection(payloadSourcePort, payloadTargetPort);

        var runTask = viewModel.StartExecutionAsync(debugMode: false);
        try
        {
            await WaitForDashboardContentAsync(() => dashboards, "topic=dataprocesses.test-signal.status");
        }
        finally
        {
            viewModel.StopExecution();
            await runTask;
        }

        var dashboard = Assert.Single(dashboards);
        var widget = Assert.Single(dashboard.Widgets, candidate => candidate.SourcePortId == payloadOutputNode.Id);
        Assert.Equal(3, widget.GridWidth);
        Assert.Equal(3, widget.GridHeight);

        using var document = JsonDocument.Parse(widget.SettingsJson);
        var content = document.RootElement.GetProperty("content").GetString() ?? string.Empty;
        Assert.Contains("topic=dataprocesses.test-signal.status", content, StringComparison.Ordinal);
        Assert.Contains("payload=", content, StringComparison.Ordinal);
        Assert.StartsWith("[", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StartExecutionAsync_RefreshesDashboardWidgetContentUntilStopped()
    {
        IReadOnlyList<DashboardDocument> dashboards = [];
        var frameIndex = -1;
        var factory = new TestNodeFactory(
            dashboardWidget: new DashboardWidgetDefinition(IsVisibleByDefault: true, GridWidth: 1, GridHeight: 2),
            packetFactory: () =>
            {
                var currentFrame = Interlocked.Increment(ref frameIndex);
                return new FastStreamFrame(
                    StartTimeUnixNanoseconds: currentFrame * 1_000_000_000L,
                    SamplePeriodNanoseconds: 1_000_000,
                    ChannelNames: ["signal"],
                    Samples: [new double[] { 0, 0.5 }.AsMemory()],
                    SequenceNumber: currentFrame);
            });
        var viewModel = new FlowEditorViewModel(
            [factory],
            new FlowRunner([factory]),
            new ProjectFileService(),
            () => dashboards,
            documents => dashboards = documents);

        viewModel.PlacePaletteNode(Assert.Single(viewModel.Palette.FilteredNodes), 320, 180);

        var runTask = viewModel.StartExecutionAsync(debugMode: false);
        try
        {
            await WaitForDashboardContentAsync(() => dashboards, "1000,0", timeoutMilliseconds: 3_000);
        }
        finally
        {
            viewModel.StopExecution();
            await runTask;
        }

        Assert.True(frameIndex >= 1);
    }

    [Fact]
    public void Palette_GroupsNodesByNodeType()
    {
        var factories = new INodeFactory[]
        {
            new TestNodeFactory("debug.block", "Debug Block", NodeType.Debug),
            new TestNodeFactory("input.block", "Input Block", NodeType.Input),
            new TestNodeFactory("process.block", "Process Block", NodeType.BasicProcess),
            new TestNodeFactory("output.block", "Output Block", NodeType.Output),
        };
        var viewModel = new FlowEditorViewModel(
            factories,
            new FlowRunner(factories),
            new ProjectFileService());

        Assert.Equal(["DEBUG", "INPUT", "Basic Process", "OUTPUT"], viewModel.Palette.Groups.Select(static group => group.DisplayName));
        Assert.All(viewModel.Palette.Groups, group => Assert.Single(group.Nodes));
    }

    [Fact]
    public void Palette_SearchMatchesNodeTypeDisplayName()
    {
        var factories = new INodeFactory[]
        {
            new TestNodeFactory("input.block", "Input Block", NodeType.Input),
            new TestNodeFactory("process.block", "Process Block", NodeType.BasicProcess),
            new TestNodeFactory("output.block", "Output Block", NodeType.Output),
        };
        var viewModel = new FlowEditorViewModel(
            factories,
            new FlowRunner(factories),
            new ProjectFileService());

        viewModel.Palette.SearchText = "Basic";

        var group = Assert.Single(viewModel.Palette.Groups);
        Assert.Equal("Basic Process", group.DisplayName);
        Assert.Equal("process.block", Assert.Single(group.Nodes).TypeId);
    }

    [Fact]
    public void NodeViewModels_ResolveIconPathsFromDefinitionPath()
    {
        var iconPath = GetRepositoryFilePath("src", "DataProcesses.Nodes.BuiltIn", "Blocks", "TestSignalTS", "icon.png");
        var factory = new TestNodeFactory(iconPath: iconPath);

        var paletteNode = new PaletteNodeViewModel(factory);
        var canvasNode = new CanvasNodeViewModel(
            new NodeInstance("node-1", factory.Definition.TypeId, 0, 0, "{}"),
            factory.Definition);

        Assert.Equal(iconPath, paletteNode.IconPath);
        Assert.Equal(iconPath, canvasNode.IconPath);
    }

    private static string GetRepositoryFilePath(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine([directory.FullName, .. segments]);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return Path.Combine(segments);
    }

    private static async Task WaitForDashboardContentAsync(
        Func<IReadOnlyList<DashboardDocument>> getDashboards,
        string expectedContent,
        int timeoutMilliseconds = 2_000)
    {
        using var timeout = new CancellationTokenSource(timeoutMilliseconds);
        while (true)
        {
            var content = getDashboards()
                .SelectMany(static dashboard => dashboard.Widgets)
                .Select(static widget => widget.SettingsJson)
                .FirstOrDefault(settingsJson => settingsJson.Contains(expectedContent, StringComparison.Ordinal));
            if (content is not null)
            {
                return;
            }

            try
            {
                await Task.Delay(25, timeout.Token).ConfigureAwait(true);
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                var snapshot = string.Join(
                    " | ",
                    getDashboards()
                        .SelectMany(static dashboard => dashboard.Widgets)
                        .Select(static widget => widget.SettingsJson));
                throw new TimeoutException($"Dashboard content did not contain '{expectedContent}'. Snapshot: {snapshot}");
            }
        }
    }

    [Fact]
    public void Palette_UsesDefinitionTitleAndSubtitle()
    {
        var factory = new TestNodeFactory(
            "test.signal",
            "Legacy Name",
            NodeType.Input,
            title: "TestSignal(TS)繝悶Ο繝・け",
            subtitle: "TS");
        var viewModel = new FlowEditorViewModel(
            [factory],
            new FlowRunner([factory]),
            new ProjectFileService());

        var paletteNode = Assert.Single(viewModel.Palette.FilteredNodes);

        Assert.Equal("TestSignal(TS)繝悶Ο繝・け", paletteNode.Title);
        Assert.Equal("TS", paletteNode.Subtitle);
        Assert.Equal("TestSignal(TS)繝悶Ο繝・け", paletteNode.DisplayName);
    }

    [Fact]
    public void Palette_SearchMatchesSubtitle()
    {
        var factory = new TestNodeFactory(
            "test.signal",
            "Legacy Name",
            NodeType.Input,
            title: "TestSignal(TS)繝悶Ο繝・け",
            subtitle: "TS");
        var viewModel = new FlowEditorViewModel(
            [factory],
            new FlowRunner([factory]),
            new ProjectFileService());

        viewModel.Palette.SearchText = "TS";

        Assert.Equal("test.signal", Assert.Single(viewModel.Palette.FilteredNodes).TypeId);
    }

    [Fact]
    public void Palette_InputDataTypeFilter_MatchesJsonInputNodes()
    {
        var factories = new INodeFactory[]
        {
            new StaticDefinitionNodeFactory(new NodeDefinition(
                "payload.input",
                "Payload Input",
                "Test",
                "0.1.0",
                [new PortDefinition("in", "Input", PortDirection.Input, PortDataKind.JsonMessage, DataSchema: PortDataSchema.JsonEnvelope)])),
            new StaticDefinitionNodeFactory(new NodeDefinition(
                "signal.input",
                "Signal Input",
                "Test",
                "0.1.0",
                [new PortDefinition("in", "Input", PortDirection.Input, PortDataKind.FastStream, DataSchema: PortDataSchema.TimeSeries1D)])),
        };
        var viewModel = new FlowEditorViewModel(
            factories,
            new FlowRunner(factories),
            new ProjectFileService());

        viewModel.Palette.SelectedInputDataType = "JSON";

        Assert.Equal("payload.input", Assert.Single(viewModel.Palette.FilteredNodes).TypeId);
    }

    [Fact]
    public void Palette_OutputDataTypeFilter_MatchesVectorOutputNodes()
    {
        var factories = new INodeFactory[]
        {
            new StaticDefinitionNodeFactory(new NodeDefinition(
                "vector.output",
                "Vector Output",
                "Test",
                "0.1.0",
                [new PortDefinition("out", "Output", PortDirection.Output, PortDataKind.FastStream, DataSchema: PortDataSchema.NumericVector1D)])),
            new StaticDefinitionNodeFactory(new NodeDefinition(
                "image.output",
                "Image Output",
                "Test",
                "0.1.0",
                [new PortDefinition("out", "Output", PortDirection.Output, PortDataKind.FastStream, DataSchema: PortDataSchema.Image2D)])),
        };
        var viewModel = new FlowEditorViewModel(
            factories,
            new FlowRunner(factories),
            new ProjectFileService());

        viewModel.Palette.SelectedOutputDataType = "VEC";

        Assert.Equal("vector.output", Assert.Single(viewModel.Palette.FilteredNodes).TypeId);
    }

    [Fact]
    public void Palette_SearchAndPortFilter_CombineWithAndCondition()
    {
        var factories = new INodeFactory[]
        {
            new StaticDefinitionNodeFactory(new NodeDefinition(
                "vector.process",
                "Vector Processor",
                "Test",
                "0.1.0",
                [new PortDefinition("out", "Output", PortDirection.Output, PortDataKind.FastStream, DataSchema: PortDataSchema.NumericVector1D)])),
            new StaticDefinitionNodeFactory(new NodeDefinition(
                "image.process",
                "Image Processor",
                "Test",
                "0.1.0",
                [new PortDefinition("out", "Output", PortDirection.Output, PortDataKind.FastStream, DataSchema: PortDataSchema.Image2D)])),
        };
        var viewModel = new FlowEditorViewModel(
            factories,
            new FlowRunner(factories),
            new ProjectFileService());

        viewModel.Palette.SearchText = "Processor";
        viewModel.Palette.SelectedOutputDataType = "IMG";

        Assert.Equal("image.process", Assert.Single(viewModel.Palette.FilteredNodes).TypeId);
    }

    [Fact]
    public void CanvasNodeViewModel_RoundTripsCommonSettings()
    {
        var definition = new NodeDefinition(
            "test.block",
            "Test Block",
            "Test",
            "0.1.0",
            []);
        var node = new CanvasNodeViewModel(
            new NodeInstance("node-1", "test.block", 10, 20, "{}", "Custom Name", "Notes", IsEnabled: false),
            definition);

        Assert.Equal("Custom Name", node.DisplayName);
        node.Name = "Renamed";
        node.Description = "Updated notes";
        node.IsEnabled = true;

        var instance = node.ToNodeInstance();

        Assert.Equal("Renamed", instance.Name);
        Assert.Equal("Updated notes", instance.Description);
        Assert.True(instance.IsEnabled);
    }

    [Fact]
    public void CanvasNodeViewModel_UpdatesDashboardTextWrapSettingInSettingsJson()
    {
        var definition = new NodeDefinition(
            "test.block",
            "Test Block",
            "Test",
            "0.1.0",
            []);
        var node = new CanvasNodeViewModel(
            new NodeInstance("node-1", "test.block", 10, 20, "{}"),
            definition);

        Assert.True(node.DashboardTextWrapEnabled);
        node.DashboardTextWrapEnabled = false;

        using var document = JsonDocument.Parse(node.SettingsJson);
        Assert.False(document.RootElement.GetProperty("dashboardTextWrapEnabled").GetBoolean());
    }

    [Fact]
    public void CanvasNodeViewModel_FallsBackToTitleWhenNameIsEmpty()
    {
        var definition = new NodeDefinition(
            "test.signal",
            "Legacy Name",
            "Test",
            "0.1.0",
            [],
            Title: "TestSignal(TS)繝悶Ο繝・け",
            Subtitle: "TS");
        var node = new CanvasNodeViewModel(
            new NodeInstance("node-1", "test.signal", 10, 20, "{}", Name: string.Empty),
            definition);

        Assert.Equal("TestSignal(TS)繝悶Ο繝・け", node.DisplayName);
    }

    [Fact]
    public void CanvasPortViewModel_UsesPayloadLabelForJsonMessagePorts()
    {
        var definition = new NodeDefinition(
            "payload.block",
            "Payload Block",
            "Test",
            "0.1.0",
            [new PortDefinition("payload", "Payload", PortDirection.Output, PortDataKind.JsonMessage)]);
        var node = new CanvasNodeViewModel(new NodeInstance("node-1", "payload.block", 0, 0, "{}"), definition);

        var port = Assert.Single(node.Outputs);

        Assert.Equal("P", port.KindLabel);
        Assert.Equal("payload", port.ShapeClass);
        Assert.Equal("#D92D20", port.KindBadgeBackground);
        Assert.Equal("#FEE2E2", port.SchemaBadgeBackground);
        Assert.Contains("Payload", port.AccessibleName, StringComparison.Ordinal);
    }

    [Fact]
    public void CanvasPortViewModel_ExposesDetailedSchemaLabels()
    {
        var definition = new NodeDefinition(
            "vector.block",
            "Vector Block",
            "Test",
            "0.1.0",
            [new PortDefinition(
                "vector",
                "Vector",
                PortDirection.Output,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.NumericVector1D)]);
        var node = new CanvasNodeViewModel(new NodeInstance("node-1", "vector.block", 0, 0, "{}"), definition);

        var port = Assert.Single(node.Outputs);

        Assert.True(port.HasDetailedSchema);
        Assert.Equal("Numeric Vector (1D)", port.SchemaLabel);
        Assert.Equal("VEC", port.SchemaBadge);
        Assert.Equal("#1D70B8", port.KindBadgeBackground);
        Assert.Equal("#DBEAFE", port.SchemaBadgeBackground);
        Assert.Contains("Numeric Vector (1D)", port.ToolTipText, StringComparison.Ordinal);
    }

    [Fact]
    public void CanvasConnectionViewModel_UsesRedDashedPayloadConnection()
    {
        var sourceDefinition = new NodeDefinition(
            "source",
            "Source",
            "Test",
            "0.1.0",
            [new PortDefinition("out", "Payload Out", PortDirection.Output, PortDataKind.JsonMessage)]);
        var targetDefinition = new NodeDefinition(
            "target",
            "Target",
            "Test",
            "0.1.0",
            [new PortDefinition("in", "Payload In", PortDirection.Input, PortDataKind.JsonMessage)]);
        var source = new CanvasNodeViewModel(new NodeInstance("source-1", "source", 0, 0, "{}"), sourceDefinition);
        var target = new CanvasNodeViewModel(new NodeInstance("target-1", "target", 100, 0, "{}"), targetDefinition);
        var connection = new CanvasConnectionViewModel(
            new Core.Connection("source-1", "out", "target-1", "in", PortDataKind.JsonMessage),
            source,
            Assert.Single(source.Outputs),
            target,
            Assert.Single(target.Inputs));

        Assert.Equal("Payload", connection.KindLabel);
        Assert.Equal("#D92D20", connection.StrokeColor);
        Assert.Equal("6,4", connection.StrokeDashArray);
    }

    [Fact]
    public void CanvasConnectionViewModel_IncludesSchemaInConnectionLabel()
    {
        var sourceDefinition = new NodeDefinition(
            "source",
            "Source",
            "Test",
            "0.1.0",
            [new PortDefinition(
                "out",
                "Matrix Out",
                PortDirection.Output,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.NumericMatrix2D)]);
        var targetDefinition = new NodeDefinition(
            "target",
            "Target",
            "Test",
            "0.1.0",
            [new PortDefinition(
                "in",
                "Matrix In",
                PortDirection.Input,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.NumericMatrix2D)]);
        var source = new CanvasNodeViewModel(new NodeInstance("source-1", "source", 0, 0, "{}"), sourceDefinition);
        var target = new CanvasNodeViewModel(new NodeInstance("target-1", "target", 100, 0, "{}"), targetDefinition);
        var connection = new CanvasConnectionViewModel(
            new Core.Connection(
                "source-1",
                "out",
                "target-1",
                "in",
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.NumericMatrix2D),
            source,
            Assert.Single(source.Outputs),
            target,
            Assert.Single(target.Inputs));

        Assert.True(connection.HasDetailedSchema);
        Assert.Equal("Numeric Matrix (2D)", connection.SchemaLabel);
        Assert.Equal("Fast Stream / Numeric Matrix (2D)", connection.ConnectionLabel);
    }

    [Fact]
    public void DashboardWidgetViewModel_UsesSettingsForTitleContentAndDisabledHeader()
    {
        var settingsJson = JsonSerializer.Serialize(new
        {
            title = "TestSignal(TS)繝悶Ο繝・け",
            contentKind = "text",
            content = "millis,value\n0,0",
            displayData = new
            {
                text = "millis,value\n0,0",
            },
            isTextWrapEnabled = false,
            isSourceNodeEnabled = false,
        });

        var widget = new DashboardWidgetViewModel(
            Guid.NewGuid(),
            "Fallback",
            "dataprocesses.dashboard.node-block",
            0,
            0,
            1,
            2,
            settingsJson: settingsJson);

        Assert.Equal("TestSignal(TS)繝悶Ο繝・け", widget.Title);
        Assert.Equal("text", widget.ContentKind);
        Assert.True(widget.IsTextContent);
        Assert.Equal("millis,value\n0,0", widget.Content);
        Assert.Contains("millis,value", widget.DisplayDataJson, StringComparison.Ordinal);
        Assert.False(widget.IsTextWrapEnabled);
        Assert.True(widget.IsTextNoWrapEnabled);
        Assert.False(widget.IsTextContentAndWrapEnabled);
        Assert.True(widget.IsTextContentAndNoWrapEnabled);
        Assert.Equal("#94A3B8", widget.HeaderBackground);
    }

    [Fact]
    public void DashboardWidgetViewModel_PreservesStructuredGraphDisplayData()
    {
        var settingsJson = JsonSerializer.Serialize(new
        {
            title = "Plot",
            contentKind = "time-series",
            displayData = new
            {
                xUnit = "ms",
                series = new[]
                {
                    new
                    {
                        name = "signal",
                        points = new[]
                        {
                            new { x = 0, y = 0.0 },
                            new { x = 1, y = 0.5 },
                        },
                    },
                },
            },
            isSourceNodeEnabled = true,
        });

        var widget = new DashboardWidgetViewModel(
            Guid.NewGuid(),
            "Fallback",
            "dataprocesses.dashboard.node-block",
            0,
            0,
            2,
            2,
            settingsJson: settingsJson);

        Assert.Equal("time-series", widget.ContentKind);
        Assert.False(widget.IsTextContent);
        Assert.Contains("series", widget.DisplayDataJson, StringComparison.Ordinal);
        Assert.Equal(string.Empty, widget.Content);
    }

    [Fact]
    public void DashboardWidgetViewModel_AutoScrollToggle_UpdatesSettingsAndLabel()
    {
        var settingsJson = JsonSerializer.Serialize(new
        {
            title = "StremOutputTS",
            contentKind = "text",
            content = "millis,value\n0,1",
            displayData = new
            {
                text = "millis,value\n0,1",
            },
            supportsAutoScroll = true,
            isAutoScrollEnabled = true,
        });

        var widget = new DashboardWidgetViewModel(
            Guid.NewGuid(),
            "Fallback",
            "dataprocesses.dashboard.node-block",
            0,
            0,
            3,
            3,
            settingsJson: settingsJson);

        Assert.True(widget.IsAutoScrollToggleVisible);
        Assert.True(widget.IsAutoScrollEnabled);
        Assert.Equal("AutoScroll ON", widget.AutoScrollButtonLabel);

        widget.ToggleAutoScroll();

        Assert.False(widget.IsAutoScrollEnabled);
        Assert.Equal("AutoScroll OFF", widget.AutoScrollButtonLabel);
        using var updatedSettings = JsonDocument.Parse(widget.SettingsJson);
        Assert.False(updatedSettings.RootElement.GetProperty("isAutoScrollEnabled").GetBoolean());
    }

    [Fact]
    public void DashboardWidgetViewModel_LegacyStreamSettings_ShowsAutoScrollToggle()
    {
        var legacySettingsJson = JsonSerializer.Serialize(new
        {
            title = "StremOutputTS",
            contentKind = "text",
            content = "millis,value\n1289,-0.1874",
            displayData = new
            {
                text = "millis,value\n1289,-0.1874",
            },
            isTextWrapEnabled = true,
            isSourceNodeEnabled = true,
        });

        var widget = new DashboardWidgetViewModel(
            Guid.NewGuid(),
            "Fallback",
            "dataprocesses.dashboard.node-block",
            0,
            0,
            3,
            3,
            settingsJson: legacySettingsJson);

        Assert.True(widget.IsAutoScrollToggleVisible);
        Assert.True(widget.IsAutoScrollEnabled);
    }

    [Fact]
    public void AppendLogEntry_TrimsToLatest500Lines()
    {
        var appendLogEntryMethod = typeof(FlowEditorViewModel)
            .GetMethod("AppendLogEntry", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(appendLogEntryMethod);

        var content = string.Empty;
        for (var index = 1; index <= 505; index++)
        {
            content = Assert.IsType<string>(appendLogEntryMethod!.Invoke(null, [content, $"line-{index}"]));
        }

        var lines = content.Split(Environment.NewLine, StringSplitOptions.None);
        Assert.Equal(500, lines.Length);
        Assert.Equal("line-6", lines[0]);
        Assert.Equal("line-505", lines[^1]);
    }

    [Fact]
    public void FormatFastStreamFrame_UsesLatest500Samples()
    {
        var formatMethod = typeof(FlowEditorViewModel)
            .GetMethod("FormatFastStreamFrame", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(formatMethod);

        var samples = Enumerable.Range(0, 1_024).Select(static value => (double)value).ToArray();
        var frame = new FastStreamFrame(
            StartTimeUnixNanoseconds: 0,
            SamplePeriodNanoseconds: 1_000_000,
            ChannelNames: ["signal"],
            Samples: [samples.AsMemory()],
            SequenceNumber: 1);

        var content = Assert.IsType<string>(formatMethod!.Invoke(null, [frame, 0L]));
        var lines = content.Split(Environment.NewLine, StringSplitOptions.None);
        Assert.Equal(501, lines.Length);
        Assert.Equal("millis,value", lines[0]);
        Assert.EndsWith(",524", lines[1], StringComparison.Ordinal);
        Assert.EndsWith(",1023", lines[^1], StringComparison.Ordinal);
    }

    private sealed class TestNodeFactory : INodeFactory
    {
        private readonly IDataPacket? packetToEmit;
        private readonly Func<IDataPacket?>? packetFactory;

        public TestNodeFactory(
            DashboardWidgetDefinition? dashboardWidget = null,
            IDataPacket? packetToEmit = null,
            Func<IDataPacket?>? packetFactory = null,
            string? iconPath = null)
            : this("test.block", "Test Block", NodeType.Input, iconPath: iconPath, dashboardWidget: dashboardWidget, packetToEmit: packetToEmit, packetFactory: packetFactory)
        {
        }

        public TestNodeFactory(
            string typeId,
            string displayName,
            NodeType nodeType,
            string? title = null,
            string? subtitle = null,
            string? iconPath = null,
            DashboardWidgetDefinition? dashboardWidget = null,
            IDataPacket? packetToEmit = null,
            Func<IDataPacket?>? packetFactory = null)
        {
            this.packetToEmit = packetToEmit;
            this.packetFactory = packetFactory;
            Definition = new NodeDefinition(
                typeId,
                displayName,
                "Legacy Category",
                "0.1.0",
                [
                    new PortDefinition("in", "Input", PortDirection.Input, PortDataKind.FastStream, IsRequired: false),
                    new PortDefinition("out", "Output", PortDirection.Output, PortDataKind.FastStream),
                ],
                nodeType,
                Title: title,
                Subtitle: subtitle,
                IconPath: iconPath,
                DashboardWidget: dashboardWidget);
        }

        public NodeDefinition Definition { get; }

        public INode CreateNode(string nodeId)
        {
            return new TestNode(Definition, packetFactory?.Invoke() ?? packetToEmit);
        }
    }

    private sealed class TestNode(NodeDefinition definition, IDataPacket? packetToEmit) : INode
    {
        private INodeContext? context;

        public NodeDefinition Definition { get; } = definition;

        public ValueTask InitializeAsync(INodeContext context, CancellationToken cancellationToken)
        {
            this.context = context;
            return ValueTask.CompletedTask;
        }

        public ValueTask OnPacketAsync(string inputPortId, IDataPacket packet, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask StartAsync(CancellationToken cancellationToken)
        {
            if (packetToEmit is not null)
            {
                return context!.EmitAsync("out", packetToEmit, cancellationToken);
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask StopAsync(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class StaticDefinitionNodeFactory(NodeDefinition definition) : INodeFactory
    {
        public NodeDefinition Definition { get; } = definition;

        public INode CreateNode(string nodeId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
            return new TestNode(Definition, packetToEmit: null);
        }
    }
}
