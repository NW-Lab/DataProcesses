using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.LogicalTree;
using Avalonia.Threading;

using DataProcesses.Desktop.Services;
using DataProcesses.Desktop.ViewModels;
using DataProcesses.Desktop.Views;
using DataProcesses.Engine;
using DataProcesses.Nodes.BuiltIn.Blocks.CameraInputImage;
using DataProcesses.Nodes.BuiltIn.Blocks.FilterTS;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Desktop.Tests;

public static class TestApplication
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<Application>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

public sealed class FlowEditorViewTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DeleteNode_ResponsePolylineRemainsMeasurable(bool useFilter, bool useContextMenu)
    {
        using var session = HeadlessUnitTestSession.StartNew(typeof(TestApplication));
        await session.Dispatch(() =>
        {
            INodeFactory factory = useFilter ? new FilterTSNodeFactory() : new CameraInputImageNodeFactory();
            var viewModel = new FlowEditorViewModel(
                [factory],
                new FlowRunner([factory]),
                new ProjectFileService());
            var node = viewModel.PlacePaletteNode(Assert.Single(viewModel.Palette.FilteredNodes), 0, 0);
            viewModel.SelectedNode = node;
            var view = new FlowEditorView { DataContext = viewModel };
            var response = Assert.Single(view.GetLogicalDescendants().OfType<Polyline>());
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(node.FilterTSResponsePoints, response.Points);

            if (useContextMenu)
            {
                viewModel.DeleteNodeCommand.Execute(node);
            }
            else
            {
                viewModel.DeleteSelectedCommand.Execute(null);
            }

            Dispatcher.UIThread.RunJobs();

            Assert.Empty(viewModel.Nodes);
            Assert.Null(viewModel.SelectedNode);
            Assert.NotNull(response.Points);
            Assert.Empty(response.Points);
            response.Measure(new Size(248, 72));
        }, CancellationToken.None);
    }

    [Fact]
    public async Task NoSelection_ResponsePolylineRemainsMeasurable()
    {
        using var session = HeadlessUnitTestSession.StartNew(typeof(TestApplication));
        await session.Dispatch(() =>
        {
            var factory = new FilterTSNodeFactory();
            var viewModel = new FlowEditorViewModel(
                [factory],
                new FlowRunner([factory]),
                new ProjectFileService());
            var view = new FlowEditorView { DataContext = viewModel };
            var response = Assert.Single(view.GetLogicalDescendants().OfType<Polyline>());
            Dispatcher.UIThread.RunJobs();

            Assert.NotNull(response.Points);
            Assert.Empty(response.Points);
            response.Measure(new Size(248, 72));

            var node = viewModel.PlacePaletteNode(Assert.Single(viewModel.Palette.FilteredNodes), 0, 0);
            viewModel.SelectedNode = node;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(node.FilterTSResponsePoints, response.Points);

            var originalPoints = response.Points.ToArray();
            node.FilterTSType = "highPass";
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(node.FilterTSResponsePoints, response.Points);
            Assert.False(originalPoints.SequenceEqual(response.Points));

            view.DataContext = null;
            Dispatcher.UIThread.RunJobs();

            Assert.NotNull(response.Points);
            Assert.Empty(response.Points);
            response.InvalidateMeasure();
            response.Measure(new Size(248, 72));
        }, CancellationToken.None);
    }
}
