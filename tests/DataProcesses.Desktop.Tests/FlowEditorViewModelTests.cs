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

    private sealed class TestNodeFactory : INodeFactory
    {
        public NodeDefinition Definition { get; } = new(
            "test.block",
            "Test Block",
            "Sources",
            "0.1.0",
            [new PortDefinition("out", "Output", PortDirection.Output, PortDataKind.FastStream)]);

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