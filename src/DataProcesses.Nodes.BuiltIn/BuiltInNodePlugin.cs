using DataProcesses.Nodes.BuiltIn.Blocks.FastFourierTransform;
using DataProcesses.Nodes.BuiltIn.Blocks.LowPassFilter;
using DataProcesses.Nodes.BuiltIn.Blocks.PayloadOutput;
using DataProcesses.Nodes.BuiltIn.Blocks.PythonOutput;
using DataProcesses.Nodes.BuiltIn.Blocks.CsvInput;
using DataProcesses.Nodes.BuiltIn.Blocks.CsvOutput;
using DataProcesses.Nodes.BuiltIn.Blocks.ImageOutput;
using DataProcesses.Nodes.BuiltIn.Blocks.NumericVectorOutput;
using DataProcesses.Nodes.BuiltIn.Blocks.StreamOutput;
using DataProcesses.Nodes.BuiltIn.Blocks.TestSignal;
using DataProcesses.Nodes.BuiltIn.Blocks.Trigger;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn;

/// <summary>
/// Exposes the set of Blocks distributed with the application.
/// </summary>
public sealed class BuiltInNodePlugin : INodePlugin
{
    private static readonly IReadOnlyCollection<INodeFactory> Factories =
    [
        new CsvInputNodeFactory(),
        new TestSignalNodeFactory(),
        new TriggerNodeFactory(),
        new LowPassFilterNodeFactory(),
        new FastFourierTransformNodeFactory(),
        new StreamOutputNodeFactory(),
        new NumericVectorOutputNodeFactory(),
        new ImageOutputNodeFactory(),
        new PythonOutputNodeFactory(),
        new PayloadOutputNodeFactory(),
        new CsvOutputNodeFactory(),
    ];

    public string Id => "dataprocesses.built-in";

    public string Version => "0.1.0";

    public IReadOnlyCollection<INodeFactory> NodeFactories => Factories;
}
