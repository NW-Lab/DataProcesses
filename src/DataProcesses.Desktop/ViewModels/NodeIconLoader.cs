using Avalonia.Media.Imaging;

namespace DataProcesses.Desktop.ViewModels;

internal static class NodeIconLoader
{
    public static Bitmap? Load(string? iconPath)
    {
        var resolvedPath = ResolvePath(iconPath);
        if (string.IsNullOrWhiteSpace(resolvedPath))
        {
            return null;
        }

        try
        {
            return new Bitmap(resolvedPath);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static string ResolvePath(string? iconPath)
    {
        if (string.IsNullOrWhiteSpace(iconPath))
        {
            return string.Empty;
        }

        var candidate = Path.Combine(AppContext.BaseDirectory, iconPath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(candidate) ? candidate : string.Empty;
    }
}