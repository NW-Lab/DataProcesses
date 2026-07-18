using DataProcesses.Core;
using DataProcesses.Desktop.Services;
using DataProcesses.Desktop.ViewModels;
using DataProcesses.Engine;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Desktop.Tests;

public sealed class FlowEditorViewModelTests
{
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