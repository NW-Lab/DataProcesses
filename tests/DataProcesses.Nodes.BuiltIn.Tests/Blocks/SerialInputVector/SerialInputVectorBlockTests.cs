using DataProcesses.Nodes.BuiltIn;
using DataProcesses.Nodes.BuiltIn.Blocks.SerialInputVector;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.SerialInputVector;

public sealed class SerialInputVectorBlockTests
{
    [Fact]
    public void Definition_DeclaresOneNumericVectorOutput()
    {
        var port = Assert.Single(SerialInputVectorBlock.Definition.Ports);

        Assert.Equal(SerialInputVectorBlock.VectorPortId, port.Id);
        Assert.Equal(PortDirection.Output, port.Direction);
        Assert.Equal(PortDataKind.FastStream, port.DataKind);
        Assert.Equal(PortDataSchema.NumericVector1D, port.DataSchema);
    }

    [Fact]
    public async Task StartAsync_EmitsOneImuVectorForEachSerialRow()
    {
        var context = new RecordingNodeContext();
        var node = new SerialInputVectorNode(
            new SerialInputVectorSettings(ComPortName: "COM7"),
            getTimestamp: () => DateTimeOffset.UnixEpoch,
            lineSourceFactory: (_, _) => CreateLineSource(["0,0.01,-0.02,9.81", "10,0.02,-0.01,9.80"]));

        await node.InitializeAsync(context, CancellationToken.None);
        await node.StartAsync(CancellationToken.None);

        Assert.Equal(2, context.EmittedPackets.Count);
        var first = Assert.IsType<NumericVectorFrame>(context.EmittedPackets[0].Packet);
        var second = Assert.IsType<NumericVectorFrame>(context.EmittedPackets[1].Packet);

        Assert.Equal(SerialInputVectorBlock.VectorPortId, context.EmittedPackets[0].OutputPortId);
        Assert.Equal("imu", first.Name);
        Assert.Equal(new double[] { 0.01, -0.02, 9.81 }, first.Values.ToArray());
        Assert.Equal(DateTimeOffset.UnixEpoch, first.Timestamp);
        Assert.Equal(DateTimeOffset.UnixEpoch.AddMilliseconds(10), second.Timestamp);
        Assert.Equal(1, second.SequenceNumber);
    }

    [Fact]
    public async Task StartAsync_RejectsRowsThatAreNotImuVectors()
    {
        var node = new SerialInputVectorNode(
            new SerialInputVectorSettings(),
            lineSourceFactory: (_, _) => CreateLineSource(["0,1,2"]));
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);

        await Assert.ThrowsAsync<InvalidDataException>(async () => await node.StartAsync(CancellationToken.None));
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