using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.FftSt;

/// <summary>
/// Declares the stable identity and Stream-to-Vector port contract for the FFTst Block.
/// </summary>
public static class FftStBlock
{
    public const string TypeId = "dataprocesses.analysis.fft-st";
    public const string InputPortId = "stream-in";
    public const string OutputPortId = "fft-vector";
    public const string IconPath = "Blocks/FftSt/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "FFTst",
        Category: "Signal Analysis",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(
                InputPortId,
                "Stream In",
                PortDirection.Input,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.TimeSeries1D),
            new PortDefinition(
                OutputPortId,
                "FFT Vector",
                PortDirection.Output,
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.NumericVector1D),
        ],
        NodeType: NodeType.BasicProcess,
        Title: "FFTst",
        Subtitle: "Dashboard spectrum vector",
        IconPath: IconPath,
        DashboardWidget: new DashboardWidgetDefinition(
            IsVisibleByDefault: true,
            GridWidth: 3,
            GridHeight: 2));
}