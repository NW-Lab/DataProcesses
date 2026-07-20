using System.Text.Json;

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
        Assert.Equal("TestSignal", widget.Title);
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
    public void NodeViewModels_ResolveIconPathsFromDefinitionPath()
    {
        var iconPath = GetRepositoryFilePath("src", "DataProcesses.Nodes.BuiltIn", "Blocks", "TestSignal", "icon.png");
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
        while (!timeout.IsCancellationRequested)
        {
            var content = getDashboards()
                .SelectMany(static dashboard => dashboard.Widgets)
                .Select(static widget => widget.SettingsJson)
                .FirstOrDefault(settingsJson => settingsJson.Contains(expectedContent, StringComparison.Ordinal));
            if (content is not null)
            {
                return;
            }

            await Task.Delay(25, timeout.Token).ConfigureAwait(true);
        }

        throw new TimeoutException($"Dashboard content did not contain '{expectedContent}'.");
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

    [Fact]
    public void DashboardWidgetViewModel_UsesSettingsForTitleContentAndDisabledHeader()
    {
        var settingsJson = JsonSerializer.Serialize(new
        {
            title = "TestSignal",
            contentKind = "text",
            content = "millis,value\n0,0",
            displayData = new
            {
                text = "millis,value\n0,0",
            },
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

        Assert.Equal("TestSignal", widget.Title);
        Assert.Equal("text", widget.ContentKind);
        Assert.True(widget.IsTextContent);
        Assert.Equal("millis,value\n0,0", widget.Content);
        Assert.Contains("millis,value", widget.DisplayDataJson, StringComparison.Ordinal);
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
                [new PortDefinition("out", "Output", PortDirection.Output, PortDataKind.FastStream)],
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
}