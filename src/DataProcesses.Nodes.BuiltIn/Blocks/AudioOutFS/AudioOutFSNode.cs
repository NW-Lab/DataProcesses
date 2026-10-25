using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.AudioOutFS;

/// <summary>
/// Sends mono or stereo Fast Stream frames to the default audio output and optionally records WAV.
/// </summary>
public sealed class AudioOutFSNode : INode
{
    private readonly string nodeId;
    private readonly AudioOutFSSettings settings;
    private readonly Action<string, FastStreamFrame, AudioOutFSSettings> outputAudio;
    private INodeContext? context;

    public AudioOutFSNode(
        string nodeId,
        AudioOutFSSettings settings,
        Action<string, FastStreamFrame, AudioOutFSSettings>? outputAudio = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        this.nodeId = nodeId;
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.settings.Validate();
        this.outputAudio = outputAudio ?? AudioOutFSPlayback.Write;
    }

    public NodeDefinition Definition => AudioOutFSBlock.Definition;

    public ValueTask InitializeAsync(INodeContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        return ValueTask.CompletedTask;
    }

    public ValueTask OnPacketAsync(string inputPortId, IDataPacket packet, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPortId);
        ArgumentNullException.ThrowIfNull(packet);
        cancellationToken.ThrowIfCancellationRequested();

        if (context is null)
        {
            throw new InvalidOperationException("AudioOutFS must be initialized before it receives packets.");
        }

        if (!string.Equals(inputPortId, AudioOutFSBlock.InputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        if (packet is not FastStreamFrame frame)
        {
            throw new ArgumentException("AudioOutFS accepts Fast Stream input only.", nameof(packet));
        }

        ValidateFrame(frame);
        outputAudio(nodeId, frame, settings);
        return ValueTask.CompletedTask;
    }

    public ValueTask StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    private static void ValidateFrame(FastStreamFrame frame)
    {
        if (frame.SampleRateHertz is not int sampleRateHertz)
        {
            throw new ArgumentException("AudioOutFS requires FS frames with SampleRateHertz.", nameof(frame));
        }

        if (frame.ChannelCount is < 1 or > 2)
        {
            throw new ArgumentException("AudioOutFS accepts mono or stereo Fast Stream frames only.", nameof(frame));
        }

        if (sampleRateHertz is < 8_000 or > 192_000)
        {
            throw new ArgumentException("AudioOutFS requires a sample rate between 8000 and 192000 Hz.", nameof(frame));
        }

        if (frame.ChannelNames.Count != frame.ChannelCount)
        {
            throw new ArgumentException("Fast Stream channel names must match the channel count.", nameof(frame));
        }

        var sampleCount = frame.SampleCount;
        for (var channelIndex = 0; channelIndex < frame.ChannelCount; channelIndex++)
        {
            if (frame.Samples[channelIndex].Length != sampleCount)
            {
                throw new ArgumentException("All Fast Stream channels must have the same sample count.", nameof(frame));
            }
        }
    }
}