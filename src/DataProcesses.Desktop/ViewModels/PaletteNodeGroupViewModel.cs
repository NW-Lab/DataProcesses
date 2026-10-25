using System.Collections.ObjectModel;

using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Desktop.ViewModels;

public sealed class PaletteNodeGroupViewModel : ViewModelBase
{
    private const string UncategorizedName = "Other";

    private readonly Action<NodeType, bool>? expandedChanged;
    private bool isExpanded;

    public PaletteNodeGroupViewModel(
        NodeType nodeType,
        IEnumerable<PaletteNodeViewModel> nodes,
        bool isExpanded = true,
        Action<NodeType, bool>? expandedChanged = null)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        NodeType = nodeType;
        Nodes = new(nodes);
        this.isExpanded = isExpanded;
        this.expandedChanged = expandedChanged;

        foreach (var category in Nodes
            .GroupBy(static node => GetCategoryName(node.Category), StringComparer.OrdinalIgnoreCase)
            .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            Categories.Add(new PaletteNodeCategoryViewModel(category.Key, category));
        }
    }

    public NodeType NodeType { get; }

    public string DisplayName => PaletteNodeViewModel.GetNodeTypeDisplayName(NodeType);

    public ObservableCollection<PaletteNodeViewModel> Nodes { get; }

    public ObservableCollection<PaletteNodeCategoryViewModel> Categories { get; } = [];

    public int Count => Nodes.Count;

    public bool ShowCategoryHeaders => Categories.Count > 1;

    public bool IsExpanded
    {
        get => isExpanded;
        set
        {
            if (SetProperty(ref isExpanded, value))
            {
                expandedChanged?.Invoke(NodeType, value);
            }
        }
    }

    private static string GetCategoryName(string? category)
    {
        return string.IsNullOrWhiteSpace(category) ? UncategorizedName : category.Trim();
    }
}