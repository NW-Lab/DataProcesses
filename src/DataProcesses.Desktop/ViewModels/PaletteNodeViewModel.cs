using Avalonia.Media.Imaging;

using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Desktop.ViewModels;

public sealed class PaletteNodeViewModel : ViewModelBase
{
    public PaletteNodeViewModel(INodeFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        Factory = factory;
        IconImage = NodeIconLoader.Load(Definition.IconPath);
        InputBadges = PalettePortBadgeViewModel.Create(Definition.Ports, PortDirection.Input);
        OutputBadges = PalettePortBadgeViewModel.Create(Definition.Ports, PortDirection.Output);
    }

    public IReadOnlyList<PalettePortBadgeViewModel> InputBadges { get; }

    public IReadOnlyList<PalettePortBadgeViewModel> OutputBadges { get; }

    public bool HasInputBadges => InputBadges.Count > 0;

    public bool HasOutputBadges => OutputBadges.Count > 0;

    public string PortSummary => $"In: {DescribeBadges(InputBadges)} / Out: {DescribeBadges(OutputBadges)}";

    public string ToolTipText => HasSubtitle ? $"{Title}\n{Subtitle}\n{PortSummary}" : $"{Title}\n{PortSummary}";

    public string AccessibleName => $"{Title}, {PortSummary}";

    public INodeFactory Factory { get; }

    public NodeDefinition Definition => Factory.Definition;

    public string TypeId => Definition.TypeId;

    public string DisplayName => Title;

    public string Title => string.IsNullOrWhiteSpace(Definition.Title) ? Definition.DisplayName : Definition.Title;

    public string Subtitle => Definition.Subtitle ?? string.Empty;

    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

    public string IconPath => NodeIconLoader.ResolvePath(Definition.IconPath);

    public Bitmap? IconImage { get; }

    public bool HasIcon => IconImage is not null;

    public string Category => Definition.Category;

    public NodeType NodeType => Definition.NodeType;

    public string NodeTypeDisplayName => GetNodeTypeDisplayName(NodeType);

    public string Version => Definition.Version;

    public static string GetNodeTypeDisplayName(NodeType nodeType)
    {
        return nodeType switch
        {
            NodeType.Debug => "DEBUG",
            NodeType.Input => "INPUT",
            NodeType.BasicProcess => "Basic Process",
            NodeType.Output => "OUTPUT",
            _ => nodeType.ToString(),
        };
    }

    private static string DescribeBadges(IReadOnlyList<PalettePortBadgeViewModel> badges)
    {
        return badges.Count == 0 ? "none" : string.Join(", ", badges.Select(static badge => badge.Description));
    }
}