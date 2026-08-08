using DataProcesses.Nodes.BuiltIn.Blocks.NumericVectorOutput;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.NumericVectorOutput;

public sealed class NumericVectorOutputBlockTests
{
    [Fact]
    public void Definition_UsesOneNumericVectorInput()
    {
        var port = Assert.Single(NumericVectorOutputBlock.Definition.Ports);

        Assert.Equal(NumericVectorOutputBlock.InputPortId, port.Id);
        Assert.Equal(PortDirection.Input, port.Direction);
        Assert.Equal(PortDataKind.FastStream, port.DataKind);
        Assert.Equal(PortDataSchema.NumericVector1D, port.DataSchema);
    }

    [Fact]
    public async Task OnPacketAsync_CapturesLatestVectorSnapshot()
    {
        var node = new NumericVectorOutputNode();
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        var values = Enumerable.Range(0, 16).Select(static value => (double)value).ToArray();
        var vector = new NumericVectorFrame(
            Name: "fft",
            Values: values.AsMemory(),
            SequenceNumber: 4,
            Timestamp: DateTimeOffset.UnixEpoch);

        await node.OnPacketAsync(NumericVectorOutputBlock.InputPortId, vector, CancellationToken.None);

        var snapshot = Assert.IsType<NumericVectorOutputSnapshot>(node.LatestSnapshot);
        Assert.Equal("fft", snapshot.Name);
        Assert.Equal(16, snapshot.SourceLength);
        Assert.Equal(16, snapshot.Values.Length);
        Assert.Equal(4, snapshot.SequenceNumber);
        Assert.True(snapshot.Values.Equals(values.AsMemory()));
    }

    [Fact]
    public async Task OnPacketAsync_DownsamplesWhenVectorExceedsMaximum()
    {
        var node = new NumericVectorOutputNode();
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        var values = Enumerable.Range(0, NumericVectorOutputNode.MaximumValues + 10)
            .Select(static value => (double)value)
            .ToArray();
        var vector = new NumericVectorFrame(
            Name: "fft",
            Values: values.AsMemory(),
            SequenceNumber: 5);

        await node.OnPacketAsync(NumericVectorOutputBlock.InputPortId, vector, CancellationToken.None);

        var snapshot = Assert.IsType<NumericVectorOutputSnapshot>(node.LatestSnapshot);
        Assert.Equal(NumericVectorOutputNode.MaximumValues, snapshot.Values.Length);
    }

    [Fact]
    public async Task OnPacketAsync_RejectsNonVectorPackets()
    {
        var node = new NumericVectorOutputNode();
        await node.InitializeAsync(new RecordingNodeContext(), CancellationToken.None);
        var payload = System.Text.Json.JsonSerializer.SerializeToElement(new { value = 1 });
        var message = new JsonMessage("test", payload, DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await node.OnPacketAsync(NumericVectorOutputBlock.InputPortId, message, CancellationToken.None));
    }
}
