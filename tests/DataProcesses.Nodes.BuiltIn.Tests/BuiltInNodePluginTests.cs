using DataProcesses.Nodes.BuiltIn;
using DataProcesses.Nodes.BuiltIn.Blocks.CsvInput;
using DataProcesses.Nodes.BuiltIn.Blocks.FastFourierTransform;
using DataProcesses.Nodes.BuiltIn.Blocks.LowPassFilter;
using DataProcesses.Nodes.BuiltIn.Blocks.PayloadOutput;
using DataProcesses.Nodes.BuiltIn.Blocks.PythonOutput;
using DataProcesses.Nodes.BuiltIn.Blocks.StreamOutput;
using DataProcesses.Nodes.BuiltIn.Blocks.TestSignal;
using DataProcesses.Nodes.BuiltIn.Blocks.Trigger;
using DataProcesses.Plugin.Abstractions;

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
                CsvInputBlock.TypeId,
                TestSignalBlock.TypeId,
                TriggerBlock.TypeId,
                LowPassFilterBlock.TypeId,
                FastFourierTransformBlock.TypeId,
                StreamOutputBlock.TypeId,
                PythonOutputBlock.TypeId,
                PayloadOutputBlock.TypeId,
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

    [Fact]
    public void BuiltInCatalog_AssignsNodeTypesForPaletteGrouping()
    {
        var plugin = new BuiltInNodePlugin();
        var nodeTypesByTypeId = plugin.NodeFactories.ToDictionary(
            static factory => factory.Definition.TypeId,
            static factory => factory.Definition.NodeType);

        Assert.Equal(NodeType.Input, nodeTypesByTypeId[TestSignalBlock.TypeId]);
        Assert.Equal(NodeType.Input, nodeTypesByTypeId[CsvInputBlock.TypeId]);
        Assert.Equal(NodeType.Input, nodeTypesByTypeId[TriggerBlock.TypeId]);
        Assert.Equal(NodeType.BasicProcess, nodeTypesByTypeId[LowPassFilterBlock.TypeId]);
        Assert.Equal(NodeType.BasicProcess, nodeTypesByTypeId[FastFourierTransformBlock.TypeId]);
        Assert.Equal(NodeType.Debug, nodeTypesByTypeId[StreamOutputBlock.TypeId]);
        Assert.Equal(NodeType.Output, nodeTypesByTypeId[PythonOutputBlock.TypeId]);
        Assert.Equal(NodeType.Debug, nodeTypesByTypeId[PayloadOutputBlock.TypeId]);
    }

    [Fact]
    public void BuiltInCatalog_DefinesPresentationMetadataForEveryBlock()
    {
        var plugin = new BuiltInNodePlugin();

        foreach (var factory in plugin.NodeFactories)
        {
            Assert.False(string.IsNullOrWhiteSpace(factory.Definition.Title));
            Assert.False(string.IsNullOrWhiteSpace(factory.Definition.Subtitle));
            Assert.False(string.IsNullOrWhiteSpace(factory.Definition.IconPath));
            Assert.EndsWith("icon.png", factory.Definition.IconPath, StringComparison.Ordinal);
        }
    }
}
