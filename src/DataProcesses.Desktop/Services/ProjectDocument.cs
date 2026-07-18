using DataProcesses.Core;

namespace DataProcesses.Desktop.Services;

public sealed record ProjectDocument(
    int SchemaVersion,
    Guid Id,
    string Name,
    IReadOnlyList<ProjectFlowReference> Flows,
    IReadOnlyList<ProjectDashboardReference> Dashboards);

public sealed record ProjectFlowReference(
    Guid Id,
    string Name,
    string Path);

public sealed record ProjectDashboardReference(
    Guid Id,
    string Name,
    string Path);

public sealed record LoadedProject(
    ProjectDocument Project,
    IReadOnlyList<FlowDocument> Flows,
    string ProjectDirectory);