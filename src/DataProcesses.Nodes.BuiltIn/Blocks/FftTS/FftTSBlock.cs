using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.FftTS;

/// <summary>
/// Declares the stable identity and Stream-to-Vector port contract for the FFT_TS Block.
/// </summary>
public static class FftTSBlock
{
    public const string TypeId = "dataprocesses.analysis.fft-st";
    public const string InputPortId = "stream-in";
    public const string OutputPortId = "fft-vector";
    public const string IconPath = "Blocks/FftTS/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "FFT_TS",
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
        Title: "FFT_TS",
        Subtitle: "Dashboard spectrum vector",
        IconPath: IconPath,
        DashboardWidget: new DashboardWidgetDefinition(
            IsVisibleByDefault: true,
            GridWidth: 3,
            GridHeight: 2));
}