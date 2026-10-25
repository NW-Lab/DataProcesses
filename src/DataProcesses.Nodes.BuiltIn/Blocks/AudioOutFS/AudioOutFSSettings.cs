using System.Text.Json;

namespace DataProcesses.Nodes.BuiltIn.Blocks.AudioOutFS;

public sealed record AudioOutFSSettings(
    bool IsMuted = false,
    bool IsRecording = false,
    string RecordingFolder = "",
    string FileNamePrefix = "audio")
{
    public static AudioOutFSSettings Default { get; } = new(
        RecordingFolder: GetDefaultRecordingFolder());

    public static AudioOutFSSettings FromJson(string settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            return Default;
        }

        using var document = JsonDocument.Parse(settingsJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("AudioOutFS settings must be a JSON object.", nameof(settingsJson));
        }

        var settings = Default;
        var root = document.RootElement;
        if (root.TryGetProperty("isMuted", out var muteElement))
        {
            settings = settings with { IsMuted = ReadBoolean(muteElement, "isMuted") };
        }

        if (root.TryGetProperty("isRecording", out var recordingElement))
        {
            settings = settings with { IsRecording = ReadBoolean(recordingElement, "isRecording") };
        }

        if (root.TryGetProperty("recordingFolder", out var folderElement))
        {
            settings = settings with { RecordingFolder = ReadString(folderElement, "recordingFolder") };
        }

        if (root.TryGetProperty("fileNamePrefix", out var prefixElement))
        {
            settings = settings with { FileNamePrefix = ReadString(prefixElement, "fileNamePrefix") };
        }

        settings.Validate();
        return settings;
    }

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(RecordingFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(FileNamePrefix);

        if (FileNamePrefix.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || FileNamePrefix.Contains('/')
            || FileNamePrefix.Contains('\\')
            || !string.Equals(Path.GetFileName(FileNamePrefix), FileNamePrefix, StringComparison.Ordinal))
        {
            throw new ArgumentException("AudioOutFS file name prefix must not contain path separators or invalid file-name characters.", nameof(FileNamePrefix));
        }
    }

    private static string GetDefaultRecordingFolder()
    {
        var musicFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        if (!string.IsNullOrWhiteSpace(musicFolder))
        {
            return Path.Combine(musicFolder, "DataProcesses");
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "DataProcesses",
            "Audio");
    }

    private static bool ReadBoolean(JsonElement value, string propertyName)
    {
        if (value.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            throw new ArgumentException($"AudioOutFS setting '{propertyName}' must be a boolean.", nameof(value));
        }

        return value.GetBoolean();
    }

    private static string ReadString(JsonElement value, string propertyName)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException($"AudioOutFS setting '{propertyName}' must be a string.", nameof(value));
        }

        return value.GetString() ?? string.Empty;
    }
}