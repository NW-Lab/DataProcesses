using System.Text;

using DataProcesses.Plugin.Abstractions;

using MQTTnet;
using MQTTnet.Protocol;

namespace DataProcesses.Nodes.BuiltIn.Blocks.MqttOut;

public sealed class MqttOutNode : INode
{
    private readonly string nodeId;
    private readonly MqttOutSettings settings;
    private readonly MqttClientFactory factory = new();
    private readonly SemaphoreSlim publishGate = new(1, 1);
    private IMqttClient? client;
    private bool isInitialized;

    public MqttOutNode(string nodeId, MqttOutSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        this.nodeId = nodeId;
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.settings.Validate();
    }

    public NodeDefinition Definition => MqttOutBlock.Definition;

    public ValueTask InitializeAsync(INodeContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);
        isInitialized = true;
        return ValueTask.CompletedTask;
    }

    public async ValueTask OnPacketAsync(
        string inputPortId,
        IDataPacket packet,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPortId);
        ArgumentNullException.ThrowIfNull(packet);

        if (!isInitialized)
        {
            throw new InvalidOperationException("The node must be initialized before it receives packets.");
        }

        if (!string.Equals(inputPortId, MqttOutBlock.InputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        if (packet is not JsonMessage message)
        {
            throw new ArgumentException("MQTT_Out accepts JSON Message packets only.", nameof(packet));
        }

        var topic = string.IsNullOrWhiteSpace(settings.Topic) ? message.Topic : settings.Topic;
        ValidatePublishTopic(topic);
        var payload = message.Payload.GetRawText();

        await publishGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var mqttClient = await GetConnectedClientAsync(cancellationToken).ConfigureAwait(false);
            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(Encoding.UTF8.GetBytes(payload))
                .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)settings.QualityOfService)
                .WithRetainFlag(settings.Retain)
                .Build();
            await mqttClient.PublishAsync(applicationMessage, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            publishGate.Release();
        }
    }

    public ValueTask StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken)
    {
        await publishGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
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
        finally
        {
            publishGate.Release();
        }
    }

    private async Task<IMqttClient> GetConnectedClientAsync(CancellationToken cancellationToken)
    {
        if (client is { IsConnected: true })
        {
            return client;
        }

        var mqttClient = client ?? factory.CreateMqttClient();
        client = mqttClient;
        await mqttClient.ConnectAsync(CreateClientOptions(), cancellationToken).ConfigureAwait(false);
        return mqttClient;
    }

    private MqttClientOptions CreateClientOptions()
    {
        var clientId = string.IsNullOrWhiteSpace(settings.ClientId)
            ? $"dataprocesses-out-{nodeId}"
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

    private static void ValidatePublishTopic(string topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        if (topic.Contains('#', StringComparison.Ordinal) || topic.Contains('+', StringComparison.Ordinal))
        {
            throw new ArgumentException("MQTT publish topics cannot contain '+' or '#' wildcards.", nameof(topic));
        }
    }
}