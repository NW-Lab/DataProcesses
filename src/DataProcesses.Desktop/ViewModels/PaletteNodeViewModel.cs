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
    }

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
            NodeType.Input => "INPUT",
            NodeType.BasicProcess => "Basic Process",
            NodeType.Output => "OUTPUT",
            _ => nodeType.ToString(),
        };
    }
}