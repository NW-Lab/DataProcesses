using DataProcesses.Nodes.BuiltIn;
using DataProcesses.Nodes.BuiltIn.Blocks.BreathImage;
using DataProcesses.Nodes.BuiltIn.Blocks.BleInputTS;
using DataProcesses.Nodes.BuiltIn.Blocks.BleInputFS;
using DataProcesses.Nodes.BuiltIn.Blocks.BleInputVector;
using DataProcesses.Nodes.BuiltIn.Blocks.AudioOutFS;
using DataProcesses.Nodes.BuiltIn.Blocks.BreathTS;
using DataProcesses.Nodes.BuiltIn.Blocks.CameraInputImage;
using DataProcesses.Nodes.BuiltIn.Blocks.CdTimeResolvedMethodTS;
using DataProcesses.Nodes.BuiltIn.Blocks.CsvInput;
using DataProcesses.Nodes.BuiltIn.Blocks.CsvOutput;
using DataProcesses.Nodes.BuiltIn.Blocks.SerialInputTS;
using DataProcesses.Nodes.BuiltIn.Blocks.SerialInputVector;
using DataProcesses.Nodes.BuiltIn.Blocks.MovieInputImage;
using DataProcesses.Nodes.BuiltIn.Blocks.MicInputFS;
using DataProcesses.Nodes.BuiltIn.Blocks.MqttIn;
using DataProcesses.Nodes.BuiltIn.Blocks.MqttOut;
using DataProcesses.Nodes.BuiltIn.Blocks.FastFourierTransform;
using DataProcesses.Nodes.BuiltIn.Blocks.FftTS;
using DataProcesses.Nodes.BuiltIn.Blocks.FftFS;
using DataProcesses.Nodes.BuiltIn.Blocks.FilterTS;
using DataProcesses.Nodes.BuiltIn.Blocks.FilterFS;
using DataProcesses.Nodes.BuiltIn.Blocks.HartRateImage;
using DataProcesses.Nodes.BuiltIn.Blocks.HartRateTS;
using DataProcesses.Nodes.BuiltIn.Blocks.HumansImage;
using DataProcesses.Nodes.BuiltIn.Blocks.StreamChartTS;
using DataProcesses.Nodes.BuiltIn.Blocks.StreamChartFS;
using DataProcesses.Nodes.BuiltIn.Blocks.StreamChartVector;
using DataProcesses.Nodes.BuiltIn.Blocks.StreamOutputImage;
using DataProcesses.Nodes.BuiltIn.Blocks.StreamOutputFS;
using DataProcesses.Nodes.BuiltIn.Blocks.MovingAverage;
using DataProcesses.Nodes.BuiltIn.Blocks.StreamOutputVector;
using DataProcesses.Nodes.BuiltIn.Blocks.PayloadOutput;
using DataProcesses.Nodes.BuiltIn.Blocks.PythonOutput;
using DataProcesses.Nodes.BuiltIn.Blocks.StremOutputTS;
using DataProcesses.Nodes.BuiltIn.Blocks.TestSignalImg;
using DataProcesses.Nodes.BuiltIn.Blocks.TestSignalTS;
using DataProcesses.Nodes.BuiltIn.Blocks.TestSignalFS;
using DataProcesses.Nodes.BuiltIn.Blocks.TestSignalVec;
using DataProcesses.Nodes.BuiltIn.Blocks.Trigger;
using DataProcesses.Nodes.BuiltIn.Blocks.UVCameraInputImage;
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
                SerialInputTSBlock.TypeId,
                SerialInputVectorBlock.TypeId,
                BleInputTSBlock.TypeId,
                BleInputFSBlock.TypeId,
                BleInputVectorBlock.TypeId,
                CameraInputImageBlock.TypeId,
                MovieInputImageBlock.TypeId,
                UVCameraInputImageBlock.TypeId,
                MicInputFSBlock.TypeId,
                AudioOutFSBlock.TypeId,
                MqttInBlock.TypeId,
                MqttOutBlock.TypeId,
                TestSignalBlock.TypeId,
                TestSignalFSBlock.TypeId,
                TestSignalVecBlock.TypeId,
                TestSignalImgBlock.TypeId,
                TriggerBlock.TypeId,
                FilterTSBlock.TypeId,
                FilterFSBlock.TypeId,
                MovingAverageBlock.TypeId,
                FastFourierTransformBlock.TypeId,
                FftFSBlock.TypeId,
                FftTSBlock.TypeId,
                CdTimeResolvedMethodTSBlock.TypeId,
                HartRateTSBlock.TypeId,
                HartRateImageBlock.TypeId,
                HumansImageBlock.TypeId,
                BreathImageBlock.TypeId,
                BreathTSBlock.TypeId,
                StremOutputTSBlock.TypeId,
                StreamOutputFSBlock.TypeId,
                StreamOutputVectorBlock.TypeId,
                StreamChartVectorBlock.TypeId,
                StreamChartTSBlock.TypeId,
                StreamChartFSBlock.TypeId,
                StreamOutputImageBlock.TypeId,
                PythonOutputBlock.TypeId,
                PayloadOutputBlock.TypeId,
                CsvOutputBlock.TypeId,
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
        Assert.Equal(NodeType.Input, nodeTypesByTypeId[TestSignalFSBlock.TypeId]);
        Assert.Equal(NodeType.Input, nodeTypesByTypeId[CsvInputBlock.TypeId]);
        Assert.Equal(NodeType.Input, nodeTypesByTypeId[SerialInputTSBlock.TypeId]);
        Assert.Equal(NodeType.Input, nodeTypesByTypeId[SerialInputVectorBlock.TypeId]);
        Assert.Equal(NodeType.Input, nodeTypesByTypeId[BleInputTSBlock.TypeId]);
        Assert.Equal(NodeType.Input, nodeTypesByTypeId[BleInputVectorBlock.TypeId]);
        Assert.Equal(NodeType.Input, nodeTypesByTypeId[CameraInputImageBlock.TypeId]);
        Assert.Equal(NodeType.Input, nodeTypesByTypeId[MovieInputImageBlock.TypeId]);
        Assert.Equal(NodeType.Input, nodeTypesByTypeId[UVCameraInputImageBlock.TypeId]);
        Assert.Equal(NodeType.Input, nodeTypesByTypeId[MicInputFSBlock.TypeId]);
        Assert.Equal(NodeType.Input, nodeTypesByTypeId[MqttInBlock.TypeId]);
            Assert.Equal(NodeType.Output, nodeTypesByTypeId[AudioOutFSBlock.TypeId]);
        Assert.Equal(NodeType.Output, nodeTypesByTypeId[MqttOutBlock.TypeId]);
        Assert.Equal(NodeType.Input, nodeTypesByTypeId[TestSignalImgBlock.TypeId]);
        Assert.Equal(NodeType.Input, nodeTypesByTypeId[TriggerBlock.TypeId]);
        Assert.Equal(NodeType.BasicProcess, nodeTypesByTypeId[FilterTSBlock.TypeId]);
            Assert.Equal(NodeType.BasicProcess, nodeTypesByTypeId[FilterFSBlock.TypeId]);
        Assert.Equal(NodeType.BasicProcess, nodeTypesByTypeId[MovingAverageBlock.TypeId]);
        Assert.Equal(NodeType.BasicProcess, nodeTypesByTypeId[FastFourierTransformBlock.TypeId]);
            Assert.Equal(NodeType.BasicProcess, nodeTypesByTypeId[FftFSBlock.TypeId]);
        Assert.Equal(NodeType.BasicProcess, nodeTypesByTypeId[FftTSBlock.TypeId]);
        Assert.Equal(NodeType.BasicProcess, nodeTypesByTypeId[CdTimeResolvedMethodTSBlock.TypeId]);
        Assert.Equal(NodeType.BasicProcess, nodeTypesByTypeId[HartRateTSBlock.TypeId]);
        Assert.Equal(NodeType.BasicProcess, nodeTypesByTypeId[HartRateImageBlock.TypeId]);
        Assert.Equal(NodeType.BasicProcess, nodeTypesByTypeId[HumansImageBlock.TypeId]);
        Assert.Equal(NodeType.BasicProcess, nodeTypesByTypeId[BreathImageBlock.TypeId]);
        Assert.Equal(NodeType.BasicProcess, nodeTypesByTypeId[BreathTSBlock.TypeId]);
        Assert.Equal(NodeType.Debug, nodeTypesByTypeId[StremOutputTSBlock.TypeId]);
        Assert.Equal(NodeType.Debug, nodeTypesByTypeId[StreamOutputFSBlock.TypeId]);
        Assert.Equal(NodeType.Debug, nodeTypesByTypeId[StreamOutputVectorBlock.TypeId]);
        Assert.Equal(NodeType.Output, nodeTypesByTypeId[StreamChartTSBlock.TypeId]);
            Assert.Equal(NodeType.Output, nodeTypesByTypeId[StreamChartFSBlock.TypeId]);
        Assert.Equal(NodeType.Debug, nodeTypesByTypeId[StreamOutputImageBlock.TypeId]);
        Assert.Equal(NodeType.Output, nodeTypesByTypeId[PythonOutputBlock.TypeId]);
        Assert.Equal(NodeType.Debug, nodeTypesByTypeId[PayloadOutputBlock.TypeId]);
        Assert.Equal(NodeType.Output, nodeTypesByTypeId[CsvOutputBlock.TypeId]);
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
