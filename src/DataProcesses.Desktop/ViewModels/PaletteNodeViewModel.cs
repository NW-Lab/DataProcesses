using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Desktop.ViewModels;

public sealed class PaletteNodeViewModel(INodeFactory factory) : ViewModelBase
{
    public INodeFactory Factory { get; } = factory;

    public NodeDefinition Definition => Factory.Definition;

    public string TypeId => Definition.TypeId;

    public string DisplayName => Title;

    public string Title => string.IsNullOrWhiteSpace(Definition.Title) ? Definition.DisplayName : Definition.Title;

    public string Subtitle => Definition.Subtitle ?? string.Empty;

    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

    public string IconPath => ResolveIconPath(Definition.IconPath);

    public bool HasIcon => !string.IsNullOrWhiteSpace(IconPath);

    public string Category => Definition.Category;

    public NodeType NodeType => Definition.NodeType;

    public string NodeTypeDisplayName => GetNodeTypeDisplayName(NodeType);

    public string Version => Definition.Version;

    private static string ResolveIconPath(string? iconPath)
    {
        if (string.IsNullOrWhiteSpace(iconPath))
        {
            return string.Empty;
        }

        var candidate = Path.Combine(AppContext.BaseDirectory, iconPath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(candidate) ? candidate : string.Empty;
    }

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