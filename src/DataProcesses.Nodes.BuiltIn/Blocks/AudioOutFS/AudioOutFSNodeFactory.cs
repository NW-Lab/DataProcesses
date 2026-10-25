using System.Collections.Concurrent;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.AudioOutFS;

/// <summary>
/// Creates runtime instances while preserving one playback session per placed Block.
/// </summary>
public sealed class AudioOutFSNodeFactory : IConfiguredNodeFactory
{
    private readonly Action<string, FastStreamFrame, AudioOutFSSettings>? outputAudio;

    public AudioOutFSNodeFactory(Action<string, FastStreamFrame, AudioOutFSSettings>? outputAudio = null)
    {
        this.outputAudio = outputAudio;
    }

    public NodeDefinition Definition => AudioOutFSBlock.Definition;

    public INode CreateNode(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return CreateNodeCore(nodeId, AudioOutFSSettings.Default);
    }

    public INode CreateNode(string nodeId, string settingsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return CreateNodeCore(nodeId, AudioOutFSSettings.FromJson(settingsJson));
    }

    private INode CreateNodeCore(string nodeId, AudioOutFSSettings settings)
    {
        return new AudioOutFSNode(nodeId, settings, outputAudio);
    }
}