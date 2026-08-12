using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
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
    private readonly Dictionary<Guid, Avalonia.Vector> manualTextOffsets = [];
    private readonly HashSet<Guid> restoringManualOffsets = [];
    private const double OffsetEpsilon = 0.5;

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

    private void ContentTextPresenterSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (sender is not TextBlock textPresenter
            || textPresenter.DataContext is not DashboardWidgetViewModel widget)
        {
            return;
        }

        var scrollViewer = FindPresenterScrollViewer(textPresenter);
        if (scrollViewer is null)
        {
            return;
        }

        if (!widget.IsAutoScrollEnabled)
        {
            if (manualTextOffsets.TryGetValue(widget.Id, out var manualOffset))
            {
                if (Math.Abs(scrollViewer.Offset.X - manualOffset.X) > OffsetEpsilon
                    || Math.Abs(scrollViewer.Offset.Y - manualOffset.Y) > OffsetEpsilon)
                {
                    Debug.WriteLine($"[Dashboard][AutoScroll] Offset drift detected while disabled: widgetId={widget.Id}, current=({scrollViewer.Offset.X:0.###},{scrollViewer.Offset.Y:0.###}), manual=({manualOffset.X:0.###},{manualOffset.Y:0.###})");
                }

                RestoreManualOffset(scrollViewer, textPresenter, widget.Id);
            }
            else
            {
                manualTextOffsets[widget.Id] = scrollViewer.Offset;
            }

            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (textPresenter.DataContext is not DashboardWidgetViewModel currentWidget
                || !currentWidget.IsAutoScrollEnabled)
            {
                return;
            }

            var currentScrollViewer = FindPresenterScrollViewer(textPresenter);
            if (currentScrollViewer is not null)
            {
                var maxVerticalOffset = Math.Max(0, currentScrollViewer.Extent.Height - currentScrollViewer.Viewport.Height);
                currentScrollViewer.Offset = new Avalonia.Vector(currentScrollViewer.Offset.X, maxVerticalOffset);
                manualTextOffsets[currentWidget.Id] = currentScrollViewer.Offset;
            }
        }, DispatcherPriority.Background);
    }

    private void ContentScrollViewerPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer
            || scrollViewer.DataContext is not DashboardWidgetViewModel widget
            || widget.IsAutoScrollEnabled)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            manualTextOffsets[widget.Id] = scrollViewer.Offset;
            Debug.WriteLine($"[Dashboard][AutoScroll] Manual offset updated by wheel: widgetId={widget.Id}, x={scrollViewer.Offset.X:0.###}, y={scrollViewer.Offset.Y:0.###}");
        }, DispatcherPriority.Background);
    }

    private void ContentScrollViewerScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer
            || scrollViewer.DataContext is not DashboardWidgetViewModel widget
            || widget.IsAutoScrollEnabled)
        {
            return;
        }

        if (restoringManualOffsets.Contains(widget.Id))
        {
            return;
        }

        if (!manualTextOffsets.TryGetValue(widget.Id, out var manualOffset))
        {
            manualTextOffsets[widget.Id] = scrollViewer.Offset;
            Debug.WriteLine($"[Dashboard][AutoScroll] Manual offset initialized on ScrollChanged: widgetId={widget.Id}, x={scrollViewer.Offset.X:0.###}, y={scrollViewer.Offset.Y:0.###}");
            return;
        }

        if (Math.Abs(scrollViewer.Offset.X - manualOffset.X) <= OffsetEpsilon
            && Math.Abs(scrollViewer.Offset.Y - manualOffset.Y) <= OffsetEpsilon)
        {
            return;
        }

        Debug.WriteLine($"[Dashboard][AutoScroll] ScrollChanged drift correction: widgetId={widget.Id}, current=({scrollViewer.Offset.X:0.###},{scrollViewer.Offset.Y:0.###}), manual=({manualOffset.X:0.###},{manualOffset.Y:0.###}), extentDelta=({e.ExtentDelta.X:0.###},{e.ExtentDelta.Y:0.###})");

        restoringManualOffsets.Add(widget.Id);
        try
        {
            ApplyManualOffset(scrollViewer, manualOffset);
        }
        finally
        {
            restoringManualOffsets.Remove(widget.Id);
        }
    }

    private void AutoScrollToggleClicked(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control toggleSurface
            || toggleSurface.DataContext is not DashboardWidgetViewModel widget)
        {
            Debug.WriteLine("[Dashboard][AutoScroll] Toggle click received but DataContext was not a DashboardWidgetViewModel.");
            return;
        }

        Debug.WriteLine($"[Dashboard][AutoScroll] Toggle click: widgetId={widget.Id}, title={widget.Title}, before={widget.IsAutoScrollEnabled}");

        widget.ToggleAutoScroll();

        if (!widget.IsAutoScrollEnabled)
        {
            var scrollViewer = FindVisibleScrollViewerForWidget(widget.Id);
            if (scrollViewer is not null)
            {
                manualTextOffsets[widget.Id] = scrollViewer.Offset;
                Debug.WriteLine($"[Dashboard][AutoScroll] Manual offset captured: widgetId={widget.Id}, x={scrollViewer.Offset.X:0.###}, y={scrollViewer.Offset.Y:0.###}");
            }
            else
            {
                Debug.WriteLine($"[Dashboard][AutoScroll] ScrollViewer not found while disabling: widgetId={widget.Id}");
            }
        }

        Debug.WriteLine($"[Dashboard][AutoScroll] Toggle complete: widgetId={widget.Id}, after={widget.IsAutoScrollEnabled}");

        e.Handled = true;
    }

    private static ScrollViewer? FindPresenterScrollViewer(TextBlock textPresenter)
    {
        return textPresenter.FindAncestorOfType<ScrollViewer>();
    }

    private ScrollViewer? FindVisibleScrollViewerForWidget(Guid widgetId)
    {
        return this
            .GetVisualDescendants()
            .OfType<ScrollViewer>()
            .FirstOrDefault(scrollViewer =>
                scrollViewer.IsVisible
                && scrollViewer.DataContext is DashboardWidgetViewModel widget
                && widget.Id == widgetId);
    }

    private static void ApplyManualOffset(ScrollViewer scrollViewer, Avalonia.Vector manualOffset)
    {
        scrollViewer.Offset = new Avalonia.Vector(
            Math.Max(0, manualOffset.X),
            Math.Max(0, manualOffset.Y));
    }

    private void RestoreManualOffset(ScrollViewer scrollViewer, TextBlock textPresenter, Guid widgetId)
    {
        if (!manualTextOffsets.TryGetValue(widgetId, out var manualOffset))
        {
            return;
        }

        ApplyManualOffset(scrollViewer, manualOffset);
        PostManualOffsetRestore(textPresenter, widgetId, DispatcherPriority.Render);
        PostManualOffsetRestore(textPresenter, widgetId, DispatcherPriority.Background);
        PostManualOffsetRestore(textPresenter, widgetId, DispatcherPriority.ContextIdle);
    }

    private void PostManualOffsetRestore(TextBlock textPresenter, Guid widgetId, DispatcherPriority priority)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (textPresenter.DataContext is not DashboardWidgetViewModel currentWidget
                || currentWidget.Id != widgetId
                || currentWidget.IsAutoScrollEnabled)
            {
                return;
            }

            var currentScrollViewer = FindPresenterScrollViewer(textPresenter);
            if (currentScrollViewer is null
                || !manualTextOffsets.TryGetValue(currentWidget.Id, out var manualOffset))
            {
                return;
            }

            ApplyManualOffset(currentScrollViewer, manualOffset);
        }, priority);
    }

    private void CopyTextBoxContentClicked(object? sender, RoutedEventArgs e)
    {
        if (TryGetContextMenuTextBox(sender, out var textBox))
        {
            textBox.Copy();
        }
    }

    private void CopyAllTextBoxContentClicked(object? sender, RoutedEventArgs e)
    {
        if (!TryGetContextMenuTextBox(sender, out var textBox))
        {
            return;
        }

        var selectionStart = textBox.SelectionStart;
        var selectionEnd = textBox.SelectionEnd;

        textBox.SelectAll();
        textBox.Copy();
        textBox.SelectionStart = selectionStart;
        textBox.SelectionEnd = selectionEnd;
    }

    private void SelectAllTextBoxContentClicked(object? sender, RoutedEventArgs e)
    {
        if (TryGetContextMenuTextBox(sender, out var textBox))
        {
            textBox.SelectAll();
        }
    }

    private static bool TryGetContextMenuTextBox(object? sender, out TextBox textBox)
    {
        if (sender is MenuItem { Parent: ContextMenu { PlacementTarget: TextBox targetTextBox } })
        {
            textBox = targetTextBox;
            return true;
        }

        textBox = null!;
        return false;
    }

    private void TriggerSurfacePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not StyledElement { DataContext: DashboardWidgetViewModel widget }
            || DataContext is not DashboardViewModel viewModel)
        {
            return;
        }

        viewModel.RequestTriggerByNodeId(widget.SourcePortId);
        e.Handled = true;
    }
}
