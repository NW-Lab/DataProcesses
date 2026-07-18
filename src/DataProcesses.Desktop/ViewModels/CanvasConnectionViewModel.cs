using DataProcesses.Core;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Desktop.ViewModels;

public sealed class CanvasConnectionViewModel : ViewModelBase
{
    private const double NodeWidth = 220;
    private const double PortRowHeight = 28;
    private const double HeaderHeight = 46;

    public CanvasConnectionViewModel(
        Connection connection,
        CanvasNodeViewModel sourceNode,
        CanvasPortViewModel sourcePort,
        CanvasNodeViewModel targetNode,
        CanvasPortViewModel targetPort)
    {
        Connection = connection;
        SourceNode = sourceNode;
        SourcePort = sourcePort;
        TargetNode = targetNode;
        TargetPort = targetPort;
    }

    public Connection Connection { get; }

    public CanvasNodeViewModel SourceNode { get; }

    public CanvasPortViewModel SourcePort { get; }

    public CanvasNodeViewModel TargetNode { get; }

    public CanvasPortViewModel TargetPort { get; }

    public PortDataKind DataKind => Connection.DataKind;

    public string KindLabel => DataKind == PortDataKind.FastStream ? "Fast Stream" : "JSON Message";

    public double X1 => SourceNode.X + NodeWidth;

    public double Y1 => SourceNode.Y + HeaderHeight + GetPortIndex(SourceNode.Outputs, SourcePort.Id) * PortRowHeight + 14;

    public double X2 => TargetNode.X;

    public double Y2 => TargetNode.Y + HeaderHeight + GetPortIndex(TargetNode.Inputs, TargetPort.Id) * PortRowHeight + 14;

    public string StrokeColor => DataKind == PortDataKind.FastStream ? "#1D70B8" : "#C45F12";

    public string StrokeDashArray => DataKind == PortDataKind.FastStream ? "" : "6,4";

    public string PathData => FormattableString.Invariant($"M {X1} {Y1} L {X2} {Y2}");

    public void Refresh()
    {
        OnPropertyChanged(nameof(X1));
        OnPropertyChanged(nameof(Y1));
        OnPropertyChanged(nameof(X2));
        OnPropertyChanged(nameof(Y2));
        OnPropertyChanged(nameof(PathData));
    }

    private static int GetPortIndex(IEnumerable<CanvasPortViewModel> ports, string portId)
    {
        var index = 0;
        foreach (var port in ports)
        {
            if (string.Equals(port.Id, portId, StringComparison.Ordinal))
            {
                return index;
            }

            index++;
        }

        return 0;
    }
}