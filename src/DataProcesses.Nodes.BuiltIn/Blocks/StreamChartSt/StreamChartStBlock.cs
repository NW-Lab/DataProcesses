using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamChartSt;

/// <summary>
/// Declares the stable identity and multi-channel Fast Stream input contract for the StreamChartSt Block.
/// </summary>
public static class StreamChartStBlock
{
    public const string TypeId = "dataprocesses.output.stream-chart-st";
    public const string IconPath = "Blocks/StreamChartSt/icon.png";
    public const int MaxStreamInputs = 4;

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "StreamChartSt",
        Category: "Output",
        Version: "0.1.0",
        Ports: CreateInputPorts(),
        NodeType: NodeType.Output,
        Title: "StreamChartSt",
        Subtitle: "Time-series Chart",
        IconPath: IconPath,
        DashboardWidget: new DashboardWidgetDefinition(
            IsVisibleByDefault: true,
            GridWidth: 3,
            GridHeight: 3));

    public static string GetStreamPortId(int channelIndex)
    {
        if (channelIndex < 1 || channelIndex > MaxStreamInputs)
        {
            throw new ArgumentOutOfRangeException(
                nameof(channelIndex),
                $"Channel index must be between 1 and {MaxStreamInputs}.");
        }

        return $"stream-{channelIndex}";
    }

    public static bool TryGetChannelIndex(string portId, out int channelIndex)
    {
        channelIndex = 0;
        if (string.IsNullOrWhiteSpace(portId))
        {
            return false;
        }

        for (var index = 1; index <= MaxStreamInputs; index++)
        {
            if (string.Equals(portId, GetStreamPortId(index), StringComparison.Ordinal)
                || string.Equals(portId, $"st-{index}", StringComparison.Ordinal)
                || string.Equals(portId, $"input-{index}", StringComparison.Ordinal))
            {
                channelIndex = index;
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyList<PortDefinition> CreateInputPorts()
    {
        var ports = new List<PortDefinition>(MaxStreamInputs);
        for (var index = 1; index <= MaxStreamInputs; index++)
        {
            ports.Add(new PortDefinition(
                Id: GetStreamPortId(index),
                DisplayName: $"Stream {index}",
                Direction: PortDirection.Input,
                DataKind: PortDataKind.FastStream,
                IsRequired: false,
                DataSchema: PortDataSchema.TimeSeries1D));
        }

        return ports;
    }
}
