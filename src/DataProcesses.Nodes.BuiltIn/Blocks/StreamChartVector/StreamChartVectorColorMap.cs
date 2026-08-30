namespace DataProcesses.Nodes.BuiltIn.Blocks.StreamChartVector;

/// <summary>
/// Intensity-to-color palettes available to the StreamChartVector Block.
/// </summary>
public enum StreamChartVectorColorMap
{
    Jet = 0,
    Grayscale = 1,
    Hot = 2,
    Viridis = 3,
}

/// <summary>
/// Maps a normalized intensity in the range 0..1 to an sRGB triple.
/// </summary>
public static class StreamChartVectorPalette
{
    private static readonly (double Position, double Red, double Green, double Blue)[] ViridisAnchors =
    [
        (0.00, 0.267, 0.005, 0.329),
        (0.25, 0.229, 0.322, 0.545),
        (0.50, 0.128, 0.567, 0.551),
        (0.75, 0.369, 0.789, 0.383),
        (1.00, 0.993, 0.906, 0.144),
    ];

    public static (byte Red, byte Green, byte Blue) Map(StreamChartVectorColorMap colorMap, double normalized)
    {
        var value = double.IsNaN(normalized) ? 0 : Math.Clamp(normalized, 0, 1);

        return colorMap switch
        {
            StreamChartVectorColorMap.Grayscale => ToBytes(value, value, value),
            StreamChartVectorColorMap.Hot => ToBytes(Clamp01(3 * value), Clamp01((3 * value) - 1), Clamp01((3 * value) - 2)),
            StreamChartVectorColorMap.Viridis => MapViridis(value),
            _ => MapJet(value),
        };
    }

    private static (byte Red, byte Green, byte Blue) MapJet(double value)
    {
        var red = Clamp01(1.5 - Math.Abs((4 * value) - 3));
        var green = Clamp01(1.5 - Math.Abs((4 * value) - 2));
        var blue = Clamp01(1.5 - Math.Abs((4 * value) - 1));
        return ToBytes(red, green, blue);
    }

    private static (byte Red, byte Green, byte Blue) MapViridis(double value)
    {
        for (var index = 1; index < ViridisAnchors.Length; index++)
        {
            var upper = ViridisAnchors[index];
            if (value > upper.Position)
            {
                continue;
            }

            var lower = ViridisAnchors[index - 1];
            var span = upper.Position - lower.Position;
            var weight = span <= 0 ? 0 : (value - lower.Position) / span;
            return ToBytes(
                lower.Red + ((upper.Red - lower.Red) * weight),
                lower.Green + ((upper.Green - lower.Green) * weight),
                lower.Blue + ((upper.Blue - lower.Blue) * weight));
        }

        var last = ViridisAnchors[^1];
        return ToBytes(last.Red, last.Green, last.Blue);
    }

    private static double Clamp01(double value) => Math.Clamp(value, 0, 1);

    private static (byte Red, byte Green, byte Blue) ToBytes(double red, double green, double blue)
    {
        return (
            (byte)Math.Clamp(Math.Round(red * 255), 0, 255),
            (byte)Math.Clamp(Math.Round(green * 255), 0, 255),
            (byte)Math.Clamp(Math.Round(blue * 255), 0, 255));
    }
}
