namespace DataProcesses.Desktop.ViewModels;

/// <summary>
/// Shared port-family colors: Fast Stream is blue, JSON Message is orange (docs/initial-specification.md §6.4).
/// Color is always paired with a shape, letter, or line style.
/// </summary>
internal static class PortKindColors
{
    public const string FastStream = "#1D70B8";
    public const string FastStreamSchemaBackground = "#DBEAFE";
    public const string FastStreamSchemaBorder = "#60A5FA";
    public const string FastStreamSchemaForeground = "#1E3A8A";

    public const string JsonMessage = "#C2410C";
    public const string JsonMessageSchemaBackground = "#FFEDD5";
    public const string JsonMessageSchemaBorder = "#FB923C";
    public const string JsonMessageSchemaForeground = "#7C2D12";
}
