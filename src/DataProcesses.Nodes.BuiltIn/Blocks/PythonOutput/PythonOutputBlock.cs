using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.PythonOutput;

/// <summary>
/// Declares the stable identity and transport boundary for the future Python Output Block.
/// The MVP records incoming packets and emits a JSON status message; it does not start a Python
/// process or execute Python code in the host process.
/// </summary>
public static class PythonOutputBlock
{
    public const string TypeId = "dataprocesses.output.python";
    public const string FastStreamInputPortId = "fast-stream";
    public const string JsonMessageInputPortId = "message";
    public const string StatusOutputPortId = "status";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "Python Output",
        Category: "Output",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(FastStreamInputPortId, "Fast Stream", PortDirection.Input, PortDataKind.FastStream, IsRequired: false),
            new PortDefinition(JsonMessageInputPortId, "JSON Message", PortDirection.Input, PortDataKind.JsonMessage, IsRequired: false),
            new PortDefinition(StatusOutputPortId, "Status", PortDirection.Output, PortDataKind.JsonMessage, IsRequired: false),
        ]);
}
