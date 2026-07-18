using DataProcesses.Core;
using DataProcesses.Engine;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Engine.Tests;

public sealed class FlowRunnerTests
{
    [Fact]
    public async Task RunAsync_DeliversPacketsAlongConnections()
    {
        var packet = new TestPacket();
        var receivedPackets = new List<IDataPacket>();
        var factories = new INodeFactory[]
        {
            new TestNodeFactory(
                new NodeDefinition(
                    "source",
                    "Source",
                    "Sources",
                    "0.1.0",
                    [new PortDefinition("out", "Output", PortDirection.Output, PortDataKind.FastStream)]),
                nodeId => new SourceNode(nodeId, packet)),
            new TestNodeFactory(
                new NodeDefinition(
                    "sink",
                    "Sink",
                    "Visualization",
                    "0.1.0",
                    [new PortDefinition("in", "Input", PortDirection.Input, PortDataKind.FastStream)]),
                nodeId => new SinkNode(nodeId, receivedPackets)),
        };
        var runner = new FlowRunner(factories);
        var document = new FlowDocument(
            Guid.NewGuid(),
            "Test flow",
            [
                new NodeInstance("source-1", "source", 0, 0, "{}"),
                new NodeInstance("sink-1", "sink", 100, 0, "{}"),
            ],
            [new Connection("source-1", "out", "sink-1", "in", PortDataKind.FastStream)]);

        var result = await runner.RunAsync(document, CancellationToken.None);

        Assert.Equal(FlowExecutionState.Stopped, result.State);
        Assert.Empty(result.ValidationIssues);
        Assert.Same(packet, Assert.Single(receivedPackets));
        Assert.Contains(result.Logs, log => log.NodeId == "sink-1" && log.Message.Contains("Delivered", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RunAsync_DeliversPacketsToEveryFanOutConnection()
    {
        var packet = new TestPacket();
        var firstReceivedPackets = new List<IDataPacket>();
        var secondReceivedPackets = new List<IDataPacket>();
        var factories = new INodeFactory[]
        {
            new TestNodeFactory(
                new NodeDefinition(
                    "source",
                    "Source",
                    "Sources",
                    "0.1.0",
                    [new PortDefinition("out", "Output", PortDirection.Output, PortDataKind.FastStream)]),
                nodeId => new SourceNode(nodeId, packet)),
            new TestNodeFactory(
                new NodeDefinition(
                    "sink",
                    "Sink",
                    "Visualization",
                    "0.1.0",
                    [new PortDefinition("in", "Input", PortDirection.Input, PortDataKind.FastStream)]),
                nodeId => nodeId == "sink-1" ? new SinkNode(nodeId, firstReceivedPackets) : new SinkNode(nodeId, secondReceivedPackets)),
        };
        var runner = new FlowRunner(factories);
        var document = new FlowDocument(
            Guid.NewGuid(),
            "Fan-out flow",
            [
                new NodeInstance("source-1", "source", 0, 0, "{}"),
                new NodeInstance("sink-1", "sink", 100, 0, "{}"),
                new NodeInstance("sink-2", "sink", 100, 120, "{}"),
            ],
            [
                new Connection("source-1", "out", "sink-1", "in", PortDataKind.FastStream),
                new Connection("source-1", "out", "sink-2", "in", PortDataKind.FastStream),
            ]);

        var result = await runner.RunAsync(document, CancellationToken.None);

        Assert.Equal(FlowExecutionState.Stopped, result.State);
        Assert.Empty(result.ValidationIssues);
        Assert.Same(packet, Assert.Single(firstReceivedPackets));
        Assert.Same(packet, Assert.Single(secondReceivedPackets));
    }

    [Fact]
    public async Task RunAsync_PassesSettingsJsonToConfiguredFactory()
    {
        var factory = new ConfiguredTestNodeFactory(
            new NodeDefinition(
                "configured",
                "Configured",
                "Sources",
                "0.1.0",
                []));
        var runner = new FlowRunner([factory]);
        var document = new FlowDocument(
            Guid.NewGuid(),
            "Configured flow",
            [new NodeInstance("configured-1", "configured", 0, 0, "{\"frequency\":12.5}")],
            []);

        var result = await runner.RunAsync(document, CancellationToken.None);

        Assert.Equal(FlowExecutionState.Stopped, result.State);
        Assert.Equal("{\"frequency\":12.5}", factory.SettingsJson);
    }

    [Fact]
    public async Task RunAsync_DoesNotCreateOrStartDisabledNodes()
    {
        var createCount = 0;
        var factories = new INodeFactory[]
        {
            new TestNodeFactory(
                new NodeDefinition(
                    "disabled-source",
                    "Disabled Source",
                    "Sources",
                    "0.1.0",
                    []),
                _ =>
                {
                    createCount++;
                    return new NoOpNode();
                }),
        };
        var runner = new FlowRunner(factories);
        var document = new FlowDocument(
            Guid.NewGuid(),
            "Disabled flow",
            [new NodeInstance("source-1", "disabled-source", 0, 0, "{}", IsEnabled: false)],
            []);

        var result = await runner.RunAsync(document, CancellationToken.None);

        Assert.Equal(FlowExecutionState.Stopped, result.State);
        Assert.Equal(0, createCount);
    }

    [Fact]
    public async Task RunAsync_ReturnsFaulted_WhenValidationFails()
    {
        var factories = new INodeFactory[]
        {
            new TestNodeFactory(
                new NodeDefinition(
                    "sink",
                    "Sink",
                    "Visualization",
                    "0.1.0",
                    [new PortDefinition("in", "Input", PortDirection.Input, PortDataKind.FastStream)]),
                nodeId => new SinkNode(nodeId, [])),
        };
        var runner = new FlowRunner(factories);
        var document = new FlowDocument(
            Guid.NewGuid(),
            "Invalid flow",
            [new NodeInstance("sink-1", "sink", 0, 0, "{}")],
            []);

        var result = await runner.RunAsync(document, CancellationToken.None);

        Assert.Equal(FlowExecutionState.Faulted, result.State);
        Assert.Contains(result.ValidationIssues, issue => issue.Code == FlowValidationIssueCode.MissingRequiredInput);
    }

    private sealed class TestPacket : IDataPacket
    {
        public PortDataKind Kind => PortDataKind.FastStream;
    }

    private sealed class TestNodeFactory(NodeDefinition definition, Func<string, INode> createNode) : INodeFactory
    {
        public NodeDefinition Definition { get; } = definition;

        public INode CreateNode(string nodeId)
        {
            return createNode(nodeId);
        }
    }

    private sealed class ConfiguredTestNodeFactory(NodeDefinition definition) : IConfiguredNodeFactory
    {
        public NodeDefinition Definition { get; } = definition;

        public string? SettingsJson { get; private set; }

        public INode CreateNode(string nodeId)
        {
            return new NoOpNode();
        }

        public INode CreateNode(string nodeId, string settingsJson)
        {
            SettingsJson = settingsJson;
            return new NoOpNode();
        }
    }

    private sealed class NoOpNode : INode
    {
        public NodeDefinition Definition { get; } = new(
            "no-op",
            "No-op",
            "Test",
            "0.1.0",
            []);

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

    private sealed class SourceNode(string nodeId, IDataPacket packet) : INode
    {
        private INodeContext? context;

        public NodeDefinition Definition { get; } = new(
            "source",
            "Source",
            "Sources",
            "0.1.0",
            [new PortDefinition("out", "Output", PortDirection.Output, PortDataKind.FastStream)]);

        public ValueTask InitializeAsync(INodeContext nodeContext, CancellationToken cancellationToken)
        {
            context = nodeContext;
            Assert.Equal(nodeId, nodeContext.NodeId);
            return ValueTask.CompletedTask;
        }

        public ValueTask OnPacketAsync(string inputPortId, IDataPacket inputPacket, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask StartAsync(CancellationToken cancellationToken)
        {
            return context!.EmitAsync("out", packet, cancellationToken);
        }

        public ValueTask StopAsync(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class SinkNode(string nodeId, ICollection<IDataPacket> receivedPackets) : INode
    {
        public NodeDefinition Definition { get; } = new(
            "sink",
            "Sink",
            "Visualization",
            "0.1.0",
            [new PortDefinition("in", "Input", PortDirection.Input, PortDataKind.FastStream)]);

        public ValueTask InitializeAsync(INodeContext context, CancellationToken cancellationToken)
        {
            Assert.Equal(nodeId, context.NodeId);
            return ValueTask.CompletedTask;
        }

        public ValueTask OnPacketAsync(string inputPortId, IDataPacket packet, CancellationToken cancellationToken)
        {
            Assert.Equal("in", inputPortId);
            receivedPackets.Add(packet);
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