// Module purpose: Defines Accessibility Presentation application behaviour without depending on concrete providers or user interfaces.
using System.Globalization;

namespace DBNotifier.Application.Presentation;

public static class AccessibilityPresentation
{
    public static double ContrastRatio(string foreground, string background)
    {
        double first = RelativeLuminance(foreground);
        double second = RelativeLuminance(background);
        return (Math.Max(first, second) + 0.05) / (Math.Min(first, second) + 0.05);
    }

    private static double RelativeLuminance(string colour)
    {
        if (colour.Length != 7 || colour[0] != '#')
        {
            throw new ArgumentException("Colour must use six-digit hexadecimal notation.", nameof(colour));
        }

        double Channel(int offset)
        {
            double value = int.Parse(colour.AsSpan(offset, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255d;
            return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(1) + 0.7152 * Channel(3) + 0.0722 * Channel(5);
    }
}
