using System.Collections.Concurrent;

using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.MicInputFS;

/// <summary>
/// Creates configured runtime instances of the MicInputFS Block.
/// </summary>
public sealed class MicInputFSNodeFactory : IConfiguredNodeFactory
{
    private readonly ConcurrentDictionary<string, SequenceCounter> sequenceCountersByNodeId = new(StringComparer.Ordinal);
    private readonly Func<MicInputFSSettings, CancellationToken, ValueTask<MicInputFSCapturedAudio>>? captureAudioAsync;

    public MicInputFSNodeFactory(
        Func<MicInputFSSettings, CancellationToken, ValueTask<MicInputFSCapturedAudio>>? captureAudioAsync = null)
    {
        this.captureAudioAsync = captureAudioAsync;
    }

    public NodeDefinition Definition => MicInputFSBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return CreateNodeCore(nodeId, MicInputFSSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return CreateNodeCore(nodeId, MicInputFSSettings.FromJson(settingsJson));
    }

    private INode CreateNodeCore(string nodeId, MicInputFSSettings settings)
    {
        var sequenceCounter = sequenceCountersByNodeId.GetOrAdd(nodeId, static _ => new SequenceCounter());
        return new MicInputFSNode(settings, captureAudioAsync, sequenceCounter.Next);
    }

    private sealed class SequenceCounter
    {
        private long nextSequenceNumber;

        public long Next() => Interlocked.Increment(ref nextSequenceNumber) - 1;
    }
}