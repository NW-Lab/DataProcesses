using DataProcesses.Core;
using DataProcesses.Desktop.Services;
using DataProcesses.Plugin.Abstractions;

namespace DataProcesses.Desktop.Tests;

public sealed class ProjectFileServiceTests : IDisposable
{
    private readonly string projectDirectory = Path.Combine(Path.GetTempPath(), "DataProcesses.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsProjectAndFlows()
    {
        var service = new ProjectFileService();
        var flowId = Guid.NewGuid();
        var flow = new FlowDocument(
            flowId,
            "Acquisition",
            [
                new NodeInstance("source-1", "source", 10, 20, "{}"),
                new NodeInstance("sink-1", "sink", 200, 20, "{\"window\":512}"),
            ],
            [new Connection("source-1", "out", "sink-1", "in", PortDataKind.FastStream)]);

        await service.SaveAsync(projectDirectory, "Project A", [flow], [], CancellationToken.None);

        var loaded = await service.LoadAsync(projectDirectory, CancellationToken.None);

        Assert.Equal("Project A", loaded.Project.Name);
        Assert.Equal(projectDirectory, loaded.ProjectDirectory);
        var loadedFlow = Assert.Single(loaded.Flows);
        Assert.Equal(flow.Id, loadedFlow.Id);
        Assert.Equal(flow.Name, loadedFlow.Name);
        Assert.Equal(flow.Nodes, loadedFlow.Nodes);
        Assert.Equal(flow.Connections, loadedFlow.Connections);
        Assert.Empty(loaded.Dashboards);
        Assert.True(File.Exists(Path.Combine(projectDirectory, "project.json")));
        Assert.True(Directory.EnumerateFiles(Path.Combine(projectDirectory, "flows"), "*.flow.json").Any());
    }

    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsDashboards()
    {
        var service = new ProjectFileService();
        var dashboardId = Guid.NewGuid();
        var dashboard = new DashboardDocument(
            dashboardId,
            "Monitor",
            [
                new DashboardWidget(
                    Guid.NewGuid(),
                    "dataprocesses.dashboard.time-series",
                    3,
                    2,
                    4,
                    3,
                    SourceFlowId: "flow-1",
                    SourcePortId: "out"),
            ]);

        await service.SaveAsync(projectDirectory, "Project B", [], [dashboard], CancellationToken.None);

        var loaded = await service.LoadAsync(projectDirectory, CancellationToken.None);

        var loadedDashboard = Assert.Single(loaded.Dashboards);
        Assert.Equal(dashboard.Id, loadedDashboard.Id);
        Assert.Equal(dashboard.Name, loadedDashboard.Name);
        Assert.Equal(dashboard.Widgets, loadedDashboard.Widgets);
        Assert.True(Directory.EnumerateFiles(Path.Combine(projectDirectory, "dashboards"), "*.dashboard.json").Any());
    }

    public void Dispose()
    {
        if (Directory.Exists(projectDirectory))
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
    }
}