using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.Input;

using DataProcesses.Core;

namespace DataProcesses.Desktop.ViewModels;

public sealed class DashboardViewModel : ViewModelBase
{
    public const int GridSizePixels = 100;
    public const int CanvasWidthPixels = 2400;
    public const int CanvasHeightPixels = 1600;

    private DashboardWidgetViewModel? selectedWidget;
    private Guid dashboardId = Guid.NewGuid();
    private string dashboardName = "Default dashboard";
    private bool isEditMode = true;
    private bool isGridVisible = true;
    private int nextDefaultGridX;
    private int nextDefaultGridY;
    private DashboardListItemViewModel? selectedDashboard;
    private readonly List<DashboardDocument> additionalDashboards = [];
    private bool isLoadingDocuments;
    private bool isSaving;
    private bool isApplyingWorkspaceMode;
    private WorkspaceRunMode workspaceMode = WorkspaceRunMode.Edit;

    public DashboardViewModel()
    {
        AddWidgetCommand = new RelayCommand(AddWidget);
        AddDashboardCommand = new RelayCommand(AddDashboard);
        RemoveDashboardCommand = new RelayCommand(RemoveSelectedDashboard);
        ToggleGridCommand = new RelayCommand(() => IsGridVisible = !IsGridVisible);
        ToggleModeCommand = new RelayCommand(ToggleMode);

        var verticalLineCount = (CanvasWidthPixels / GridSizePixels) + 1;
        for (var index = 0; index < verticalLineCount; index++)
        {
            VerticalGridLines.Add(index * GridSizePixels);
        }

        var horizontalLineCount = (CanvasHeightPixels / GridSizePixels) + 1;
        for (var index = 0; index < horizontalLineCount; index++)
        {
            HorizontalGridLines.Add(index * GridSizePixels);
        }

        var initialDashboard = new DashboardListItemViewModel(dashboardId, DashboardName);
        Dashboards.Add(initialDashboard);
        SelectedDashboard = initialDashboard;
    }

    public ObservableCollection<DashboardListItemViewModel> Dashboards { get; } = [];

    public ObservableCollection<DashboardWidgetViewModel> Widgets { get; } = [];

    public ObservableCollection<double> VerticalGridLines { get; } = [];

    public ObservableCollection<double> HorizontalGridLines { get; } = [];

    public IRelayCommand AddWidgetCommand { get; }

    public IRelayCommand AddDashboardCommand { get; }

    public IRelayCommand RemoveDashboardCommand { get; }

    public IRelayCommand ToggleGridCommand { get; }

    public IRelayCommand ToggleModeCommand { get; }

    public DashboardWidgetViewModel? SelectedWidget
    {
        get => selectedWidget;
        set => SetProperty(ref selectedWidget, value);
    }

