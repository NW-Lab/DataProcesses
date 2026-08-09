using System.Collections.ObjectModel;

using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Desktop.ViewModels;

public sealed class NodePaletteViewModel : ViewModelBase
{
    private const string FilterAll = "ALL";
    private const string FilterJson = "JSON";
    private const string FilterTimeSeries = "TS";
    private const string FilterVector = "VEC";
    private const string FilterImage = "IMG";

    private static readonly IReadOnlyList<NodeType> NodeTypeOrder =
    [
        NodeType.Debug,
        NodeType.Input,
        NodeType.BasicProcess,
        NodeType.Output,
    ];

    private static readonly IReadOnlyList<string> DataTypeFilterOptions =
    [
        FilterAll,
        FilterJson,
        FilterTimeSeries,
        FilterVector,
        FilterImage,
    ];

    private readonly IReadOnlyList<PaletteNodeViewModel> allNodes;
    private string searchText = string.Empty;
    private string selectedInputDataType = FilterAll;
    private string selectedOutputDataType = FilterAll;

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

    public IReadOnlyList<string> DataTypeOptions => DataTypeFilterOptions;

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

    public string SelectedInputDataType
    {
        get => selectedInputDataType;
        set
        {
            if (SetProperty(ref selectedInputDataType, NormalizeFilter(value)))
            {
                RefreshFilteredNodes();
            }
        }
    }

    public string SelectedOutputDataType
    {
        get => selectedOutputDataType;
        set
        {
            if (SetProperty(ref selectedOutputDataType, NormalizeFilter(value)))
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
            if (MatchesSearch(node) && MatchesPortFilters(node))
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
            || node.Subtitle.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || node.NodeTypeDisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || node.TypeId.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesPortFilters(PaletteNodeViewModel node)
    {
        return MatchesPortFilter(node.Definition.Ports, PortDirection.Input, SelectedInputDataType)
            && MatchesPortFilter(node.Definition.Ports, PortDirection.Output, SelectedOutputDataType);
    }

    private static bool MatchesPortFilter(
        IReadOnlyList<PortDefinition> ports,
        PortDirection direction,
        string filter)
    {
        if (string.Equals(filter, FilterAll, StringComparison.Ordinal))
        {
            return true;
        }

        foreach (var port in ports)
        {
            if (port.Direction == direction && MatchesSchemaFilter(port.DataSchema, filter))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesSchemaFilter(PortDataSchema schema, string filter)
    {
        return filter switch
        {
            FilterJson => schema == PortDataSchema.JsonEnvelope,
            FilterTimeSeries => schema == PortDataSchema.TimeSeries1D,
            FilterVector => schema == PortDataSchema.Spectrum1D
                || schema == PortDataSchema.NumericVector1D
                || schema == PortDataSchema.NumericMatrix2D,
            FilterImage => schema == PortDataSchema.Image2D,
            _ => false,
        };
    }

    private static string NormalizeFilter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return FilterAll;
        }

        foreach (var option in DataTypeFilterOptions)
        {
            if (string.Equals(option, value, StringComparison.OrdinalIgnoreCase))
            {
                return option;
            }
        }

        return FilterAll;
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