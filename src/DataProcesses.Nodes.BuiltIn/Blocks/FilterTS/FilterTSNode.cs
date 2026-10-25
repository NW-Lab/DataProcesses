using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.FilterTS;

/// <summary>
/// Applies configurable cascaded one-pole filters independently to each Fast Stream channel.
/// </summary>
public sealed class FilterTSNode : INode
{
    private readonly FilterTSSettings settings;
    private INodeContext? context;
    private ChannelFilterState[]? channelStates;

    public FilterTSNode(FilterTSSettings settings)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.settings.Validate();
    }

    public NodeDefinition Definition => FilterTSBlock.Definition;

    public ValueTask InitializeAsync(INodeContext nodeContext, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        context = nodeContext ?? throw new ArgumentNullException(nameof(nodeContext));
        return ValueTask.CompletedTask;
    }

    public async ValueTask OnPacketAsync(string inputPortId, IDataPacket packet, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPortId);
        ArgumentNullException.ThrowIfNull(packet);
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(inputPortId, FilterTSBlock.InputPortId, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unknown input port '{inputPortId}'.", nameof(inputPortId));
        }

        if (packet is not FastStreamFrame inputFrame)
        {
            throw new ArgumentException("FilterTS accepts Fast Stream input only.", nameof(packet));
        }

        if (!inputFrame.HasValidTiming)
        {
            throw new ArgumentException("FilterTS requires a positive sample period or sample rate.", nameof(packet));
        }

        var sampleRateHertz = inputFrame.GetSampleRateHertz();
        ValidateFrequenciesForSampleRate(sampleRateHertz);

        var nodeContext = context ?? throw new InvalidOperationException("The node must be initialized before it receives packets.");
        var states = EnsureChannelStates(inputFrame.ChannelCount);
        var filteredChannels = new ReadOnlyMemory<double>[inputFrame.ChannelCount];
        var samplePeriodSeconds = 1.0 / sampleRateHertz;

        for (var channelIndex = 0; channelIndex < inputFrame.ChannelCount; channelIndex++)
        {
            var inputSamples = inputFrame.Samples[channelIndex].Span;
            var filteredSamples = new double[inputSamples.Length];
            var state = states[channelIndex];

            for (var sampleIndex = 0; sampleIndex < inputSamples.Length; sampleIndex++)
            {
                filteredSamples[sampleIndex] = ApplyFilter(inputSamples[sampleIndex], samplePeriodSeconds, state);
            }

            filteredChannels[channelIndex] = filteredSamples;
        }

        var filteredFrame = new FastStreamFrame(
            inputFrame.StartTimeUnixNanoseconds,
            inputFrame.SamplePeriodNanoseconds,
            inputFrame.ChannelNames,
            filteredChannels,
            inputFrame.SequenceNumber,
            inputFrame.SampleRateHertz);

        await nodeContext.EmitAsync(FilterTSBlock.OutputPortId, filteredFrame, cancellationToken);
    }

    public ValueTask StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        channelStates = null;
        return ValueTask.CompletedTask;
    }

    private double ApplyFilter(double sample, double samplePeriodSeconds, ChannelFilterState state)
    {
        return settings.FilterType switch
        {
            FilterTSKind.LowPass => ApplyLowPassCascade(sample, settings.CutoffFrequencyHertz, samplePeriodSeconds, state.LowPass),
            FilterTSKind.HighPass => ApplyHighPassCascade(sample, settings.CutoffFrequencyHertz, samplePeriodSeconds, state.HighPass),
            FilterTSKind.BandPass => ApplyLowPassCascade(
                ApplyHighPassCascade(sample, settings.LowerCutoffFrequencyHertz, samplePeriodSeconds, state.BandPassHighPass),
                settings.UpperCutoffFrequencyHertz,
                samplePeriodSeconds,
                state.BandPassLowPass),
            FilterTSKind.BandStop => ApplyLowPassCascade(sample, settings.LowerCutoffFrequencyHertz, samplePeriodSeconds, state.BandStopLowPass)
                + ApplyHighPassCascade(sample, settings.UpperCutoffFrequencyHertz, samplePeriodSeconds, state.BandStopHighPass),
            _ => throw new InvalidOperationException($"Unsupported filter type '{settings.FilterType}'."),
        };
    }

    private static double ApplyLowPassCascade(double sample, double cutoffFrequencyHertz, double samplePeriodSeconds, OnePoleLowPassState[] states)
    {
        var value = sample;
        var alpha = GetLowPassAlpha(cutoffFrequencyHertz, samplePeriodSeconds);
        for (var index = 0; index < states.Length; index++)
        {
            value = states[index].Process(value, alpha);
        }

        return value;
    }

    private static double ApplyHighPassCascade(double sample, double cutoffFrequencyHertz, double samplePeriodSeconds, OnePoleHighPassState[] states)
    {
        var value = sample;
        var alpha = GetHighPassAlpha(cutoffFrequencyHertz, samplePeriodSeconds);
        for (var index = 0; index < states.Length; index++)
        {
            value = states[index].Process(value, alpha);
        }

        return value;
    }

    private void ValidateFrequenciesForSampleRate(double sampleRateHertz)
    {
        var nyquistHertz = sampleRateHertz / 2.0;
        if (settings.FilterType is FilterTSKind.LowPass or FilterTSKind.HighPass)
        {
            if (settings.CutoffFrequencyHertz >= nyquistHertz)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleRateHertz), "FilterTS cutoff frequency must be below Nyquist for the input stream.");
            }

            return;
        }

        if (settings.UpperCutoffFrequencyHertz >= nyquistHertz)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRateHertz), "FilterTS upper cutoff frequency must be below Nyquist for the input stream.");
        }
    }

    private ChannelFilterState[] EnsureChannelStates(int channelCount)
    {
        if (channelStates is null || channelStates.Length != channelCount)
        {
            channelStates = new ChannelFilterState[channelCount];
            for (var index = 0; index < channelStates.Length; index++)
            {
                channelStates[index] = new ChannelFilterState(settings.Order);
            }
        }

        return channelStates;
    }

    private static double GetLowPassAlpha(double cutoffFrequencyHertz, double samplePeriodSeconds)
    {
        var rcSeconds = 1.0 / (2.0 * Math.PI * cutoffFrequencyHertz);
        return samplePeriodSeconds / (rcSeconds + samplePeriodSeconds);
    }

    private static double GetHighPassAlpha(double cutoffFrequencyHertz, double samplePeriodSeconds)
    {
        var rcSeconds = 1.0 / (2.0 * Math.PI * cutoffFrequencyHertz);
        return rcSeconds / (rcSeconds + samplePeriodSeconds);
    }

    private sealed class ChannelFilterState
    {
        public ChannelFilterState(int order)
        {
            LowPass = CreateLowPassStates(order);
            HighPass = CreateHighPassStates(order);
            BandPassHighPass = CreateHighPassStates(order);
            BandPassLowPass = CreateLowPassStates(order);
            BandStopLowPass = CreateLowPassStates(order);
            BandStopHighPass = CreateHighPassStates(order);
        }

        public OnePoleLowPassState[] LowPass { get; }

        public OnePoleHighPassState[] HighPass { get; }

        public OnePoleHighPassState[] BandPassHighPass { get; }

        public OnePoleLowPassState[] BandPassLowPass { get; }

        public OnePoleLowPassState[] BandStopLowPass { get; }

        public OnePoleHighPassState[] BandStopHighPass { get; }

        private static OnePoleLowPassState[] CreateLowPassStates(int count)
        {
            var states = new OnePoleLowPassState[count];
            for (var index = 0; index < states.Length; index++)
            {
                states[index] = new OnePoleLowPassState();
            }

            return states;
        }

        private static OnePoleHighPassState[] CreateHighPassStates(int count)
        {
            var states = new OnePoleHighPassState[count];
            for (var index = 0; index < states.Length; index++)
            {
                states[index] = new OnePoleHighPassState();
            }

            return states;
        }
    }

    private sealed class OnePoleLowPassState
    {
        private bool hasPreviousOutput;
        private double previousOutput;

        public double Process(double sample, double alpha)
        {
            if (!hasPreviousOutput)
            {
                hasPreviousOutput = true;
                previousOutput = sample;
                return sample;
            }

            previousOutput += alpha * (sample - previousOutput);
            return previousOutput;
        }
    }

    private sealed class OnePoleHighPassState
    {
        private bool hasPreviousSample;
        private double previousInput;
        private double previousOutput;

        public double Process(double sample, double alpha)
        {
            if (!hasPreviousSample)
            {
                hasPreviousSample = true;
                previousInput = sample;
                previousOutput = 0.0;
                return 0.0;
            }

            previousOutput = alpha * (previousOutput + sample - previousInput);
            previousInput = sample;
            return previousOutput;
        }
    }
}