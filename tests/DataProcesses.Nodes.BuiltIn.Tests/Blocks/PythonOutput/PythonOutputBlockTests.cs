using System.Text.Json;
using DataProcesses.Nodes.BuiltIn.Blocks.PythonOutput;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.PythonOutput;

public sealed class PythonOutputBlockTests
{
    [Fact]
    public void Definition_AcceptsFastStreamAndJsonMessageAndEmitsStatus()
    {
        var ports = PythonOutputBlock.Definition.Ports;

        Assert.Collection(
            ports,
            fastStream =>
            {
                Assert.Equal(PythonOutputBlock.FastStreamInputPortId, fastStream.Id);
                Assert.Equal(PortDirection.Input, fastStream.Direction);
                Assert.Equal(PortDataKind.FastStream, fastStream.DataKind);
            },
            jsonMessage =>
            {
                Assert.Equal(PythonOutputBlock.JsonMessageInputPortId, jsonMessage.Id);
                Assert.Equal(PortDirection.Input, jsonMessage.Direction);
                Assert.Equal(PortDataKind.JsonMessage, jsonMessage.DataKind);
            },
            status =>
            {
                Assert.Equal(PythonOutputBlock.StatusOutputPortId, status.Id);
                Assert.Equal(PortDirection.Output, status.Direction);
                Assert.Equal(PortDataKind.JsonMessage, status.DataKind);
            });
    }

    [Fact]
    public async Task OnPacketAsync_RecordsReceiptAndEmitsDeferredStatus()
    {
        var context = new RecordingNodeContext();
        var node = new PythonOutputNode();
        await node.InitializeAsync(context, CancellationToken.None);
        var payload = JsonSerializer.SerializeToElement(new { value = 42 });
        var input = new JsonMessage("experiment.event", payload, DateTimeOffset.UtcNow, "correlation-1");

        await node.OnPacketAsync(PythonOutputBlock.JsonMessageInputPortId, input, CancellationToken.None);

        var receipt = Assert.IsType<PythonOutputReceipt>(node.LastReceipt);
        Assert.Equal(PythonOutputBlock.JsonMessageInputPortId, receipt.InputPortId);
        Assert.Equal(PortDataKind.JsonMessage, receipt.DataKind);
        var emitted = Assert.Single(context.EmittedPackets);
        Assert.Equal(PythonOutputBlock.StatusOutputPortId, emitted.OutputPortId);
        var status = Assert.IsType<JsonMessage>(emitted.Packet);
        Assert.Equal("dataprocesses.python-output.received", status.Topic);
        Assert.Equal("deferred", status.Payload.GetProperty("delivery").GetString());
    }
}
