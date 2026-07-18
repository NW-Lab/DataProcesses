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

    private sealed class TestNodeFactory : INodeFactory
    {
        public TestNodeFactory()
            : this("test.block", "Test Block", NodeType.Input)
        {
        }

        public TestNodeFactory(string typeId, string displayName, NodeType nodeType)
        {
            Definition = new NodeDefinition(
                typeId,
                displayName,
                "Legacy Category",
                "0.1.0",
                [new PortDefinition("out", "Output", PortDirection.Output, PortDataKind.FastStream)],
                nodeType);
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