using DataProcesses.Desktop.ViewModels;
using DataProcesses.Core;

namespace DataProcesses.Desktop.Tests;

public sealed class DashboardViewModelTests
{
    [Fact]
    public void RemoveDashboard_KeepsAtLeastOneDashboard()
    {
        var viewModel = new DashboardViewModel();

        viewModel.RemoveDashboardCommand.Execute(null);
        Assert.Single(viewModel.Dashboards);

        viewModel.AddDashboardCommand.Execute(null);
        Assert.Equal(2, viewModel.Dashboards.Count);

        viewModel.RemoveDashboardCommand.Execute(null);
        Assert.Single(viewModel.Dashboards);
    }

    [Fact]
    public void DashboardDirtyFlag_SetsOnEdit_AndClearsOnMarkAllClean()
    {
        var viewModel = new DashboardViewModel();

        viewModel.AddWidgetCommand.Execute(null);
        Assert.True(viewModel.Dashboards[0].IsDirty);

        viewModel.MarkAllClean();
        Assert.False(viewModel.Dashboards[0].IsDirty);
    }

    [Fact]
    public void LoadDocuments_TracksMultipleDashboards()
    {
        var viewModel = new DashboardViewModel();
        var dashboards = new[]
        {
            new DashboardDocument(
                Guid.NewGuid(),
                "Main",
                [new DashboardWidget(Guid.NewGuid(), "dataprocesses.dashboard.time-series", 0, 0, 2, 2)]),
            new DashboardDocument(
                Guid.NewGuid(),
                "Secondary",
                [new DashboardWidget(Guid.NewGuid(), "dataprocesses.dashboard.time-series", 2, 1, 3, 2)]),
        };

        viewModel.LoadDocuments(dashboards);

        Assert.Equal(2, viewModel.Dashboards.Count);
        Assert.Equal("Main", Assert.IsType<DashboardListItemViewModel>(viewModel.SelectedDashboard).Name);
        Assert.Single(viewModel.Widgets);

        viewModel.SelectedDashboard = viewModel.Dashboards[1];
        Assert.Equal("Secondary", Assert.IsType<DashboardListItemViewModel>(viewModel.SelectedDashboard).Name);
    }

    [Fact]
    public void LoadDocuments_ReplacesSelectedDashboardWidgetsWhenDocumentsAreReloaded()
    {
        var viewModel = new DashboardViewModel();
        var dashboardId = Guid.NewGuid();

        viewModel.LoadDocuments([new DashboardDocument(dashboardId, "Main", [])]);
        Assert.Empty(viewModel.Widgets);

        viewModel.LoadDocuments([
            new DashboardDocument(
                dashboardId,
                "Main",
                [new DashboardWidget(Guid.NewGuid(), "dataprocesses.dashboard.node-block", 0, 0, 1, 2)]),
        ]);

        var widget = Assert.Single(viewModel.Widgets);
        Assert.Equal("dataprocesses.dashboard.node-block", widget.WidgetType);
        Assert.Equal(1, widget.GridWidth);
        Assert.Equal(2, widget.GridHeight);
    }

    [Fact]
    public void AddDashboardCommand_AddsAnotherDocument()
    {
        var viewModel = new DashboardViewModel();

        viewModel.AddDashboardCommand.Execute(null);

        Assert.Equal(2, viewModel.Dashboards.Count);
        var documents = viewModel.GetDocuments();
        Assert.Equal(2, documents.Count);
    }

    [Fact]
    public void AddDashboardCommand_DoesNotRenameExistingDashboard()
    {
        var viewModel = new DashboardViewModel();

        Assert.Equal("Default dashboard", viewModel.Dashboards[0].Name);

        viewModel.AddDashboardCommand.Execute(null);

        Assert.Equal("Default dashboard", viewModel.Dashboards[0].Name);
        Assert.Equal("Dashboard 2", viewModel.Dashboards[1].Name);
        Assert.Equal("Dashboard 2", viewModel.DashboardName);
    }

