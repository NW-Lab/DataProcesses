using DataProcesses.Core;

namespace DataProcesses.Desktop.ViewModels;

public sealed class ValidationIssueViewModel(FlowValidationIssue issue) : ViewModelBase
{
    public FlowValidationIssue Issue { get; } = issue;

    public string Severity => Issue.Severity.ToString();

    public string Code => Issue.Code.ToString();

    public string Message => Issue.Message;

    public string? NodeId => Issue.NodeId;
}