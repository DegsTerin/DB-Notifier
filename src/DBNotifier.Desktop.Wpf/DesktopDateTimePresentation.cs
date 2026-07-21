// Module purpose: Provides one explicit UTC formatting contract for WPF and notification-area presentation surfaces.
using System.Globalization;

namespace DBNotifier.Desktop.Wpf;

/// <summary>Formats presentation timestamps in UTC while leaving the owning localised label responsible for naming the time zone.</summary>
internal static class DesktopDateTimePresentation
{
    /// <summary>Formats one instant as a stable UTC date and time without adding a language-specific zone suffix.</summary>
    /// <param name="value">Instant to convert to UTC.</param>
    /// <param name="culture">Culture used for numeric formatting.</param>
    /// <returns>The UTC date and time in a sortable, second-precision form.</returns>
    internal static string FormatUtc(DateTimeOffset value, CultureInfo culture) =>
        value.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", culture);
}
