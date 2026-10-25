using System.Text.Json;

using DataProcesses.Plugin.Abstractions;

using MQTTnet;
using MQTTnet.Exceptions;
using MQTTnet.Protocol;

namespace DataProcesses.Nodes.BuiltIn.Blocks.MqttIn;

public sealed class MqttInNode : INode
{
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);
    private readonly string nodeId;
    private readonly MqttInSettings settings;
    private readonly MqttClientFactory factory = new();
    private INodeContext? context;
    private IMqttClient? client;
    private TaskCompletionSource disconnected = CreateSignal();

    public MqttInNode(string nodeId, MqttInSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        this.nodeId = nodeId;
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.settings.Validate();
    }

    public NodeDefinition Definition => MqttInBlock.Definition;

    public ValueTask InitializeAsync(INodeContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        return ValueTask.CompletedTask;
    }

    public ValueTask OnPacketAsync(string inputPortId, IDataPacket packet, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new InvalidOperationException("MQTT_In does not define input ports.");
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken)
    {
        var initializedContext = context
            ?? throw new InvalidOperationException("The node must be initialized before it starts.");

        client = factory.CreateMqttClient();
        client.ApplicationMessageReceivedAsync += async eventArgs =>
        {
            using var document = JsonDocument.Parse(eventArgs.ApplicationMessage.Payload);
            var message = new JsonMessage(
                eventArgs.ApplicationMessage.Topic,
                document.RootElement.Clone(),
                DateTimeOffset.UtcNow);
            await initializedContext.EmitAsync(MqttInBlock.OutputPortId, message, cancellationToken).ConfigureAwait(false);
        };
        client.DisconnectedAsync += _ =>
        {
            disconnected.TrySetResult();
            return Task.CompletedTask;
        };

        await ConnectAndSubscribeAsync(cancellationToken).ConfigureAwait(false);

        while (true)
        {
            await disconnected.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            disconnected = CreateSignal();

            while (!client.IsConnected)
            {
                await Task.Delay(ReconnectDelay, cancellationToken).ConfigureAwait(false);
                try
                {
                    await ConnectAndSubscribeAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (MqttCommunicationException) when (!cancellationToken.IsCancellationRequested)
                {
                }
            }
        }
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken)
    {
        var currentClient = client;
        client = null;
        if (currentClient is null)
        {
            return;
        }

        if (currentClient.IsConnected)
        {
            await currentClient.DisconnectAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        currentClient.Dispose();
    }

    private async Task ConnectAndSubscribeAsync(CancellationToken cancellationToken)
    {
        var currentClient = client
            ?? throw new InvalidOperationException("MQTT client has not been created.");
        await currentClient.ConnectAsync(CreateClientOptions(), cancellationToken).ConfigureAwait(false);

        var topicFilter = factory.CreateTopicFilterBuilder()
            .WithTopic(settings.Topic)
            .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)settings.QualityOfService)
            .Build();
        var subscribeOptions = factory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(topicFilter)
            .Build();
        await currentClient.SubscribeAsync(subscribeOptions, cancellationToken).ConfigureAwait(false);
    }

    private MqttClientOptions CreateClientOptions()
    {
        var clientId = string.IsNullOrWhiteSpace(settings.ClientId)
            ? $"dataprocesses-in-{nodeId}"
            : settings.ClientId;
        var optionsBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(settings.BrokerHost, settings.Port)
            .WithClientId(clientId);

        if (!string.IsNullOrWhiteSpace(settings.Username))
        {
            optionsBuilder.WithCredentials(settings.Username, settings.Password);
        }

        if (settings.UseTls)
        {
            optionsBuilder.WithTlsOptions(tlsOptions => tlsOptions.UseTls());
        }

        return optionsBuilder.Build();
    }

    private static TaskCompletionSource CreateSignal()
    {
        return new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}