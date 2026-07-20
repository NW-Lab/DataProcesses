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
    private static readonly IBrush GridStrokeBrush = new SolidColorBrush(Color.Parse("#2B4D87"));
    private DashboardWidgetViewModel? draggingWidget;
    private DashboardWidgetViewModel? resizingWidget;
    private Avalonia.Point lastPointerPosition;

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

    private void WidgetPointerPressed(object? sender, PointerPressedEventArgs e)
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
        lastPointerPosition = e.GetPosition(DashboardCanvas);
        viewModel.SelectedWidget = widget;
        e.Pointer.Capture(sender as IInputElement);
        e.Handled = true;
    }

    private void WidgetPointerMoved(object? sender, PointerEventArgs e)
    {
        if (draggingWidget is null || DataContext is not DashboardViewModel viewModel || !viewModel.IsEditMode)
        {
            return;
        }

        var currentPosition = e.GetPosition(DashboardCanvas);
        viewModel.MoveWidgetByPixels(
            draggingWidget,
            currentPosition.X - lastPointerPosition.X,
            currentPosition.Y - lastPointerPosition.Y);
        lastPointerPosition = currentPosition;
        e.Handled = true;
    }

    private void WidgetPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
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
        draggingWidget = null;
        lastPointerPosition = e.GetPosition(DashboardCanvas);
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
        viewModel.ResizeWidgetByPixels(
            resizingWidget,
            currentPosition.X - lastPointerPosition.X,
            currentPosition.Y - lastPointerPosition.Y);
        lastPointerPosition = currentPosition;
        e.Handled = true;
    }

    private void ResizeHandleReleased(object? sender, PointerReleasedEventArgs e)
    {
        resizingWidget = null;
        e.Pointer.Capture(null);
        e.Handled = true;
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
