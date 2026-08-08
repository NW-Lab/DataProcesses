using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.CsvInput;

/// <summary>
/// Declares the stable identity and port contract for the CSV Input Block.
/// </summary>
public static class CsvInputBlock
{
    public const string TypeId = "dataprocesses.input.csv";
    public const string IconPath = "Blocks/CsvInput/icon.png";
    public const int MaxStreamOutputs = 16;

    public static NodeDefinition Definition { get; } = new(
        TypeId: TypeId,
        DisplayName: "CSV Input",
        Category: "Sources",
        Version: "0.1.0",
        Ports: CreateOutputPorts(),
        NodeType: NodeType.Input,
        Title: "CsvInput",
        Subtitle: "File/COM CSV",
        IconPath: IconPath,
        DashboardWidget: new DashboardWidgetDefinition(
            IsVisibleByDefault: false,
            GridWidth: 2,
            GridHeight: 1));

    public static string GetStreamPortId(int channelIndex)
    {
        if (channelIndex < 1 || channelIndex > MaxStreamOutputs)
        {
            throw new ArgumentOutOfRangeException(nameof(channelIndex));
        }

        return $"stream-{channelIndex}";
    }

    private static IReadOnlyList<PortDefinition> CreateOutputPorts()
    {
        var ports = new List<PortDefinition>(MaxStreamOutputs);
        for (var index = 1; index <= MaxStreamOutputs; index++)
        {
            ports.Add(new PortDefinition(
                Id: GetStreamPortId(index),
                DisplayName: $"Stream{index}",
                Direction: PortDirection.Output,
                DataKind: PortDataKind.FastStream,
                IsRequired: false,
                DataSchema: PortDataSchema.TimeSeries1D));
        }

        return ports;
    }
}
