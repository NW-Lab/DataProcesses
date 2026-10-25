using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.FftFS;

/// <summary>
/// Declares the Fast Stream FFT input and spectrum output contract.
/// </summary>
public static class FftFSBlock
{
    public const string TypeId = "dataprocesses.analysis.fft-fs";
    public const string InputPortId = "input";
    public const string OutputPortId = "spectrum";
    public const string IconPath = "Blocks/FastFourierTransform/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "FFT_FS",
        Category: "Signal Processing",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(
                InputPortId,
                "Input",
                PortDirection.Input,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.FixedRateTimeSeries1D),
            new PortDefinition(
                OutputPortId,
                "Spectrum",
                PortDirection.Output,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.Spectrum1D),
        ],
        NodeType: NodeType.BasicProcess,
        Title: "FFT_FS",
        Subtitle: "Fast Stream Spectrum",
        IconPath: IconPath);
}