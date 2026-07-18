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

        await service.SaveAsync(projectDirectory, "Project A", [flow], CancellationToken.None);

        var loaded = await service.LoadAsync(projectDirectory, CancellationToken.None);

        Assert.Equal("Project A", loaded.Project.Name);
        Assert.Equal(projectDirectory, loaded.ProjectDirectory);
        var loadedFlow = Assert.Single(loaded.Flows);
        Assert.Equal(flow.Id, loadedFlow.Id);
        Assert.Equal(flow.Name, loadedFlow.Name);
        Assert.Equal(flow.Nodes, loadedFlow.Nodes);
        Assert.Equal(flow.Connections, loadedFlow.Connections);
        Assert.True(File.Exists(Path.Combine(projectDirectory, "project.json")));
        Assert.True(Directory.EnumerateFiles(Path.Combine(projectDirectory, "flows"), "*.flow.json").Any());
    }

    public void Dispose()
    {
        if (Directory.Exists(projectDirectory))
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
    }
}