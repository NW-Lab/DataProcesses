using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using DataProcesses.Desktop.ViewModels;

using System.Diagnostics;

namespace DataProcesses.Desktop.Views;

public partial class FlowEditorView : UserControl
{
    private CanvasNodeViewModel? draggingNode;
    private PaletteNodeViewModel? draggingPaletteNode;
    private Avalonia.Point lastPointerPosition;

    public FlowEditorView()
    {
        InitializeComponent();
        AddHandler(PointerPressedEvent, FlowEditorPointerPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, FlowEditorPointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private void FlowEditorPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (draggingPaletteNode is not null
            || DataContext is not FlowEditorViewModel viewModel
            || FindPaletteNode(e.Source) is not { } paletteNode)
        {
            return;
        }

        var properties = e.GetCurrentPoint(sender as Control).Properties;
        if (!properties.IsLeftButtonPressed)
        {
            return;
        }

        draggingPaletteNode = paletteNode;
        viewModel.SelectPaletteNodeCommand.Execute(paletteNode);
        Log($"Palette drag started: {paletteNode.DisplayName} ({paletteNode.TypeId})");
        e.Pointer.Capture(this);
        e.Handled = true;
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
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void NodePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: CanvasNodeViewModel node })
        {
            return;
        }

        draggingNode = node;
        lastPointerPosition = e.GetPosition(CanvasRoot);
        Log($"Node drag started: {node.DisplayName} ({node.Id})");
        (DataContext as FlowEditorViewModel)?.SelectNodeCommand.Execute(node);
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

        if (e.Key == Key.Delete)
        {
            viewModel.DeleteSelectedCommand.Execute(null);
            e.Handled = true;
        }
    }

    private static void Log(string message)
    {
        var formattedMessage = $"[FlowEditor] {message}";
        Debug.WriteLine(formattedMessage);
        Trace.WriteLine(formattedMessage);
        Console.WriteLine(formattedMessage);
    }
}