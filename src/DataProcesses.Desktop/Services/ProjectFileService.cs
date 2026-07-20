using System.Text.Json;
using System.Text.Json.Serialization;

using DataProcesses.Core;

namespace DataProcesses.Desktop.Services;

public sealed class ProjectFileService
{
    public const int CurrentSchemaVersion = 1;

    private const string ProjectFileName = "project.json";
    private const string FlowsDirectoryName = "flows";
    private const string DashboardsDirectoryName = "dashboards";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public async ValueTask SaveAsync(
        string projectDirectory,
        string projectName,
        IReadOnlyList<FlowDocument> flows,
        IReadOnlyList<DashboardDocument>? dashboards,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName);
        ArgumentNullException.ThrowIfNull(flows);

        Directory.CreateDirectory(projectDirectory);

        var flowsDirectory = Path.Combine(projectDirectory, FlowsDirectoryName);
        Directory.CreateDirectory(flowsDirectory);
        var dashboardsDirectory = Path.Combine(projectDirectory, DashboardsDirectoryName);
        Directory.CreateDirectory(dashboardsDirectory);

        var flowReferences = new List<ProjectFlowReference>(flows.Count);
        var dashboardDocuments = dashboards ?? [];
        var dashboardReferences = new List<ProjectDashboardReference>(dashboardDocuments.Count);

        foreach (var flow in flows)
        {
            var fileName = CreateFlowFileName(flow);
            var relativePath = Path.Combine(FlowsDirectoryName, fileName).Replace(Path.DirectorySeparatorChar, '/');
            var flowPath = Path.Combine(projectDirectory, relativePath);

            await WriteJsonAsync(flowPath, flow, cancellationToken).ConfigureAwait(false);
            flowReferences.Add(new ProjectFlowReference(flow.Id, flow.Name, relativePath));
        }

        foreach (var dashboard in dashboardDocuments)
        {
            var fileName = CreateDashboardFileName(dashboard);
            var relativePath = Path.Combine(DashboardsDirectoryName, fileName).Replace(Path.DirectorySeparatorChar, '/');
            var dashboardPath = Path.Combine(projectDirectory, relativePath);

            await WriteJsonAsync(dashboardPath, dashboard, cancellationToken).ConfigureAwait(false);
            dashboardReferences.Add(new ProjectDashboardReference(dashboard.Id, dashboard.Name, relativePath));
        }

        var project = new ProjectDocument(
            CurrentSchemaVersion,
            flows.Count > 0 ? flows[0].Id : Guid.NewGuid(),
            projectName,
            flowReferences,
            dashboardReferences);

        await WriteJsonAsync(Path.Combine(projectDirectory, ProjectFileName), project, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<LoadedProject> LoadAsync(
        string projectDirectory,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectDirectory);

        var projectPath = Path.Combine(projectDirectory, ProjectFileName);
        var project = await ReadJsonAsync<ProjectDocument>(projectPath, cancellationToken).ConfigureAwait(false);

        if (project.SchemaVersion != CurrentSchemaVersion)
        {
            throw new InvalidOperationException($"Unsupported project schema version '{project.SchemaVersion}'.");
        }

        var flowReferences = project.Flows ?? [];
        var dashboardReferences = project.Dashboards ?? [];
        var flows = new List<FlowDocument>(flowReferences.Count);
        var dashboards = new List<DashboardDocument>(dashboardReferences.Count);

        foreach (var flowReference in flowReferences)
        {
            var flowPath = Path.Combine(projectDirectory, flowReference.Path);
            var flow = await ReadJsonAsync<FlowDocument>(flowPath, cancellationToken).ConfigureAwait(false);
            flows.Add(flow);
        }

        foreach (var dashboardReference in dashboardReferences)
        {
            var dashboardPath = Path.Combine(projectDirectory, dashboardReference.Path);
            var dashboard = await ReadJsonAsync<DashboardDocument>(dashboardPath, cancellationToken).ConfigureAwait(false);
            dashboards.Add(dashboard);
        }

        return new LoadedProject(project, flows, dashboards, projectDirectory);
    }

    private static string CreateDashboardFileName(DashboardDocument dashboard)
    {
        var safeName = string.Join(
            '-',
            dashboard.Name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "dashboard";
        }

        return $"{safeName}-{dashboard.Id:N}.dashboard.json";
    }

    private static string CreateFlowFileName(FlowDocument flow)
    {
        var safeName = string.Join(
            '-',
            flow.Name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "flow";
        }

        return $"{safeName}-{flow.Id:N}.flow.json";
    }

    private static async ValueTask WriteJsonAsync<T>(
        string path,
        T value,
        CancellationToken cancellationToken)
    {
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask<T> ReadJsonAsync<T>(
        string path,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var value = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);

        return value ?? throw new InvalidOperationException($"Could not read JSON document '{path}'.");
    }
}