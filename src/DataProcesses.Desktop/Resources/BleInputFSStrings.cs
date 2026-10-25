using System.Globalization;
using System.Resources;

namespace DataProcesses.Desktop.Resources;

public static class BleInputFSStrings
{
    private static readonly ResourceManager Manager = new("DataProcesses.Desktop.Resources.BleInputFSStrings", typeof(BleInputFSStrings).Assembly);

    public static string Format => Get(nameof(Format));
    public static string SampleRate => Get(nameof(SampleRate));
    public static string NumericType => Get(nameof(NumericType));
    public static string ByteOrder => Get(nameof(ByteOrder));

    private static string Get(string key) => Manager.GetString(key, CultureInfo.CurrentUICulture)
        ?? throw new MissingManifestResourceException($"Missing BleInputFS resource '{key}'.");
}
