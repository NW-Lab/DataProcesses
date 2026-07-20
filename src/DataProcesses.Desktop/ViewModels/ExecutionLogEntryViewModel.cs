using DataProcesses.Engine;

namespace DataProcesses.Desktop.ViewModels;

public sealed class ExecutionLogEntryViewModel(FlowExecutionLogEntry entry) : ViewModelBase
{
    public string Timestamp => entry.Timestamp.ToLocalTime().ToString("HH:mm:ss");

    public string Level => entry.Level.ToString();

    public string Message => entry.Message;

    public string? NodeId => entry.NodeId;

    public string ClipboardText => string.IsNullOrWhiteSpace(NodeId)
        ? $"{Timestamp}\t{Level}\t{Message}"
        : $"{Timestamp}\t{Level}\t{NodeId}\t{Message}";
}