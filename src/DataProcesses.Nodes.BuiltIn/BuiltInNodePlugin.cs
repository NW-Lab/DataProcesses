using DataProcesses.Nodes.BuiltIn.Blocks.FastFourierTransform;
using DataProcesses.Nodes.BuiltIn.Blocks.LowPassFilter;
using DataProcesses.Nodes.BuiltIn.Blocks.PythonOutput;
using DataProcesses.Nodes.BuiltIn.Blocks.TestSignal;
using DataProcesses.Nodes.BuiltIn.Blocks.TimeSeriesDisplay;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn;

/// <summary>
/// Exposes the set of Blocks distributed with the application.
/// </summary>
public sealed class BuiltInNodePlugin : INodePlugin
{
    private static readonly IReadOnlyCollection<INodeFactory> Factories =
    [
        new TestSignalNodeFactory(),
        new LowPassFilterNodeFactory(),
        new FastFourierTransformNodeFactory(),
        new TimeSeriesDisplayNodeFactory(),
        new PythonOutputNodeFactory(),
    ];

    public string Id => "dataprocesses.built-in";

    public string Version => "0.1.0";

    public IReadOnlyCollection<INodeFactory> NodeFactories => Factories;
}
