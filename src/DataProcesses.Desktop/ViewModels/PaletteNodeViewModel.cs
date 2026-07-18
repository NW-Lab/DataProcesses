using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Desktop.ViewModels;

public sealed class PaletteNodeViewModel(INodeFactory factory) : ViewModelBase
{
    public INodeFactory Factory { get; } = factory;

    public NodeDefinition Definition => Factory.Definition;

    public string TypeId => Definition.TypeId;

    public string DisplayName => Definition.DisplayName;

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