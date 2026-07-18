using System.Collections.ObjectModel;

using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Desktop.ViewModels;

public sealed class NodePaletteViewModel : ViewModelBase
{
    private static readonly IReadOnlyList<NodeType> NodeTypeOrder =
    [
        NodeType.Input,
        NodeType.BasicProcess,
        NodeType.Output,
    ];

    private readonly IReadOnlyList<PaletteNodeViewModel> allNodes;
    private string searchText = string.Empty;

    public NodePaletteViewModel(IEnumerable<INodeFactory> factories)
    {
        ArgumentNullException.ThrowIfNull(factories);

        allNodes = factories
            .OrderBy(static factory => GetNodeTypeSortIndex(factory.Definition.NodeType))
            .ThenBy(static factory => factory.Definition.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(static factory => new PaletteNodeViewModel(factory))
            .ToArray();

        RefreshFilteredNodes();
    }

    public ObservableCollection<PaletteNodeViewModel> FilteredNodes { get; } = [];

    public ObservableCollection<PaletteNodeGroupViewModel> Groups { get; } = [];

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
        Groups.Clear();

        foreach (var node in allNodes)
        {
            if (MatchesSearch(node))
            {
                FilteredNodes.Add(node);
            }
        }

        foreach (var group in FilteredNodes
            .GroupBy(static node => node.NodeType)
            .OrderBy(static group => GetNodeTypeSortIndex(group.Key)))
        {
            Groups.Add(new PaletteNodeGroupViewModel(
                group.Key,
                group.OrderBy(static node => node.DisplayName, StringComparer.OrdinalIgnoreCase)));
        }
    }

    private bool MatchesSearch(PaletteNodeViewModel node)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        return node.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || node.NodeTypeDisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || node.TypeId.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }

    private static int GetNodeTypeSortIndex(NodeType nodeType)
    {
        for (var index = 0; index < NodeTypeOrder.Count; index++)
        {
            if (NodeTypeOrder[index] == nodeType)
            {
                return index;
            }
        }

        return NodeTypeOrder.Count;
    }
}