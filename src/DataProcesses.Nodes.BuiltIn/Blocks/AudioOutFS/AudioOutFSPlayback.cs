using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;

#if WINDOWS
using NAudio.Wave;
#endif

using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.AudioOutFS;

internal static class AudioOutFSPlayback
{
#if WINDOWS
    private static readonly ConcurrentDictionary<string, AudioOutFSRuntime> RuntimesByNodeId = new(StringComparer.Ordinal);
#endif

    public static void Write(string nodeId, FastStreamFrame frame, AudioOutFSSettings settings)
    {
#if WINDOWS
        var runtime = RuntimesByNodeId.GetOrAdd(nodeId, CreateRuntime);
        runtime.Write(frame, settings);
#else
        throw new PlatformNotSupportedException("Audio output is available only in the Windows desktop application.");
#endif
    }

#if WINDOWS
    private static AudioOutFSRuntime CreateRuntime(string nodeId)
    {
        AudioOutFSRuntime? runtime = null;
        runtime = new AudioOutFSRuntime(() =>
        {
            if (RuntimesByNodeId.TryGetValue(nodeId, out var current) && ReferenceEquals(current, runtime))
            {
                RuntimesByNodeId.TryRemove(nodeId, out _);
            }
        });
        return runtime;
    }

    private sealed class AudioOutFSRuntime(Action onIdle)
    {
        private static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(2);
        private readonly object sync = new();
        private readonly Timer idleTimer = new(static state => ((AudioOutFSRuntime)state!).CloseIfIdle(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        private readonly Action onIdle = onIdle;
        private WasapiPlayer? player;
        private BufferedWaveProvider? playbackBuffer;
        private WaveFormat? playbackFormat;
        private WaveFileWriter? waveFileWriter;
        private string? recordingFolder;
        private string? fileNamePrefix;
        private long lastWriteTimestamp;

        public void Write(FastStreamFrame frame, AudioOutFSSettings settings)
        {
            lock (sync)
            {
                lastWriteTimestamp = Stopwatch.GetTimestamp();
                idleTimer.Change(IdleTimeout, Timeout.InfiniteTimeSpan);

                var sampleRateHertz = frame.SampleRateHertz
                    ?? throw new ArgumentException("AudioOutFS requires FS frames with SampleRateHertz.", nameof(frame));
                var format = WaveFormat.CreateIeeeFloatWaveFormat(sampleRateHertz, frame.ChannelCount);
                EnsurePlayer(format);
                player!.IsMuted = settings.IsMuted;

                var interleavedSamples = InterleaveAndNormalize(frame);
                var outputBytes = new byte[interleavedSamples.Length * sizeof(float)];
                for (var index = 0; index < interleavedSamples.Length; index++)
                {
                    BinaryPrimitives.WriteInt32LittleEndian(
                        outputBytes.AsSpan(index * sizeof(float), sizeof(float)),
                        BitConverter.SingleToInt32Bits(interleavedSamples[index]));
                }

                playbackBuffer!.AddSamples(outputBytes, 0, outputBytes.Length);
                UpdateRecording(settings, format, outputBytes);

                if (player.PlaybackState != PlaybackState.Playing)
                {
                    player.Play();
                }
            }
        }

        private void EnsurePlayer(WaveFormat format)
        {
            if (playbackFormat?.Equals(format) == true)
            {
                return;
            }

            CloseRecording();
            ClosePlayer();
            playbackFormat = format;
            playbackBuffer = new BufferedWaveProvider(format)
            {
                DiscardOnBufferOverflow = true,
            };
            player = new WasapiPlayerBuilder()
                .WithSharedMode()
                .WithLatency(100)
                .WithMmcssThreadPriority("Pro Audio")
                .Build();
            player.Init(playbackBuffer);
        }

        private void UpdateRecording(AudioOutFSSettings settings, WaveFormat format, byte[] audioBytes)
        {
            if (!settings.IsRecording)
            {
                CloseRecording();
                return;
            }

            if (waveFileWriter is not null
                && (!string.Equals(recordingFolder, settings.RecordingFolder, StringComparison.Ordinal)
                    || !string.Equals(fileNamePrefix, settings.FileNamePrefix, StringComparison.Ordinal)
                    || !waveFileWriter.WaveFormat.Equals(format)))
            {
                CloseRecording();
            }

            if (waveFileWriter is null)
            {
                Directory.CreateDirectory(settings.RecordingFolder);
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", System.Globalization.CultureInfo.InvariantCulture);
                var fileName = $"{settings.FileNamePrefix}_{timestamp}_{Guid.NewGuid():N}.wav";
                var filePath = Path.Combine(settings.RecordingFolder, fileName);
                waveFileWriter = new WaveFileWriter(filePath, format);
                recordingFolder = settings.RecordingFolder;
                fileNamePrefix = settings.FileNamePrefix;
            }

            waveFileWriter.Write(audioBytes, 0, audioBytes.Length);
            waveFileWriter.Flush();
        }

        private static float[] InterleaveAndNormalize(FastStreamFrame frame)
        {
            var channelCount = frame.ChannelCount;
            var sampleCount = frame.SampleCount;
            var interleaved = new float[checked(channelCount * sampleCount)];

            for (var sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
            {
                for (var channelIndex = 0; channelIndex < channelCount; channelIndex++)
                {
                    var sample = frame.Samples[channelIndex].Span[sampleIndex];
                    interleaved[(sampleIndex * channelCount) + channelIndex] = double.IsFinite(sample)
                        ? (float)Math.Clamp(sample, -1.0, 1.0)
                        : 0.0f;
                }
            }

            return interleaved;
        }

        private void CloseIfIdle()
        {
            lock (sync)
            {
                var elapsed = Stopwatch.GetElapsedTime(lastWriteTimestamp);
                if (elapsed < IdleTimeout)
                {
                    idleTimer.Change(IdleTimeout - elapsed, Timeout.InfiniteTimeSpan);
                    return;
                }

                CloseRecording();
                ClosePlayer();
                playbackFormat = null;
                idleTimer.Dispose();
                onIdle();
            }
        }

        private void CloseRecording()
        {
            waveFileWriter?.Dispose();
            waveFileWriter = null;
            recordingFolder = null;
            fileNamePrefix = null;
        }

        private void ClosePlayer()
        {
            if (player is not null)
            {
                player.Stop();
                player.Dispose();
                player = null;
            }

            playbackBuffer = null;
        }
    }
#endif
}