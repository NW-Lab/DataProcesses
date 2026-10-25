using System.Collections.ObjectModel;

namespace DataProcesses.Desktop.ViewModels;

public sealed class PaletteNodeCategoryViewModel(string displayName, IEnumerable<PaletteNodeViewModel> nodes) : ViewModelBase
{
    public string DisplayName { get; } = displayName;

    public ObservableCollection<PaletteNodeViewModel> Nodes { get; } = new(nodes);
}
