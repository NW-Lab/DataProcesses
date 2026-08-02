using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform.Storage;

using DataProcesses.Desktop.ViewModels;

using System.Diagnostics;

namespace DataProcesses.Desktop.Views;

public partial class FlowEditorView : UserControl
{
    private CanvasNodeViewModel? draggingNode;
    private PaletteNodeViewModel? draggingPaletteNode;
    private CanvasPortViewModel? draggingPort;
    private Avalonia.Point lastPointerPosition;

    public FlowEditorView()
    {
        InitializeComponent();
        AddHandler(PointerPressedEvent, FlowEditorPointerPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(PointerMovedEvent, FlowEditorPointerMoved, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, FlowEditorPointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private void FlowEditorPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        if (DataContext is FlowEditorViewModel viewModelForConnection && viewModelForConnection.IsCanvasEditingEnabled)
        {
            var point = e.GetCurrentPoint(sender as Control).Properties;
            var pressedPort = FindPortFromSource(e.Source) ?? FindPortAtPointer(e);

            if (point.IsLeftButtonPressed && pressedPort is { IsOutput: true })
            {
                draggingPort = pressedPort;
                viewModelForConnection.StartPendingConnection(pressedPort);

                var position = e.GetPosition(CanvasRoot);
                viewModelForConnection.PreviewConnectionEndX = position.X / viewModelForConnection.Zoom;
                viewModelForConnection.PreviewConnectionEndY = position.Y / viewModelForConnection.Zoom;

                e.Pointer.Capture(this);
                e.Handled = true;
                return;
            }
        }

        if (draggingPaletteNode is not null
            || DataContext is not FlowEditorViewModel viewModel
            || FindPaletteNode(e.Source) is not { } paletteNode)
        {
            return;
        }

        if (!viewModel.IsCanvasEditingEnabled)
        {
            return;
        }

        var properties = e.GetCurrentPoint(sender as Control).Properties;
        if (!properties.IsLeftButtonPressed)
        {
            return;
        }

        if (FindPortFromSource(e.Source) is not null)
        {
            return;
        }

        draggingPaletteNode = paletteNode;
        viewModel.SelectPaletteNodeCommand.Execute(paletteNode);
        PaletteDragPreviewTitle.Text = paletteNode.DisplayName;
        UpdatePaletteDragPreview(e.GetPosition(CanvasRoot));
        Log($"Palette drag started: {paletteNode.DisplayName} ({paletteNode.TypeId})");
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    private void FlowEditorPointerMoved(object? sender, PointerEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        if (DataContext is not FlowEditorViewModel viewModel)
        {
            return;
        }

        var position = e.GetPosition(CanvasRoot);

        if (draggingPaletteNode is not null)
        {
            UpdatePaletteDragPreview(position);
            return;
        }

        if (viewModel.ShowPreviewConnection)
        {
            viewModel.PreviewConnectionEndX = position.X / viewModel.Zoom;
            viewModel.PreviewConnectionEndY = position.Y / viewModel.Zoom;
        }

        if (draggingPort is not null)
        {
            viewModel.PreviewConnectionEndX = position.X / viewModel.Zoom;
            viewModel.PreviewConnectionEndY = position.Y / viewModel.Zoom;

            var hoveredPort = FindPortAtPointer(e);
            viewModel.UpdatePendingConnectionTarget(hoveredPort);
        }
    }

    private static PaletteNodeViewModel? FindPaletteNode(object? source)
    {
        var current = source as Control;

        while (current is not null)
        {
            if (current.DataContext is PaletteNodeViewModel paletteNode)
            {
                return paletteNode;
            }

            current = current.Parent as Control;
        }

        return null;
    }

    private void FlowEditorPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        if (draggingPort is not null)
        {
            if (DataContext is FlowEditorViewModel viewModel)
            {
                viewModel.HandlePortConnection(draggingPort, viewModel.PendingConnectionTarget);
            }

            draggingPort = null;
            e.Pointer.Capture(null);
            e.Handled = true;
            return;
        }

        CompletePaletteDrop(e, "view");
    }

    private void CompletePaletteDrop(PointerReleasedEventArgs e, string source)
    {
        if (draggingPaletteNode is null)
        {
            return;
        }

        if (DataContext is not FlowEditorViewModel viewModel)
        {
            Log("Palette drop ignored: DataContext is not FlowEditorViewModel.");
            draggingPaletteNode = null;
            e.Pointer.Capture(null);
            return;
        }

        if (!viewModel.IsCanvasEditingEnabled)
        {
            draggingPaletteNode = null;
            HidePaletteDragPreview();
            e.Pointer.Capture(null);
            return;
        }

        var position = e.GetPosition(CanvasRoot);
        Log($"Palette drop released from {source}: {draggingPaletteNode.DisplayName} at {position.X:0.0}, {position.Y:0.0}");

        if (position.X >= 0
            && position.Y >= 0
            && position.X <= CanvasRoot.Bounds.Width
            && position.Y <= CanvasRoot.Bounds.Height)
        {
            viewModel.PlacePaletteNode(draggingPaletteNode, position.X, position.Y);
            Log($"Palette drop placed: {draggingPaletteNode.DisplayName}");
        }
        else
        {
            viewModel.InteractionStatus = "Drop ignored: release the Block over the canvas.";
            Log("Palette drop ignored: release point was outside CanvasRoot.");
        }

        draggingPaletteNode = null;
        HidePaletteDragPreview();
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void UpdatePaletteDragPreview(Avalonia.Point position)
    {
        var isInsideCanvas = position.X >= 0
            && position.Y >= 0
            && position.X <= CanvasRoot.Bounds.Width
            && position.Y <= CanvasRoot.Bounds.Height;

        PaletteDragPreview.IsVisible = isInsideCanvas;

        if (!isInsideCanvas)
        {
            return;
        }

        Canvas.SetLeft(PaletteDragPreview, Math.Max(0, position.X + 12));
        Canvas.SetTop(PaletteDragPreview, Math.Max(0, position.Y + 12));
    }

    private void HidePaletteDragPreview()
    {
        PaletteDragPreview.IsVisible = false;
        PaletteDragPreviewTitle.Text = string.Empty;
    }

    private static CanvasPortViewModel? FindPortFromSource(object? source)
    {
        var current = source as Control;
        while (current is not null)
        {
            if (current.DataContext is CanvasPortViewModel port)
            {
                return port;
            }

            current = current.Parent as Control;
        }

        return null;
    }

    private void ConnectionPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control)
        {
            return;
        }

        var properties = e.GetCurrentPoint(control).Properties;
        if (!properties.IsRightButtonPressed)
        {
            return;
        }

        if (control.ContextMenu is { } menu)
        {
            menu.Open(control);
            e.Handled = true;
        }
    }

    private CanvasPortViewModel? FindPortAtPointer(PointerEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return null;
        }

        var current = topLevel.InputHitTest(e.GetPosition(topLevel)) as Control;
        while (current is not null)
        {
            if (current is Control { DataContext: CanvasPortViewModel port })
            {
                return port;
            }

            current = current.Parent as Control;
        }

        return null;
    }

