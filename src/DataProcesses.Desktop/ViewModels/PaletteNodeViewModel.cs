using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Desktop.ViewModels;

public sealed class PaletteNodeViewModel(INodeFactory factory) : ViewModelBase
{
    public INodeFactory Factory { get; } = factory;

    public NodeDefinition Definition => Factory.Definition;

    public string TypeId => Definition.TypeId;

    public string DisplayName => Definition.DisplayName;

    public string Category => Definition.Category;

    public string Version => Definition.Version;
}