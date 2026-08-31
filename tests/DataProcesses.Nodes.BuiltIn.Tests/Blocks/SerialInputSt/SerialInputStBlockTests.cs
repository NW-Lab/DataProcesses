using DataProcesses.Nodes.BuiltIn;
using DataProcesses.Nodes.BuiltIn.Blocks.SerialInputSt;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.SerialInputSt;

public sealed class SerialInputStBlockTests
{
    [Fact]
    public void Definition_DeclaresOneFastStreamOutput()
    {
        var port = Assert.Single(SerialInputStBlock.Definition.Ports);

        Assert.Equal(SerialInputStBlock.StreamPortId, port.Id);
        Assert.Equal(PortDirection.Output, port.Direction);
        Assert.Equal(PortDataKind.FastStream, port.DataKind);
        Assert.Equal(PortDataSchema.TimeSeries1D, port.DataSchema);
    }

    [Fact]
    public async Task StartAsync_ConvertsArduinoRowsToMultiChannelFrames()
    {
        var settings = new SerialInputStSettings(ComPortName: "COM7", BaudRate: 115200, ChannelCount: 2);
        var context = new RecordingNodeContext();
        var node = new SerialInputStNode(
            settings,
            getTimestamp: () => DateTimeOffset.UnixEpoch,
            lineSourceFactory: (_, _) => CreateLineSource(["0,1.5,2.5", "10,1.7,2.7"]));

        await node.InitializeAsync(context, CancellationToken.None);
        await node.StartAsync(CancellationToken.None);

        Assert.Equal(2, context.EmittedPackets.Count);
        var first = Assert.IsType<FastStreamFrame>(context.EmittedPackets[0].Packet);
        var second = Assert.IsType<FastStreamFrame>(context.EmittedPackets[1].Packet);

        Assert.Equal(SerialInputStBlock.StreamPortId, context.EmittedPackets[0].OutputPortId);
        Assert.Equal(["data1", "data2"], first.ChannelNames);
        Assert.Equal(1.5, first.Samples[0].Span[0]);
        Assert.Equal(2.5, first.Samples[1].Span[0]);
        Assert.Equal(1_000_000L, first.SamplePeriodNanoseconds);
        Assert.Equal(10_000_000L, second.SamplePeriodNanoseconds);
        Assert.Equal(1, second.SequenceNumber);
    }

    [Fact]
    public async Task StartAsync_RejectsRowsWithUnexpectedChannelCount()
    {
        var node = new SerialInputStNode(
            new SerialInputStSettings(ChannelCount: 2),
            lineSourceFactory: (_, _) => CreateLineSource(["0,1"]));
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);

        await Assert.ThrowsAsync<InvalidDataException>(async () => await node.StartAsync(CancellationToken.None));
    }

    [Fact]
    public void BuiltInCatalog_RegistersSerialInputStBlock()
    {
        var plugin = new BuiltInNodePlugin();

        Assert.Contains(plugin.NodeFactories, factory => factory.Definition.TypeId == SerialInputStBlock.TypeId);
    }

    private static async IAsyncEnumerable<string> CreateLineSource(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            yield return line;
            await Task.Yield();
        }
    }
}