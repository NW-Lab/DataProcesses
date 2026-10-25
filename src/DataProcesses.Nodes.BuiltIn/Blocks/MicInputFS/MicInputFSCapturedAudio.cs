namespace DataProcesses.Nodes.BuiltIn.Blocks.MicInputFS;

/// <summary>
/// Interleaved normalized audio samples captured from the selected microphone format.
/// </summary>
/// <param name="SampleRateHertz">The sample rate represented by the samples.</param>
/// <param name="ChannelCount">The number of interleaved capture channels.</param>
/// <param name="SamplesInterleaved">Normalized float-range samples in frame-major channel order.</param>
/// <param name="StartTimeUnixNanoseconds">Timestamp of the first sample.</param>
public sealed record MicInputFSCapturedAudio(
    int SampleRateHertz,
    int ChannelCount,
    ReadOnlyMemory<double> SamplesInterleaved,
    long StartTimeUnixNanoseconds);