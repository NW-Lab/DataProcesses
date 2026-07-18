using System.Collections.ObjectModel;

using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Desktop.ViewModels;

public sealed class NodePaletteViewModel : ViewModelBase
{
    private readonly IReadOnlyList<PaletteNodeViewModel> allNodes;
    private string searchText = string.Empty;

    public NodePaletteViewModel(IEnumerable<INodeFactory> factories)
    {
        ArgumentNullException.ThrowIfNull(factories);

        allNodes = factories
            .OrderBy(static factory => factory.Definition.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static factory => factory.Definition.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(static factory => new PaletteNodeViewModel(factory))
            .ToArray();

        RefreshFilteredNodes();
    }

    public ObservableCollection<PaletteNodeViewModel> FilteredNodes { get; } = [];

    public string SearchText
    {
        get => searchText;
        set
        {
            if (SetProperty(ref searchText, value))
            {
                RefreshFilteredNodes();
            }
        }
    }

    private void RefreshFilteredNodes()
    {
        FilteredNodes.Clear();

        foreach (var node in allNodes)
        {
            if (MatchesSearch(node))
            {
                FilteredNodes.Add(node);
            }
        }
    }

    private bool MatchesSearch(PaletteNodeViewModel node)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        return node.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || node.Category.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || node.TypeId.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }
}