    public string DashboardName
    {
        get => dashboardName;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            if (SetProperty(ref dashboardName, value) && SelectedDashboard is not null)
            {
                SelectedDashboard.Name = value;
                if (!isLoadingDocuments)
                {
                    MarkCurrentDashboardDirty();
                }
            }
        }
    }

    public DashboardListItemViewModel? SelectedDashboard
    {
        get => selectedDashboard;
        set
        {
            if (ReferenceEquals(selectedDashboard, value))
            {
                return;
            }

            if (!isLoadingDocuments)
            {
                PersistCurrentDashboard();
            }

            if (SetProperty(ref selectedDashboard, value) && value is not null)
            {
                LoadDashboard(value.Id);
            }
        }
    }

    public bool IsEditMode
    {
        get => isEditMode;
        private set => SetProperty(ref isEditMode, value);
    }

    public bool IsGridVisible
    {
        get => isGridVisible;
        set
        {
            if (SetProperty(ref isGridVisible, value) && !isLoadingDocuments && !isApplyingWorkspaceMode)
            {
                MarkCurrentDashboardDirty();
            }
        }
    }

    public string ModeLabel => workspaceMode switch
    {
        WorkspaceRunMode.Edit => "Edit",
        WorkspaceRunMode.Run => "Run",
        WorkspaceRunMode.RunDebug => "Run(Debug)",
        _ => "Edit",
    };

    public void ApplyWorkspaceMode(WorkspaceRunMode mode)
    {
        workspaceMode = mode;
        isApplyingWorkspaceMode = true;

        try
        {
            IsEditMode = mode == WorkspaceRunMode.Edit;
            IsGridVisible = mode != WorkspaceRunMode.Run;
        }
        finally
        {
            isApplyingWorkspaceMode = false;
        }

        OnPropertyChanged(nameof(ModeLabel));
    }

    public void MoveWidgetByPixels(DashboardWidgetViewModel widget, double deltaX, double deltaY)
    {
        ArgumentNullException.ThrowIfNull(widget);

        widget.GridX = SnapPixelsToGrid(widget.PixelX + deltaX);
        widget.GridY = SnapPixelsToGrid(widget.PixelY + deltaY);
        MarkCurrentDashboardDirty();
    }

    public void MoveWidgetFromDrag(DashboardWidgetViewModel widget, int startGridX, int startGridY, double deltaX, double deltaY)
    {
        ArgumentNullException.ThrowIfNull(widget);

        widget.GridX = SnapPixelsToGrid((startGridX * GridSizePixels) + deltaX);
        widget.GridY = SnapPixelsToGrid((startGridY * GridSizePixels) + deltaY);
        MarkCurrentDashboardDirty();
    }

    public void ResizeWidgetByPixels(DashboardWidgetViewModel widget, double deltaWidth, double deltaHeight)
    {
        ArgumentNullException.ThrowIfNull(widget);

        var newGridWidth = SnapPixelsToGrid(widget.PixelWidth + deltaWidth);
        var newGridHeight = SnapPixelsToGrid(widget.PixelHeight + deltaHeight);

        widget.GridWidth = Math.Max(1, newGridWidth);
        widget.GridHeight = Math.Max(1, newGridHeight);
        MarkCurrentDashboardDirty();
    }

    public void ResizeWidgetLeftByPixels(DashboardWidgetViewModel widget, double deltaX)
    {
        ArgumentNullException.ThrowIfNull(widget);

        var requestedLeft = SnapPixelsToGrid(widget.PixelX + deltaX);
        var currentRight = widget.GridX + widget.GridWidth;
        var newGridX = Math.Min(requestedLeft, currentRight - 1);

        widget.GridX = newGridX;
        widget.GridWidth = currentRight - newGridX;
        MarkCurrentDashboardDirty();
    }

    public void ResizeWidgetRightByPixels(DashboardWidgetViewModel widget, double deltaX)
    {
        ArgumentNullException.ThrowIfNull(widget);

        widget.GridWidth = Math.Max(1, SnapPixelsToGrid(widget.PixelWidth + deltaX));
        MarkCurrentDashboardDirty();
    }

    public void ResizeWidgetTopByPixels(DashboardWidgetViewModel widget, double deltaY)
    {
        ArgumentNullException.ThrowIfNull(widget);

        var requestedTop = SnapPixelsToGrid(widget.PixelY + deltaY);
        var currentBottom = widget.GridY + widget.GridHeight;
        var newGridY = Math.Min(requestedTop, currentBottom - 1);

        widget.GridY = newGridY;
        widget.GridHeight = currentBottom - newGridY;
        MarkCurrentDashboardDirty();
    }

    public void ResizeWidgetBottomByPixels(DashboardWidgetViewModel widget, double deltaY)
    {
        ArgumentNullException.ThrowIfNull(widget);

        widget.GridHeight = Math.Max(1, SnapPixelsToGrid(widget.PixelHeight + deltaY));
        MarkCurrentDashboardDirty();
    }

    public void ResizeWidgetLeftFromDrag(DashboardWidgetViewModel widget, int startGridX, int startGridWidth, double deltaX)
    {
        ArgumentNullException.ThrowIfNull(widget);

        var requestedLeft = SnapPixelsToGrid((startGridX * GridSizePixels) + deltaX);
        var currentRight = startGridX + startGridWidth;
        var newGridX = Math.Min(requestedLeft, currentRight - 1);

        widget.GridX = newGridX;
        widget.GridWidth = currentRight - newGridX;
        MarkCurrentDashboardDirty();
    }

    public void ResizeWidgetRightFromDrag(DashboardWidgetViewModel widget, int startGridWidth, double deltaX)
    {
        ArgumentNullException.ThrowIfNull(widget);

        widget.GridWidth = Math.Max(1, SnapPixelsToGrid((startGridWidth * GridSizePixels) + deltaX));
        MarkCurrentDashboardDirty();
    }

    public void ResizeWidgetTopFromDrag(DashboardWidgetViewModel widget, int startGridY, int startGridHeight, double deltaY)
    {
        ArgumentNullException.ThrowIfNull(widget);

        var requestedTop = SnapPixelsToGrid((startGridY * GridSizePixels) + deltaY);
        var currentBottom = startGridY + startGridHeight;
        var newGridY = Math.Min(requestedTop, currentBottom - 1);

        widget.GridY = newGridY;
        widget.GridHeight = currentBottom - newGridY;
        MarkCurrentDashboardDirty();
    }

    public void ResizeWidgetBottomFromDrag(DashboardWidgetViewModel widget, int startGridHeight, double deltaY)
    {
        ArgumentNullException.ThrowIfNull(widget);

        widget.GridHeight = Math.Max(1, SnapPixelsToGrid((startGridHeight * GridSizePixels) + deltaY));
        MarkCurrentDashboardDirty();
    }

    public IReadOnlyList<DashboardDocument> GetDocuments()
    {
        PersistCurrentDashboard();
        return [..additionalDashboards];
    }

    public void MarkAllClean()
    {
        isSaving = true;

        foreach (var dashboard in Dashboards)
        {
            dashboard.IsDirty = false;
        }

        isSaving = false;
    }

    public void LoadDocuments(IReadOnlyList<DashboardDocument> documents)
    {
        ArgumentNullException.ThrowIfNull(documents);

        isLoadingDocuments = true;

        try
        {
            Widgets.Clear();
            SelectedWidget = null;
            nextDefaultGridX = 0;
            nextDefaultGridY = 0;
            additionalDashboards.Clear();
            Dashboards.Clear();
            selectedDashboard = null;

            if (documents.Count == 0)
            {
                dashboardId = Guid.NewGuid();
                DashboardName = "Default dashboard";
                ApplyWorkspaceMode(workspaceMode);
                var defaultDashboard = new DashboardListItemViewModel(dashboardId, DashboardName);
                Dashboards.Add(defaultDashboard);
                SelectedDashboard = defaultDashboard;
                MarkCurrentDashboardClean();
                return;
            }

            additionalDashboards.AddRange(documents);

            foreach (var document in documents)
            {
                Dashboards.Add(new DashboardListItemViewModel(document.Id, document.Name));
            }

            selectedDashboard = Dashboards[0];
            OnPropertyChanged(nameof(SelectedDashboard));
            LoadDashboard(selectedDashboard.Id);
        }
        finally
        {
            isLoadingDocuments = false;
        }
    }

    private void PersistCurrentDashboard()
    {
        var document = new DashboardDocument(
            dashboardId,
            DashboardName,
            Widgets.Select(static widget => new DashboardWidget(
                widget.Id,
                widget.WidgetType,
                widget.GridX,
                widget.GridY,
                widget.GridWidth,
                widget.GridHeight,
                widget.SourceFlowId,
                widget.SourcePortId,
                widget.SettingsJson)).ToArray(),
            ShowGridInEditMode: true,
            ShowGridInRunMode: false,
            GridSizePixels: GridSizePixels);

        var index = additionalDashboards.FindIndex(existing => existing.Id == document.Id);
        if (index >= 0)
        {
            additionalDashboards[index] = document;
        }
        else
        {
            additionalDashboards.Add(document);
        }
    }

    private void LoadDashboard(Guid id)
    {
        var document = additionalDashboards.FirstOrDefault(candidate => candidate.Id == id);
        if (document is null)
        {
            return;
        }

        dashboardId = document.Id;
        DashboardName = document.Name;
        ApplyWorkspaceMode(workspaceMode);

        Widgets.Clear();
        nextDefaultGridX = 0;
        nextDefaultGridY = 0;

        foreach (var widget in document.Widgets)
        {
            Widgets.Add(new DashboardWidgetViewModel(
                widget.Id,
                $"Widget {Widgets.Count + 1}",
                widget.WidgetType,
                widget.GridX,
                widget.GridY,
                widget.GridWidth,
                widget.GridHeight,
                widget.SourceFlowId,
                widget.SourcePortId,
                widget.SettingsJson));
        }
    }

    private void AddDashboard()
    {
        PersistCurrentDashboard();

        dashboardId = Guid.NewGuid();
        dashboardName = $"Dashboard {Dashboards.Count + 1}";
        OnPropertyChanged(nameof(DashboardName));
        ApplyWorkspaceMode(workspaceMode);

        Widgets.Clear();
        SelectedWidget = null;
        nextDefaultGridX = 0;
        nextDefaultGridY = 0;

        PersistCurrentDashboard();
        var item = new DashboardListItemViewModel(dashboardId, dashboardName);
        Dashboards.Add(item);
        selectedDashboard = item;
        OnPropertyChanged(nameof(SelectedDashboard));
        MarkCurrentDashboardClean();
    }

    private void RemoveSelectedDashboard()
    {
        if (Dashboards.Count <= 1 || SelectedDashboard is null)
        {
            return;
        }

        var removedDashboardId = SelectedDashboard.Id;
        var removeIndex = Dashboards.IndexOf(SelectedDashboard);

        isLoadingDocuments = true;
        try
        {
            Dashboards.RemoveAt(removeIndex);
            additionalDashboards.RemoveAll(dashboard => dashboard.Id == removedDashboardId);

            var nextIndex = Math.Max(0, removeIndex - 1);
            SelectedDashboard = Dashboards[nextIndex];
        }
        finally
        {
            isLoadingDocuments = false;
        }
    }

    private static int SnapPixelsToGrid(double pixelValue)
    {
        var snapped = (int)Math.Round(pixelValue / GridSizePixels, MidpointRounding.AwayFromZero);
        return Math.Max(0, snapped);
    }

    private void AddWidget()
    {
        var widgetIndex = Widgets.Count + 1;
        var widget = new DashboardWidgetViewModel(
            Guid.NewGuid(),
            $"Widget {widgetIndex}",
            "dataprocesses.dashboard.time-series",
            nextDefaultGridX,
            nextDefaultGridY,
            2,
            2);

        Widgets.Add(widget);
        SelectedWidget = widget;

        nextDefaultGridX += 2;
        if ((nextDefaultGridX + 2) * GridSizePixels > CanvasWidthPixels)
        {
            nextDefaultGridX = 0;
            nextDefaultGridY += 2;
        }

        MarkCurrentDashboardDirty();
    }

    private void ToggleMode()
    {
        ApplyWorkspaceMode(workspaceMode == WorkspaceRunMode.Edit ? WorkspaceRunMode.Run : WorkspaceRunMode.Edit);
    }

    private void MarkCurrentDashboardDirty()
    {
        if (isLoadingDocuments || isSaving || isApplyingWorkspaceMode || SelectedDashboard is null)
        {
            return;
        }

        SelectedDashboard.IsDirty = true;
    }

    private void MarkCurrentDashboardClean()
    {
        if (SelectedDashboard is null)
        {
            return;
        }

        SelectedDashboard.IsDirty = false;
    }
}
