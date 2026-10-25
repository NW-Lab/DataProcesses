using System.Net;
using System.Net.Sockets;
using System.Text.Json;

using DataProcesses.Nodes.BuiltIn.Blocks.MqttOut;
using DataProcesses.Plugin.Abstractions;

using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace DataProcesses.Nodes.BuiltIn.Tests.Blocks.MqttOut;

public sealed class MqttOutBlockTests
{
    [Fact]
    public void Definition_DeclaresJsonMessageInput()
    {
        var input = Assert.Single(MqttOutBlock.Definition.Ports);

        Assert.Equal("dataprocesses.output.mqtt-out", MqttOutBlock.TypeId);
        Assert.Equal(MqttOutBlock.InputPortId, input.Id);
        Assert.Equal(PortDirection.Input, input.Direction);
        Assert.Equal(PortDataKind.JsonMessage, input.DataKind);
        Assert.Equal(PortDataSchema.JsonEnvelope, input.DataSchema);
    }

    [Fact]
    public void Settings_ParsesAndValidatesBrokerConfiguration()
    {
        var settings = MqttOutSettings.FromJson(
            """{"brokerHost":"mqtt.example","port":8883,"useTls":true,"qualityOfService":1,"retain":true}""");

        Assert.Equal("mqtt.example", settings.BrokerHost);
        Assert.Equal(8883, settings.Port);
        Assert.True(settings.UseTls);
        Assert.Equal(1, settings.QualityOfService);
        Assert.True(settings.Retain);
        Assert.Throws<ArgumentOutOfRangeException>(() => MqttOutSettings.FromJson("""{"port":0}"""));
        Assert.Throws<ArgumentOutOfRangeException>(() => MqttOutSettings.FromJson("""{"qualityOfService":-1}"""));
    }

    [Fact]
    public async Task OnPacketAsync_PublishesJsonPayloadUsingMessageTopicAndRetain()
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
        using var firstSubscriber = clientFactory.CreateMqttClient();
        var receivedMessage = new TaskCompletionSource<MqttApplicationMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        firstSubscriber.ApplicationMessageReceivedAsync += eventArgs =>
        {
            receivedMessage.TrySetResult(eventArgs.ApplicationMessage);
            return Task.CompletedTask;
        };
        await firstSubscriber.ConnectAsync(new MqttClientOptionsBuilder()
            .WithTcpServer("127.0.0.1", port)
            .WithClientId("mqtt-out-test-subscriber")
            .Build());
        await firstSubscriber.SubscribeAsync(clientFactory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(clientFactory.CreateTopicFilterBuilder()
                .WithTopic("devices/temperature")
                .WithAtLeastOnceQoS()
                .Build())
            .Build());

        var node = new MqttOutNode(
            "mqtt-out-test",
            MqttOutSettings.Default with
            {
                Port = port,
                ClientId = "mqtt-out-test-publisher",
                QualityOfService = 1,
                Retain = true,
            });
        await node.InitializeAsync(new EmptyNodeContext(), CancellationToken.None);
        using var payloadDocument = JsonDocument.Parse("""{"value":21.5,"unit":"C"}""");
        var inputMessage = new JsonMessage(
            "devices/temperature",
            payloadDocument.RootElement.Clone(),
            DateTimeOffset.UnixEpoch,
            "correlation-42");

        try
        {
            await node.OnPacketAsync(MqttOutBlock.InputPortId, inputMessage, CancellationToken.None);

            var published = await receivedMessage.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal("devices/temperature", published.Topic);
            Assert.Equal(MqttQualityOfServiceLevel.AtLeastOnce, published.QualityOfServiceLevel);
            using var publishedPayload = JsonDocument.Parse(published.Payload);
            Assert.Equal(21.5, publishedPayload.RootElement.GetProperty("value").GetDouble());
            Assert.Equal("C", publishedPayload.RootElement.GetProperty("unit").GetString());

            using var retainedSubscriber = clientFactory.CreateMqttClient();
            var retainedMessage = new TaskCompletionSource<MqttApplicationMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            retainedSubscriber.ApplicationMessageReceivedAsync += eventArgs =>
            {
                retainedMessage.TrySetResult(eventArgs.ApplicationMessage);
                return Task.CompletedTask;
            };
            await retainedSubscriber.ConnectAsync(new MqttClientOptionsBuilder()
                .WithTcpServer("127.0.0.1", port)
                .WithClientId("mqtt-out-test-retained-subscriber")
                .Build());
            await retainedSubscriber.SubscribeAsync(clientFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(clientFactory.CreateTopicFilterBuilder()
                    .WithTopic("devices/temperature")
                    .WithAtLeastOnceQoS()
                    .Build())
                .Build());

            var retained = await retainedMessage.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(retained.Retain);
            Assert.Equal("devices/temperature", retained.Topic);
        }
        finally
        {
            await node.StopAsync(CancellationToken.None);
            if (firstSubscriber.IsConnected)
            {
                await firstSubscriber.DisconnectAsync();
            }

            await server.StopAsync();
        }
    }

    [Fact]
    public async Task OnPacketAsync_RejectsNonJsonMessageAndWildcardPublishTopic()
    {
        var node = new MqttOutNode(
            "mqtt-out-validation",
            MqttOutSettings.Default with { Topic = "invalid/+" });
        await node.InitializeAsync(new EmptyNodeContext(), CancellationToken.None);
        var message = new JsonMessage("valid/topic", JsonDocument.Parse("{}").RootElement.Clone(), DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await node.OnPacketAsync(MqttOutBlock.InputPortId, new TestPacket(), CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await node.OnPacketAsync(MqttOutBlock.InputPortId, message, CancellationToken.None));
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private sealed class EmptyNodeContext : INodeContext
    {
        public string NodeId => "mqtt-out-test";

        public ValueTask EmitAsync(string outputPortId, IDataPacket packet, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.CompletedTask;
        }
    }

    private sealed record TestPacket : IDataPacket
    {
        public PortDataKind Kind => PortDataKind.FastStream;
    }
}
