using System.Text.Json;

namespace DataProcesses.Nodes.BuiltIn.Blocks.UVCameraInputImage;

public sealed record UVCameraInputImageSettings(
    int DeviceIndex = 0,
    int RequestedWidth = 1920,
    int RequestedHeight = 1080,
    double FramesPerSecond = 10.0,
    bool IsPlay = true,
    bool IsWhiteBalanceAuto = true,
    double WhiteBalanceTemperature = 4_500,
    long ExecutionSessionId = 0)
{
    public const int MinimumDimension = 1;
    public const int MaximumWidth = 3_840;
    public const int MaximumHeight = 2_160;
    public const double MinimumFramesPerSecond = 0.1;
    public const double MaximumFramesPerSecond = 60.0;
    public const double MinimumWhiteBalanceTemperature = 2_000;
    public const double MaximumWhiteBalanceTemperature = 10_000;

    public static UVCameraInputImageSettings Default { get; } = new();

    public void Validate()
    {
        if (DeviceIndex < 0) throw new ArgumentOutOfRangeException(nameof(DeviceIndex));
        if (RequestedWidth < MinimumDimension || RequestedWidth > MaximumWidth) throw new ArgumentOutOfRangeException(nameof(RequestedWidth));
        if (RequestedHeight < MinimumDimension || RequestedHeight > MaximumHeight) throw new ArgumentOutOfRangeException(nameof(RequestedHeight));
        if (!double.IsFinite(FramesPerSecond) || FramesPerSecond < MinimumFramesPerSecond || FramesPerSecond > MaximumFramesPerSecond) throw new ArgumentOutOfRangeException(nameof(FramesPerSecond));
        if (!double.IsFinite(WhiteBalanceTemperature) || WhiteBalanceTemperature < MinimumWhiteBalanceTemperature || WhiteBalanceTemperature > MaximumWhiteBalanceTemperature) throw new ArgumentOutOfRangeException(nameof(WhiteBalanceTemperature));
    }

    public static UVCameraInputImageSettings FromJson(string settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson)) return Default;
        try
        {
            using var document = JsonDocument.Parse(settingsJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object) throw new ArgumentException("UVCameraInputImage settings must be a JSON object.", nameof(settingsJson));
            var settings = new UVCameraInputImageSettings(
                GetInt(document.RootElement, "deviceIndex", Default.DeviceIndex),
                GetInt(document.RootElement, "width", Default.RequestedWidth),
                GetInt(document.RootElement, "height", Default.RequestedHeight),
                GetDouble(document.RootElement, "fps", Default.FramesPerSecond),
                GetBool(document.RootElement, "isPlay", Default.IsPlay),
                GetBool(document.RootElement, "isWhiteBalanceAuto", Default.IsWhiteBalanceAuto),
                GetDouble(document.RootElement, "whiteBalanceTemperature", Default.WhiteBalanceTemperature),
                GetLong(document.RootElement, "executionSessionId", Default.ExecutionSessionId));
            settings.Validate();
            return settings;
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("UVCameraInputImage settings JSON is invalid.", nameof(settingsJson), ex);
        }
    }

    private static int GetInt(JsonElement element, string name, int fallback) => element.TryGetProperty(name, out var value) ? value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var parsed) ? parsed : throw new ArgumentException($"UVCameraInputImage settings field '{name}' must be an integer.") : fallback;
    private static long GetLong(JsonElement element, string name, long fallback) => element.TryGetProperty(name, out var value) ? value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var parsed) ? parsed : throw new ArgumentException($"UVCameraInputImage settings field '{name}' must be an integer.") : fallback;
    private static double GetDouble(JsonElement element, string name, double fallback) => element.TryGetProperty(name, out var value) ? value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var parsed) ? parsed : throw new ArgumentException($"UVCameraInputImage settings field '{name}' must be a number.") : fallback;
    private static bool GetBool(JsonElement element, string name, bool fallback) => element.TryGetProperty(name, out var value) ? value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : throw new ArgumentException($"UVCameraInputImage settings field '{name}' must be a boolean.") : fallback;
}