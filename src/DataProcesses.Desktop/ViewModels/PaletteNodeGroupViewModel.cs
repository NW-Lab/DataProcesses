using System.Collections.ObjectModel;

using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Desktop.ViewModels;

public sealed class PaletteNodeGroupViewModel(NodeType nodeType, IEnumerable<PaletteNodeViewModel> nodes) : ViewModelBase
{
    public NodeType NodeType { get; } = nodeType;

    public string DisplayName => PaletteNodeViewModel.GetNodeTypeDisplayName(NodeType);

    public ObservableCollection<PaletteNodeViewModel> Nodes { get; } = new(nodes);
}