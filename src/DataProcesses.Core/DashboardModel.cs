namespace DataProcesses.Core;

public sealed record DashboardDocument(
    Guid Id,
    string Name,
    IReadOnlyList<DashboardWidget> Widgets,
    bool ShowGridInEditMode = true,
    bool ShowGridInRunMode = false,
    int GridSizePixels = DashboardGrid.DefaultSizePixels,
    DashboardDisplayHint? DisplayHint = null,
    int SchemaVersion = 1);

public sealed record DashboardWidget(
    Guid Id,
    string WidgetType,
    int GridX,
    int GridY,
    int GridWidth,
    int GridHeight,
    string? SourceFlowId = null,
    string? SourcePortId = null,
    string SettingsJson = "{}");

public static class DashboardGrid
{
    public const int DefaultSizePixels = 100;
}

public sealed record DashboardDisplayHint(
    DashboardDisplayRole PreferredRole = DashboardDisplayRole.Any,
    DashboardOpenMode OpenMode = DashboardOpenMode.Embedded);

public enum DashboardDisplayRole
{
    Any,
    Primary,
    Secondary,
}

public enum DashboardOpenMode
{
    Embedded,
    DetachedWindow,
}
