namespace DataProcesses.Desktop.ViewModels;

public sealed class DashboardWidgetViewModel : ViewModelBase
{
    private const int DefaultGridWidth = 2;
    private const int DefaultGridHeight = 2;

    private int gridX;
    private int gridY;
    private int gridWidth = DefaultGridWidth;
    private int gridHeight = DefaultGridHeight;
    private string? sourceFlowId;
    private string? sourcePortId;
    private string settingsJson = "{}";

    public DashboardWidgetViewModel(
        Guid id,
        string title,
        string widgetType,
        int gridX,
        int gridY,
        int gridWidth,
        int gridHeight,
        string? sourceFlowId = null,
        string? sourcePortId = null,
        string settingsJson = "{}")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(widgetType);
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsJson);

        Id = id;
        Title = title;
        WidgetType = widgetType;
        this.gridX = Math.Max(0, gridX);
        this.gridY = Math.Max(0, gridY);
        this.gridWidth = Math.Max(1, gridWidth);
        this.gridHeight = Math.Max(1, gridHeight);
        this.sourceFlowId = sourceFlowId;
        this.sourcePortId = sourcePortId;
        this.settingsJson = settingsJson;
    }

    public Guid Id { get; }

    public string Title { get; }

    public string WidgetType { get; }

    public string? SourceFlowId
    {
        get => sourceFlowId;
        set => SetProperty(ref sourceFlowId, value);
    }

    public string? SourcePortId
    {
        get => sourcePortId;
        set => SetProperty(ref sourcePortId, value);
    }

    public string SettingsJson
    {
        get => settingsJson;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            SetProperty(ref settingsJson, value);
        }
    }

    public int GridX
    {
        get => gridX;
        set
        {
            if (SetProperty(ref gridX, Math.Max(0, value)))
            {
                OnPropertyChanged(nameof(PixelX));
            }
        }
    }

    public int GridY
    {
        get => gridY;
        set
        {
            if (SetProperty(ref gridY, Math.Max(0, value)))
            {
                OnPropertyChanged(nameof(PixelY));
            }
        }
    }

    public int GridWidth
    {
        get => gridWidth;
        set
        {
            if (SetProperty(ref gridWidth, Math.Max(1, value)))
            {
                OnPropertyChanged(nameof(PixelWidth));
            }
        }
    }

    public int GridHeight
    {
        get => gridHeight;
        set
        {
            if (SetProperty(ref gridHeight, Math.Max(1, value)))
            {
                OnPropertyChanged(nameof(PixelHeight));
            }
        }
    }

    public double PixelX => GridX * DashboardViewModel.GridSizePixels;

    public double PixelY => GridY * DashboardViewModel.GridSizePixels;

    public double PixelWidth => GridWidth * DashboardViewModel.GridSizePixels;

    public double PixelHeight => GridHeight * DashboardViewModel.GridSizePixels;
}
