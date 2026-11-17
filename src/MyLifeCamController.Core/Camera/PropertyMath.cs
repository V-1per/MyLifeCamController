using System.Globalization;

namespace MyLifeCamController.Core.Camera;

/// <summary>Pure helpers for property values (no hardware access, fully unit-tested).</summary>
public static class PropertyMath
{
    /// <summary>Clamps <paramref name="value"/> to [min, max] and snaps it to the nearest step from min.</summary>
    public static int Snap(int value, int min, int max, int step)
    {
        if (max < min)
        {
            (min, max) = (max, min);
        }

        int clamped = Math.Clamp(value, min, max);
        if (step <= 1)
        {
            return clamped;
        }

        long offset = clamped - (long)min;
        long snapped = min + (long)Math.Round(offset / (double)step, MidpointRounding.AwayFromZero) * step;
        if (snapped > max)
        {
            snapped -= step;
        }

        return (int)Math.Clamp(snapped, min, max);
    }

    /// <summary>Human-readable value for the UI.</summary>
    public static string Format(PropertyDefinition definition, int value)
    {
        return definition.Name switch
        {
            // DirectShow exposure is log2(seconds): -5 means 1/32 s.
            "Exposure" => FormatExposure(value),
            "WhiteBalance" => $"{value} K",
            "ColorEnable" => value != 0 ? "Ligada" : "Desligada",
            _ => value.ToString(CultureInfo.InvariantCulture),
        };
    }

    public static string FormatExposure(int log2Seconds)
    {
        if (log2Seconds < 0)
        {
            double denominator = Math.Pow(2, -log2Seconds);
            return $"1/{denominator.ToString("0", CultureInfo.InvariantCulture)} s";
        }

        double seconds = Math.Pow(2, log2Seconds);
        return $"{seconds.ToString("0", CultureInfo.InvariantCulture)} s";
    }
}
