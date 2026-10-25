using System.Buffers.Binary;

#if WINDOWS
using NAudio.CoreAudioApi;
using NAudio.Wave;
#endif

namespace DataProcesses.Nodes.BuiltIn.Blocks.MicInputFS;

internal static class MicInputFSCapture
{
    public static async ValueTask<MicInputFSCapturedAudio> CaptureAsync(
        MicInputFSSettings settings,
        CancellationToken cancellationToken)
    {
#if WINDOWS
        return await CaptureWindowsAsync(settings, cancellationToken).ConfigureAwait(false);
#else
        await Task.CompletedTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        throw new PlatformNotSupportedException("Microphone capture is available only in the Windows desktop application.");
#endif
    }

#if WINDOWS
    private static async ValueTask<MicInputFSCapturedAudio> CaptureWindowsAsync(
        MicInputFSSettings settings,
        CancellationToken cancellationToken)
    {
        var requestedFormat = WaveFormat.CreateIeeeFloatWaveFormat(settings.SampleRateHertz, 2);
        await using var recorder = new WasapiRecorderBuilder()
            .WithFormat(requestedFormat)
            .WithBufferLength(50)
            .WithMmcssThreadPriority("Audio")
            .Build();

        var waveFormat = recorder.WaveFormat;
        if (waveFormat.Encoding != WaveFormatEncoding.IeeeFloat || waveFormat.BitsPerSample != sizeof(float) * 8)
        {
            throw new InvalidDataException($"MicInputFS expected IEEE float audio but the capture format was {waveFormat}.");
        }

        var channelCount = waveFormat.Channels;
        var requiredSampleValues = checked(MicInputFSSettings.SamplesPerFrame * channelCount);
        var samples = new double[requiredSampleValues];
        var samplesWritten = 0;
        var startTimeUnixNanoseconds = GetUnixTimeNanoseconds(DateTimeOffset.UtcNow);

        await foreach (var buffer in recorder.CaptureAsync(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            samplesWritten += CopySamples(
                buffer.Data.Span,
                buffer.Flags.HasFlag(AudioClientBufferFlags.Silent),
                waveFormat.BlockAlign,
                channelCount,
                samples.AsSpan(samplesWritten),
                requiredSampleValues - samplesWritten);

            if (samplesWritten == requiredSampleValues)
            {
                break;
            }
        }

        if (samplesWritten != requiredSampleValues)
        {
            throw new EndOfStreamException("Microphone capture ended before a complete frame was available.");
        }

        return new MicInputFSCapturedAudio(
            waveFormat.SampleRate,
            channelCount,
            samples,
            startTimeUnixNanoseconds);
    }

    private static int CopySamples(
        ReadOnlySpan<byte> source,
        bool isSilent,
        int blockAlign,
        int channelCount,
        Span<double> destination,
        int destinationSampleLimit)
    {
        var framesToCopy = Math.Min(source.Length / blockAlign, destinationSampleLimit / channelCount);
        var samplesToCopy = framesToCopy * channelCount;
        if (isSilent)
        {
            destination[..samplesToCopy].Clear();
            return samplesToCopy;
        }

        for (var sampleIndex = 0; sampleIndex < samplesToCopy; sampleIndex++)
        {
            var byteOffset = sampleIndex * sizeof(float);
            var sampleBits = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(byteOffset, sizeof(float)));
            destination[sampleIndex] = BitConverter.Int32BitsToSingle(sampleBits);
        }

        return samplesToCopy;
    }

    private static long GetUnixTimeNanoseconds(DateTimeOffset timestamp)
    {
        var ticksSinceEpoch = timestamp.UtcDateTime.Ticks - DateTime.UnixEpoch.Ticks;
        return checked(ticksSinceEpoch * 100L);
    }
#endif
}