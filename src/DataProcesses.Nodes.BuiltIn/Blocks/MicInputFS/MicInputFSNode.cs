using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.MicInputFS;

/// <summary>
/// Captures one microphone block and emits it as a sample-rate-aware Fast Stream frame.
/// </summary>
public sealed class MicInputFSNode : INode
{
    private readonly MicInputFSSettings settings;
    private readonly Func<MicInputFSSettings, CancellationToken, ValueTask<MicInputFSCapturedAudio>> captureAudioAsync;
    private readonly Func<long>? getNextSequenceNumber;
    private INodeContext? context;
    private long sequenceNumber;

    public MicInputFSNode(
        MicInputFSSettings settings,
        Func<MicInputFSSettings, CancellationToken, ValueTask<MicInputFSCapturedAudio>>? captureAudioAsync = null,
        Func<long>? getNextSequenceNumber = null)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.settings.Validate();
        this.captureAudioAsync = captureAudioAsync ?? MicInputFSCapture.CaptureAsync;
        this.getNextSequenceNumber = getNextSequenceNumber;
    }

    public NodeDefinition Definition => MicInputFSBlock.Definition;

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
        throw new ArgumentException("MicInputFS does not define input ports.", nameof(inputPortId));
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var initializedContext = context
            ?? throw new InvalidOperationException("The node must be initialized before it starts.");
        var audio = await captureAudioAsync(settings, cancellationToken).ConfigureAwait(false);
        ValidateCapturedAudio(audio);

        var outputChannelCount = settings.ChannelMode == MicInputFSChannelMode.Stereo
            ? Math.Min(2, audio.ChannelCount)
            : 1;
        var outputSamples = new ReadOnlyMemory<double>[outputChannelCount];
        var channelNames = new string[outputChannelCount];
        var sourceChannelStart = settings.ChannelMode switch
        {
            MicInputFSChannelMode.Right when audio.ChannelCount > 1 => 1,
            _ => 0,
        };

        for (var outputChannelIndex = 0; outputChannelIndex < outputChannelCount; outputChannelIndex++)
        {
            var sourceChannelIndex = sourceChannelStart + outputChannelIndex;
            var channelSamples = new double[MicInputFSSettings.SamplesPerFrame];
            for (var sampleIndex = 0; sampleIndex < channelSamples.Length; sampleIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                channelSamples[sampleIndex] = audio.SamplesInterleaved.Span[(sampleIndex * audio.ChannelCount) + sourceChannelIndex];
            }

            outputSamples[outputChannelIndex] = channelSamples;
            channelNames[outputChannelIndex] = audio.ChannelCount == 1
                ? "mono"
                : settings.ChannelMode switch
                {
                    MicInputFSChannelMode.Stereo => outputChannelIndex == 0 ? "left" : "right",
                    MicInputFSChannelMode.Left => "left",
                    MicInputFSChannelMode.Right => "right",
                    _ => throw new InvalidOperationException($"Unsupported channel mode '{settings.ChannelMode}'."),
                };
        }

        var frame = new FastStreamFrame(
            StartTimeUnixNanoseconds: audio.StartTimeUnixNanoseconds,
            SamplePeriodNanoseconds: Math.Max(1L, (long)Math.Round(1_000_000_000.0 / audio.SampleRateHertz, MidpointRounding.AwayFromZero)),
            ChannelNames: channelNames,
            Samples: outputSamples,
            SequenceNumber: getNextSequenceNumber?.Invoke() ?? sequenceNumber++,
            SampleRateHertz: audio.SampleRateHertz);

        await initializedContext.EmitAsync(MicInputFSBlock.StreamOutputPortId, frame, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    private static void ValidateCapturedAudio(MicInputFSCapturedAudio audio)
    {
        ArgumentNullException.ThrowIfNull(audio);
        if (audio.SampleRateHertz < MicInputFSSettings.MinimumSampleRateHertz
            || audio.SampleRateHertz > MicInputFSSettings.MaximumSampleRateHertz)
        {
            throw new InvalidDataException("Microphone capture returned an unsupported sample rate.");
        }

        if (audio.ChannelCount < 1 || audio.ChannelCount > 2)
        {
            throw new InvalidDataException("Microphone capture must return one or two channels.");
        }

        if (audio.SamplesInterleaved.Length != MicInputFSSettings.SamplesPerFrame * audio.ChannelCount)
        {
            throw new InvalidDataException("Microphone capture returned an incomplete or malformed audio frame.");
        }
    }
}