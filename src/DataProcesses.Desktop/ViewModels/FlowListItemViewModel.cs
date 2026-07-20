namespace DataProcesses.Desktop.ViewModels;

public sealed class FlowListItemViewModel : ViewModelBase
{
    private string name;
    private bool isDirty;

    public FlowListItemViewModel(Guid id, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        this.name = name;
    }

    public Guid Id { get; }

    public string Name
    {
        get => name;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            if (SetProperty(ref name, value))
            {
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    public bool IsDirty
    {
        get => isDirty;
        set
        {
            if (SetProperty(ref isDirty, value))
            {
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    public string DisplayName => IsDirty ? $"{Name} *" : Name;
}
