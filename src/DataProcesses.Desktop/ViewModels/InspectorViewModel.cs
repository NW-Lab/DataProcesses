namespace DataProcesses.Desktop.ViewModels;

public sealed class InspectorViewModel : ViewModelBase
{
    private CanvasNodeViewModel? selectedNode;

    public CanvasNodeViewModel? SelectedNode
    {
        get => selectedNode;
        set => SetProperty(ref selectedNode, value);
    }

    public bool HasSelection => SelectedNode is not null;
}