    [Fact]
    public void MoveWidgetByPixels_SnapsTo100PixelGrid()
    {
        var viewModel = new DashboardViewModel();
        viewModel.AddWidgetCommand.Execute(null);
        var widget = Assert.Single(viewModel.Widgets);

        viewModel.MoveWidgetByPixels(widget, 149, 151);

        Assert.Equal(1, widget.GridX);
        Assert.Equal(2, widget.GridY);
        Assert.Equal(100, widget.PixelX);
        Assert.Equal(200, widget.PixelY);
    }

    [Fact]
    public void MoveWidgetFromDrag_UsesAccumulatedDragDistance()
    {
        var viewModel = new DashboardViewModel();
        viewModel.AddWidgetCommand.Execute(null);
        var widget = Assert.Single(viewModel.Widgets);

        viewModel.MoveWidgetFromDrag(widget, widget.GridX, widget.GridY, 49, 49);
        Assert.Equal(0, widget.GridX);
        Assert.Equal(0, widget.GridY);

        viewModel.MoveWidgetFromDrag(widget, 0, 0, 151, 151);
        Assert.Equal(2, widget.GridX);
        Assert.Equal(2, widget.GridY);
    }

    [Fact]
    public void ResizeWidgetByPixels_SnapsAndKeepsMinimumOneCell()
    {
        var viewModel = new DashboardViewModel();
        viewModel.AddWidgetCommand.Execute(null);
        var widget = Assert.Single(viewModel.Widgets);

        viewModel.ResizeWidgetByPixels(widget, 130, -400);

        Assert.Equal(3, widget.GridWidth);
        Assert.Equal(1, widget.GridHeight);
        Assert.Equal(300, widget.PixelWidth);
        Assert.Equal(100, widget.PixelHeight);
    }

    [Fact]
    public void ResizeWidgetEdges_AdjustSizeAndPosition()
    {
        var viewModel = new DashboardViewModel();
        viewModel.AddWidgetCommand.Execute(null);
        var widget = Assert.Single(viewModel.Widgets);
        widget.GridX = 2;
        widget.GridY = 2;
        widget.GridWidth = 3;
        widget.GridHeight = 3;

        viewModel.ResizeWidgetLeftByPixels(widget, -100);
        Assert.Equal(1, widget.GridX);
        Assert.Equal(4, widget.GridWidth);

        viewModel.ResizeWidgetRightByPixels(widget, -250);
        Assert.Equal(2, widget.GridWidth);

        viewModel.ResizeWidgetTopByPixels(widget, -100);
        Assert.Equal(1, widget.GridY);
        Assert.Equal(4, widget.GridHeight);

        viewModel.ResizeWidgetBottomByPixels(widget, -250);
        Assert.Equal(2, widget.GridHeight);
    }

    [Fact]
    public void ResizeWidgetEdgesFromDrag_UseAccumulatedDragDistance()
    {
        var viewModel = new DashboardViewModel();
        viewModel.AddWidgetCommand.Execute(null);
        var widget = Assert.Single(viewModel.Widgets);
        widget.GridX = 2;
        widget.GridY = 2;
        widget.GridWidth = 3;
        widget.GridHeight = 3;

        viewModel.ResizeWidgetLeftFromDrag(widget, 2, 3, -49);
        Assert.Equal(2, widget.GridX);
        Assert.Equal(3, widget.GridWidth);

        viewModel.ResizeWidgetLeftFromDrag(widget, 2, 3, -151);
        Assert.Equal(0, widget.GridX);
        Assert.Equal(5, widget.GridWidth);

        viewModel.ResizeWidgetTopFromDrag(widget, 2, 3, -151);
        Assert.Equal(0, widget.GridY);
        Assert.Equal(5, widget.GridHeight);
    }

    [Fact]
    public void ToggleMode_SetsGridDefaultByMode()
    {
        var viewModel = new DashboardViewModel();

        Assert.True(viewModel.IsEditMode);
        Assert.True(viewModel.IsGridVisible);

        viewModel.ToggleModeCommand.Execute(null);

        Assert.False(viewModel.IsEditMode);
        Assert.False(viewModel.IsGridVisible);

        viewModel.ToggleGridCommand.Execute(null);
        Assert.True(viewModel.IsGridVisible);
    }
}
