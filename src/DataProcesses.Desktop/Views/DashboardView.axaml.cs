using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Collections;

using DataProcesses.Desktop.ViewModels;

namespace DataProcesses.Desktop.Views;

public partial class DashboardView : UserControl
{
    private enum ResizeEdge
    {
        None,
        Left,
        Right,
        Top,
        Bottom,
    }

    private static readonly IBrush GridStrokeBrush = new SolidColorBrush(Color.Parse("#2B4D87"));
    private DashboardWidgetViewModel? draggingWidget;
    private DashboardWidgetViewModel? resizingWidget;
    private ResizeEdge resizingEdge;
    private Avalonia.Point dragStartPointerPosition;
    private int dragStartGridX;
    private int dragStartGridY;
    private int dragStartGridWidth;
    private int dragStartGridHeight;

    public DashboardView()
    {
        InitializeComponent();
        BuildGridLines();
    }

    private void BuildGridLines()
    {
        GridCanvas.Children.Clear();

        for (var x = 0; x <= DashboardViewModel.CanvasWidthPixels; x += DashboardViewModel.GridSizePixels)
        {
            var verticalLine = new Line
            {
                StartPoint = new Avalonia.Point(0.5, 0),
                EndPoint = new Avalonia.Point(0.5, DashboardViewModel.CanvasHeightPixels),
                Stroke = GridStrokeBrush,
                StrokeThickness = 1.5,
                StrokeDashArray = new AvaloniaList<double> { 4, 3 },
                Opacity = 0.7,
                IsHitTestVisible = false,
            };

            Canvas.SetLeft(verticalLine, x);
            Canvas.SetTop(verticalLine, 0);
            GridCanvas.Children.Add(verticalLine);
        }

        for (var y = 0; y <= DashboardViewModel.CanvasHeightPixels; y += DashboardViewModel.GridSizePixels)
        {
            var horizontalLine = new Line
            {
                StartPoint = new Avalonia.Point(0, 0.5),
                EndPoint = new Avalonia.Point(DashboardViewModel.CanvasWidthPixels, 0.5),
                Stroke = GridStrokeBrush,
                StrokeThickness = 1.5,
                StrokeDashArray = new AvaloniaList<double> { 4, 3 },
                Opacity = 0.7,
                IsHitTestVisible = false,
            };

            Canvas.SetLeft(horizontalLine, 0);
            Canvas.SetTop(horizontalLine, y);
            GridCanvas.Children.Add(horizontalLine);
        }
    }

    private void MoveHandlePressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: DashboardWidgetViewModel widget })
        {
            return;
        }

        if (DataContext is not DashboardViewModel viewModel || !viewModel.IsEditMode)
        {
            return;
        }

        draggingWidget = widget;
        resizingWidget = null;
        resizingEdge = ResizeEdge.None;
        dragStartPointerPosition = e.GetPosition(DashboardCanvas);
        CaptureDragStart(widget);
        widget.IsInteractionAdornerVisible = true;
        viewModel.SelectedWidget = widget;
        e.Pointer.Capture(sender as IInputElement);
        e.Handled = true;
    }

    private void MoveHandleMoved(object? sender, PointerEventArgs e)
    {
        if (draggingWidget is null || DataContext is not DashboardViewModel viewModel || !viewModel.IsEditMode)
        {
            return;
        }

        var currentPosition = e.GetPosition(DashboardCanvas);
        viewModel.MoveWidgetFromDrag(
            draggingWidget,
            dragStartGridX,
            dragStartGridY,
            currentPosition.X - dragStartPointerPosition.X,
            currentPosition.Y - dragStartPointerPosition.Y);
        e.Handled = true;
    }

    private void MoveHandleReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (draggingWidget is not null)
        {
            draggingWidget.IsInteractionAdornerVisible = false;
        }

        draggingWidget = null;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void ResizeHandlePressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: DashboardWidgetViewModel widget })
        {
            return;
        }

        if (DataContext is not DashboardViewModel viewModel || !viewModel.IsEditMode)
        {
            return;
        }

        resizingWidget = widget;
        resizingEdge = GetResizeEdge((sender as Control)?.Name);
        if (resizingEdge == ResizeEdge.None)
        {
            return;
        }

        draggingWidget = null;
    dragStartPointerPosition = e.GetPosition(DashboardCanvas);
    CaptureDragStart(widget);
    widget.IsInteractionAdornerVisible = true;
        viewModel.SelectedWidget = widget;
        e.Pointer.Capture(sender as IInputElement);
        e.Handled = true;
    }

    private void ResizeHandleMoved(object? sender, PointerEventArgs e)
    {
        if (resizingWidget is null || DataContext is not DashboardViewModel viewModel || !viewModel.IsEditMode)
        {
            return;
        }

        var currentPosition = e.GetPosition(DashboardCanvas);
        var deltaX = currentPosition.X - dragStartPointerPosition.X;
        var deltaY = currentPosition.Y - dragStartPointerPosition.Y;

        switch (resizingEdge)
        {
            case ResizeEdge.Left:
                viewModel.ResizeWidgetLeftFromDrag(resizingWidget, dragStartGridX, dragStartGridWidth, deltaX);
                break;
            case ResizeEdge.Right:
                viewModel.ResizeWidgetRightFromDrag(resizingWidget, dragStartGridWidth, deltaX);
                break;
            case ResizeEdge.Top:
                viewModel.ResizeWidgetTopFromDrag(resizingWidget, dragStartGridY, dragStartGridHeight, deltaY);
                break;
            case ResizeEdge.Bottom:
                viewModel.ResizeWidgetBottomFromDrag(resizingWidget, dragStartGridHeight, deltaY);
                break;
        }

        e.Handled = true;
    }

    private void ResizeHandleReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (resizingWidget is not null)
        {
            resizingWidget.IsInteractionAdornerVisible = false;
        }

        resizingWidget = null;
        resizingEdge = ResizeEdge.None;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void CaptureDragStart(DashboardWidgetViewModel widget)
    {
        dragStartGridX = widget.GridX;
        dragStartGridY = widget.GridY;
        dragStartGridWidth = widget.GridWidth;
        dragStartGridHeight = widget.GridHeight;
    }

    private static ResizeEdge GetResizeEdge(string? name)
    {
        return name switch
        {
            "ResizeLeftHandle" => ResizeEdge.Left,
            "ResizeRightHandle" => ResizeEdge.Right,
            "ResizeTopHandle" => ResizeEdge.Top,
            "ResizeBottomHandle" => ResizeEdge.Bottom,
            _ => ResizeEdge.None,
        };
    }

    private async void ShowWidgetSettingsPlaceholderDialog(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
        {
            return;
        }

        var closeButton = new Button
        {
            Content = "OK",
            Width = 88,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
        };

        var dialog = new Window
        {
            Title = "Widget Settings",
            Width = 460,
            Height = 220,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Dashboard widget settings are not implemented yet.",
                        TextWrapping = TextWrapping.Wrap,
                        FontWeight = FontWeight.SemiBold,
                    },
                    new TextBlock
                    {
                        Text = "Future plan: node Block settings will include dashboard display and widget configuration.",
                        TextWrapping = TextWrapping.Wrap,
                    },
                    closeButton,
                },
            },
        };

        closeButton.Click += (_, _) => dialog.Close();
        await dialog.ShowDialog(owner).ConfigureAwait(true);
    }
}
