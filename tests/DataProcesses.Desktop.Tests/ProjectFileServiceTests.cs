using DataProcesses.Core;
using DataProcesses.Desktop.Services;
using DataProcesses.Plugin.Abstractions;
using System.Text.Json;

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
            [new Connection(
                "source-1",
                "out",
                "sink-1",
                "in",
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.TimeSeries1D)]);

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
    public async Task SaveAndLoadAsync_PreservesConnectionDataSchema()
    {
        var service = new ProjectFileService();
        var flow = new FlowDocument(
            Guid.NewGuid(),
            "Schema flow",
            [
                new NodeInstance("source-1", "source", 10, 20, "{}"),
                new NodeInstance("sink-1", "sink", 200, 20, "{}"),
            ],
            [new Connection(
                "source-1",
                "out",
                "sink-1",
                "in",
                PortDataKind.FastStream,
                DataSchema: PortDataSchema.NumericVector1D)]);

        await service.SaveAsync(projectDirectory, "Project Schema", [flow], [], CancellationToken.None);

        var loaded = await service.LoadAsync(projectDirectory, CancellationToken.None);
        var connection = Assert.Single(Assert.Single(loaded.Flows).Connections);

        Assert.Equal(PortDataSchema.NumericVector1D, connection.DataSchema);
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
                    "dataprocesses.output.stream",
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

        [Fact]
        public async Task LoadAsync_ReadsLegacyFlowWithoutConnectionSchema()
        {
                var service = new ProjectFileService();
                var flowId = Guid.NewGuid();
                var projectId = Guid.NewGuid();
                var flowFileName = "legacy.flow.json";

                Directory.CreateDirectory(projectDirectory);
                Directory.CreateDirectory(Path.Combine(projectDirectory, "flows"));
                Directory.CreateDirectory(Path.Combine(projectDirectory, "dashboards"));

                var projectJson =
                        $$"""
                        {
                            "schemaVersion": 1,
                            "id": "{{projectId}}",
                            "name": "Legacy Project",
                            "flows": [
                                {
                                    "id": "{{flowId}}",
                                    "name": "Legacy Flow",
                                    "path": "flows/{{flowFileName}}"
                                }
                            ],
                            "dashboards": []
                        }
                        """;

                var flowJson =
                        $$"""
                        {
                            "id": "{{flowId}}",
                            "name": "Legacy Flow",
                            "nodes": [
                                {
                                    "id": "source-1",
                                    "typeId": "source",
                                    "x": 0,
                                    "y": 0,
                                    "settingsJson": "{}",
                                    "isEnabled": true
                                },
                                {
                                    "id": "sink-1",
                                    "typeId": "sink",
                                    "x": 100,
                                    "y": 0,
                                    "settingsJson": "{}",
                                    "isEnabled": true
                                }
                            ],
                            "connections": [
                                {
                                    "sourceNodeId": "source-1",
                                    "sourcePortId": "out",
                                    "targetNodeId": "sink-1",
                                    "targetPortId": "in",
                                    "dataKind": 0
                                }
                            ],
                            "schemaVersion": 1
                        }
                        """;

                await File.WriteAllTextAsync(Path.Combine(projectDirectory, "project.json"), projectJson, CancellationToken.None);
                await File.WriteAllTextAsync(Path.Combine(projectDirectory, "flows", flowFileName), flowJson, CancellationToken.None);

                var loaded = await service.LoadAsync(projectDirectory, CancellationToken.None);
                var loadedFlow = Assert.Single(loaded.Flows);
                var loadedConnection = Assert.Single(loadedFlow.Connections);

                Assert.Equal(PortDataKind.FastStream, loadedConnection.DataKind);
                Assert.Equal(PortDataSchema.Unspecified, loadedConnection.DataSchema);
        }

    public void Dispose()
    {
        if (Directory.Exists(projectDirectory))
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
    }
}