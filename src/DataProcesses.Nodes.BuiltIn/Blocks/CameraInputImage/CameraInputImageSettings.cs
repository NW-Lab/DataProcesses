using System.Text.Json;

namespace DataProcesses.Nodes.BuiltIn.Blocks.CameraInputImage;

public sealed record CameraInputImageSettings(
    int DeviceIndex = 0,
    int RequestedWidth = 1920,
    int RequestedHeight = 1080,
    bool ContinuousCapture = false,
    double FramesPerSecond = 10.0,
    bool IsWhiteBalanceAuto = true,
    double WhiteBalanceTemperature = 4_500,
    long ExecutionSessionId = 0,
    long ManualTriggerNonce = 0)
{
    public const int MinimumDimension = 1;
    public const int MaximumWidth = 3_840;
    public const int MaximumHeight = 2_160;
    public const double MinimumFramesPerSecond = 0.1;
    public const double MaximumFramesPerSecond = 60.0;
    public const double MinimumWhiteBalanceTemperature = 2_000;
    public const double MaximumWhiteBalanceTemperature = 10_000;

    public static CameraInputImageSettings Default { get; } = new();

    public void Validate()
    {
        if (DeviceIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(DeviceIndex), DeviceIndex, "Device index must be zero or greater.");
        }

        if (RequestedWidth < MinimumDimension || RequestedWidth > MaximumWidth)
        {
            throw new ArgumentOutOfRangeException(nameof(RequestedWidth), RequestedWidth, $"Requested width must be between {MinimumDimension} and {MaximumWidth}.");
        }

        if (RequestedHeight < MinimumDimension || RequestedHeight > MaximumHeight)
        {
            throw new ArgumentOutOfRangeException(nameof(RequestedHeight), RequestedHeight, $"Requested height must be between {MinimumDimension} and {MaximumHeight}.");
        }

        if (FramesPerSecond < MinimumFramesPerSecond
            || FramesPerSecond > MaximumFramesPerSecond
            || !double.IsFinite(FramesPerSecond))
        {
            throw new ArgumentOutOfRangeException(nameof(FramesPerSecond), FramesPerSecond, $"FPS must be between {MinimumFramesPerSecond} and {MaximumFramesPerSecond}.");
        }

        if (WhiteBalanceTemperature < MinimumWhiteBalanceTemperature
            || WhiteBalanceTemperature > MaximumWhiteBalanceTemperature
            || !double.IsFinite(WhiteBalanceTemperature))
        {
            throw new ArgumentOutOfRangeException(nameof(WhiteBalanceTemperature), WhiteBalanceTemperature, $"White balance temperature must be between {MinimumWhiteBalanceTemperature} and {MaximumWhiteBalanceTemperature}.");
        }

        if (ManualTriggerNonce < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ManualTriggerNonce), ManualTriggerNonce, "Manual trigger nonce must be zero or greater.");
        }
    }

    public static CameraInputImageSettings FromJson(string settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            return Default;
        }

        try
        {
            using var document = JsonDocument.Parse(settingsJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("CameraInputImage settings must be a JSON object.", nameof(settingsJson));
            }

            var settings = new CameraInputImageSettings(
                DeviceIndex: GetOptionalInt(document.RootElement, "deviceIndex", Default.DeviceIndex),
                RequestedWidth: GetOptionalInt(document.RootElement, "width", Default.RequestedWidth),
                RequestedHeight: GetOptionalInt(document.RootElement, "height", Default.RequestedHeight),
                ContinuousCapture: GetOptionalBoolean(document.RootElement, "continuousCapture", Default.ContinuousCapture),
                FramesPerSecond: GetOptionalDouble(document.RootElement, "fps", Default.FramesPerSecond),
                IsWhiteBalanceAuto: GetOptionalBoolean(document.RootElement, "isWhiteBalanceAuto", Default.IsWhiteBalanceAuto),
                WhiteBalanceTemperature: GetOptionalDouble(document.RootElement, "whiteBalanceTemperature", Default.WhiteBalanceTemperature),
                ExecutionSessionId: GetOptionalLong(document.RootElement, "executionSessionId", Default.ExecutionSessionId),
                ManualTriggerNonce: GetOptionalLong(document.RootElement, "manualTriggerNonce", Default.ManualTriggerNonce));
            settings.Validate();
            return settings;
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("CameraInputImage settings JSON is invalid.", nameof(settingsJson), ex);
        }
    }

    private static int GetOptionalInt(JsonElement element, string propertyName, int defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        if (property.ValueKind != JsonValueKind.Number || !property.TryGetInt32(out var value))
        {
            throw new ArgumentException($"CameraInputImage settings field '{propertyName}' must be an integer.", nameof(property));
        }

        return value;
    }

    private static double GetOptionalDouble(JsonElement element, string propertyName, double defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        if (property.ValueKind != JsonValueKind.Number || !property.TryGetDouble(out var value))
        {
            throw new ArgumentException($"CameraInputImage settings field '{propertyName}' must be a number.", nameof(property));
        }

        return value;
    }

    private static bool GetOptionalBoolean(JsonElement element, string propertyName, bool defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        if (property.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            throw new ArgumentException($"CameraInputImage settings field '{propertyName}' must be a boolean.", nameof(property));
        }

        return property.GetBoolean();
    }

    private static long GetOptionalLong(JsonElement element, string propertyName, long defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return defaultValue;
        }

        if (property.ValueKind != JsonValueKind.Number || !property.TryGetInt64(out var value))
        {
            throw new ArgumentException($"CameraInputImage settings field '{propertyName}' must be an integer.", nameof(property));
        }

        return value;
    }
}