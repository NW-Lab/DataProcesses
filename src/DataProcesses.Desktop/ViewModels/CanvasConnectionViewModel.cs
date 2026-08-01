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

    public string KindLabel => DataKind == PortDataKind.FastStream ? "Fast Stream" : "Payload";

    public double X1 => SourceNode.X + NodeWidth;

    public double Y1 => SourceNode.Y + HeaderHeight + GetPortIndex(SourceNode.Outputs, SourcePort.Id) * PortRowHeight + 14;

    public double X2 => TargetNode.X;

    public double Y2 => TargetNode.Y + HeaderHeight + GetPortIndex(TargetNode.Inputs, TargetPort.Id) * PortRowHeight + 14;

    public string StrokeColor => DataKind == PortDataKind.FastStream ? "#1D70B8" : "#D92D20";

    public string StrokeDashArray => DataKind == PortDataKind.FastStream ? "" : "6,4";

    public string PathData
    {
        get
        {
            var dx = X2 - X1;
            var curvature = Math.Clamp(Math.Abs(dx) * 0.45, 80, 240);
            var c1x = X1 + curvature;
            var c1y = Y1;
            var c2x = X2 - curvature;
            var c2y = Y2;
            return FormattableString.Invariant($"M {X1} {Y1} C {c1x} {c1y}, {c2x} {c2y}, {X2} {Y2}");
        }
    }

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