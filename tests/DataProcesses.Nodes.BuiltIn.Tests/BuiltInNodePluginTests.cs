using DataProcesses.Nodes.BuiltIn;
using DataProcesses.Nodes.BuiltIn.Blocks.FastFourierTransform;
using DataProcesses.Nodes.BuiltIn.Blocks.LowPassFilter;
using DataProcesses.Nodes.BuiltIn.Blocks.PythonOutput;
using DataProcesses.Nodes.BuiltIn.Blocks.TestSignal;
using DataProcesses.Nodes.BuiltIn.Blocks.TimeSeriesDisplay;

namespace DataProcesses.Nodes.BuiltIn.Tests;

public sealed class BuiltInNodePluginTests
{
    [Fact]
    public void BuiltInCatalog_RegistersAllInitialBlocks()
    {
        var plugin = new BuiltInNodePlugin();
        var typeIds = plugin.NodeFactories.Select(static factory => factory.Definition.TypeId).ToArray();

        Assert.Equal(
            new[]
            {
                TestSignalBlock.TypeId,
                LowPassFilterBlock.TypeId,
                FastFourierTransformBlock.TypeId,
                TimeSeriesDisplayBlock.TypeId,
                PythonOutputBlock.TypeId,
            },
            typeIds);
    }

    [Fact]
    public void Factories_CreateIndependentNodeInstances()
    {
        var plugin = new BuiltInNodePlugin();

        foreach (var factory in plugin.NodeFactories)
        {
            var first = factory.CreateNode("first");
            var second = factory.CreateNode("second");

            Assert.NotSame(first, second);
            Assert.Equal(factory.Definition, first.Definition);
        }
    }
}
