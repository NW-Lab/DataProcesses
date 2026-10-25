using System.Net;
using System.Net.Sockets;
using System.Text.Json;

using DataProcesses.Nodes.BuiltIn.Blocks.MqttIn;
using DataProcesses.Plugin.Abstractions;

using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.MqttIn;

public sealed class MqttInBlockTests
{
    [Fact]
    public void Definition_DeclaresJsonMessageOutput()
    {
        var output = Assert.Single(MqttInBlock.Definition.Ports);

        Assert.Equal("dataprocesses.input.mqtt-in", MqttInBlock.TypeId);
        Assert.Equal(MqttInBlock.OutputPortId, output.Id);
        Assert.Equal(PortDirection.Output, output.Direction);
        Assert.Equal(PortDataKind.JsonMessage, output.DataKind);
        Assert.Equal(PortDataSchema.JsonEnvelope, output.DataSchema);
    }

    [Fact]
    public void Settings_ParsesAndValidatesBrokerConfiguration()
    {
        var settings = MqttInSettings.FromJson(
            """{"brokerHost":"mqtt.example","port":8883,"useTls":true,"topic":"sensors/+/state","qualityOfService":2}""");

        Assert.Equal("mqtt.example", settings.BrokerHost);
        Assert.Equal(8883, settings.Port);
        Assert.True(settings.UseTls);
        Assert.Equal("sensors/+/state", settings.Topic);
        Assert.Equal(2, settings.QualityOfService);
        Assert.Throws<ArgumentOutOfRangeException>(() => MqttInSettings.FromJson("""{"port":70000}"""));
        Assert.Throws<ArgumentOutOfRangeException>(() => MqttInSettings.FromJson("""{"qualityOfService":3}"""));
    }

    [Fact]
    public async Task StartAsync_EmitsPublishedJsonMessageWithBrokerTopic()
    {
        var port = GetFreePort();
        var serverFactory = new MqttServerFactory();
        var serverOptions = serverFactory.CreateServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(port)
            .Build();
        using var server = serverFactory.CreateMqttServer(serverOptions);
        await server.StartAsync();

        var clientFactory = new MqttClientFactory();
        using var publisher = clientFactory.CreateMqttClient();
        await publisher.ConnectAsync(new MqttClientOptionsBuilder()
            .WithTcpServer("127.0.0.1", port)
            .WithClientId("mqtt-in-test-publisher")
            .Build());

        var settings = MqttInSettings.Default with
        {
            Port = port,
            Topic = "sensors/temperature",
            ClientId = "mqtt-in-test-subscriber",
            QualityOfService = 1,
        };
        var context = new CapturingNodeContext();
        var node = new MqttInNode("mqtt-in-test", settings);
        await node.InitializeAsync(context, CancellationToken.None);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var startTask = node.StartAsync(cancellation.Token).AsTask();

        try
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic("sensors/temperature")
                .WithPayload("""{"value":21.5}""")
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(true)
                .Build();
            await publisher.PublishAsync(message);

            var emitted = await context.EmittedPacket.WaitAsync(TimeSpan.FromSeconds(5));
            var jsonMessage = Assert.IsType<JsonMessage>(emitted.Packet);
            Assert.Equal(MqttInBlock.OutputPortId, emitted.PortId);
            Assert.Equal("sensors/temperature", jsonMessage.Topic);
            Assert.Equal(JsonValueKind.Object, jsonMessage.Payload.ValueKind);
            Assert.Equal(21.5, jsonMessage.Payload.GetProperty("value").GetDouble());
            Assert.True(jsonMessage.Timestamp > DateTimeOffset.MinValue);
        }
        finally
        {
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => startTask);
            await node.StopAsync(CancellationToken.None);
            if (publisher.IsConnected)
            {
                await publisher.DisconnectAsync();
            }

            await server.StopAsync();
        }
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private sealed class CapturingNodeContext : INodeContext
    {
        private readonly TaskCompletionSource<(string PortId, IDataPacket Packet)> completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string NodeId => "mqtt-in-test";

        public Task<(string PortId, IDataPacket Packet)> EmittedPacket => completion.Task;

        public ValueTask EmitAsync(string outputPortId, IDataPacket packet, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            completion.TrySetResult((outputPortId, packet));
            return ValueTask.CompletedTask;
        }
    }
}
