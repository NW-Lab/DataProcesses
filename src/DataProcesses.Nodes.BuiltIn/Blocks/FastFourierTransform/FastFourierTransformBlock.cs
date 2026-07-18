using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.FastFourierTransform;

/// <summary>
/// Declares the stable identity and port contract for the Fast Fourier Transform Block.
/// </summary>
public static class FastFourierTransformBlock
{
    public const string TypeId = "dataprocesses.analysis.fft";
    public const string InputPortId = "input";
    public const string OutputPortId = "spectrum";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "FFT",
        Category: "Signal Processing",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(InputPortId, "Input", PortDirection.Input, PortDataKind.FastStream),
            new PortDefinition(OutputPortId, "Spectrum", PortDirection.Output, PortDataKind.FastStream),
        ]);
}
