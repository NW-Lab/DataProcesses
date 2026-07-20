using DataProcesses.Core;
using DataProcesses.Desktop.Services;
using DataProcesses.Desktop.ViewModels;
using DataProcesses.Engine;
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
                [new DashboardWidget(Guid.NewGuid(), "dataprocesses.dashboard.time-series", 1, 2, 3, 2)]),
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
    public void Palette_GroupsNodesByNodeType()
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

        Assert.Equal(["INPUT", "Basic Process", "OUTPUT"], viewModel.Palette.Groups.Select(static group => group.DisplayName));
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
    public void Palette_UsesDefinitionTitleAndSubtitle()
    {
        var factory = new TestNodeFactory(
            "test.signal",
            "Legacy Name",
            NodeType.Input,
            title: "TestSignal",
            subtitle: "Sin&squeare");
        var viewModel = new FlowEditorViewModel(
            [factory],
            new FlowRunner([factory]),
            new ProjectFileService());

        var paletteNode = Assert.Single(viewModel.Palette.FilteredNodes);

        Assert.Equal("TestSignal", paletteNode.Title);
        Assert.Equal("Sin&squeare", paletteNode.Subtitle);
        Assert.Equal("TestSignal", paletteNode.DisplayName);
    }

    [Fact]
    public void Palette_SearchMatchesSubtitle()
    {
        var factory = new TestNodeFactory(
            "test.signal",
            "Legacy Name",
            NodeType.Input,
            title: "TestSignal",
            subtitle: "Sin&squeare");
        var viewModel = new FlowEditorViewModel(
            [factory],
            new FlowRunner([factory]),
            new ProjectFileService());

        viewModel.Palette.SearchText = "squeare";

        Assert.Equal("test.signal", Assert.Single(viewModel.Palette.FilteredNodes).TypeId);
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
    public void CanvasNodeViewModel_FallsBackToTitleWhenNameIsEmpty()
    {
        var definition = new NodeDefinition(
            "test.signal",
            "Legacy Name",
            "Test",
            "0.1.0",
            [],
            Title: "TestSignal",
            Subtitle: "Sin&squeare");
        var node = new CanvasNodeViewModel(
            new NodeInstance("node-1", "test.signal", 10, 20, "{}", Name: string.Empty),
            definition);

        Assert.Equal("TestSignal", node.DisplayName);
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
        Assert.Contains("Payload", port.AccessibleName, StringComparison.Ordinal);
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

    private sealed class TestNodeFactory : INodeFactory
    {
        public TestNodeFactory()
            : this("test.block", "Test Block", NodeType.Input)
        {
        }

        public TestNodeFactory(
            string typeId,
            string displayName,
            NodeType nodeType,
            string? title = null,
            string? subtitle = null)
        {
            Definition = new NodeDefinition(
                typeId,
                displayName,
                "Legacy Category",
                "0.1.0",
                [new PortDefinition("out", "Output", PortDirection.Output, PortDataKind.FastStream)],
                nodeType,
                Title: title,
                Subtitle: subtitle);
        }

        public NodeDefinition Definition { get; }

        public INode CreateNode(string nodeId)
        {
            return new TestNode(Definition);
        }
    }

    private sealed class TestNode(NodeDefinition definition) : INode
    {
        public NodeDefinition Definition { get; } = definition;

        public ValueTask InitializeAsync(INodeContext context, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask OnPacketAsync(string inputPortId, IDataPacket packet, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask StartAsync(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask StopAsync(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }
}