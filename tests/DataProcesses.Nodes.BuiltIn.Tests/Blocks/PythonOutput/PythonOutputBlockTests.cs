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
                Assert.Equal(PortDataSchema.Unspecified, fastStream.DataSchema);
            },
            jsonMessage =>
            {
                Assert.Equal(PythonOutputBlock.JsonMessageInputPortId, jsonMessage.Id);
                Assert.Equal(PortDirection.Input, jsonMessage.Direction);
                Assert.Equal(PortDataKind.JsonMessage, jsonMessage.DataKind);
                Assert.Equal(PortDataSchema.JsonEnvelope, jsonMessage.DataSchema);
            },
            status =>
            {
                Assert.Equal(PythonOutputBlock.StatusOutputPortId, status.Id);
                Assert.Equal(PortDirection.Output, status.Direction);
                Assert.Equal(PortDataKind.JsonMessage, status.DataKind);
                Assert.Equal(PortDataSchema.JsonEnvelope, status.DataSchema);
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

    [Fact]
    public async Task OnPacketAsync_IncludesFastStreamSampleRateInDeferredStatus()
    {
        var context = new RecordingNodeContext();
        var node = new PythonOutputNode();
        await node.InitializeAsync(context, CancellationToken.None);
        var input = new FastStreamFrame(
            StartTimeUnixNanoseconds: 0,
            SamplePeriodNanoseconds: 22_676,
            ChannelNames: ["signal"],
            Samples: [new double[] { 0.0, 1.0 }.AsMemory()],
            SequenceNumber: 1,
            SampleRateHertz: 44_100);

        await node.OnPacketAsync(PythonOutputBlock.FastStreamInputPortId, input, CancellationToken.None);

        var status = Assert.IsType<JsonMessage>(Assert.Single(context.EmittedPackets).Packet);
        var packet = status.Payload.GetProperty("packet");
        Assert.Equal(44_100, packet.GetProperty("SampleRateHertz").GetInt32());
    }
}
