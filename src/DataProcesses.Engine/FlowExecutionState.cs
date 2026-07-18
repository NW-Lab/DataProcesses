namespace DataProcesses.Engine;

public enum FlowExecutionState
{
    Stopped,
    Validating,
    Starting,
    Running,
    Stopping,
    Faulted,
}