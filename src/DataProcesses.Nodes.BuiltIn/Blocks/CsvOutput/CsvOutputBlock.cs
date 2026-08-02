using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Nodes.BuiltIn.Blocks.CsvOutput;

/// <summary>
/// Declares the stable identity and Fast Stream sink contract for the CSV Output Block.
/// </summary>
public static class CsvOutputBlock
{
    public const string TypeId = "dataprocesses.output.csv";
    public const string InputPortId = "input";
    public const string IconPath = "Blocks/CsvOutput/icon.png";

    public static readonly NodeDefinition Definition = new(
        TypeId: TypeId,
        DisplayName: "CSV Output",
        Category: "Output",
        Version: "0.1.0",
        Ports:
        [
            new PortDefinition(InputPortId, "Input", PortDirection.Input, PortDataKind.FastStream),
        ],
        NodeType: NodeType.Output,
        Title: "CsvOutput",
        Subtitle: "File sink",
        IconPath: IconPath,
        DashboardWidget: new DashboardWidgetDefinition(
            IsVisibleByDefault: false,
            GridWidth: 2,
            GridHeight: 1));
}
