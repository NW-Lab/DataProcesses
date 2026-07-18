using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Desktop.ViewModels;

public sealed class CanvasPortViewModel(CanvasNodeViewModel node, PortDefinition definition) : ViewModelBase
{
    public CanvasNodeViewModel Node { get; } = node;

    public PortDefinition Definition { get; } = definition;

    public string Id => Definition.Id;

    public string DisplayName => Definition.DisplayName;

    public PortDirection Direction => Definition.Direction;

    public PortDataKind DataKind => Definition.DataKind;

    public bool IsInput => Direction == PortDirection.Input;

    public bool IsOutput => Direction == PortDirection.Output;

    public bool IsFastStream => DataKind == PortDataKind.FastStream;

    public bool IsJsonMessage => DataKind == PortDataKind.JsonMessage;

    public string KindLabel => DataKind == PortDataKind.FastStream ? "S" : "P";

    public string ShapeClass => DataKind == PortDataKind.FastStream ? "fastStream" : "payload";

    public string DirectionLabel => IsInput ? "Input" : "Output";

    public string AccessibleName => $"{DirectionLabel}: {DisplayName} ({(IsFastStream ? "Fast Stream" : "Payload")})";
}