    private void NodePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: CanvasNodeViewModel node })
        {
            return;
        }

        if (DataContext is not FlowEditorViewModel viewModel || !viewModel.IsCanvasEditingEnabled)
        {
            return;
        }

        var properties = e.GetCurrentPoint(sender as Control).Properties;

        if (properties.IsRightButtonPressed)
        {
            if (viewModel.ShowPreviewConnection)
            {
                viewModel.CancelPendingConnection();
                e.Handled = true;
                return;
            }

            viewModel.SelectNodeCommand.Execute(node);
            Log($"Node right-clicked: {node.DisplayName} ({node.Id})");
            e.Handled = true;
            return;
        }

        // Handle left-click drag
        if (!properties.IsLeftButtonPressed)
        {
            return;
        }

        draggingNode = node;
        lastPointerPosition = e.GetPosition(CanvasRoot);
        Log($"Node drag started: {node.DisplayName} ({node.Id})");
        viewModel.SelectNodeCommand.Execute(node);
        e.Pointer.Capture(sender as IInputElement);
        e.Handled = true;
    }

    private void NodePointerMoved(object? sender, PointerEventArgs e)
    {
        if (draggingNode is null || DataContext is not FlowEditorViewModel viewModel)
        {
            return;
        }

        var pointerPosition = e.GetPosition(CanvasRoot);
        viewModel.MoveNode(
            draggingNode,
            pointerPosition.X - lastPointerPosition.X,
            pointerPosition.Y - lastPointerPosition.Y);
        lastPointerPosition = pointerPosition;
        e.Handled = true;
    }

    private void NodePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (draggingNode is not null)
        {
            Log($"Node drag finished: {draggingNode.DisplayName} ({draggingNode.Id})");
        }

        draggingNode = null;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void FlowEditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not FlowEditorViewModel viewModel)
        {
            return;
        }

        if (e.Key == Key.Escape && viewModel.ShowPreviewConnection)
        {
            viewModel.CancelPendingConnection();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Delete && viewModel.IsCanvasEditingEnabled)
        {
            viewModel.DeleteSelectedCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void DeleteNodeMenuItemClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not FlowEditorViewModel viewModel
            || sender is not MenuItem { DataContext: CanvasNodeViewModel node })
        {
            return;
        }

        viewModel.DeleteNodeCommand.Execute(node);
        e.Handled = true;
    }

    private void DeleteConnectionMenuItemClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not FlowEditorViewModel viewModel
            || sender is not MenuItem { DataContext: CanvasConnectionViewModel connection })
        {
            return;
        }

        viewModel.DeleteConnectionCommand.Execute(connection);
        e.Handled = true;
    }

    private async void SetConnectionTagMenuItemClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not FlowEditorViewModel viewModel
            || sender is not MenuItem { DataContext: CanvasConnectionViewModel connection })
        {
            return;
        }

        var updatedTag = await ShowConnectionTagDialogAsync(connection.Tag).ConfigureAwait(true);
        if (updatedTag is null)
        {
            return;
        }

        viewModel.UpdateConnectionTag(connection, updatedTag);
        e.Handled = true;
    }

    private void ClearConnectionTagMenuItemClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not FlowEditorViewModel viewModel
            || sender is not MenuItem { DataContext: CanvasConnectionViewModel connection })
        {
            return;
        }

        viewModel.UpdateConnectionTag(connection, null);
        e.Handled = true;
    }

    private void NodeTriggerButtonPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
    }

    private void NodeTriggerButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not FlowEditorViewModel viewModel
            || sender is not Control { DataContext: CanvasNodeViewModel node })
        {
            return;
        }

        viewModel.TriggerNodeCommand.Execute(node);
        e.Handled = true;
    }

    private async void CopyExecutionLogClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not FlowEditorViewModel viewModel
            || TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard)
        {
            return;
        }

        await clipboard.SetValueAsync(DataFormat.Text, viewModel.GetExecutionLogsClipboardText()).ConfigureAwait(true);
    }

    private static void Log(string message)
    {
        var formattedMessage = $"[FlowEditor] {message}";
        Debug.WriteLine(formattedMessage);
        Trace.WriteLine(formattedMessage);
        Console.WriteLine(formattedMessage);
    }

    private async void BrowseCsvOutputFilePathClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not FlowEditorViewModel { SelectedNode: { } selectedNode } viewModel
            || !selectedNode.IsCsvOutputNode)
        {
            return;
        }

        if (TopLevel.GetTopLevel(this) is not TopLevel topLevel || topLevel.StorageProvider is not { } storageProvider)
        {
            return;
        }

        var pickedFile = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Select CSV output file",
            SuggestedFileName = "output.csv",
            FileTypeChoices =
            [
                new FilePickerFileType("CSV")
                {
                    Patterns = ["*.csv"],
                },
            ],
            ShowOverwritePrompt = true,
        }).ConfigureAwait(true);

        if (pickedFile is null)
        {
            return;
        }

        selectedNode.CsvOutputFilePath = pickedFile.Path.LocalPath;
        viewModel.InteractionStatus = "CSV output path updated.";
        e.Handled = true;
    }

    private async Task<string?> ShowConnectionTagDialogAsync(string currentTag)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
        {
            return null;
        }

        var tagTextBox = new TextBox
        {
            Text = currentTag,
        };

        var saveButton = new Button
        {
            Content = "Save",
            Width = 88,
            HorizontalAlignment = HorizontalAlignment.Right,
        };

        var cancelButton = new Button
        {
            Content = "Cancel",
            Width = 88,
            HorizontalAlignment = HorizontalAlignment.Right,
        };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children =
            {
                cancelButton,
                saveButton,
            },
        };

        var dialog = new Window
        {
            Title = "Connection Tag",
            Width = 440,
            Height = 190,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 10,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Set a tag for this flow line.",
                        FontWeight = Avalonia.Media.FontWeight.SemiBold,
                    },
                    tagTextBox,
                    buttons,
                },
            },
        };

        var tcs = new TaskCompletionSource<string?>();
        saveButton.Click += (_, _) =>
        {
            tcs.TrySetResult(tagTextBox.Text);
            dialog.Close();
        };

        cancelButton.Click += (_, _) =>
        {
            tcs.TrySetResult(null);
            dialog.Close();
        };

        dialog.Closed += (_, _) => tcs.TrySetResult(null);

        _ = dialog.ShowDialog(owner);
        return await tcs.Task.ConfigureAwait(true);
    }
}