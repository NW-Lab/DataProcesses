using DataProcesses.Nodes.BuiltIn;
using DataProcesses.Nodes.BuiltIn.Blocks.TestSignal;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.TestSignal;

public sealed class TestSignalBlockTests
{
    [Fact]
    public void BuiltInCatalog_RegistersTestSignalBlock()
    {
        var plugin = new BuiltInNodePlugin();

        var factory = Assert.Single(plugin.NodeFactories);

        Assert.Equal(TestSignalBlock.TypeId, factory.Definition.TypeId);
        Assert.Equal("Test Signal", factory.Definition.DisplayName);
    }

    [Fact]
    public void TestSignalBlock_DefinesOneFastStreamOutput()
    {
        var port = Assert.Single(TestSignalBlock.Definition.Ports);

        Assert.Equal(TestSignalBlock.OutputPortId, port.Id);
        Assert.Equal(PortDirection.Output, port.Direction);
        Assert.Equal(PortDataKind.FastStream, port.DataKind);
    }

    [Fact]
    public void Factory_CreatesAnIndependentNodeInstance()
    {
        var factory = new TestSignalNodeFactory();

        var first = factory.CreateNode("test-signal-1");
        var second = factory.CreateNode("test-signal-2");

        Assert.IsType<TestSignalNode>(first);
        Assert.IsType<TestSignalNode>(second);
        Assert.NotSame(first, second);
    }
}